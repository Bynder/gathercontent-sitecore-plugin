
namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class MappingModel
    {
        public MappingModel() { }

        public MappingModel(string gcProjectName, string gcTemplateId, string gcTemplateName, string scMappingId,
            string scTemplateName, string mappingTitle, string lastMappedDateTime, string lastUpdatedDate, string editButtonTitle, bool isMapped, bool isHighlightingDate)
        {
            GcProjectName = gcProjectName;
            GcTemplateId = gcTemplateId;
            GcTemplateName = gcTemplateName;
            ScTemplateName = scTemplateName;
            MappingTitle = mappingTitle;
            LastMappedDateTime = lastMappedDateTime;
            LastUpdatedDate = lastUpdatedDate;
            EditButtonTitle = editButtonTitle;
            IsMapped = isMapped;
            IsHighlightingDate = isHighlightingDate;
            ScMappingId = scMappingId;
        }
        public string GcProjectName { get; set; }
        public string GcTemplateId { get; set; }
        public string GcTemplateName { get; set; }
        public string ScTemplateName { get; set; }
        public string MappingTitle { get; set; }
        public string LastMappedDateTime { get; set; }
        public string LastUpdatedDate { get; set; }
        public string EditButtonTitle { get; set; }
        public bool IsMapped { get; set; }
        public bool RemovedFromGc { get; set; }
        public bool IsHighlightingDate { get; set; }
        public string ScMappingId { get; set; }
    }
}
