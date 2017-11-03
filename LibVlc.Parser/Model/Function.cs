using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LibVlc.Parser.Model
{
	[DebuggerDisplay("Function(Name:{Name});")]
	public class Function
	{
		public string FileName { get; set; }
		public string Name { get; set; }
		public string UnmanagedFunction { get; set; }
		public string ReturnType { get; set; }
		public List<Parameter> Parameters { get; set; }
		public string Comment { get; set; }
		public string CallingConvention { get; set; }
		public bool IsDelegate { get; set; }

		public Function()
		{
			Parameters = new List<Parameter>();
		}

		public override int GetHashCode()
		{
			var p = (Parameters.Count > 0 ? Parameters.Select(i => i.Name + " " + i.Type).Aggregate((i, j) => i + ", " + j) : "");
			return new { Name, ReturnType, p }.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;

			var p = (Function)obj;
			return Name == p.Name && ReturnType == p.ReturnType && Parameters.SequenceEqual(p.Parameters);
		}

		public override string ToString()
		{
			var paramsString = Parameters.Count > 0 ? Parameters.Select(i => i.ToString()).Aggregate((i, j) => i + j) : "";
			return string.Format("\r\nFunction(Name:'{0}',ReturnType:'{1}', Parameters:'{2}');", Name, ReturnType, paramsString);
		}
	}
}
