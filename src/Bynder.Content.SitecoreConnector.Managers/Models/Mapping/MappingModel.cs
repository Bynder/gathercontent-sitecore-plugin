
using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Managers.Models.Mapping
{
    public class MappingModel
    {
        public MappingModel()
        {
            FieldMappings = new List<FieldMappingModel>();
        }

        public bool IsRelated { get; set; }
        public string MappingId { get; set; }
        public string MappingTitle { get; set; }
        public string MappingGroupId { get; set; }
        public CwbProjectModel CwbProject { get; set; }
        public CwbTemplateModel CwbTemplate { get; set; }
        public CmsTemplateModel CmsTemplate { get; set; }
        public string DefaultLocationId { get; set; }
        public string DefaultLocationTitle { get; set; }
        public string LastMappedDateTime { get; set; }
        public string LastUpdatedDate { get; set; }

        public IList<FieldMappingModel> FieldMappings { get; set; }   
    }
}
