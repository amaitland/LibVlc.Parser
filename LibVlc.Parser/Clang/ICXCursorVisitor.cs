using System;
using ClangSharp;

namespace LibVlc.Parser.Clang
{
    internal interface ICXCursorVisitor
    {
        CXChildVisitResult Visit(CXCursor cursor, CXCursor parent, IntPtr data);
    }
}