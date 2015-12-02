using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class TableDataModel
    {
        public List<UpdateListItem> Items { get; set; }

        public TableDataModel() { }

        public TableDataModel(List<UpdateListItem> items)
        {
            Items = items;
        }
    }
}
