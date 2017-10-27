using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using ClangSharp;

namespace LibVlc.Parser.Clang
{
	internal sealed class FunctionVisitor : ICXCursorVisitor
	{
		private readonly IndentedTextWriter tw;

		private readonly HashSet<string> visitedFunctions = new HashSet<string>();

		private readonly string prefixStrip;

		public FunctionVisitor(IndentedTextWriter tw, string libraryPath, string prefixStrip)
		{
			this.prefixStrip = prefixStrip;
			this.tw = tw;
			this.tw.WriteLine("private const string libraryPath = \"" + libraryPath + "\";");
			this.tw.WriteLine();
		}

		public CXChildVisitResult Visit(CXCursor cursor, CXCursor parent, IntPtr data)
		{
			if (cursor.IsInSystemHeader())
			{
				return CXChildVisitResult.CXChildVisit_Continue;
			}

			CXCursorKind curKind = clang.getCursorKind(cursor);

			// look only at function decls
			if (curKind == CXCursorKind.CXCursor_FunctionDecl)
			{
				var functionName = clang.getCursorSpelling(cursor).ToString();
				//var isDeprecated = clang.getCursorAvailability(cursor) == CXAvailabilityKind.CXAvailability_Deprecated;
				var isDeprecated = false; //TODO; 

				if (this.visitedFunctions.Contains(functionName) || isDeprecated)
				{
					return CXChildVisitResult.CXChildVisit_Continue;
				}

				var location = clang.getCursorLocation(cursor);

				clang.getFileLocation(location, out CXFile file, out uint line, out uint column, out uint offset);

				var fileName = clang.getFileName(file).ToString();

				visitedFunctions.Add(functionName);

				WriteFunctionInfoHelper(cursor, tw, prefixStrip);

				return CXChildVisitResult.CXChildVisit_Continue;
			}

			return CXChildVisitResult.CXChildVisit_Recurse;
		}

		private static void WriteFunctionInfoHelper(CXCursor cursor, TextWriter tw, string prefixStrip)
		{
			var functionType = clang.getCursorType(cursor);
			var functionName = clang.getCursorSpelling(cursor).ToString();
			var resultType = clang.getCursorResultType(cursor);

			tw.WriteLine("[DllImport(libraryPath, EntryPoint = \"" + functionName + "\", CallingConvention = " + functionType.CallingConventionSpelling() + ")]");
			tw.Write("public static extern ");

			tw.Write(resultType.ReturnTypeHelper());

			if (functionName.StartsWith(prefixStrip))
			{
				functionName = functionName.Substring(prefixStrip.Length);
			}

			tw.Write(" " + functionName + "(");

			int numArgTypes = clang.getNumArgTypes(functionType);

			for (uint i = 0; i < numArgTypes; ++i)
			{
				tw.Write(Extensions.ArgumentHelper(functionType, clang.Cursor_getArgument(cursor, i), i));

				if (i != (numArgTypes - 1))
				{
					tw.Write(", ");
				}
			}

			tw.WriteLine(");");
			tw.WriteLine();
		}
	}
}