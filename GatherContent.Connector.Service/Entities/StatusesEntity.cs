using System.Collections.Generic;
using Newtonsoft.Json;

namespace GatherContent.Connector.Service.Entities
{
    public class StatusesEntity
    {
        [JsonProperty(PropertyName = "data")]
        public List<Status> Data { get; set; }
    }
}