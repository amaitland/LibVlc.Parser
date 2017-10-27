using System;
using System.Collections.Generic;

namespace LibVlc.Parser
{
	public class Enum
	{
		public string Name { get; set; }
		public string ManagedName { get; set; }
		public string Type { get; set;  }
		public List<Tuple<string, int>> Values { get; set; }

		public Enum()
		{
			Values = new List<Tuple<string, int>>();
		}		
	}
}
