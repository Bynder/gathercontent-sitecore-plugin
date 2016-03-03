
using System.Collections.Generic;

namespace GatherContent.Connector.IRepositories.Models.Mapping
{
    public class CmsMapping
    {
        public CmsMapping()
        {
            CmsFields = new List<CmsField>();
        }

        public string Id { get; set; }
        //public string GcProjectId { get; set; } 
        public string GcProjectName { get; set; }
        public string GcTemplateId { get; set; }
        public string GcTemplateName { get; set; }
        public string CmsTemplateId { get; set; }
        public string CmsTemplateName { get; set; }
        public string CmsMappingTitle { get; set; }
        public string LastMappedDateTime { get; set; }
        public string LastUpdatedDate { get; set; }
        public string DefaultLocationId { get; set; }
        public string DefaultLocationTitle { get; set; }
        public List<CmsField> CmsFields { get; set; }

    }
}
