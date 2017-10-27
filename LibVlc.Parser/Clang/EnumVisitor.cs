using System;
using System.Collections.Generic;
using System.Linq;
using ClangSharp;
using Enum = LibVlc.Parser.Model.Enum;

namespace LibVlc.Parser.Clang
{
    internal sealed class EnumVisitor : ICXCursorVisitor
    {
        private readonly IDictionary<string, Enum> enums = new Dictionary<string, Enum>();

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

                // if we've already parsed this enum previously, skip them
                if (enums.ContainsKey(enumName))
                {
                    return CXChildVisitResult.CXChildVisit_Continue;
                }

                var e = new Enum { Name = enumName, Type = inheritedEnumType };

                // visit all the enum values
                clang.visitChildren(cursor, (cxCursor, _, __) =>
                {
                    var key = clang.getCursorSpelling(cxCursor).ToString();
                    var val = clang.getEnumConstantDeclValue(cxCursor);
                    e.Values.Add(new KeyValuePair<string, long>(key, val));

                    return CXChildVisitResult.CXChildVisit_Continue;
                },
                new CXClientData(IntPtr.Zero));

                enums.Add(enumName, e);
            }

            return CXChildVisitResult.CXChildVisit_Recurse;
        }

        public IList<Enum> Enums => enums.Select(x => x.Value).ToList(); 
    }
}