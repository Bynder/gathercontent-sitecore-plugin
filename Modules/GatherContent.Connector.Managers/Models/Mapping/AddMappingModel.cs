using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class AddMappingModel
    {
        public AddMappingModel()
        {
            SelectedFields = new List<FieldMappingModel>();
        }
        public string MappingId { get; set; }
        public string MappingTitle { get; set; }
        public string GcProjectId { get; set; }
        public string GcProjectName { get; set; }
        public string GcTemplateId { get; set; }
        public string GcTemplateName { get; set; }
        public string CmsTemplateId { get; set; }
        public string DefaultLocation { get; set; }
        public string DefaultLocationTitle { get; set; }
        public List<FieldMappingModel> SelectedFields { get; set; }
    }

}