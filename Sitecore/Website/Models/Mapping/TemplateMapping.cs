using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GatherContent.Connector.Website.Models.Mapping
{
    public class TemplateMapping
    {
        public string Name { get; set; }
        public string SitecoreTemplateId { get; set; }
        public string GcTemplateId { get; set; }
        public string GcProjectId { get; set; }
        public string LastUpdated { get; set; }
    }
}