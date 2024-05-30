
using System.Collections.Generic;
using Bynder.Content.SitecoreConnector.Managers.Models.ImportItems.New;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems
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
}
