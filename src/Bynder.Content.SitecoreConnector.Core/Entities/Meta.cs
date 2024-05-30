namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using Newtonsoft.Json;

    public class Meta
    {
        [JsonProperty(PropertyName = "templates")]
        public int Templates { get; set; }
    }
}
