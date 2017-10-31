using System;
using System.Collections.Generic;
using ClangSharp;
using System.CodeDom.Compiler;
using LibVlc.Parser.Model;

namespace LibVlc.Parser.Clang
{
    internal sealed class TypeDefVisitor : ICXCursorVisitor
    {
        private readonly IndentedTextWriter tw;
        public readonly IList<Struct> Pointers = new List<Struct>();
        public readonly IList<Function> Delegates = new List<Function>();
        public readonly IList<Struct> DataPointers = new List<Struct>();

        private readonly HashSet<string> visitedTypeDefs = new HashSet<string>();

        public TypeDefVisitor(IndentedTextWriter tw)
        {
            this.tw = tw;
        }

        public CXChildVisitResult Visit(CXCursor cursor, CXCursor parent, IntPtr data)
        {
            if (cursor.IsInSystemHeader())
            {
                return CXChildVisitResult.CXChildVisit_Continue;
            }

            CXCursorKind curKind = clang.getCursorKind(cursor);
            if (curKind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var spelling = clang.getCursorSpelling(cursor).ToString();

                if (visitedTypeDefs.Contains(spelling))
                {
                    return CXChildVisitResult.CXChildVisit_Continue;
                }

                visitedTypeDefs.Add(spelling);

                CXType type = clang.getCanonicalType(clang.getTypedefDeclUnderlyingType(cursor));

                // we handle enums and records in struct and enum visitors with forward declarations also
                if (type.kind == CXTypeKind.CXType_Record || type.kind == CXTypeKind.CXType_Enum)
                {
                    return CXChildVisitResult.CXChildVisit_Continue;
                }

                // no idea what this is? -- template stuff?
                if (type.kind == CXTypeKind.CXType_Unexposed)
                {
                    var canonical = clang.getCanonicalType(type);
                    if (canonical.kind == CXTypeKind.CXType_Unexposed)
                    {
                        return CXChildVisitResult.CXChildVisit_Continue; 
                    }
                }

                if (type.kind == CXTypeKind.CXType_Pointer)
                {
                    var pointee = clang.getPointeeType(type);
                    if (pointee.kind == CXTypeKind.CXType_Record || pointee.kind == CXTypeKind.CXType_Void)
                    {
                        var s = new Struct { Name = spelling };
                        s.Fields.Add(new Field { Name = "Pointer", Type = "IntPtr" });

                        Pointers.Add(s);

                        return CXChildVisitResult.CXChildVisit_Continue;
                    }

                    if (pointee.kind == CXTypeKind.CXType_FunctionProto)
                    {
                        var location = clang.getCursorLocation(cursor);

                        clang.getFileLocation(location, out CXFile file, out uint line, out uint column, out uint offset);

                        var fileName = clang.getFileName(file).ToString();

                        var d = new Function
                        {
                            Name = spelling,
                            ReturnType = Extensions.ReturnTypeHelper(clang.getResultType(pointee)),
                            FileName = fileName,
                            UnmanagedFunction = pointee.CallingConventionSpelling(),
                            Comment = "" //TODO: Get comment
                        };

                        uint argumentCounter = 0;
                        int numArgTypes = clang.getNumArgTypes(pointee);

                        clang.visitChildren(cursor, delegate(CXCursor cxCursor, CXCursor parent1, IntPtr ptr)
                        {
                            if (cxCursor.kind == CXCursorKind.CXCursor_ParmDecl)
                            {
                                var p = Extensions.ArgumentHelper(pointee, cxCursor, argumentCounter++);
                                d.Parameters.Add(p);
                            }

                            return CXChildVisitResult.CXChildVisit_Continue;
                        }, new CXClientData(IntPtr.Zero));

                        Delegates.Add(d);

                        return CXChildVisitResult.CXChildVisit_Continue;
                    }
                }

                if (clang.isPODType(type) != 0)
                {
                    var podType = type.ToPlainTypeString();

                    var s = new Struct { Name = spelling };
                    s.Fields.Add(new Field { Type = podType, Name = "Pointer" });

                    DataPointers.Add(s);
                }

                return CXChildVisitResult.CXChildVisit_Continue;
            }

            return CXChildVisitResult.CXChildVisit_Recurse;
        }
    }
}