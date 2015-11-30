using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class TableDataModel
    {
        public List<ItemModel> Items { get; set; }

        public TableDataModel() { }

        public TableDataModel(List<ItemModel> items)
        {
            Items = items;
        }
    }
}
