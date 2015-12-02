using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class SelectItemsForUpdateModel
    {
        public FiltersModel Filters { get; set; }

        public TableDataModel Data { get; set; }

        public SelectItemsForUpdateModel(List<UpdateListItem> items, List<GCStatus> statuses, List<GCTemplate> templates)
        {
            Filters = new FiltersModel(templates, statuses);
            Data = new TableDataModel(items);
        }
    }
}
