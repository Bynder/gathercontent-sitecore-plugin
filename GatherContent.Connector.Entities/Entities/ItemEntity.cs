using Newtonsoft.Json;

namespace GatherContent.Connector.Entities.Entities
{
    public class ItemEntity
    {
        [JsonProperty(PropertyName = "data")]
        public Item Data { get; set; }
    }

}