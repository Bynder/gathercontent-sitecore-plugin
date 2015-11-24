using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.Website.Models.Import
{
    public class ImportResponseModel
    {
        public ProjectModel Project { get; set; }

        public IEnumerable<ProjectModel> Projects { get; set; }

        public IEnumerable<ItemModel> Items { get; set; }

        public IEnumerable<StatusModel> Statuses { get; set; }

        public IEnumerable<TemplateModel> Templates { get; set; }

        public ImportResponseModel(Project project, ItemsEntity items, ProjectsEntity projects, TemplatesEntity templates)
        {
            Project = new ProjectModel(project);
            items.Data = items.Data.Where(i => !string.IsNullOrEmpty(i.TemplateId.ToString())).ToList();
            Items = items.Data.Select(i => new ItemModel(i, templates.Data.FirstOrDefault(j => j.Id == i.TemplateId)));
            Statuses = project.Statuses.Data.Select(i => new StatusModel(i));
            Templates = templates.Data.Select(i => new TemplateModel(i));
            Projects = projects.Data.Select(i => new ProjectModel(i));
        }
    }
}