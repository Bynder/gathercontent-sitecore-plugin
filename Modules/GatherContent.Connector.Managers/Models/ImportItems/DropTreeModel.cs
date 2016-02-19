using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class DropTreeModel
    {
        public DropTreeModel()
        {
            children = new List<DropTreeModel>();
        }

        //[JsonProperty(PropertyName = "id")]
        public string title { get; set; }
        public bool isFolder { get; set; }
        public string key { get; set; }
        public List<DropTreeModel> children { get; set; }
        public bool isLazy { get; set; }
        public string icon { get; set; }
    }
}
