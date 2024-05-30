namespace Bynder.Content.SitecoreConnector.Core.Entities
{
    using Newtonsoft.Json;

    public class TemplateEntity
    {
        [JsonProperty(PropertyName = "data")]
        public CWBTemplate Data { get; set; }
    }

}
