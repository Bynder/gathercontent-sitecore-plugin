namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using Newtonsoft.Json;

    public class StatusEntity
    {
        [JsonProperty(PropertyName = "data")]
        public CWBStatus Data { get; set; }
    }
}
