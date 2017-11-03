using System;
using ClangSharp;

namespace LibVlc.Parser.Clang
{
    internal sealed class ForwardDeclarationVisitor : ICXCursorVisitor
    {
        private readonly CXCursor beginningCursor;

        private bool beginningCursorReached;

        public ForwardDeclarationVisitor(CXCursor beginningCursor)
        {
            this.beginningCursor = beginningCursor;
        }

        public CXCursor ForwardDeclarationCursor { get; private set; }

        public CXChildVisitResult Visit(CXCursor cursor, CXCursor parent, IntPtr data)
        {
            if (cursor.IsInSystemHeader())
            {
                return CXChildVisitResult.CXChildVisit_Continue;
            }

            if (clang.equalCursors(cursor, beginningCursor) != 0)
            {
                beginningCursorReached = true;
                return CXChildVisitResult.CXChildVisit_Continue;
            }

            if (beginningCursorReached)
            {
                ForwardDeclarationCursor = cursor;
                return CXChildVisitResult.CXChildVisit_Break;
            }

            return CXChildVisitResult.CXChildVisit_Recurse;
        }
    }
}