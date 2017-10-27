using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClangSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using LibVlc.Parser.Clang;

namespace LibVlc.Parser
{
	public static class Program
	{
		private static DirectoryInfo VlcHeaderDirectory = new DirectoryInfo(@"..\..\..\..\packages\VideoLAN.LibVLC.Win.x64.1.0.3\build\x64\include\vlc");

		public static void Main(string[] args)
		{
			var files = VlcHeaderDirectory.GetFiles("libvlc*.h");

			var createIndex = clang.createIndex(0, 1);

			var translationUnits = new List<CXTranslationUnit>();

			foreach (var file in files)
			{
				var compilerArgs = BuildCompilerArgs(VlcHeaderDirectory.FullName, file.Name, files);

				var translationUnitError = clang.parseTranslationUnit2(createIndex, file.FullName, compilerArgs, compilerArgs.Length, out CXUnsavedFile unsavedFile, 0, 0, out CXTranslationUnit translationUnit);

				if (translationUnitError != CXErrorCode.CXError_Success)
				{
					Console.WriteLine("Error: " + translationUnitError);
					var numDiagnostics = clang.getNumDiagnostics(translationUnit);

					for (uint i = 0; i < numDiagnostics; ++i)
					{
						var diagnostic = clang.getDiagnostic(translationUnit, i);
						Console.WriteLine(clang.getDiagnosticSpelling(diagnostic).ToString());
						clang.disposeDiagnostic(diagnostic);
					}
				}

				translationUnits.Add(translationUnit);
			}

			var @namespace = "LibVlc";
			string methodClassName = "Functions";
			var libraryPath = "libvlc";

			using (var sw = new StreamWriter("temp.cs"))
			using (var indentedWriter = new IndentedTextWriter(sw))
			using (ICodeGenerator codeGenerator = new DefaultCodeGenerator(indentedWriter, @namespace))
			{
				codeGenerator.BeginGeneration();
				
				var structVisitor = new StructVisitor();
				foreach (var tu in translationUnits)
				{
					clang.visitChildren(clang.getTranslationUnitCursor(tu), structVisitor.Visit, new CXClientData(IntPtr.Zero));
				}

				foreach(var s in structVisitor.Structs)
				{
					codeGenerator.StructDecleration(s.Name, s.Fields.ToArray());
				}

				var typeDefVisitor = new TypeDefVisitor(indentedWriter);
				foreach (var tu in translationUnits)
				{
					clang.visitChildren(clang.getTranslationUnitCursor(tu), typeDefVisitor.Visit, new CXClientData(IntPtr.Zero));
				}

				foreach(var p in typeDefVisitor.Pointers)
				{
					indentedWriter.WriteLine("public partial struct " + p.Name);
					indentedWriter.WriteLine("{");
					indentedWriter.Indent++;
					indentedWriter.WriteLine("public " + p.Name + "(IntPtr pointer)");
					indentedWriter.WriteLine("{");
					indentedWriter.Indent++;
					indentedWriter.WriteLine("Pointer = pointer;");
					indentedWriter.Indent--;
					indentedWriter.WriteLine("}");
					indentedWriter.WriteLine();
					indentedWriter.Indent--;
					indentedWriter.WriteLine("public IntPtr Pointer;");
					indentedWriter.WriteLine("}");
					indentedWriter.WriteLine();
				}

				foreach (var d in typeDefVisitor.Delegates)
				{
					indentedWriter.WriteLine("[UnmanagedFunctionPointer(" + d.UnmanagedFunction + ")]");
					indentedWriter.Write("public delegate ");
					indentedWriter.Write(d.Name);
					indentedWriter.Write("(");

					var count = 1;
					foreach(var p in d.Parameters)
					{
						indentedWriter.Write(p.Type + " " + p.Name);

						if (count < d.Parameters.Count)
						{
							indentedWriter.Write(", ");
						}

						count++;
					}

					indentedWriter.WriteLine(");");
					indentedWriter.WriteLine();
				}

				foreach (var p in typeDefVisitor.DataPointers)
				{
					indentedWriter.WriteLine("public partial struct " + p.Name);
					indentedWriter.WriteLine("{");
					indentedWriter.Indent++;
					indentedWriter.WriteLine("public " + p.Name + "(" + p.Fields[0].Item1 + " value)");
					indentedWriter.WriteLine("{");
					indentedWriter.Indent++;
					indentedWriter.WriteLine("Value = value;");
					indentedWriter.Indent--;
					indentedWriter.WriteLine("}");
					indentedWriter.WriteLine();
					indentedWriter.WriteLine("public " + p.Fields[0].Item1 + " Value;");
					indentedWriter.Indent--;
					indentedWriter.WriteLine("}");
					indentedWriter.WriteLine();
				}

				var enumVisitor = new EnumVisitor();
				foreach (var tu in translationUnits)
				{
					clang.visitChildren(clang.getTranslationUnitCursor(tu), enumVisitor.Visit, new CXClientData(IntPtr.Zero));
				}

				foreach(var e in enumVisitor.Enums)
				{
					codeGenerator.EnumDeclaration(e.Name, e.Type, e.Values.ToArray());
				}

				indentedWriter.WriteLine("public static partial class " + methodClassName);
				indentedWriter.WriteLine("{");

				indentedWriter.Indent++;

				var functionVisitor = new FunctionVisitor();
				foreach (var tu in translationUnits)
				{
					clang.visitChildren(clang.getTranslationUnitCursor(tu), functionVisitor.Visit, new CXClientData(IntPtr.Zero));
				}

				indentedWriter.WriteLine("private const string libraryPath = \"" + libraryPath + "\";");
				indentedWriter.WriteLine();

				foreach(var f in functionVisitor.Functions)
				{
					indentedWriter.WriteLine("[DllImport(libraryPath, EntryPoint = \"" + f.UnmanagedFunction + "\", CallingConvention = " + f.CallingConvention + ")]");
					indentedWriter.Write("public static extern ");
					indentedWriter.Write(f.ReturnType);
					indentedWriter.Write(" " + f.Name + "(");

					for (int i = 0; i < f.Parameters.Count; ++i)
					{
						indentedWriter.Write(f.Parameters[i].Type + " " + f.Parameters[i].Name);

						if (i != (f.Parameters.Count - 1))
						{
							indentedWriter.Write(", ");
						}
					}

					indentedWriter.WriteLine(");");
					indentedWriter.WriteLine();
				}

				indentedWriter.Indent--;

				indentedWriter.WriteLine("}");

				codeGenerator.EndGeneration();
			}

			foreach (var tu in translationUnits)
			{
				clang.disposeTranslationUnit(tu);
			}

			clang.disposeIndex(createIndex);

			//var visitor = RunVisitor(compilerArgs, headers);

			////Take all the functions and filter them by path, we only need the ones contained in the libvlc functions
			//// Exclude deprecated functions

			////var deprecated = visitor.Functions.Where(x => x.Value.IsDeprecated && x.Key.StartsWith("vlc")).ToList();
			//var functions = visitor.Functions.Select(x => x.Value).ToList();
			//var enums = visitor.Enums.Select(x => x.Value).ToList();
			//var structs = visitor.Structs.Select(x => x.Value).ToList();

			////Console.WriteLine(functions.ToJson());
			////Console.WriteLine(visitor.Enums.ToJson());

			////UpdateFunctionParamsAndReturnType(functions);

			////var enums = GenerateManagedEnumModel(visitor.Enums);

			////Console.WriteLine(functions.ToJson());
			////Console.WriteLine(visitor.Structs.ToJson());
			////Console.WriteLine(visitor.Enums.ToJson());
			//Console.WriteLine("Finished parsing");

			////Console.ReadLine();

			//// Get a workspace
			//var workspace = new AdhocWorkspace();

			//// Get the SyntaxGenerator for the specified language
			//var generator = SyntaxGenerator.GetGenerator(workspace, LanguageNames.CSharp);

			//// Create using/Imports directives
			//var usingDirectives = generator.NamespaceImportDeclaration("System");

			////Enums

			//var enumMembers = new List<SyntaxNode>();
			//foreach (var e in enums)
			//{
			//	var enumValues = new List<SyntaxNode>();
			//	foreach (var val in e.Values)
			//	{
			//		var member = generator.EnumMember(val.Item1, generator.LiteralExpression(val.Item2));					
			//		enumValues.Add(member);
			//	}

			//	var declaration = generator.EnumDeclaration(e.ManagedName,
			//		accessibility: Accessibility.Public,
			//		members: enumValues);

			//	enumMembers.Add(declaration);
			//}


			////Functions
			//var members = new List<SyntaxNode>();

			//foreach (var function in functions)
			//{
			//	var paramaters = new List<SyntaxNode>();

			//	foreach(var param in function.Parameters)
			//	{
			//		var p = generator.ParameterDeclaration(param.Name, generator.TypeExpression(SpecialType.System_String));
			//		paramaters.Add(p);
			//	}

			//	//TODO: Map return type

			//	var method = generator.DelegateDeclaration(function.Name,
			//								parameters: paramaters,
			//								//returnType: generator.TypeExpression(SpecialType.System_Void),
			//								modifiers: DeclarationModifiers.Static,
			//								accessibility: Accessibility.Public);

			//	members.Add(method);
			//}

			//// Generate the class
			////Vlc.DotNet.Core.Interops.Signatures
			//var classDefinition = generator.ClassDeclaration(
			//  "FunctionDelegates", typeParameters: null,
			//  accessibility: Accessibility.Public,
			//  modifiers: DeclarationModifiers.Static,
			//  baseType: null,			  
			//  members: members);

			//var signatureNameSpaceDeclaration = generator.NamespaceDeclaration("Vlc.DotNet.Core.Interop.Signatures", classDefinition);
			//var enumNamespaceDeclaration = generator.NamespaceDeclaration("Vlc.DotNet.Core.Interop.Enums", enumMembers);

			//// Get a CompilationUnit (code file) for the generated code
			//var functionNode = generator.CompilationUnit(usingDirectives, signatureNameSpaceDeclaration).
			//  NormalizeWhitespace();

			//var str = functionNode.ToFullString();

			//// Get a CompilationUnit (code file) for the generated code
			//var enumNode = generator.CompilationUnit(enumNamespaceDeclaration).
			//  NormalizeWhitespace();

			//str = enumNode.ToFullString();

			Console.ReadLine();
		}

		//private static IEnumerable<ManagedEnum> GenerateManagedEnumModel(IDictionary<string, Enum> enums)
		//{
		//	foreach(var e in enums)
		//	{
		//		yield return new ManagedEnum { Name = e.Key, Values = e.Value.Values.Select(x => Tuple.Create(x.Item1, int.Parse(x.Item2))) };
		//	}
		//}

		private static string[] BuildCompilerArgs(string includeDir, string header, FileInfo[] files)
		{
			//specify language as C using -x command line argument	
			var compilerArgs = new List<string>
			{
				"-x",
				"c++"
			};

			//Include folders
			compilerArgs.Add("-I");
			compilerArgs.Add(includeDir);

			//if (header != "libvlc.h")
			//{
			//	var headers = files.Select(x => x.Name).Where(y => y != header).ToList();

			//	foreach (var h in headers)
			//	{
			//		compilerArgs.Add("-include");
			//		compilerArgs.Add(h);
			//	}
			//}

			if (header != "libvlc.h")
			{
				//Include file before parsing (need the LIBVLC_API and LIBVLC_DEPRECATED macros included)
				compilerArgs.Add("-include");
				compilerArgs.Add("libvlc.h");
			}

			return compilerArgs.ToArray();
		}
	}
}
