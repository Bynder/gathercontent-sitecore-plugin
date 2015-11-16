using Newtonsoft.Json;

namespace GatherContent.Connector.Service.Entities
{
    public class TemplateEntity
    {
        [JsonProperty(PropertyName = "data")]
        public Template Data { get; set; }
    }

}