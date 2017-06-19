using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Common
{
	[JsonConverter(typeof(DepConverter))]
	public class Dep : IEquatable<Dep>
	{
        public string Name { get; }
        public string Treeish { get; set; }
        public string Configuration { get;  set; }
		public bool NeedSrc { get; set; }

        public Dep(string name, string treeish = null, string configuration = null)
        {
            Name = name;
            Treeish = treeish;
            Configuration = configuration;
        }

        public Dep(string fromYamlString)
        {
	        var tokens = new List<String>();
	        var currentToken = "";
	        fromYamlString += "@";
			for (int pos = 0; pos < fromYamlString.Length; pos++)
	        {
		        if ((fromYamlString[pos] == '/' || fromYamlString[pos] == '@') &&
					(pos == 0 || fromYamlString[pos - 1] != '\\'))
			    {
				    tokens.Add(currentToken);
				    currentToken = fromYamlString[pos].ToString();
			    }
			    else
				    currentToken += fromYamlString[pos];
	        }

			Name = tokens[0];
	        foreach (var token in tokens.Select(UnEscapeBadChars))
	        {
		        if (token.StartsWith("@"))
			        Treeish = token.Substring(1);
		        if (token.StartsWith("/"))
			        Configuration = token.Substring(1);
	        }

            if (Treeish == "")
                Treeish = null;
            if (Configuration == "")
                Configuration = null;
        }

		public void UpdateConfigurationIfNull()
		{
			UpdateConfigurationIfNull(Helper.CurrentWorkspace);
		}

		public void UpdateConfigurationIfNull(string workspace)
		{
			Configuration = Configuration ??
				new ConfigurationParser(new FileInfo(Path.Combine(workspace, Name)))
					.GetDefaultConfigurationName();
		}

		private string UnEscapeBadChars(string str)
		{
			return str.Replace("\\@", "@").Replace("\\/", "/");
		}

		public bool Equals(Dep dep)
		{
			if (dep == null)
				return false;
			return Name == dep.Name && Treeish == dep.Treeish && Configuration == dep.Configuration;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}

		public override string ToString()
		{
		    return Name +
		           (string.IsNullOrEmpty(Configuration) ? "" : Helper.ConfigurationDelimiter + Configuration) +
                   (string.IsNullOrEmpty(Treeish) ? "" : "@" + Treeish);
		}

        public string ToYamlString()
        {
            return Name +
                   (string.IsNullOrEmpty(Configuration) ? "" : Helper.ConfigurationDelimiter + Configuration) +
                   (string.IsNullOrEmpty(Treeish) ? "" : "@" + Treeish.Replace("@", "\\@").Replace("/", "\\/"));
        }

        public string ToBuildString()
		{
			return Name +
			       (Configuration == null || Configuration.Equals("full-build") ? "" : "/" + Configuration);
		}
	}

	public class DepConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Dep);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var str = reader.Value.ToString();
			return new Dep(str);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var item = (Dep)value;
			writer.WriteValue(ToJsonValueString(item));
			writer.Flush();
		}

		public string ToJsonValueString(Dep dep)
		{
			var str = dep.Name;
			if (dep.Configuration != null)
				str += "/" + EscapeBadChars(dep.Configuration);
			if (dep.Treeish != null)
				str += "@" + EscapeBadChars(dep.Treeish);
			return str;
		}

		private string EscapeBadChars(string str)
		{
			return str.Replace("@", "\\@").Replace("/", "\\/");
		}
	}

	public class DepWithCommitHash
	{
		public readonly Dep Dep;
		public readonly string CommitHash;

		public DepWithCommitHash(Dep dep, string commitHash)
		{
			Dep = dep;
			CommitHash = commitHash;
		}
	}
}
