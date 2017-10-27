using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LibVlc.Parser
{
	public class DefaultVlcParser : IVlcParser
	{
		private const string DefaultNamingPrefix = "libvlc_";
		private const string DefaultStructValuePrefix = "psz_";
		private static TextInfo DefaultTextInfo = new CultureInfo("en-US").TextInfo;

		string IVlcParser.ParseEnumName(string name)
		{
			var managedName = name;

			//Remove the trailing _t from struct names
			if (managedName.EndsWith("_t") || managedName.EndsWith("_e"))
			{
				managedName = managedName.Substring(0, managedName.Length - 2);
			}

			managedName = SimpleNameConversion(managedName, DefaultNamingPrefix);
			
			return managedName;
		}

		string IVlcParser.ParseEnumValue(string enumName, string value)
		{
			if (enumName.EndsWith("_t") || enumName.EndsWith("_e"))
			{
				enumName = enumName.Substring(0, enumName.Length - 2);
			}

			var managedValue = SimpleNameConversion(value, enumName, DefaultNamingPrefix);
			
			return managedValue;
		}

		string IVlcParser.ParseStructName(string name)
		{
			var managedName = name;

			//Remove the trailing _t from struct names
			if(managedName.EndsWith("_t"))
			{
				managedName = managedName.Substring(0, managedName.Length - 2);
			}

			managedName = SimpleNameConversion(managedName, DefaultNamingPrefix);

			return managedName;
		}

		string IVlcParser.ParseStructValue(string value)
		{
			//Remove type prefix that's used for some structs
			var managedValue = value.Replace("i_", "")
									.Replace("f_", "")
									.Replace("p_", "");
			managedValue = SimpleNameConversion(managedValue, DefaultStructValuePrefix);

			return managedValue;
		}

		string IVlcParser.MapUnnamedEnum(string firstMemeberName)
		{
			if(firstMemeberName == "libvlc_media_option_trusted")
			{
				return "libvlc_media_option";
			}
			return "";
		}

		private static string SimpleNameConversion(string input, params string[] prefixes)
		{
			string output = input;
			foreach (var prefix in prefixes)
			{
				output = Regex.Replace(output, prefix, "", RegexOptions.IgnoreCase);
			}
			output = DefaultTextInfo.ToTitleCase(output.ToLower());
			output = output.Replace("_", "");

			return output;
		}		
	}
}
