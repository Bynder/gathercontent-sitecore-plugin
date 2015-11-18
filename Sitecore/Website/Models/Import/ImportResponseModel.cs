using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GatherContent.Connector.Service.Entities;

namespace GatherContent.Connector.Website.Models.Import
{
    public class ImportResponseModel
    {
        public ProjectModel Project { get; set; }

        public IEnumerable<ProjectModel> Projects { get; set; }

        public IEnumerable<ItemModel> Items { get; set; }

        public IEnumerable<StatusModel> Statuses { get; set; }

        public IEnumerable<TemplateModel> Templates { get; set; }

        public ImportResponseModel(Project project, ItemsEntity items, ProjectsEntity projects)
        {
            Project = new ProjectModel(project);
            Items = items.Data.Select(i => new ItemModel(i));
            Statuses = project.Statuses.Data.Select(i => new StatusModel(i));
            Templates = items.Data.Select(i => new TemplateModel(i.TemplateId.ToString())).Distinct();
            Projects = projects.Data.Select(i => new ProjectModel(i));
        }
    }
}