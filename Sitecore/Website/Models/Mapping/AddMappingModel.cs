using System.Collections.Generic;

namespace GatherContent.Connector.Website.Models.Mapping
{
    public class AddMappingModel
    {
        public string GcTemplateId { get; set; }
        public List<TemplateTab> Tabs { get; set; }
        public string SelectedTemplateId { get; set; }
    }
}