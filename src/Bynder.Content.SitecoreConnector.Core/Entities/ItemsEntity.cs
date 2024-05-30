namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class ItemsEntity
    {

        [JsonProperty(PropertyName = "data")]
        public List<CWBItem> Data { get; set; }
    }
}
