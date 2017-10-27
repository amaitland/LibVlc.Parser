using System.Collections.Generic;
using System.Linq;

namespace LibVlc.Parser
{
	public class Function
	{
		public string FileName { get; set; }
		public string Name { get; set; }
		public string ManagedName { get; set;  }
		public string ReturnType { get; set; }
		public List<Parameter> Parameters { get; set; }
		public bool IsDeprecated { get; set; }

		public Function(string fileName, string name, string returnType, bool isDeprecated)
		{
			FileName = fileName;
			Name = name;
			ReturnType = returnType;
			Parameters = new List<Parameter>();
			IsDeprecated = isDeprecated;
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
			return string.Format("\r\nMethod(Name:'{0}',ReturnType:'{1}', Parameters:'{2}');", Name, ReturnType, paramsString);
		}
	}
}
