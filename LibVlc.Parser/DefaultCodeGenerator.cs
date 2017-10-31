using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using LibVlc.Parser.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace LibVlc.Parser
{
	public class DefaultCodeGenerator : ICodeGenerator
	{
		private AdhocWorkspace _workspace;
		private Project _project;
		private IndentedTextWriter _textWriter;
		private string _namespace;
		private string _outputFolder;
		private string _enumNamespace;
		private string _structNamespace;

		public DefaultCodeGenerator(string outputFolder, IndentedTextWriter textWriter, string ns)
		{
			_textWriter = textWriter;
			_namespace = ns;
			_outputFolder = outputFolder;

			_enumNamespace = _namespace + ".Enums";
			_structNamespace = _namespace + ".Structs"; //TODO: Maybe a different name

			_workspace = new AdhocWorkspace();

			string projName = "LibVlc.Interop";
			var projectId = ProjectId.CreateNewId();
			var versionStamp = VersionStamp.Create();
			var projectInfo = ProjectInfo.Create(projectId, versionStamp, projName, projName, LanguageNames.CSharp);
			_project = _workspace.AddProject(projectInfo);
			//https://joshvarty.wordpress.com/2014/09/12/learn-roslyn-now-part-6-working-with-workspaces/
			//var sourceText = SourceText.From("class A {}");

			//// Create using/Imports directives
			//var usingDirectives = _syntaxGenerator.NamespaceImportDeclaration("System");
		}

		void ICodeGenerator.BeginGeneration()
		{
			_textWriter.WriteLine("namespace " + _namespace);
			_textWriter.WriteLine("{");

			_textWriter.Indent++;

			_textWriter.WriteLine("using System;");
			_textWriter.WriteLine("using System.Runtime.InteropServices;");
			_textWriter.WriteLine();
		}

		void ICodeGenerator.EndGeneration()
		{
			_textWriter.Indent--;
			_textWriter.WriteLine("}");
		}

		void IDisposable.Dispose()
		{

		}

		void ICodeGenerator.EnumDeclaration(string name, string inheritedType, KeyValuePair<string, long>[] values)
		{
			// Get the SyntaxGenerator for the specified language
			var syntaxGenerator = SyntaxGenerator.GetGenerator(_workspace, LanguageNames.CSharp);

			var enumValues = new List<SyntaxNode>();
			foreach (var val in values)
			{
				var member = syntaxGenerator.EnumMember(val.Key, syntaxGenerator.LiteralExpression(Convert.ToInt32(val.Value)));
				enumValues.Add(member);
			}

			var declaration = syntaxGenerator.EnumDeclaration(name,
				accessibility: Accessibility.Public,
				members: enumValues);

			var enumNamespaceDeclaration = syntaxGenerator.NamespaceDeclaration(_enumNamespace, declaration);

			//// Get a CompilationUnit (code file) for the generated code
			var enumNode = syntaxGenerator.CompilationUnit(enumNamespaceDeclaration).NormalizeWhitespace();

			var outputFile = Path.Combine(_outputFolder, @"Enums\" + name + ".cs");

			enumNode.WriteToFilePath(outputFile);
		}

		void ICodeGenerator.StructDecleration(string name, Field[] fields)
		{
			var outputFile = Path.Combine(_outputFolder, @"Structs\" + name + ".cs");

			var file = new FileInfo(outputFile);
			file.Directory.Create(); // Create folder if doesn't exist

			//var docInfo = DocumentInfo.Create(DocumentId.CreateNewId(_project.Id), file.Name, isGenerated: true);

			//var newDocument = _workspace.AddDocument(docInfo);

			// Get the SyntaxGenerator for the specified language
			var syntaxGenerator = SyntaxGenerator.GetGenerator(_workspace, LanguageNames.CSharp);

			var usingDirectives = syntaxGenerator.NamespaceImportDeclaration("System");

			//[StructLayout(LayoutKind.Sequential)]

			int i = 0;

			var structFields = new List<SyntaxNode>();
			foreach (var val in fields)
			{
				//TODO: map val.Item2 to a TypeExpression
				var type = UnmanagedTypeToSpecialType(val.Type);
				var member = syntaxGenerator.FieldDeclaration(val.Name, syntaxGenerator.TypeExpression(type), Accessibility.Public);
				structFields.Add(syntaxGenerator.AddAttributes(member, syntaxGenerator.Attribute("[FieldOffset(" + i++ +")]")));
			}

			var declaration = syntaxGenerator.StructDeclaration(name,
				accessibility: Accessibility.Public,
				modifiers: DeclarationModifiers.Partial,
				
				members: structFields);

			//var s = syntaxGenerator.AddAttributes(declaration, syntaxGenerator.Attribute("[StructLayout(LayoutKind.Sequential, Pack=1, CharSet=CharSet.Unicode)]"));

			var namespaceDeclaration = syntaxGenerator.NamespaceDeclaration(_structNamespace, declaration);

			// Get a CompilationUnit (code file) for the generated code
			var structNode = syntaxGenerator.CompilationUnit(usingDirectives, namespaceDeclaration).NormalizeWhitespace();

			structNode.WriteToFilePath(outputFile);
		}

		private SpecialType UnmanagedTypeToSpecialType(string type)
		{
			switch(type)
			{
				case "string":
				{
					return SpecialType.System_String;
				}
				case "int":
				{
					return SpecialType.System_Int32;
				}
				//case "IntPtr":
				//{
				//	return SpecialType.System_IntPtr;
				//}
			}

			return SpecialType.System_String;
		}

		
	}
}
