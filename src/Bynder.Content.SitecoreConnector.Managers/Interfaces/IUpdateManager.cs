using System.Collections.Generic;
using Bynder.Content.SitecoreConnector.Managers.Models.ImportItems.New;
using Bynder.Content.SitecoreConnector.Managers.Models.UpdateItems;
using Bynder.Content.SitecoreConnector.Managers.Models.UpdateItems.New;

namespace Bynder.Content.SitecoreConnector.Managers.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IUpdateManager : IManager
    {
        UpdateModel GetItemsForUpdate(string itemId, string languageId);

        List<ItemResultModel> UpdateItems(string itemId, List<UpdateListIds> models, string language);
    }
}
