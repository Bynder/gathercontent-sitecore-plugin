namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class ItemFiles
    {
        [JsonProperty(PropertyName = "data")]
        public List<FileEntity> Data { get; set; }
    }
}
