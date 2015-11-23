using System;

namespace GatherContent.Connector.Website.Models
{
    public class MappingModel
    {
        public string GcProjectName { get; set; }
        public string GcTemplateId { get; set; }
        public string GcTemplateName { get; set; }
        public string ScTemplateName { get; set; }
        public string LastMappedDateTime { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public string EditButtonTitle { get; set; }
    }
}