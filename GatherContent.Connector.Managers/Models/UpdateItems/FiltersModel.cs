using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class FiltersModel
    {
        public Project CurrentProject { get; set; }

        public List<Project> Projects { get; set; }

        public List<Template> Templates { get; set; }

        public List<Status> Statuses { get; set; }

        public FiltersModel() { }

        public FiltersModel(Project project, List<Project> projects, List<Template> templates, List<Status> statuses)
        {
            CurrentProject = project;
            Projects = projects;
            Templates = templates;
            Statuses = statuses;
        }

    }
}
