using System.Collections.Generic;
using GatherContent.Connector.Managers.Models.ImportItems.New;
using GatherContent.Connector.Managers.Models.UpdateItems;

namespace GatherContent.Connector.Managers.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUpdateManager : IManager
    {
        SelectItemsForUpdateModel GetItemsForUpdate(string itemId);

        List<ItemResultModel> UpdateItems(string itemId, List<UpdateListIds> models, string language);
    }
}