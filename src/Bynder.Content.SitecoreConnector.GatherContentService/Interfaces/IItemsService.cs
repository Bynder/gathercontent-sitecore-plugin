using System.IO;

namespace Bynder.Content.SitecoreConnector.GatherContentService.Interfaces
{
    using Core.Entities;

    /// <summary>
    /// 
    /// </summary>
    public interface IItemsService : IService
    {
        ItemsEntity GetItems(string projectId);

        ItemEntity GetSingleItem(string itemId);

        void ChooseStatusForItem(string itemId, string statusId);

        ItemFiles GetItemFiles(string itemId);

        Stream DownloadFile(int fileId);
    }
}
