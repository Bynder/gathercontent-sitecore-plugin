using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class TableDataModel
    {
        public List<ImportListItem> Items { get; set; }

        public TableDataModel() { }

        public TableDataModel(List<ImportListItem> items)
        {
            Items = items;
        }
    }
}
