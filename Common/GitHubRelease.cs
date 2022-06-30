using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Common
{
    [JsonObject]
    [PublicAPI]
    public class GitHubRelease
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("target_commitish")]
        public string TargetCommitsh { get; set; }

        [JsonProperty("prerelease")]
        public bool Prerelease { get; set; }

        [JsonProperty("created_at")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("published_at")]
        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTimeOffset PublishedAt { get; set; }

        [JsonProperty("assets")]
        public IReadOnlyList<GitHubAsset> Assets { get; set; }

        public override string ToString()
        {
            return $"{nameof(Id)}: {Id}, " +
                   $"{nameof(Name)}: '{Name}', " +
                   $"{nameof(TagName)}: '{TagName}', " +
                   $"{nameof(TargetCommitsh)}: '{TargetCommitsh}', " +
                   $"{nameof(Prerelease)}: {Prerelease}, " +
                   $"{nameof(CreatedAt)}: '{CreatedAt:O}', " +
                   $"{nameof(PublishedAt)}: '{PublishedAt:O}', " +
                   $"{nameof(Assets)}: [{Assets}]";
        }
    }
}
