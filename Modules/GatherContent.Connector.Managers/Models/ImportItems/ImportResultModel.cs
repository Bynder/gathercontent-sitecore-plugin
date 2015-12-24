using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.Import;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class ImportResultModel
    {
        public List<MappingResultModel> Items { get; set; }

        public ImportResultModel(List<MappingResultModel> items)
        {
            Items = items;
        }
    }
}
