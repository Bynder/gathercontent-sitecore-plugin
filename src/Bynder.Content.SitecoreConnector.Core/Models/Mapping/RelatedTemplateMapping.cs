namespace Bynder.Content.SitecoreConnector.Core.Models.Mapping
{
    public class RelatedTemplateMapping : TemplateMapping
    {
        public RelatedTemplateMapping() : base()
        {
            
        }
        public string CmsContainerTemplateId { get; set; }
        public string CmsMainMappingId { get; set; }
    }
}
