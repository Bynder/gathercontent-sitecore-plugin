using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Mapping;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMappingRepository : IRepository
    {
	    List<AvailableMappingModel> GetAllMappingsForGcTemplate(string gcProjectId, string gcTemplateId);

	    List<CmsMappingModel> GetMappings();

	    AddMapping GetAddMappingModel(string projectId, TemplateEntity template, string scMappingId);

	    void UpdateMapping(int projectId, TemplateMapping templateMappingModel, List<CmsTemplateField> fields);

	    void CreateMapping(string projectId, TemplateMapping templateMapping);

	    void CreateMapping(int projectId, TemplateMapping templateMappingModel, List<CmsTemplateField> fields);

	    void DeleteMapping(string cmsMappingId);

	    MappingTemplateModel GetTemplateMappingsByTemplateId(string templateId);
    }
}
