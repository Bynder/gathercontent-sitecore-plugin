using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class TemplateMapModel
    {
        public TemplateMapModel()
        {
            AvailableCmsTemplates = new List<CmsTemplateModel>();
        }

        public string MappingId { get; set; }
        public string GcProjectName { get; set; }
        public string GcTemplateName { get; set; }
        public List<CmsTemplateModel> AvailableCmsTemplates { get; set; }
        public AddMappingModel AddMappingModel { get; set; }     
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
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string SelectedFieldId { get; set; }  
    }

    public class CmsTemplateModel
    {
        public CmsTemplateModel()
        {
            Fields = new List<CmsTemplateFieldModel>();
        }
        public string Name { get; set; }
        public string Id { get; set; }
        public List<CmsTemplateFieldModel> Fields { get; set; }
    }

    public class CmsTemplateFieldModel
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
    }
}