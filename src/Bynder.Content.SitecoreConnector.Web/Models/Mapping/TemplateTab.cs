using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Web.Models.Mapping
{
    public class TemplateTab
    {
        public TemplateTab()
        {
            Fields = new List<TemplateField>();
        }

        public string TabName { get; set; }
        public List<TemplateField> Fields { get; set; }
    }
}
