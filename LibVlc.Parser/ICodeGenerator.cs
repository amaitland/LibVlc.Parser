using System;
using System.Collections.Generic;
using LibVlc.Parser.Model;

namespace LibVlc.Parser
{
	public interface ICodeGenerator : IDisposable
	{
		void BeginGeneration();
		void EndGeneration();

		void EnumDeclaration(string name, string inheritedType, KeyValuePair<string, long>[] values);
		void StructDecleration(string name, Field[] fields);
	}
}
