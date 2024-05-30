namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class TemplatesEntity
    {
        [JsonProperty(PropertyName = "data")]
        public List<CWBTemplate> Data { get; set; }
    }
}
