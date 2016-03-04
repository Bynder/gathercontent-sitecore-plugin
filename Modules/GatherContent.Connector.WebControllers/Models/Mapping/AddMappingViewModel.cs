using System.Collections.Generic;

namespace GatherContent.Connector.WebControllers.Models.Mapping
{
    public class AddMappingViewModel
    {
        public AddMappingViewModel()
        {
            Tabs = new List<TemplateTab>();
        }

        public string GcTemplateId { get; set; }
        public List<TemplateTab> Tabs { get; set; }
        public string SelectedTemplateId { get; set; }
        public bool IsEdit { get; set; }
        public string GcMappingTitle { get; set; }
        public string OpenerId { get; set; }
        public string DefaultLocation { get; set; }
        public string DefaultLocationTitle { get; set; }
        
    }
}