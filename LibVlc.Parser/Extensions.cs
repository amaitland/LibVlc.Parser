using System;
using System.Text;
using ClangSharp;
using Newtonsoft.Json;

namespace LibVlc.Parser
{
	public static class Extensions
	{
		public static string ToJson(this object obj)
		{
			return JsonConvert.SerializeObject(obj, Formatting.Indented);
		}

		public static string CallingConventionSpelling(this CXType type)
		{
			var callingConvention = clang.getFunctionTypeCallingConv(type);
			switch (callingConvention)
			{
				case CXCallingConv.CXCallingConv_X86StdCall:
				case CXCallingConv.CXCallingConv_X86_64Win64:
				return "CallingConvention.StdCall";
				default:
				return "CallingConvention.Cdecl";
			}
		}

		public static bool IsInSystemHeader(this CXCursor cursor)
		{
			return clang.Location_isInSystemHeader(clang.getCursorLocation(cursor)) != 0;
		}

		public static bool IsPtrToConstChar(this CXType type)
		{
			var pointee = clang.getPointeeType(type);

			if (clang.isConstQualifiedType(pointee) != 0)
			{
				switch (pointee.kind)
				{
					case CXTypeKind.CXType_Char_S:
					return true;
				}
			}

			return false;
		}

		public static string ToPlainTypeString(this CXType type, string unknownType = "UnknownType")
		{
			var canonical = clang.getCanonicalType(type);
			switch (type.kind)
			{
				case CXTypeKind.CXType_Bool:
					return "bool";
				case CXTypeKind.CXType_UChar:
				case CXTypeKind.CXType_Char_U:
					return "char";
				case CXTypeKind.CXType_SChar:
				case CXTypeKind.CXType_Char_S:
					return "sbyte";
				case CXTypeKind.CXType_UShort:
					return "ushort";
				case CXTypeKind.CXType_Short:
					return "short";
				case CXTypeKind.CXType_Float:
					return "float";
				case CXTypeKind.CXType_Double:
					return "double";
				case CXTypeKind.CXType_Int:
					return "int";
				case CXTypeKind.CXType_UInt:
					return "uint";
				case CXTypeKind.CXType_Pointer:
				case CXTypeKind.CXType_NullPtr: // ugh, what else can I do?
					return "IntPtr";
				case CXTypeKind.CXType_Long:
					return "int";
				case CXTypeKind.CXType_ULong:
					return "int";
				case CXTypeKind.CXType_LongLong:
					return "long";
				case CXTypeKind.CXType_ULongLong:
					return "ulong";
				case CXTypeKind.CXType_Void:
					return "void";
				case CXTypeKind.CXType_Unexposed:
					if (canonical.kind == CXTypeKind.CXType_Unexposed)
					{
						return clang.getTypeSpelling(canonical).ToString();
					}
					return canonical.ToPlainTypeString();
				default:
					return unknownType;
			}
		}

		public static Tuple<string, string> ToMarshalString(this CXCursor cursor, string cursorSpelling)
		{
			var canonical = clang.getCanonicalType(clang.getCursorType(cursor));
			
			switch (canonical.kind)
			{
				case CXTypeKind.CXType_ConstantArray:
					long arraySize = clang.getArraySize(canonical);
					var elementType = clang.getCanonicalType(clang.getArrayElementType(canonical));

					var sb = new StringBuilder();
					for (int i = 0; i < arraySize; ++i)
					{
						sb.Append("public " + elementType.ToPlainTypeString() + " " + cursorSpelling + i + "; ");
					}

					return Tuple.Create(sb.ToString(), "");
				case CXTypeKind.CXType_Pointer:
					var pointeeType = clang.getCanonicalType(clang.getPointeeType(canonical));
					switch (pointeeType.kind)
					{
						case CXTypeKind.CXType_Char_S:
							return Tuple.Create("public string " + cursorSpelling + ";", "[MarshalAs(UnmanagedType.LPStr)]");
						case CXTypeKind.CXType_WChar:
							return Tuple.Create("public string " + cursorSpelling + ";", "[MarshalAs(UnmanagedType.LPWStr)]");
						default:
							return Tuple.Create("public IntPtr " + cursorSpelling + ";", "");
					}
				case CXTypeKind.CXType_Record:
				case CXTypeKind.CXType_Enum:
					return Tuple.Create("public " + clang.getTypeSpelling(canonical).ToString() + " " + cursorSpelling + ";", "");
				default:
					return Tuple.Create("public " + canonical.ToPlainTypeString() + " " + cursorSpelling + ";", "");
			}
		}
	}
}
