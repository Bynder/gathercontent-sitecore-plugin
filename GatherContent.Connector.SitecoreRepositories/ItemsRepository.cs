using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models;
using Sitecore.Data.Items;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class ItemsRepository : BaseSitecoreRepository, IItemsRepository
    {
        public ItemsRepository() : base() { }

        public List<ImportItemsResponseModel> ImportItems(string itemId, List<ImportCMSItem> items)
        {
            Item parentItem = GetItem(itemId);
         
            List<ImportItemsResponseModel> result = AddItems(parentItem, items);
            
            return result;
        }

        private List<ImportItemsResponseModel> AddItems(Item parent, List<ImportCMSItem> items)
        {
            List<ImportItemsResponseModel> result = items.Select(i => AddItem(parent, i)).ToList();
            return result;
        }

        private ImportItemsResponseModel AddItem(Item parent, ImportCMSItem item)
        {
            var result = new ImportItemsResponseModel(item.GCItem, "View in sitecore", true);

            return result;
        }
    }
}
