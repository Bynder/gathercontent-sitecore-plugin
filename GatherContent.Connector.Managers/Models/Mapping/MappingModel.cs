
namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class MappingModel
    {
        public MappingModel() { }

        public MappingModel(string gcProjectName, string gcTemplateId, string gcTemplateName, string scTemplateName, string lastMappedDateTime,
            string lastUpdatedDate, string editButtonTitle, bool isMapped)
        {
            GcProjectName = gcProjectName;
            GcTemplateId = gcTemplateId;
            GcTemplateName = gcTemplateName;
            ScTemplateName = scTemplateName;
            LastMappedDateTime = lastMappedDateTime;
            LastUpdatedDate = lastUpdatedDate;
            EditButtonTitle = editButtonTitle;
            IsMapped = isMapped;
        }
        public string GcProjectName { get; set; }
        public string GcTemplateId { get; set; }
        public string GcTemplateName { get; set; }
        public string ScTemplateName { get; set; }
        public string LastMappedDateTime { get; set; }
        public string LastUpdatedDate { get; set; }
        public string EditButtonTitle { get; set; }
        public bool IsMapped { get; set; }
    }
}
