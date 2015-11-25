using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class ImportResultModel
    {
        public List<ImportResultItemModel> Items { get; set; }

        public ImportResultModel(List<ImportResultItemModel> items)
        {
            Items = items;
        }
    }
}
