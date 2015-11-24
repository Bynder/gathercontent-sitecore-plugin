using System.Collections.Generic;

namespace GatherContent.Connector.Website.Models.Mapping
{
    public class GcScMapModel
    {
        public GcScMapModel()
        {
            Tabs = new List<TemplateTab>();
            SitecoreTemplates = new List<SitecoreTemplate>();
        }

        public string GcProjectName { get; set; }
        public string GcTemplateName { get; set; }
        public string SelectedTemplateId { get; set; }
        public int GcTemplateId { get; set; }
        public List<TemplateTab> Tabs { get; set; }
        public List<SitecoreTemplate> SitecoreTemplates { get; set; }
        
    }

    public class TemplateTab
    {
        public TemplateTab()
        {
            Fields = new List<TemplateField>();
        }

        public string TabName { get; set; }
        public List<TemplateField> Fields { get; set; }
    }

    public class TemplateField
    {
        public string FieldName { get; set; }
        public string SelectedField { get; set; }
    }

    public class SitecoreTemplate
    {
        public SitecoreTemplate()
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
    }
}