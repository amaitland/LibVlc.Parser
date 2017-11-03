using System;
using System.IO;
using System.Text;
using ClangSharp;
using LibVlc.Parser.Model;
using Microsoft.CodeAnalysis;

namespace LibVlc.Parser
{
	public static class Extensions
	{
		public static void WriteToFilePath(this SyntaxNode node, string filePath)
		{
			var file = new FileInfo(filePath);
			file.Directory.Create(); // Create folder if doesn't exist

			//Delete old file as was having problems with the output being appended
			if (file.Exists)
			{
				file.Delete();
			}

			using (var stream = File.OpenWrite(file.FullName))
			using (var textWriter = new StreamWriter(stream))
			{
				node.WriteTo(textWriter);
			}
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

		public static string ToPlainTypeString(this CXType type)
		{
			var canonical = clang.getCanonicalType(type);
			switch (type.kind)
			{
				case CXTypeKind.CXType_Bool:
					return "Boolean";
				case CXTypeKind.CXType_UChar:
				case CXTypeKind.CXType_Char_U:
					return "Char";
				case CXTypeKind.CXType_SChar:
				case CXTypeKind.CXType_Char_S:
					return "SByte";
				case CXTypeKind.CXType_UShort:
					return "UInt16";
				case CXTypeKind.CXType_Short:
					return "Int16";
				case CXTypeKind.CXType_Float:
					return "Single";
				case CXTypeKind.CXType_Double:
					return "Double";
				case CXTypeKind.CXType_Int:
					return "Int32";
				case CXTypeKind.CXType_UInt:
					return "UInt32";
				case CXTypeKind.CXType_Pointer:
				case CXTypeKind.CXType_NullPtr: // ugh, what else can I do?
					return "IntPtr";
				case CXTypeKind.CXType_Long:
					return "Int32";
				case CXTypeKind.CXType_ULong:
					return "Int32";
				case CXTypeKind.CXType_LongLong:
					return "Int64";
				case CXTypeKind.CXType_ULongLong:
					return "UInt64";
				case CXTypeKind.CXType_Void:
					return "void";
				case CXTypeKind.CXType_Unexposed:
					if (canonical.kind == CXTypeKind.CXType_Unexposed)
					{
						return clang.getTypeSpelling(canonical).ToString();
					}
					return canonical.ToPlainTypeString();
				default:
					return "IntPtr";
			}
		}

		public static Field ToField(this CXCursor cursor, string cursorSpelling)
		{
			var field = new Field
			{
				AccessModifier = "public",
				Name = cursorSpelling
			};

			var canonical = clang.getCanonicalType(clang.getCursorType(cursor));
			
			switch (canonical.kind)
			{
				case CXTypeKind.CXType_ConstantArray:
				{
					//long arraySize = clang.getArraySize(canonical);
					//var elementType = clang.getCanonicalType(clang.getArrayElementType(canonical));

					//var sb = new StringBuilder();
					//for (int i = 0; i < arraySize; ++i)
					//{
					//	sb.Append("public " + elementType.ToPlainTypeString() + " " + cursorSpelling + i + "; ");
					//}

					//return Tuple.Create(sb.ToString(), "");
					throw new NotImplementedException();
				}
				case CXTypeKind.CXType_Pointer:
				{
					var pointeeType = clang.getCanonicalType(clang.getPointeeType(canonical));
					switch (pointeeType.kind)
					{
						case CXTypeKind.CXType_Char_S:
						{
							field.Type = "string";
							field.Attribute = "[MarshalAs(UnmanagedType.LPStr)]";
							break;
						}
						case CXTypeKind.CXType_WChar:
						{
							field.Type = "string";
							field.Attribute = "[MarshalAs(UnmanagedType.LPWStr)]";
							break;
						}
						default:
						{
							field.Type = "IntPtr";
							break;
						}
					}
					break;
				}
				case CXTypeKind.CXType_Record:
				case CXTypeKind.CXType_Enum:
				{
					field.Type = clang.getTypeSpelling(canonical).ToString();
					break;
				}
				default:
				{
					field.Type = canonical.ToPlainTypeString();
					break;
				}
			}

			return field;
		}
	}
}
