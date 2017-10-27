using System;
using System.Collections.Generic;

namespace LibVlc.Parser.Model
{
	public class Struct
	{
		public string Name { get; set; }
		public string ManagedName { get; set;  }
		public string FileName { get; set; }
		public List<Tuple<string, string>> Params { get; set; }

		public Struct()
		{
			Params = new List<Tuple<string, string>>();
		}
		
	}
}
