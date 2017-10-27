﻿using System.Collections.Generic;

namespace LibVlc.Parser.Model
{
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
