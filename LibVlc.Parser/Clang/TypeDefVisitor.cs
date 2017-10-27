using System;
using System.Collections.Generic;
using ClangSharp;
using System.CodeDom.Compiler;

namespace LibVlc.Parser.Clang
{
    internal sealed class TypeDefVisitor : ICXCursorVisitor
    {
        private readonly IndentedTextWriter tw;

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

                if (this.visitedTypeDefs.Contains(spelling))
                {
                    return CXChildVisitResult.CXChildVisit_Continue;
                }

                this.visitedTypeDefs.Add(spelling);

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
                        this.tw.WriteLine("public partial struct " + spelling);
                        this.tw.WriteLine("{");
                        tw.Indent++;
                        this.tw.WriteLine("public " + spelling + "(IntPtr pointer)");
                        this.tw.WriteLine("{");
                        tw.Indent++;
                        this.tw.WriteLine("this.Pointer = pointer;");
                        tw.Indent--;
                        this.tw.WriteLine("}");
                        this.tw.WriteLine();
                        tw.Indent--;
                        this.tw.WriteLine("public IntPtr Pointer;");
                        this.tw.WriteLine("}");
                        this.tw.WriteLine();

                        return CXChildVisitResult.CXChildVisit_Continue;
                    }

                    if (pointee.kind == CXTypeKind.CXType_FunctionProto)
                    {
                        this.tw.WriteLine("[UnmanagedFunctionPointer(" + pointee.CallingConventionSpelling() + ")]");
                        this.tw.Write("public delegate ");
                        tw.Write(Extensions.ReturnTypeHelper(clang.getResultType(pointee)));
                        this.tw.Write(" ");
                        this.tw.Write(spelling);
                        this.tw.Write("(");

                        uint argumentCounter = 0;

                        clang.visitChildren(cursor, delegate(CXCursor cxCursor, CXCursor parent1, IntPtr ptr)
                        {
                            if (cxCursor.kind == CXCursorKind.CXCursor_ParmDecl)
                            {
                                int numArgTypes = clang.getNumArgTypes(pointee);
                                tw.Write(Extensions.ArgumentHelper(pointee, cxCursor, argumentCounter++));

                                if (argumentCounter < numArgTypes)
                                {
                                    tw.Write(", ");
                                }
                            }

                            return CXChildVisitResult.CXChildVisit_Continue;
                        }, new CXClientData(IntPtr.Zero));

                        this.tw.WriteLine(");");
                        this.tw.WriteLine();

                        return CXChildVisitResult.CXChildVisit_Continue;
                    }
                }

                if (clang.isPODType(type) != 0)
                {
                    var podType = type.ToPlainTypeString();
                    this.tw.WriteLine("public partial struct " + spelling);
                    this.tw.WriteLine("{");
                    tw.Indent++;
                    this.tw.WriteLine("public " + spelling + "(" + podType + " value)");
                    this.tw.WriteLine("{");
                    tw.Indent++;
                    this.tw.WriteLine("this.Value = value;");
                    tw.Indent--;
                    this.tw.WriteLine("}");
                    this.tw.WriteLine();
                    this.tw.WriteLine("public " + type.ToPlainTypeString() + " Value;");
                    tw.Indent--;
                    this.tw.WriteLine("}");
                    this.tw.WriteLine();
                }

                return CXChildVisitResult.CXChildVisit_Continue;
            }

            return CXChildVisitResult.CXChildVisit_Recurse;
        }
    }
}