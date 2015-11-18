using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GatherContent.Connector.Website.Models.Import
{
    public class TemplateModel
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public TemplateModel (string id)
        {
            Id = id;
            Title = "new template " + id;
        }
    }
}