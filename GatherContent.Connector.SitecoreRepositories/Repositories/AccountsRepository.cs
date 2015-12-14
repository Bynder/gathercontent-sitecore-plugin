using GatherContent.Connector.Entities;
using GatherContent.Connector.IRepositories.Interfaces;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    public class AccountsRepository : BaseSitecoreRepository, IAccountsRepository
    {
        public AccountsRepository() : base() { }

        public GCAccountSettings GetAccountSettings()
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            if (accountSettingItem != null)
            {
                return new GCAccountSettings
                {
                    ApiUrl = accountSettingItem["API Url"],
                    Username = accountSettingItem["Account Owner Email Address"],
                    ApiKey = accountSettingItem["API Key"],
                    TemplateFolderId = accountSettingItem["Templates Root Folder"],
                    DateFormat = accountSettingItem["Date Format"],
                    GatherContentUrl = accountSettingItem["GatherContent page URL"],
                    AccountItemId = accountSettingItem.ID.ToString()
                };

            }

            return null;
        }
    }
}
