using System;
using System.Collections.Generic;
using System.Linq;
using ClangSharp;
using LibVlc.Parser.Model;

namespace LibVlc.Parser.Clang
{
	internal sealed class StructVisitor : ICXCursorVisitor
	{
		private readonly IDictionary<string, Struct> structs = new Dictionary<string, Struct>();

		public CXChildVisitResult Visit(CXCursor cursor, CXCursor parent, IntPtr data)
		{
			if (cursor.IsInSystemHeader())
			{
				return CXChildVisitResult.CXChildVisit_Continue;
			}

			CXCursorKind curKind = clang.getCursorKind(cursor);
			if (curKind == CXCursorKind.CXCursor_StructDecl)
			{
				var structName = clang.getCursorSpelling(cursor).ToString();

				// struct names can be empty, and so we visit its sibling to find the name
				if (string.IsNullOrEmpty(structName))
				{
					var forwardDeclaringVisitor = new ForwardDeclarationVisitor(cursor);
					clang.visitChildren(clang.getCursorSemanticParent(cursor), forwardDeclaringVisitor.Visit, new CXClientData(IntPtr.Zero));
					structName = clang.getCursorSpelling(forwardDeclaringVisitor.ForwardDeclarationCursor).ToString();

					if (string.IsNullOrEmpty(structName))
					{
						structName = "_";
					}
				}

				if (!structs.ContainsKey(structName))
				{
					var s = new Struct { Name = structName } ;

					var fieldPosition = 0;

					// visit all the enum values
					clang.visitChildren(cursor, (cxCursor, _, __) =>
					{
						var fieldName = clang.getCursorSpelling(cxCursor).ToString();
						if (string.IsNullOrEmpty(fieldName))
						{
							fieldName = "field" + fieldPosition;
						}

						fieldPosition++;
						s.Fields.Add(cxCursor.ToField(fieldName));

						return CXChildVisitResult.CXChildVisit_Continue;
					},
					new CXClientData(IntPtr.Zero));

					structs.Add(structName, s);
				}

				return CXChildVisitResult.CXChildVisit_Continue;
			}

			return CXChildVisitResult.CXChildVisit_Recurse;
		}

		public IList<Struct> Structs => structs.Select(x => x.Value).ToList();
	}
}