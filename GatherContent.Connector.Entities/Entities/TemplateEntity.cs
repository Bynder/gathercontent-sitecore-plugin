using Newtonsoft.Json;

namespace GatherContent.Connector.Entities.Entities
{
    public class TemplateEntity
    {
        [JsonProperty(PropertyName = "data")]
        public Template Data { get; set; }
    }

}