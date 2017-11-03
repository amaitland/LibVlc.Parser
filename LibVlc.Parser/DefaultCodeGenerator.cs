using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LibVlc.Parser.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace LibVlc.Parser
{
	public class DefaultCodeGenerator : ICodeGenerator
	{
		private const string ProjectName = "LibVlc.Interop";
		private const string SolutionName = ProjectName + ".sln";
		private static TextInfo TextInfo = new CultureInfo("en-US", false).TextInfo;
		private const string NameSpace = "LibVlc.Interop";
		private const string EnumNamespace = NameSpace + ".Enums";
		private const string StructNamespace = NameSpace + ".Structs"; //TODO: Maybe a different name
		
		private string _outputFolder;
		private readonly AdhocWorkspace _workspace;
		private readonly SyntaxGenerator _generator;

		public DefaultCodeGenerator(string outputFolder)
		{
			_outputFolder = outputFolder;
			_workspace = new AdhocWorkspace();

			var projectInfo = ProjectInfo.Create(ProjectId.CreateNewId(),
				VersionStamp.Default,
				ProjectName,
				ProjectName,
				LanguageNames.CSharp,
				Path.Combine(outputFolder, ProjectName, ".csproj"),
				Path.Combine(outputFolder, ProjectName));

			var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, Path.Combine(outputFolder, SolutionName), new[] { projectInfo });

			var solution = _workspace.AddSolution(solutionInfo);

			var project = solution.Projects.First();

			_generator = SyntaxGenerator.GetGenerator(_workspace, LanguageNames.CSharp);
		}

		void IDisposable.Dispose()
		{
			
		}

		void ICodeGenerator.EnumDeclaration(string name, string inheritedType, KeyValuePair<string, long>[] values)
		{
			var enumValues = new List<SyntaxNode>();
			foreach (var val in values)
			{
				var member = _generator.EnumMember(val.Key, _generator.LiteralExpression(Convert.ToInt32(val.Value)));
				enumValues.Add(member);
			}

			var declaration = _generator.EnumDeclaration(name,
				accessibility: Accessibility.Public,
				members: enumValues);

			var enumNamespaceDeclaration = _generator.NamespaceDeclaration(EnumNamespace, declaration);

			//// Get a CompilationUnit (code file) for the generated code
			var enumNode = _generator.CompilationUnit(enumNamespaceDeclaration).NormalizeWhitespace();

			var outputFile = Path.Combine(_outputFolder, @"Enums\" + name + ".cs");

			enumNode.WriteToFilePath(outputFile);
		}

		void ICodeGenerator.StructDecleration(string name, Field[] fields)
		{
			var outputFile = Path.Combine(_outputFolder, @"Structs\" + name + ".cs");

			// Get the SyntaxGenerator for the specified language
			var usingDirectives = _generator.NamespaceImportDeclaration("System");

			int i = 0;

			var structFields = new List<SyntaxNode>();
			foreach (var val in fields)
			{
				var type = UnmanagedTypeToSpecialType(_generator, val.Type);
				var member = _generator.FieldDeclaration(val.Name, type, Accessibility.Public);
				structFields.Add(_generator.AddAttributes(member, _generator.Attribute("FieldOffset(" + i++ +")")));
			}

			var declaration = _generator.StructDeclaration(name,
				accessibility: Accessibility.Public,
				modifiers: DeclarationModifiers.Partial,				
				members: structFields);

			var s = _generator.AddAttributes(declaration, _generator.Attribute("StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Unicode)"));

			var namespaceDeclaration = _generator.NamespaceDeclaration(StructNamespace, s);

			// Get a CompilationUnit (code file) for the generated code
			var structNode = _generator.CompilationUnit(usingDirectives, namespaceDeclaration).NormalizeWhitespace();

			structNode.WriteToFilePath(outputFile);
		}

		//void ICodeGenerator.GenerateDelegates(IList<Function> functions)
		//{
		//	var outputFile = Path.Combine(_outputFolder, @"Delegates.cs");

		//	var usingDirectives = _syntaxGenerator.NamespaceImportDeclaration("System");

		//	var delegates = new List<SyntaxNode>();

		//	foreach (var func in functions)
		//	{
		//		int i = 0;

		//		var paramaters = new List<SyntaxNode>();
		//		foreach (var val in func.Parameters)
		//		{
		//			var paramType = GetRefKind(val.Type, out RefKind refKind);
		//			var type = UnmanagedTypeToSpecialType(_syntaxGenerator, paramType);
		//			var paramDec = _syntaxGenerator.ParameterDeclaration(val.Name, type, refKind: refKind);
		//			paramaters.Add(paramDec);
		//		}

		//		var declaration = _syntaxGenerator.DelegateDeclaration(func.Name,
		//			accessibility: Accessibility.Public,
		//			parameters: paramaters);

		//		var d = _syntaxGenerator.AddAttributes(declaration, _syntaxGenerator.Attribute("UnmanagedFunctionPointer(CallingConvention.Cdecl)"));

		//		delegates.Add(d);
		//	}

		//	var namespaceDeclaration = _syntaxGenerator.NamespaceDeclaration(NameSpace, delegates);

		//	// Get a CompilationUnit (code file) for the generated code
		//	var structNode = _syntaxGenerator.CompilationUnit(usingDirectives, namespaceDeclaration).NormalizeWhitespace();

		//	structNode.WriteToFilePath(outputFile);
		//}

		void ICodeGenerator.GenerateFunctions(IList<Function> functions)
		{
			var groupedFunctions = functions.GroupBy(x => x.FileName);

			foreach (var group in groupedFunctions)
			{
				var fileName = Path.GetFileNameWithoutExtension(group.Key);
				var outputFile = Path.Combine(_outputFolder, fileName + ".cs");

				var nodes = new List<SyntaxNode>();

				foreach (var func in group)
				{
					int i = 0;

					var paramaters = new List<SyntaxNode>();
					foreach (var val in func.Parameters)
					{
						var paramType = GetRefKind(val.Type, out RefKind refKind);
						var type = UnmanagedTypeToSpecialType(_generator, paramType);
						var paramDec = _generator.ParameterDeclaration(val.Name, type, refKind: refKind);
						paramaters.Add(paramDec);
					}

					var declaration = _generator.DelegateDeclaration(func.Name,
						accessibility: Accessibility.Public,
						returnType: UnmanagedTypeToSpecialType(_generator, func.ReturnType),
						parameters: paramaters);

					SyntaxNode node;

					if (func.IsDelegate)
					{
						node = _generator.AddAttributes(declaration,
																_generator.Attribute("UnmanagedFunctionPointer(CallingConvention.Cdecl)"));
					}
					else
					{
						node = _generator.AddAttributes(declaration,
															_generator.Attribute("UnmanagedFunctionDetails(\"" + func.UnmanagedFunction + "\")"),
															_generator.Attribute("UnmanagedFunctionPointer(CallingConvention.Cdecl)"));
					}

					//TODO: Get new lines working for easier to read code
					nodes.Add(node.WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));
				}

				var namespaceDeclaration = _generator.NamespaceDeclaration(NameSpace, nodes);

				// Get a CompilationUnit (code file) for the generated code
				var structNode = _generator.CompilationUnit(_generator.NamespaceImportDeclaration("System"),
																  _generator.NamespaceImportDeclaration(EnumNamespace),
																  _generator.NamespaceImportDeclaration(StructNamespace),
																  namespaceDeclaration).NormalizeWhitespace();

				structNode.WriteToFilePath(outputFile);
			}
		}

		void ICodeGenerator.GenerateDataPointers(IList<Struct> structs)
		{
			foreach (var p in structs)
			{
				var outputFile = Path.Combine(_outputFolder, @"Structs\" + p.Name + ".cs");

				// Get the SyntaxGenerator for the specified language
				var usingDirectives = _generator.NamespaceImportDeclaration("System");

				int i = 0;

				var members = new List<SyntaxNode>();

				var val = p.Fields[0];
				var type = UnmanagedTypeToSpecialType(_generator, val.Type);
				var member = _generator.FieldDeclaration("Value", type, Accessibility.Public);
				members.Add(member);

				var param = _generator.ParameterDeclaration("value", type: type);
				var statement = _generator.AssignmentStatement(_generator.IdentifierName("Value"),
					_generator.IdentifierName("value"));

				var constructor = _generator.ConstructorDeclaration(p.Name,
					parameters: new[] { param },
					statements: new[] { statement },
					accessibility: Accessibility.Public);

				members.Add(constructor);

				var declaration = _generator.StructDeclaration(p.Name,
					accessibility: Accessibility.Public,
					modifiers: DeclarationModifiers.Partial,
					members: members);

				var namespaceDeclaration = _generator.NamespaceDeclaration(StructNamespace, declaration);

				// Get a CompilationUnit (code file) for the generated code
				var structNode = _generator.CompilationUnit(usingDirectives, namespaceDeclaration).NormalizeWhitespace();

				structNode.WriteToFilePath(outputFile);
			}
		}

		private string GetRefKind(string type, out RefKind refKind)
		{
			if(type.StartsWith("out"))
			{
				refKind = RefKind.Out;
				return type.Substring(4);
			}


			refKind = RefKind.None;
			return type;
		}

		private SyntaxNode UnmanagedTypeToSpecialType(SyntaxGenerator generator, string type)
		{
			switch(type)
			{
				case "void":
				{
					return null;
				}
				case "int":
				{
					return generator.TypeExpression(SpecialType.System_Int32);
				}
				case "IntPtr":
				{
					return generator.IdentifierName(typeof(IntPtr).Name);
				}
				case "float":
				{
					return generator.TypeExpression(SpecialType.System_Single);
				}
				case "uint":
				{
					return generator.TypeExpression(SpecialType.System_UInt32);
				}
				case "long":
				{
					return generator.TypeExpression(SpecialType.System_Int64);
				}
				default:
				{
					var name = "System_" + TextInfo.ToTitleCase(type);

					if(System.Enum.TryParse(name, out SpecialType enumVal))
					{
						return generator.TypeExpression(enumVal);
					}

					if(type.Contains("anonymous union"))
					{
						//TODO: Look at improving this - haven't deal with the struct unions yet
						return generator.IdentifierName(typeof(IntPtr).Name);
					}

					return generator.IdentifierName(type);
				}
			}
		}
	}
}
