using System;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace GatherContent.Connector.SitecoreRepositories
{
    public abstract class BaseSitecoreRepository
    {
        protected readonly Database ContextDatabase;
        protected readonly Language ContextLanguage;

        protected BaseSitecoreRepository(Database contextDatabase, Language contextLanguage)
        {
            var a = Sitecore.Context.Database;
            ContextDatabase = contextDatabase;
            ContextLanguage = contextLanguage;
        }

        public Item GetItem(string sitecoreId)
        {
            Item resultItem = null;
            if (!String.IsNullOrEmpty(sitecoreId))
                resultItem = ContextDatabase.GetItem(sitecoreId, ContextLanguage);

            return resultItem;
        }
    }
}
