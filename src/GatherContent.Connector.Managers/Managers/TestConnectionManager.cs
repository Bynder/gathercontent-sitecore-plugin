using GatherContent.Connector.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.Managers.Interfaces;

namespace GatherContent.Connector.Managers.Managers
{
	/// <summary>
	/// 
	/// </summary>
	public class TestConnectionManager : BaseManager
	{
		public TestConnectionManager(
			IAccountsService accountsService,
			IProjectsService projectsService,
			ITemplatesService templateService,
			ICacheManager cacheManager,
			GCAccountSettings gcAccountSettings) : base(accountsService, projectsService, templateService, cacheManager, gcAccountSettings)
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
