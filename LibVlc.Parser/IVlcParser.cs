namespace LibVlc.Parser
{
	public interface IVlcParser
	{
		string ParseEnumName(string name);
		string ParseEnumValue(string enumName, string value);
		string ParseStructName(string name);
		string ParseStructValue(string value);
		string MapUnnamedEnum(string firstMemeberName);
	}
}
