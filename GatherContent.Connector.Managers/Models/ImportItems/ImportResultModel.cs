using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.Import;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class ImportResultModel
    {
        public List<ImportItemResponseModel> Items { get; set; }

        public ImportResultModel(List<ImportItemResponseModel> items)
        {
            Items = items;
        }
    }
}
