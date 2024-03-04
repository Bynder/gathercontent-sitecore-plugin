
namespace Bynder.Content.SitecoreConnector.Web.Models.Import
{
   public class ImportListItemViewModel
    {
       public ImportListItemViewModel()
       {
           AvailableMappings = new AvailableMappingsViewModel();
       }
        public string Id { get; set; }

        //public bool Checked { get; set; }

        public StatusViewModel Status { get; set; }

        public string Title { get; set; }

        public string LastUpdatedInCwb { get; set; }

        public string Breadcrumb { get; set; }

        public TemplateViewModel Template { get; set; }

        public AvailableMappingsViewModel AvailableMappings { get; set; }
    }
}
