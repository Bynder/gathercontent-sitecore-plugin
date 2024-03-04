namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

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
}
