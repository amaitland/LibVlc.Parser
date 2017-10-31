using System.Diagnostics;

namespace LibVlc.Parser.Model
{
	[DebuggerDisplay("Parameter(Name:{Name},Type:{Type});")]
	public struct Parameter
	{
		public string Name { get; set; }
		public string Type { get; set; }
	}
}
