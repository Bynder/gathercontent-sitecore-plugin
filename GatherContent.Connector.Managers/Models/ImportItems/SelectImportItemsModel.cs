using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class SelectImportItemsModel
    {
        public FiltersModel Filters { get; set; }

        public TableDataModel Data { get; set; }

        public SelectImportItemsModel(List<ItemModel> items, Project project, List<Project> projects, List<Status> statuses, List<Template> templates)
        {
            Filters = new FiltersModel(project, projects, templates, statuses);
            Data = new TableDataModel(items);
        }
    }
}
