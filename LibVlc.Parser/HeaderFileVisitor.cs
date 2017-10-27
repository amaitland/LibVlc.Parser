using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ClangSharp;

namespace LibVlc.Parser
{
	public class HeaderFileVisitor
	{
		private readonly Dictionary<string, Function> _visited = new Dictionary<string, Function>();
		private readonly Dictionary<string, Enum> _enums = new Dictionary<string, Enum>();
		private readonly Dictionary<string, Struct> _structs = new Dictionary<string, Struct>();
		private IVlcParser _parser;
		private string _headerFolder;

		public IDictionary<string, Function> Functions
		{
			get { return _visited; }
		}

		public IDictionary<string, Enum> Enums
		{
			get { return _enums; }
		}

		public IDictionary<string, Struct> Structs
		{
			get { return _structs; }
		}

		public HeaderFileVisitor(string headerFolder, IVlcParser parser)
		{
			_headerFolder = headerFolder;	
			_parser = parser;
		}

		public CXChildVisitResult Visit(CXCursor cursor, CXCursor parent)
		{
			if(cursor.IsInSystemHeader())
			{
				return CXChildVisitResult.CXChildVisit_Continue;
			}

			Debug.WriteLine("Type = {0}, Value = {1}", cursor.kind, cursor.ToString());
			var location = clang.getCursorLocation(cursor);

			clang.getFileLocation(location, out CXFile file, out uint line, out uint column, out uint offset);

			var fileName = clang.getFileName(file).ToString();
			var directory = Path.GetDirectoryName(fileName);

			//If the file isn't contained within our search path then we'll just keep going
			if (!string.Equals(_headerFolder, directory, StringComparison.OrdinalIgnoreCase))
			{
				return CXChildVisitResult.CXChildVisit_Continue;
			}

			var kind = cursor.kind;

			//We're interested in all the functions/enums/structs/etc
			if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
			{
				var typeDefName = cursor.ToString();

			}
			else if (cursor.kind == CXCursorKind.CXCursor_FunctionDecl)
			{
				var functionName = cursor.ToString();
			
				//Ignore duplicate methods - they're likely the system ones anyway
				if (_visited.ContainsKey(functionName))
				{
					//TODO: Add logging
					return CXChildVisitResult.CXChildVisit_Continue;
				}
				
				var type = clang.getCursorResultType(cursor);
				var resultType = type.ToString();
				//TODO: NOt working yet
				var deprecated = clang.getCursorAvailability(cursor) == CXAvailabilityKind.CXAvailability_Deprecated;
				var currentFunction = new Function(fileName, functionName, resultType, deprecated);				

				_visited.Add(functionName, currentFunction);

				clang.visitChildren(cursor, (c, p, d) => VisitParams(c, p, currentFunction), new CXClientData());
			}
			else if(cursor.kind == CXCursorKind.CXCursor_EnumDecl)
			{
				var enumName = cursor.ToString();
				var enumNameManaged = _parser.ParseEnumName(enumName);
				var enumType = clang.getEnumDeclIntegerType(cursor);

				var e1 = new Enum { Name = enumName, Type = enumType.ToString(), ManagedName = enumNameManaged };
				_enums.Add(enumName, e1);

				clang.visitChildren(cursor, (c, p, d) => VisitEnumValues(c, p, e1), new CXClientData());
			}
			else if (cursor.kind == CXCursorKind.CXCursor_StructDecl)
			{
				var structName = cursor.ToString();
				var structNameManaged = _parser.ParseStructName(structName);
				if (!_structs.ContainsKey(structName))
				{
					var e1 = new Struct { Name = structName, FileName = fileName, ManagedName = structNameManaged };
					_structs.Add(structName, e1);

					clang.visitChildren(cursor, (c, p, d) => VisitStruct(c, p, e1), new CXClientData());
				}
			}
			else
			{
				var t = cursor.kind;
			}

			return CXChildVisitResult.CXChildVisit_Continue;
		}

		private CXChildVisitResult VisitStruct(CXCursor cursor, CXCursor parent, Struct e1)
		{
			if (cursor.kind == CXCursorKind.CXCursor_FieldDecl)
			{
				var name = cursor.ToString();
				var managedName = _parser.ParseStructValue(name);
				var type = clang.getCursorType(cursor).ToString();

				e1.Params.Add(Tuple.Create(managedName, type));
			}

			return CXChildVisitResult.CXChildVisit_Continue;
		}

		private CXChildVisitResult VisitEnumValues(CXCursor cursor, CXCursor parent, Enum e1)
		{
			if (cursor.kind == CXCursorKind.CXCursor_EnumConstantDecl)
			{
				if(string.IsNullOrEmpty(e1.Name))
				{
					//If the enum is unmaned then we'll map it here based on it's children
					e1.Name = _parser.MapUnnamedEnum(cursor.ToString());
					e1.ManagedName = _parser.ParseEnumName(e1.Name);
				}
					//Get the enums int value
				var enumStringValue = _parser.ParseEnumValue(e1.Name, cursor.ToString());
				var enumIntValue = int.Parse(clang.getEnumConstantDeclValue(cursor).ToString());
				
			
				e1.Values.Add(Tuple.Create(enumStringValue, enumIntValue));
			}
			
			return CXChildVisitResult.CXChildVisit_Continue;
		}

		private CXChildVisitResult VisitParams(CXCursor cursor, CXCursor parent, Function method)
		{
			if (cursor.kind == CXCursorKind.CXCursor_ParmDecl)
			{
				var type = clang.getCursorType(cursor);
				method.Parameters.Add(new Parameter { Name = cursor.ToString(), Type = type.ToString() });
			}

			return CXChildVisitResult.CXChildVisit_Continue;
		}
	}
}
