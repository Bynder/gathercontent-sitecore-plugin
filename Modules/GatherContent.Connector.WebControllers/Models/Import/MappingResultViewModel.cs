namespace GatherContent.Connector.WebControllers.Models.Import
{
    public class MappingResultViewModel
    {
        public bool IsImportSuccessful { get; set; }

        public string Message { get; set; }

        public string GCItemId { get; set; }

        public string GCTemplate { get; set; }

        public string Title { get; set; }
        
        //public GCStatus Status { get; set; }
        
        public string CMSId { get; set; }

        public string CMSTemplateId { get; set; }
        public string CmsLink { get; set; }
        public string GcLink { get; set; }

        public string DefaultLocation { get; set; }

        //public List<ImportCMSField> Fields { get; set; }

    }
}
