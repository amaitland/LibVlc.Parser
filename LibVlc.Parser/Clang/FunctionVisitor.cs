using System;
using System.Collections.Generic;
using System.Linq;
using ClangSharp;
using LibVlc.Parser.Model;

namespace LibVlc.Parser.Clang
{
	internal sealed class FunctionVisitor : ICXCursorVisitor
	{
		private readonly Dictionary<string, Method> functions = new Dictionary<string, Method>();

		public IList<Method> Functions => functions.Select(x => x.Value).ToList();

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

		private static Method WriteFunctionInfoHelper(CXCursor cursor)
		{
			var functionType = clang.getCursorType(cursor);
			var resultType = clang.getCursorResultType(cursor);

			var f = new Method();
			f.Name = clang.getCursorSpelling(cursor).ToString();
			f.UnmanagedFunction = f.Name;
			f.CallingConvention = functionType.CallingConventionSpelling();
			f.ReturnType = resultType.ReturnTypeHelper();

			int numArgTypes = clang.getNumArgTypes(functionType);

			for (uint i = 0; i < numArgTypes; ++i)
			{
				f.Parameters.Add(Extensions.ArgumentHelper(functionType, clang.Cursor_getArgument(cursor, i), i));
			}

			return f;
		}
	}
}