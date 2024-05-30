namespace Bynder.Content.SitecoreConnector.Core.Models.Mapping
{
    using System.Collections.Generic;

    public class TemplateMapping
    {
        public TemplateMapping()
        {
            FieldMappings = new List<FieldMapping>();
        }
        public string MappingId { get; set; }

        public bool IsRelated { get; set; }
        public string MappingGroupId { get; set; }

        public CmsTemplate CmsTemplate { get; set; }

        public CmsTemplate OptionsTemplate { get; set; }

        public CwbTemplate CwbTemplate { get; set; }

        public string MappingTitle { get; set; }

        public string CwbProjectName { get; set; }

        public string CwbProjectId { get; set; }

        public string LastMappedDateTime { get; set; }

        public string LastUpdatedDate { get; set; }

        public string DefaultLocationId { get; set; }

        public string DefaultLocationTitle { get; set; }

        public IList<FieldMapping> FieldMappings { get; set; }
    }
}
