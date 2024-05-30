using System.Collections.Generic;
using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;

namespace Bynder.Content.SitecoreConnector.Managers.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMappingManager : IManager
    {
        MappingModel GetSingleMappingModel(string cwbTemplateId, string cmsMappingId);
        RelatedMappingModel GetSingleRelatedMappingModel(string cwbTemplateId, string cmsMappingId);

        List<MappingModel> GetMappingModel();

        List<MappingModel> GetMappingModel(string cwbProjectId);

        List<CwbProjectModel> GetAllCwbProjects();

        List<CmsTemplateModel> GetAvailableTemplates();

        List<CmsTemplateModel> GetAvailableOptionsContentFolder();

        List<CmsTemplateModel> GetAvailableOptionTemplates();

        List<CwbTemplateModel> GetTemplatesByProjectId(string cwbProjectId);

        List<CwbTabModel> GetFieldsByTemplateId(string cwbTemplateId);

        void UpdateMapping(MappingModel model);
        void UpdateRelatedMapping(RelatedMappingModel model);

        void CreateMapping(MappingModel model);

        void CreateRelatedMapping(RelatedMappingModel model);

        void DeleteMapping(string scMappingId);
        Dictionary<string, string> GetAllowedMappings();
    }
}
