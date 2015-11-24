using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Interfaces;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class ItemsRepository : BaseSitecoreRepository, IItemsRepository
    {
        public ItemsRepository() : base() { }

        public Dictionary<GCItem, bool> ImportItems(string itemId, List<GCItem> items)
        {
            var result = new Dictionary<GCItem, bool>();

            return result;
        }
    }
}
