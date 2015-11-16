using Newtonsoft.Json;

namespace GatherContent.Connector.Service.Entities
{
    public class ItemEntity
    {
        [JsonProperty(PropertyName = "data")]
        public Item Data { get; set; }
    }

}