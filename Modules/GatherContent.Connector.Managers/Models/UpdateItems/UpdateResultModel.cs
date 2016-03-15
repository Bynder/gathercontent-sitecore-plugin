using System.Collections.Generic;
using GatherContent.Connector.Managers.Models.ImportItems;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class UpdateResultModel
    {
        public List<MappingResultModel> Items { get; set; }

        public UpdateResultModel(List<MappingResultModel> items)
        {
            Items = items;
        }
    }
}
