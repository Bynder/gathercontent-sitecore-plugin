using System;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace GatherContent.Connector.SitecoreRepositories
{
    public abstract class BaseSitecoreRepository
    {
        protected readonly Database ContextDatabase;
        protected readonly Language ContextLanguage;

        protected BaseSitecoreRepository()
        {
            ContextDatabase = Sitecore.Configuration.Factory.GetDatabase("master");
            ContextLanguage = Sitecore.Context.Language;
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
