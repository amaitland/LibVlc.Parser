using System;
using System.Collections.Generic;
using LibVlc.Parser.Model;

namespace LibVlc.Parser
{
	public interface ICodeGenerator : IDisposable
	{
		void EnumDeclaration(string name, string inheritedType, KeyValuePair<string, long>[] values);
		void StructDecleration(string name, Field[] fields);
		//void GenerateDelegates(IList<Function> functions);
		void GenerateFunctions(IList<Function> functions);
		void GenerateDataPointers(IList<Struct> structs);
	}
}
