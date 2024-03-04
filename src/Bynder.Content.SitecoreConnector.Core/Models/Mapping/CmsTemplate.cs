namespace Bynder.Content.SitecoreConnector.Core.Models.Mapping
{
    using System.Collections.Generic;

    using Import;

    public class CmsTemplate
    {
        public CmsTemplate()
        {
            TemplateFields = new List<CmsTemplateField>();
        }
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public List<CmsTemplateField> TemplateFields { get; set; }
    }
}
