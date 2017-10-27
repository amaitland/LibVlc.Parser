using System;
using System.Collections.Generic;
using System.Linq;
using ClangSharp;
using LibVlc.Parser.Model;

namespace LibVlc.Parser.Clang
{
	internal sealed class FunctionVisitor : ICXCursorVisitor
	{
		private readonly Dictionary<string, Function> functions = new Dictionary<string, Function>();

		public IList<Function> Functions => functions.Select(x => x.Value).ToList();

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

				if (functions.ContainsKey(functionName) || isDeprecated)
				{
					return CXChildVisitResult.CXChildVisit_Continue;
				}

				var location = clang.getCursorLocation(cursor);

				clang.getFileLocation(location, out CXFile file, out uint line, out uint column, out uint offset);

				var fileName = clang.getFileName(file).ToString();

				var f = WriteFunctionInfoHelper(cursor);

				functions.Add(functionName, f);

				return CXChildVisitResult.CXChildVisit_Continue;
			}

			return CXChildVisitResult.CXChildVisit_Recurse;
		}

		private static Function WriteFunctionInfoHelper(CXCursor cursor)
		{
			var functionType = clang.getCursorType(cursor);
			var resultType = clang.getCursorResultType(cursor);

			var f = new Function();
			f.Name = clang.getCursorSpelling(cursor).ToString();
			f.UnmanagedFunction = f.Name;
			f.CallingConvention = functionType.CallingConventionSpelling();
			f.ReturnType = resultType.ReturnTypeHelper();
			f.Comment = ""; //When clang is updated to 6.0.0 there's a clang_Cursor_getParsedComment() https://clang.llvm.org/doxygen/group__CINDEX__COMMENT.html#gab4f95ae3b2e0bd63b10cecc3727a391e

			int numArgTypes = clang.getNumArgTypes(functionType);

			for (uint i = 0; i < numArgTypes; ++i)
			{
				f.Parameters.Add(Extensions.ArgumentHelper(functionType, clang.Cursor_getArgument(cursor, i), i));
			}

			return f;
		}
	}
}