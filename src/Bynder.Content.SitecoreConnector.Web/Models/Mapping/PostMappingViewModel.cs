using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Web.Models.Mapping
{
    public class PostRelatedMappingViewModel : PostMappingViewModel
    {
        public PostRelatedMappingViewModel() : base()
        {
            
        }
        public string SelectedContainerTemplateId { get; set; }
        public string SelectedCwbRelatedMapping { get; set; }
    }
}
