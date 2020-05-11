using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BingPic
{
    public partial class BingResponse
    {
        [JsonProperty("images")]
        public Image[] Images { get; set; }

        [JsonProperty("tooltips")]
        public Tooltips Tooltips { get; set; }
    }

    public partial class Image
    {
        [JsonProperty("startdate")]
        public string Startdate { get; set; }

        [JsonProperty("fullstartdate")]
        public string Fullstartdate { get; set; }

        [JsonProperty("enddate")]
        public string Enddate { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("urlbase")]
        public string Urlbase { get; set; }

        [JsonProperty("copyright")]
        public string Copyright { get; set; }

        [JsonProperty("copyrightlink")]
        public Uri Copyrightlink { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("quiz")]
        public string Quiz { get; set; }

        [JsonProperty("wp")]
        public bool Wp { get; set; }

        [JsonProperty("hsh")]
        public string Hsh { get; set; }

        [JsonProperty("drk")]
        public long Drk { get; set; }

        [JsonProperty("top")]
        public long Top { get; set; }

        [JsonProperty("bot")]
        public long Bot { get; set; }

        [JsonProperty("hs")]
        public dynamic[] Hs { get; set; }
    }

    public partial class Tooltips
    {
        [JsonProperty("loading")]
        public string Loading { get; set; }

        [JsonProperty("previous")]
        public string Previous { get; set; }

        [JsonProperty("next")]
        public string Next { get; set; }

        [JsonProperty("walle")]
        public string Walle { get; set; }

        [JsonProperty("walls")]
        public string Walls { get; set; }
    }

    public partial class BingResponse
    {
        public static BingResponse FromJson(string json) => JsonConvert.DeserializeObject<BingResponse>(json, BingPic.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this BingResponse self) => JsonConvert.SerializeObject(self, BingPic.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
