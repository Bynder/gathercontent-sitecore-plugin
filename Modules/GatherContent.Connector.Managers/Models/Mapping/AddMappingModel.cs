using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class AddMappingModel
    {
        public AddMappingModel()
        {
            Tabs = new List<TemplateTab>();
        }
        public string GcMappingTitle { get; set; }
        public string GcTemplateId { get; set; }
        public string CmsTemplateId { get; set; }      
        public string DefaultLocation { get; set; }
        public string DefaultLocationTitle { get; set; }
        public List<TemplateTab> Tabs { get; set; }
    }
}