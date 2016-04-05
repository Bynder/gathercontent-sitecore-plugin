using System.Collections.Generic;
using GatherContent.Connector.Managers.Models.ImportItems.New;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.Managers.Models.UpdateItems.New;

namespace GatherContent.Connector.Managers.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUpdateManager : IManager
    {
        List<UpdateItemModel> GetItemsForUpdate(string itemId, string languageId);

        UpdateFiltersModel GetFilters(string itemId, string languageId);

        List<ItemResultModel> UpdateItems(string itemId, List<UpdateListIds> models, string language);
    }
}