namespace Bynder.Content.SitecoreConnector.Managers.Managers
{
    using Bynder.Content.SitecoreConnector.GatherContentService.Interfaces;
    using Interfaces;
    using Bynder.Content.SitecoreConnector.Core.Interfaces;

	public class TestConnectionManager : BaseManager
	{
		public TestConnectionManager(
			IAccountsService accountsService,
			IProjectsService projectsService,
			ITemplatesService templateService,
			ICacheManager cacheManager,
            IAccountsRepository accountsRepository) : base(accountsService, projectsService, templateService, cacheManager, accountsRepository)
		{
		}

		public bool TestConnection()
		{
			try
			{
				AccountsService.GetAccounts();
			}
			catch
			{
				return false;
			}

			return true;
		}
	}
}
