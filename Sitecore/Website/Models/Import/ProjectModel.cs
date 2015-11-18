using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GatherContent.Connector.Service.Entities;

namespace GatherContent.Connector.Website.Models.Import
{
    public class ProjectModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public ProjectModel(Project project)
        {
            Id = project.Id.ToString();
            Name = project.Name;
        }
    }
}