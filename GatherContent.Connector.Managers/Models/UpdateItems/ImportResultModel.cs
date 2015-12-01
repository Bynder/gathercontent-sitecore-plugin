using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.IRepositories.Models.Import;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class ImportResultModel
    {
        public List<ImportItemsResponseModel> Items { get; set; }

        public ImportResultModel(List<ImportItemsResponseModel> items)
        {
            Items = items;
        }
    }
}
