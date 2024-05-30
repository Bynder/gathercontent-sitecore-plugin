using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Web.Models.Mapping
{
    public class SitecoreTemplateViewModel
    {
        public SitecoreTemplateViewModel()
        {
            SitecoreFields = new List<SitecoreTemplateField>();
        }

        public string SitrecoreTemplateName { get; set; }
        public string SitrecoreTemplateId { get; set; }
        public List<SitecoreTemplateField> SitecoreFields { get; set; }
    }

    public class SitecoreTemplateField
    {
        public string SitrecoreFieldName { get; set; }
        public string SitecoreFieldId { get; set; }
        public string SitecoreFieldType { get; set; }
    }
}
