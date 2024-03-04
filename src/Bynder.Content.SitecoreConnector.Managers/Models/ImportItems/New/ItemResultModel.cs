using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems.New
{
    public class ItemResultModel
    {
        public string CmsId { get; set; }      
        public string CmsLink { get; set; }
        public string CwbLink { get; set; }
        public CwbItemModel CwbItem { get; set; }
        public CwbTemplateModel CwbTemplate { get; set; }
        public CwbStatusModel Status { get; set; }      
        public bool IsImportSuccessful { get; set; }
        public string ImportMessage { get; set; }
    }
}
