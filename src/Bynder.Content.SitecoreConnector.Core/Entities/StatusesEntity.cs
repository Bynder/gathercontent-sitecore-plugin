namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class StatusesEntity
    {
        [JsonProperty(PropertyName = "data")]
        public List<CWBStatus> Data { get; set; }
    }
}
