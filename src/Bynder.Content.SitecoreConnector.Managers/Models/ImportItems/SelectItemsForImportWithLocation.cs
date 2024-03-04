using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems
{
    using Core.Entities;

    public class SelectItemsForImportWithLocation
    {
        public FiltersModel Filters { get; set; }

        public ItemWithLocationDataModel Data { get; set; }

        public SelectItemsForImportWithLocation(List<ImportItembyLocation> items, Project project, List<Project> projects, List<CWBStatus> statuses, List<CWBTemplate> templates)
        {
            Filters = new FiltersModel(project, projects, templates, statuses);
            Data = new ItemWithLocationDataModel(items);
        }
    }
}
