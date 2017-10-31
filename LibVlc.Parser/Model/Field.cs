using System.Diagnostics;

namespace LibVlc.Parser.Model
{
	[DebuggerDisplay("Field(Name:{Name});")]
	public class Field
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public string AccessModifier { get; set; }
		public string Attribute { get; set; }
	}
}
