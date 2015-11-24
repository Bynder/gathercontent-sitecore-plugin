using GatherContent.Connector.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using Sitecore.Data;
using Sitecore.Globalization;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class AccountsRepository : BaseSitecoreRepository, IAccountsRepository
    {
        public AccountsRepository(Database contextDatabase, Language contextLanguage)
            : base(contextDatabase, contextLanguage)
        { }

        public GCAccountSettings GetAccountSettings()
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            if (accountSettingItem != null)
            {
                return new GCAccountSettings
                {
                    ApiUrl = accountSettingItem["API Url"],
                    Username = accountSettingItem["Username"],
                    ApiKey = accountSettingItem["API Key"],
                };

            }

            return null;
        }
    }
}
