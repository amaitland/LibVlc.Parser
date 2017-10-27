using System;
using System.Collections.Generic;

namespace Vlc.Parser.ManagedTypes
{
	public class ManagedEnum
	{
		public string Name { get; set; }
		public IEnumerable<Tuple<string, int>> Values { get; set; }
	}
}
