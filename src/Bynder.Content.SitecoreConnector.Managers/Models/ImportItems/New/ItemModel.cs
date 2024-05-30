using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems.New
{
   public class ItemModel
    {
       public ItemModel()
       {
           AvailableMappings = new AvailableMappings();
       }
        public CwbItemModel CwbItem { get; set; }
        public string Breadcrumb { get; set; }
        public CwbTemplateModel CwbTemplate { get; set; }
        public CwbStatusModel Status { get; set; }
        public AvailableMappings AvailableMappings { get; set; }
    }
}
