using System.Collections.Generic;

namespace GatherContent.Connector.IRepositories.Models.Import
{
    public class MappingTemplateModel
    {
        public string CMSTemplateId { get; set; }

        public string GCTemplateId { get; set; }

        public List<MappingFieldModel> Fields { get; set; }

        public MappingTemplateModel(string cmsTemplate, string gcTemplate, List<MappingFieldModel> fields)
        {
            CMSTemplateId = cmsTemplate;
            GCTemplateId = gcTemplate;
            Fields = fields;
        }
    }
}
