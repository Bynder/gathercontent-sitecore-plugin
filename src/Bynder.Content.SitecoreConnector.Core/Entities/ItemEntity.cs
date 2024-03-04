namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using Newtonsoft.Json;

    public class ItemEntity
    {
        [JsonProperty(PropertyName = "data")]
        public CWBItem Data { get; set; }
    }

}
