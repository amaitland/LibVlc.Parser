namespace LibVlc.Parser.Clang
{
	using System;
	using System.Collections.Generic;
	using ClangSharp;

	internal sealed class EnumVisitor : ICXCursorVisitor
    {
        private readonly ISet<string> printedEnums = new HashSet<string>();

        private readonly ICodeGenerator codeGenerator;

        public EnumVisitor(ICodeGenerator codeGenerator)
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
            if (curKind == CXCursorKind.CXCursor_EnumDecl)
            {
                string inheritedEnumType;
                CXTypeKind kind = clang.getEnumDeclIntegerType(cursor).kind;

                switch (kind)
                {
                    case CXTypeKind.CXType_Int:
                        inheritedEnumType = "int";
                        break;
                    case CXTypeKind.CXType_UInt:
                        inheritedEnumType = "uint";
                        break;
                    case CXTypeKind.CXType_Short:
                        inheritedEnumType = "short";
                        break;
                    case CXTypeKind.CXType_UShort:
                        inheritedEnumType = "ushort";
                        break;
                    case CXTypeKind.CXType_LongLong:
                        inheritedEnumType = "long";
                        break;
                    case CXTypeKind.CXType_ULongLong:
                        inheritedEnumType = "ulong";
                        break;
                    default:
                        inheritedEnumType = "int";
                        break;
                }

                var enumName = clang.getCursorSpelling(cursor).ToString();

                // enumName can be empty because of typedef enum { .. } enumName;
                // so we have to find the sibling, and this is the only way I've found
                // to do with libclang, maybe there is a better way?
                if (string.IsNullOrEmpty(enumName))
                {
                    var forwardDeclaringVisitor = new ForwardDeclarationVisitor(cursor);
                    clang.visitChildren(clang.getCursorLexicalParent(cursor), forwardDeclaringVisitor.Visit, new CXClientData(IntPtr.Zero));
                    enumName = clang.getCursorSpelling(forwardDeclaringVisitor.ForwardDeclarationCursor).ToString();

                    if (string.IsNullOrEmpty(enumName))
                    {
                        enumName = "_";
                    }
                }

                // if we've printed these previously, skip them
                if (this.printedEnums.Contains(enumName))
                {
                    return CXChildVisitResult.CXChildVisit_Continue;
                }

                this.printedEnums.Add(enumName);

				var enumValues = new List<Tuple<string, string>>();

                // visit all the enum values
                clang.visitChildren(cursor, (cxCursor, _, __) =>
                {
					enumValues.Add(Tuple.Create(clang.getCursorSpelling(cxCursor).ToString(), clang.getEnumConstantDeclValue(cxCursor).ToString()));

					return CXChildVisitResult.CXChildVisit_Continue;
                },
				new CXClientData(IntPtr.Zero));

				codeGenerator.EnumDeclaration(enumName, inheritedEnumType, enumValues.ToArray());

			}

            return CXChildVisitResult.CXChildVisit_Recurse;
        }
    }
}