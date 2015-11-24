using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class ImportResultModel
    {
        public List<ItemModel> Items { get; set; }

        public ImportResultModel(List<ItemModel> items)
        {
            Items = items;
        }
    }
}
