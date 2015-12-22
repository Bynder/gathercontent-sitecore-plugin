using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class TemplateMapModel
    {
        public TemplateMapModel()
        {
            SitecoreTemplates = new List<SitecoreTemplate>();
            Rules = new Dictionary<string, string>();
        }

        public string GcProjectName { get; set; }
        public string GcTemplateName { get; set; }
        public List<SitecoreTemplate> SitecoreTemplates { get; set; }
        public SitecoreTemplate SelectedTemplate { get; set; }
        public AddMappingModel AddMappingModel { get; set; }
        public Dictionary<string, string> Rules { get; set; }
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
        public string FieldId { get; set; }
        public string SelectedField { get; set; }
        public string FieldType { get; set; }
        
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
        public string SitecoreFieldType { get; set; }
    }
}