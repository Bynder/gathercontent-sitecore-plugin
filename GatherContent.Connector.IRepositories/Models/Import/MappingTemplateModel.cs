using System.Collections.Generic;

namespace GatherContent.Connector.IRepositories.Models.Import
{
    public class MappingTemplateModel
    {
        public string CMSTemplate { get; set; }

        public string GCTemplate { get; set; }

        public List<MappingFieldModel> Fields { get; set; }

        public MappingTemplateModel(string cmsTemplate, string gcTemplate, List<MappingFieldModel> fields)
        {
            CMSTemplate = cmsTemplate;
            GCTemplate = gcTemplate;
            Fields = fields;
        }
    }
}
