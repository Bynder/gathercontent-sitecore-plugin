using System.Collections.Generic;

namespace GatherContent.Connector.WebControllers.Models.Mapping
{
    public class TemplateMapModel
    {
        public TemplateMapModel()
        {
            SitecoreTemplates = new List<SitecoreTemplate>();
            Rules = new Dictionary<string, string>();
            GcProjects = new List<ProjectViewModel>();
            SelectedFields = new List<FieldMappingViewModel>();
        }

        public string GcProjectName { get; set; }
        public string GcProjectId { get; set; }
        public string GcTemplateName { get; set; }
        public List<SitecoreTemplate> SitecoreTemplates { get; set; }
        public List<ProjectViewModel> GcProjects { get; set; }
        public AddMappingViewModel AddMappingModel { get; set; }
        public Dictionary<string, string> Rules { get; set; }
        public string ScMappingId { get; set; }
        public List<FieldMappingViewModel> SelectedFields { get; set; }
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
        public string SelectedScField { get; set; }
        public string FieldType { get; set; }      
    }
}