namespace Bynder.Content.SitecoreConnector.Web.Models.Import
{
    public class ImportResultViewModel
    {
        public bool IsImportSuccessful { get; set; }
        public string Message { get; set; }
        public string CwbTemplateName { get; set; }
        public string Title { get; set; }       
        public StatusViewModel Status { get; set; }       
        public string CmsLink { get; set; }
        public string CwbLink { get; set; }
    }
}
