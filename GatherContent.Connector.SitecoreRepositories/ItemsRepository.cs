using System;
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

        public List<ImportItemsResponseModel> ImportItems(string itemId, List<ImportItemsResponseModel> items)
        {
            Item parentItem = GetItem(itemId);
         
            List<ImportItemsResponseModel> result = AddItems(parentItem, items);
            
            return result;
        }

        private List<ImportItemsResponseModel> AddItems(Item parent, List<ImportItemsResponseModel> items)
        {
            List<ImportItemsResponseModel> result = items.Select(i => AddItem(parent, i)).ToList();
            return result;
        }

        private ImportItemsResponseModel AddItem(Item parent, ImportItemsResponseModel item)
        {
            return null;
        }

        public List<CMSUpdateItem> GetItems(string targetItemId)
        {
            Item parentItem = GetItem(targetItemId);
            var items = parentItem.Axes.SelectItems(String.Format("./descendant::*[@@templatename='{0}']", Constants.GCLinkItemTemplate));
            var result = items.Select(i => new CMSUpdateItem { GSItemId = i["GC Content Id"], LastUpdatedTime = DateTime.Now.AddDays(-1) }).ToList();

            return result;
        }
    }
}
