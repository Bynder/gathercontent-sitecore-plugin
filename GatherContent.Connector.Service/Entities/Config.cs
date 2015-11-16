using System.Collections.Generic;
using Newtonsoft.Json;

namespace GatherContent.Connector.Service.Entities
{
    public class Config
    {
        [JsonProperty(PropertyName = "label")]
        public string Label { get; set; }

        [JsonProperty(PropertyName = "hidden")]
        public bool Hidden { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "elements")]
        public List<Element> Elements { get; set; }

    }

    public class Element
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "required")]
        public bool Required { get; set; }

        [JsonProperty(PropertyName = "label")]
        public string Label { get; set; }

        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }

        [JsonProperty(PropertyName = "microcopy")]
        public string Microcopy { get; set; }

        [JsonProperty(PropertyName = "limit_type")]
        public string LimitType { get; set; }

        [JsonProperty(PropertyName = "limit")]
        public int Limit { get; set; }

        [JsonProperty(PropertyName = "plain_text")]
        public bool PlainText { get; set; }
    }
}