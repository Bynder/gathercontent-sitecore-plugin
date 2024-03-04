namespace Bynder.Content.SitecoreConnector.Core.Interfaces
{
    using System.Collections.Generic;

    using Models.Mapping;

    public interface IMappingRepository : IRepository
    {
        List<TemplateMapping> GetMappings();
        List<TemplateMapping> GetMappingsByCwbProjectId(string projectId);
        List<TemplateMapping> GetMappingsByCwbTemplateId(string cwbTemplateId);
        RelatedTemplateMapping GetRelatedMappingById(string id);
        List<RelatedTemplateMapping> GetRelatedMappingCollection(TemplateMapping mainMapping);
        TemplateMapping GetMappingById(string id);
        TemplateMapping GetMappingByItemId(string itemId, string language);
        List<CmsTemplate> GetAvailableCmsTemplates();

        List<CmsTemplate> GetAvailableOptionsContentFolder();

        List<CmsTemplate> GetAvailableOptionTemplates();

        Dictionary<string, string> GetAllowedMappings();

        void CreateMapping(TemplateMapping templateMapping);
        void CreateRelatedMapping(RelatedTemplateMapping templateMapping);
        void UpdateMapping(TemplateMapping templateMapping);
        void UpdateRelatedMapping(RelatedTemplateMapping templateMapping);
        void DeleteMapping(string id);
        string GetOrCreatePageComponentsFolder(string parentId);
        string GetOrCreateContainer(string parentId, string templateId);
    }
}
