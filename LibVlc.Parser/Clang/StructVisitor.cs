using System;
using System.Collections.Generic;
using ClangSharp;

namespace LibVlc.Parser.Clang
{
	internal sealed class StructVisitor : ICXCursorVisitor
	{
		private readonly ISet<string> visitedStructs = new HashSet<string>();

		private readonly ICodeGenerator codeGenerator;

		public StructVisitor(ICodeGenerator codeGenerator)
		{
			this.codeGenerator = codeGenerator;
		}

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

				if (!this.visitedStructs.Contains(structName))
				{
					var values = new List<Tuple<string, string>>();

					var fieldPosition = 0;

					// visit all the enum values
					clang.visitChildren(cursor, (cxCursor, _, __) =>
					{
						var fieldName = clang.getCursorSpelling(cxCursor).ToString();
						if (string.IsNullOrEmpty(fieldName))
						{
							fieldName = "field" + fieldPosition; // what if they have fields called field*? :)
						}

						fieldPosition++;
						values.Add(cxCursor.ToMarshalString(fieldName));

						return CXChildVisitResult.CXChildVisit_Continue;
					},
					new CXClientData(IntPtr.Zero));

					this.visitedStructs.Add(structName);

					codeGenerator.StructDecleration(structName, values.ToArray());

				}

				return CXChildVisitResult.CXChildVisit_Continue;
			}

			return CXChildVisitResult.CXChildVisit_Recurse;
		}

		
	}
}