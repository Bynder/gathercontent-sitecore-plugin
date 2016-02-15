using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class AddMappingModel
    {
        public AddMappingModel()
        {
            Tabs = new List<TemplateTab>();
        }

        public string GcTemplateId { get; set; }
        public List<TemplateTab> Tabs { get; set; }
        public string SelectedTemplateId { get; set; }
        public bool IsEdit { get; set; }
        public string GcMappingTitle { get; set; }
    }
}