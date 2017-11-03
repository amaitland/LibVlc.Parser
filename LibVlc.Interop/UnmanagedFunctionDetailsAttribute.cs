using System;

namespace LibVlc.Interop
{
    public class UnmanagedFunctionDetailsAttribute : Attribute
    {
        public UnmanagedFunctionDetailsAttribute(string functionName)
        {
            FunctionName = functionName;
        }

        public string FunctionName { get; private set; }
    }
}
