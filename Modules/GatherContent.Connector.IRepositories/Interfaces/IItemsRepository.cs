using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Update;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    public interface IItemsRepository : IRepository
    {
	    List<MappingResultModel> ImportItems(string itemId, string languageName, List<MappingResultModel> items);

	    List<MappingResultModel> ImportItemsWithLocation(string languageName, List<MappingResultModel> successfulImportedItems);

	    List<CMSUpdateItem> GetItemsForUpdate(string targetItemId);

	    void UpdateItems(List<MappingResultModel> items);
    }
}
