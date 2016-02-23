namespace GatherContent.Connector.IRepositories.Models.Mapping
{
   public class TemplateMapping
    {
        public string Name { get; set; }
        public string SitecoreTemplateId { get; set; }
        public string GcTemplateId { get; set; }
        public string LastUpdated { get; set; }
        public string GcMappingTitle { get; set; }
        public string GcTemplateProxy { get; set; }
        public string DefaultLocation { get; set; }
    }
}
