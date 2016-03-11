using System.Collections.Generic;
using GatherContent.Connector.Managers.Models.Mapping;

namespace GatherContent.Connector.Managers.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMappingManager : IManager
    {
        AddMappingModel GetSingleMappingModel(string gcTemplateId, string cmsMappingId);

        List<MappingModel> GetMappingModel();

        List<GcProjectModel> GetAllGcProjects();

        List<CmsTemplateModel> GetAvailableTemplates();

        List<GcTemplateModel> GetTemplatesByProjectId(string gcProjectId);

        List<TemplateTabModel> GetFieldsByTemplateId(string gcTemplateId);

        void UpdateMapping(PostMappingModel model);

        void CreateMapping(PostMappingModel model);

        void DeleteMapping(string scMappingId);
    }
}