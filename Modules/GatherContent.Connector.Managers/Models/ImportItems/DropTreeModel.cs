using Newtonsoft.Json;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class DropTreeModel
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        //public bool isFolder { get; set; }

        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; }

        //public List<DropTreeModel> children { get; set; }

        [JsonProperty(PropertyName = "isLazy")]
        public bool IsLazy { get; set; }
        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; set; }
    }
}
