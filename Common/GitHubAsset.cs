using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common
{
    [JsonObject]
    [PublicAPI]
    public class GitHubAsset
    {
        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("content_type")]
        public string ContentType { get; set; }

        [JsonProperty("created_at")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTimeOffset UpdatedAt { get; set; }

        public override string ToString()
        {
            return $"{nameof(BrowserDownloadUrl)}: '{BrowserDownloadUrl}', " +
                   $"{nameof(Name)}: '{Name}', " +
                   $"{nameof(Label)}: '{Label}', " +
                   $"{nameof(ContentType)}: '{ContentType}', " +
                   $"{nameof(CreatedAt)}: '{CreatedAt:O}', " +
                   $"{nameof(UpdatedAt)}: '{UpdatedAt:O}'";
        }
    }
}
