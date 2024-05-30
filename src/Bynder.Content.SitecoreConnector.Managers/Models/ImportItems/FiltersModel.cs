using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems
{
    using Core.Entities;

    public class FiltersModel
    {
        public Project CurrentProject { get; set; }

        public List<Project> Projects { get; set; }

        public List<CWBTemplate> Templates { get; set; }

        public List<CWBStatus> Statuses { get; set; }

        public FiltersModel() { }

        public FiltersModel(Project project, List<Project> projects, List<CWBTemplate> templates, List<CWBStatus> statuses)
        {
            CurrentProject = project;
            Projects = projects;
            Templates = templates;
            Statuses = statuses;
        }

    }
}
