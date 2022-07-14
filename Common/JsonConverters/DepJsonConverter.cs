#nullable enable
using System;
using System.Text;
using Newtonsoft.Json;

namespace Common.JsonConverters
{
    internal sealed class DepJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dep);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var str = reader.Value?.ToString();
            return new Dep(str);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is not Dep item)
                return;

            writer.WriteValue(ToJsonValueString(item));
            writer.Flush();
        }

        private static string ToJsonValueString(Dep source)
        {
            var result = new StringBuilder(source.Name);

            if (source.Configuration != null)
                result.Append('/').Append(EscapeBadChars(source.Configuration));

            if (source.Treeish != null)
                result.Append('@').Append(EscapeBadChars(source.Treeish));

            return result.ToString();
        }

        private static string EscapeBadChars(string str)
        {
            return str.Replace("@", "\\@").Replace("/", "\\/");
        }
    }
}
