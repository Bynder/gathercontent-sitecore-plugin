using System.Collections.Generic;
using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;
using Bynder.Content.SitecoreConnector.Web.Models.Import;

namespace Bynder.Content.SitecoreConnector.Web.Models.Mapping
{
    public class RelatedTemplateMapViewModel: TemplateMapViewModel
    {
        public RelatedTemplateMapViewModel() : base()
        {
        }

        public List<TemplateMappingViewModel> Mappings { get; set; }
    }
}
