namespace LibVlc.Parser.Model
{
	public struct Parameter
	{
		public string Name { get; set; }
		public string Type { get; set; }

		public override string ToString()
		{
			return string.Format("Parameter(Name:'{0}',Type:'{1}');", Name, Type);
		}
	}
}
