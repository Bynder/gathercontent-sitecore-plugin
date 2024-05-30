namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using Newtonsoft.Json;

    public class Usage
    {
        [JsonProperty(PropertyName = "item_count")]
        public int Count { get; set; }
    }
}
