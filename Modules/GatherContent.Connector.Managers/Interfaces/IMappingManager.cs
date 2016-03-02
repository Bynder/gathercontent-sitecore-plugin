using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.Managers.Models.Mapping;

namespace GatherContent.Connector.Managers.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMappingManager : IManager
    {
	    List<MappingResultModel> MapItems(List<GCItem> items);

	    List<MappingResultModel> MapItems(List<ImportItemModel> items);

	    List<MappingModel> GetMappingModel();

	    TemplateMapModel GetTemplateMappingModel(string id, string scMappingId);

	    void PostMapping(PostMappingModel model);

	    void DeleteMapping(string scMappingId);
    }
}
