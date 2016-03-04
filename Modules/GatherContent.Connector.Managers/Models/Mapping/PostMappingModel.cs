using System.Collections.Generic;

namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class PostMappingModel
    {
        public PostMappingModel()
        {
            FieldMappings = new List<FieldMappingModel>();
        }
        public string MappingId { get; set; }
        public string MappingTitle { get; set; }
        public string GcTemplateId { get; set; }
        public string CmsTemplateId { get; set; }
        public string DefaultLocation { get; set; }
        public IList<FieldMappingModel> FieldMappings { get; set; }
    }
}
