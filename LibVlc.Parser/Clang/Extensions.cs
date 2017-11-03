using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ClangSharp;
using LibVlc.Parser.Model;

namespace LibVlc.Parser.Clang
{
	public static class Extensions
	{
		public static T[] MarshalUnmananagedArray2Struct<T>(IntPtr unmanagedArray, int length)
		{
			var size = Marshal.SizeOf(typeof(T));
			var mangagedArray = new T[length];

			for (int i = 0; i < length; i++)
			{
				IntPtr ins = new IntPtr(unmanagedArray.ToInt64() + i * size);
				mangagedArray[i] = Marshal.PtrToStructure<T>(ins);
			}

			return mangagedArray;
		}

		public static string ReturnTypeHelper(this CXType resultType)
		{
			switch (resultType.kind)
			{
				case CXTypeKind.CXType_Pointer:
				{
					// const char* gets special treatment	
					if (resultType.IsPtrToConstChar())
					{
						return "string";
					}
					var pointee = clang.getPointeeType(resultType);

					switch (pointee.kind)
					{
						case CXTypeKind.CXType_Typedef:
						{
							return clang.getTypeSpelling(pointee).ToString();
						}
						default:
						{
							return "IntPtr"; 
						}
					}
				}
				default:
				{
					return CommonTypeHandling(resultType);
				}
			}
		}

		public static Parameter ArgumentHelper(CXType functionType, CXCursor paramCursor, uint index)
		{
			var type = clang.getArgType(functionType, index);
			var cursorType = clang.getCursorType(paramCursor);

			var spelling = clang.getCursorSpelling(paramCursor).ToString();
			if (string.IsNullOrEmpty(spelling))
			{
				spelling = "param" + index;
			}

			string arg;
			bool isUnicodeString = false;

			switch (type.kind)
			{
				case CXTypeKind.CXType_Pointer:
				var pointee = clang.getPointeeType(type);
				switch (pointee.kind)
				{
					case CXTypeKind.CXType_Pointer:
					{
						arg = pointee.IsPtrToConstChar() && clang.isConstQualifiedType(pointee) != 0 ? "string[]" : "out IntPtr";
						break;
					}
					case CXTypeKind.CXType_FunctionProto:
					{
						arg = clang.getTypeSpelling(cursorType).ToString();
						break;
					}
					case CXTypeKind.CXType_Void:
					{
						arg = "IntPtr";
						break;
					}
					case CXTypeKind.CXType_Char_S:
					{
						arg = type.IsPtrToConstChar() ? "string" : "IntPtr"; // if it's not a const, it's best to go with IntPtr
						isUnicodeString = false;
						break;
					}
					case CXTypeKind.CXType_WChar:
					{
						arg = type.IsPtrToConstChar() ? "string" : "IntPtr";
						isUnicodeString = true;
						break;
					}
					default:
					{
						arg = CommonTypeHandling(pointee, "out ");
						break;
					}
				}
				break;
				default:
				{
					arg = CommonTypeHandling(type);
					break;
				}
			}

			return new Parameter { Name = spelling, Type = arg, IsUnicodeString = isUnicodeString };
		}

		private static string CommonTypeHandling(CXType type, string outParam = "")
		{
			bool isConstQualifiedType = clang.isConstQualifiedType(type) != 0;
			string spelling;
			switch (type.kind)
			{
				case CXTypeKind.CXType_Typedef:
				{
					var cursor = clang.getTypeDeclaration(type);
					if (clang.Location_isInSystemHeader(clang.getCursorLocation(cursor)) != 0)
					{
						spelling = clang.getCanonicalType(type).ToPlainTypeString();
					}
					else
					{
						spelling = clang.getCursorSpelling(cursor).ToString();
					}
					break;
				}
				case CXTypeKind.CXType_Record:
				case CXTypeKind.CXType_Enum:
				{
					spelling = clang.getTypeSpelling(type).ToString();
					break;
				}
				case CXTypeKind.CXType_IncompleteArray:
				{
					//CommonTypeHandling(clang.getArrayElementType(type), tw);					
					spelling = "[]";
					throw new NotImplementedException();
					break;
				}
				//case CXTypeKind.CXType_Elaborated:
				case CXTypeKind.CXType_Unexposed: // Often these are enums and canonical type gets you the enum spelling
				{
					var canonical = clang.getCanonicalType(type);
					// unexposed decl which turns into a function proto seems to be an un-typedef'd fn pointer
					if (canonical.kind == CXTypeKind.CXType_FunctionProto)
					{
						spelling = "IntPtr";
					}
					else
					{
						spelling = clang.getTypeSpelling(canonical).ToString();
					}
					break;
				}
				default:
				{
					spelling = clang.getCanonicalType(type).ToPlainTypeString();
					break;
				}
			}

			if (isConstQualifiedType)
			{
				spelling = spelling.Replace("const ", string.Empty); // ugh
			}

			return outParam + spelling;
		}
	}
}
