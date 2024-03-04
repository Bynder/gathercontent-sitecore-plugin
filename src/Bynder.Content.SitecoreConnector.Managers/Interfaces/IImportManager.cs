using System.Collections.Generic;

using Bynder.Content.SitecoreConnector.Managers.Models.ImportItems;
using Bynder.Content.SitecoreConnector.Managers.Models.ImportItems.New;

namespace Bynder.Content.SitecoreConnector.Managers.Interfaces
{
    using Core.Entities;

    /// <summary>
    /// 
    /// </summary>
    public interface IImportManager : IManager
    {

        List<ItemResultModel> ImportItems(string itemId, List<ImportItemModel> items, string projectId, string statusId, string language);

        List<ItemResultModel> ImportItemsWithLocation(List<LocationImportItemModel> items, string projectId, string statusId, string language);

        List<ItemModel> GetImportDialogModel(string itemId, string projectId);

        Bynder.Content.SitecoreConnector.Managers.Models.ImportItems.New.FiltersModel GetFilters(string projectId);

        List<MappingResultModel> MapItems(List<CWBItem> items);
    }
}
