using Bynder.Content.SitecoreConnector.Web.Models.Import;

namespace Bynder.Content.SitecoreConnector.Web.Models.Update
{
   public class UpdateListItemViewModel
    {
 
       //public bool IsNeedToHighlightingDate { get; set; }

        public string Id { get; set; }
        public string ScTitle { get; set; }
        public string ScTemplateName { get; set; }

        public StatusViewModel Status { get; set; }
        public ItemViewModel CwbItem  { get; set; }
        public ProjectViewModel CwbProject { get; set; }

        public string LastUpdatedInCwb { get; set; }
        public string LastUpdatedInSitecore { get; set; }

        public TemplateViewModel CwbTemplate { get; set; }

        public string CmsLink { get; set; }
        public string CwbLink { get; set; }
        
    }
}
