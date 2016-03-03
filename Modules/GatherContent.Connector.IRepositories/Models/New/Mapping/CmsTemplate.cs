using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.New.Import;

namespace GatherContent.Connector.IRepositories.Models.New.Mapping
{
    public class CmsTemplate
    {
        public CmsTemplate()
        {
            TemplateFields = new List<CmsTemplateField>();
        }
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public IList<CmsTemplateField> TemplateFields { get; set; }
    }
}
