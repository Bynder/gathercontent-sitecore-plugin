using System.Collections.Generic;

namespace GatherContent.Connector.IRepositories.Models.Mapping
{
    public class CmsTemplate
    {
        public CmsTemplate()
        {
            CmsFields = new List<CmsField>();
        }

        public string CmsTemplateName { get; set; }
        public string CmsTemplateId { get; set; }
        public List<CmsField> CmsFields { get; set; }
    }
}
