namespace Bynder.Content.SitecoreConnector.SitecoreRepositories.Repositories
{
    using Core.DependencyInjection;
    using Core;
    using Core.Interfaces;

    [Service(typeof(IAccountsRepository))]
    public class AccountsRepository : BaseSitecoreRepository, IAccountsRepository
	{
		public AccountsRepository() : base() { }

		public CWBAccountSettings GetAccountSettings()
		{
			var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
			if (accountSettingItem != null)
			{
				var tenantName = string.Empty;
				var tenantMatch = System.Text.RegularExpressions.Regex.Match(accountSettingItem["Platform Url"], @"^(http(s)?:\/\/)?(?<tenant>.*)\.gathercontent\.com(\/)?$");
				if (tenantMatch.Groups["tenant"] != null)
				{
					tenantName = tenantMatch.Groups["tenant"].Value.ToLower();
				}
				return new CWBAccountSettings
				{
					TenantName = tenantName,
					ApiUrl = "https://api.gathercontent.com/", //hardcoded value requested by client 
					Username = accountSettingItem["Account Owner Email Address"],
					ApiKey = accountSettingItem["API Key"],
                    TemplatesRootFolderId = accountSettingItem["Templates Root Folder"],
                    OptionsContentFolderId = accountSettingItem["Options Content Folder"],
                    OptionsTemplateId = accountSettingItem["Options Template"],
                    DateFormat = accountSettingItem["Date Format"], // "Output Date Format" - display name. Appears in the user interface.
                    DateTimeParseFormat = accountSettingItem["DateTime Field Format"],
					GatherContentUrl = accountSettingItem["Platform Url"],
					DropTreeHomeNode = accountSettingItem["DropTree Home Node"],
					AccountItemId = accountSettingItem.ID.ToString()
				};

			}

			return null;
		}
	}
}
