using System;

namespace LibVlc.Parser
{
	public interface ICodeGenerator : IDisposable
	{
		void BeginGeneration();
		void EndGeneration();

		void EnumDeclaration(string name, string inheritedType, Tuple<string, string>[] values);
		void StructDecleration(string name, Tuple<string, string>[] fields);
	}
}
