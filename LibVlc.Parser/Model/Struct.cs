using System.Collections.Generic;
using System.Diagnostics;

namespace LibVlc.Parser.Model
{
	[DebuggerDisplay("Struct(Name:{Name});")]
	public class Struct
	{
		public string Name { get; set; }
		public List<Field> Fields { get; set; }

		public Struct()
		{
			Fields = new List<Field>();
		}		
	}
}
