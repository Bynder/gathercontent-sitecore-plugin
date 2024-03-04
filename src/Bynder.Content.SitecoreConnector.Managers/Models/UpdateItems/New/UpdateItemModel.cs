using Bynder.Content.SitecoreConnector.Managers.Models.ImportItems.New;
using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;

namespace Bynder.Content.SitecoreConnector.Managers.Models.UpdateItems.New
{
   public class UpdateItemModel
    {
        public string CmsId { get; set; }
        public string Title { get; set; }
        public string LastUpdatedInCms { get; set; }
        public CwbStatusModel Status { get; set; }
        public CwbItemModel CwbItem { get; set; }
        public CwbProjectModel Project { get; set; }
        public CwbTemplateModel CwbTemplate { get; set; }
        public CmsTemplateModel CmsTemplate { get; set; }
        public string CmsLink { get; set; }
        public string CwbLink { get; set; }


    }

}
