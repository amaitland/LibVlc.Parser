using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClangSharp;
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

			using (ICodeGenerator codeGenerator = new DefaultCodeGenerator("output"))
			{
				var structVisitor = new StructVisitor();
				foreach (var tu in translationUnits)
				{
					clang.visitChildren(clang.getTranslationUnitCursor(tu), structVisitor.Visit, new CXClientData(IntPtr.Zero));
				}

				foreach(var s in structVisitor.Structs)
				{
					codeGenerator.StructDecleration(s.Name, s.Fields.ToArray());
				}

				var typeDefVisitor = new TypeDefVisitor();
				foreach (var tu in translationUnits)
				{
					clang.visitChildren(clang.getTranslationUnitCursor(tu), typeDefVisitor.Visit, new CXClientData(IntPtr.Zero));
				}

				foreach(var p in typeDefVisitor.Pointers)
				{
					codeGenerator.StructDecleration(p.Name, p.Fields.ToArray());
				}

				codeGenerator.GenerateDataPointers(typeDefVisitor.DataPointers);

				var enumVisitor = new EnumVisitor();
				foreach (var tu in translationUnits)
				{
					clang.visitChildren(clang.getTranslationUnitCursor(tu), enumVisitor.Visit, new CXClientData(IntPtr.Zero));
				}

				foreach(var e in enumVisitor.Enums)
				{
					codeGenerator.EnumDeclaration(e.Name, e.Type, e.Values.ToArray());
				}

				var functionVisitor = new FunctionVisitor();
				foreach (var tu in translationUnits)
				{
					clang.visitChildren(clang.getTranslationUnitCursor(tu), functionVisitor.Visit, new CXClientData(IntPtr.Zero));
				}

				var functions = functionVisitor.Functions;
				functions.AddRange(typeDefVisitor.Delegates);

				codeGenerator.GenerateFunctions(functions);
			}

			foreach (var tu in translationUnits)
			{
				clang.disposeTranslationUnit(tu);
			}

			clang.disposeIndex(createIndex);

			Console.ReadLine();
		}

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
