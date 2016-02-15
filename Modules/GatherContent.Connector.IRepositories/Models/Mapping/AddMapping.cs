using System.Collections.Generic;

namespace GatherContent.Connector.IRepositories.Models.Mapping
{
    public class AddMapping
    {
        public AddMapping()
        {
            Tabs = new List<CmsTemplateTab>();
        }

        public string GcTemplateId { get; set; }
        public List<CmsTemplateTab> Tabs { get; set; }
        public string SelectedTemplateId { get; set; }
        public bool IsEdit { get; set; }
        public string GcMappingTitle { get; set; }
    }
}