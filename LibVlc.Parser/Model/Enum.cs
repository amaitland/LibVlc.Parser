using System.Collections.Generic;
using System.Diagnostics;

namespace LibVlc.Parser.Model
{
	[DebuggerDisplay("Enum(Name:{Name}, Type:{Type});")]
	public class Enum
	{
		public string Name { get; set; }
		public string Type { get; set;  }
		public List<KeyValuePair<string, long>> Values { get; set; }

		public Enum()
		{
			Values = new List<KeyValuePair<string, long>>();
		}		
	}
}
