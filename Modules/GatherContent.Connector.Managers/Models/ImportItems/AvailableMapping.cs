
using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class AvailableMappings
    {
        public AvailableMappings()
        {
            Mappings = new List<AvailableMapping>();
        }
        public string SelectedMappingId { get; set; }
        public List<AvailableMapping> Mappings { get; set; }
    }

    public class AvailableMapping
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }
}
