using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.New.Import;
using GatherContent.Connector.IRepositories.Models.Update;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IItemsRepository : IRepository
    {
        IList<CmsItem> GetItems(string parentId, string language);

        CmsItem GetItem(string itemId, string language, bool readAllFields = false);

        void CreateItem(string parentId, CmsItem cmsItem);

        void UpdateItem(CmsItem cmsItem);

        List<CMSUpdateItem> GetItemsForUpdate(string targetItemId);

        void UpdateItems(List<MappingResultModel> items);

        List<MappingResultModel> ImportItems(string itemId, string languageName, List<MappingResultModel> items);

        List<MappingResultModel> ImportItemsWithLocation(string languageName, List<MappingResultModel> items);
    }
}
