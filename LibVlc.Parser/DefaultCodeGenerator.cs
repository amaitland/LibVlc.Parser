using System;
using System.CodeDom.Compiler;

namespace LibVlc.Parser
{
	public class DefaultCodeGenerator : ICodeGenerator
	{
		private IndentedTextWriter textWriter;
		private string @namespace;

		public DefaultCodeGenerator(IndentedTextWriter textWriter, string @namespace)
		{
			this.textWriter = textWriter;
			this.@namespace = @namespace;
		}

		void ICodeGenerator.BeginGeneration()
		{
			textWriter.WriteLine("namespace " + @namespace);
			textWriter.WriteLine("{");

			textWriter.Indent++;

			textWriter.WriteLine("using System;");
			textWriter.WriteLine("using System.Runtime.InteropServices;");
			textWriter.WriteLine();
		}

		void ICodeGenerator.EndGeneration()
		{
			textWriter.Indent--;
			textWriter.WriteLine("}");
		}

		void IDisposable.Dispose()
		{

		}

		void ICodeGenerator.EnumDeclaration(string name, string inheritedType, Tuple<string, string>[] values)
		{
			
			textWriter.WriteLine("public enum " + name + " : " + inheritedType);
			textWriter.WriteLine("{");

			textWriter.Indent++;

			foreach (var val in values)
			{
				textWriter.WriteLine(val.Item1.Replace("libvlc_", "") + " = " + val.Item2 + ",");
			}

			textWriter.Indent--;

			textWriter.WriteLine("}");
			textWriter.WriteLine();			
		}

		void ICodeGenerator.StructDecleration(string name, Tuple<string, string>[] fields)
		{
			textWriter.WriteLine("public partial struct " + name);
			textWriter.WriteLine("{");

			textWriter.Indent++;

			foreach (var field in fields)
			{
				if(field.Item2 != "")
				{
					textWriter.WriteLine(field.Item2);
				}
				textWriter.WriteLine(field.Item1);
			}

			textWriter.Indent--;

			textWriter.WriteLine("}");
			textWriter.WriteLine();
		}
	}
}
