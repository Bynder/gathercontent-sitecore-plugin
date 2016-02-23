using System.Collections.Generic;


namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class PostMappingModel
    {
        public PostMappingModel()
        {
            TemplateTabs = new List<TemplateTab>();
        }

        public List<TemplateTab> TemplateTabs { get; set; }
        public bool IsEdit { get; set; }
        public string TemplateId { get; set; }
        public string SelectedTemplateId { get; set; }
        public string GcMappingTitle { get; set; }
        public string GcTemplateProxyId { get; set; }
        public string DefaultLocation { get; set; }
    }
}
