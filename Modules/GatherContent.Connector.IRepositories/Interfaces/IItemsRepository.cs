using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.Import;

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

    }
}
