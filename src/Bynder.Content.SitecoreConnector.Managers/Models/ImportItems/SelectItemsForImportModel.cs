using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems
{
    using Core.Entities;

    public class SelectItemsForImportModel
    {
        public FiltersModel Filters { get; set; }

        public TableDataModel Data { get; set; }

        public SelectItemsForImportModel(List<ImportListItem> items, Project project, List<Project> projects, List<CWBStatus> statuses, List<CWBTemplate> templates)
        {
            Filters = new FiltersModel(project, projects, templates, statuses);
            Data = new TableDataModel(items);
        }
    }
}
