using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class FiltersModel
    {
        public List<Project> Projects { get; set; }

        public List<GCTemplate> Templates { get; set; }

        public List<GCStatus> Statuses { get; set; }

        public FiltersModel() { }

        public FiltersModel(List<GCTemplate> templates, List<GCStatus> statuses, List<Project> projects)
        {
            Projects = projects;
            Templates = templates;
            Statuses = statuses;
        }

    }
}
