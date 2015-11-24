using GatherContent.Connector.IRepositories.Interfaces;
using Sitecore.Data;
using Sitecore.Globalization;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class ItemsRepository : BaseSitecoreRepository, IItemsRepository
    {
        public ItemsRepository(Database contextDatabase, Language contextLanguage)
            : base(contextDatabase, contextLanguage)
        { }
    }
}
