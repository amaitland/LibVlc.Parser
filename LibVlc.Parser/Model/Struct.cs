using System;
using System.Collections.Generic;

namespace LibVlc.Parser.Model
{
	public class Struct
	{
		public string Name { get; set; }
		public List<Tuple<string, string>> Fields { get; set; }

		public Struct()
		{
			Fields = new List<Tuple<string, string>>();
		}
		
	}
}
