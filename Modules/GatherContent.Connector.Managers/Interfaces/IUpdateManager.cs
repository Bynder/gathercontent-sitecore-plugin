using System.Collections.Generic;
using GatherContent.Connector.Managers.Models.UpdateItems;

namespace GatherContent.Connector.Managers.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUpdateManager
    {
	    SelectItemsForUpdateModel GetItemsForUpdate(string itemId);

	    UpdateResultModel UpdateItems(string itemId, List<UpdateListIds> models);
    }
}
