namespace GatherContent.Connector.IRepositories.Models.Mapping
{
    public class CmsMappingModel
    {
        public string GcProjectName { get; set; }
        public string GcTemplateId { get; set; }
        public string GcTemplateName { get; set; }
        public string CmsTemplateName { get; set; }
        public string LastMappedDateTime { get; set; }
        public string LastUpdatedDate { get; set; }
        public string EditButtonTitle { get; set; }
        public bool IsMapped { get; set; }
    }
}
