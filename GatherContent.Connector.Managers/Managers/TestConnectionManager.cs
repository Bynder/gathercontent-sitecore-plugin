using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.SitecoreRepositories.Repositories;

namespace GatherContent.Connector.Managers.Managers
{
    public class TestConnectionManager : BaseManager
    {
        private readonly AccountsService _accountsService;

        public TestConnectionManager()
        {
            var accountsRepository = new AccountsRepository();
            var accountSettings = accountsRepository.GetAccountSettings();
            _accountsService = new AccountsService(accountSettings);
        }

        public bool TestConnection()
        {
            try
            {
                _accountsService.GetAccounts();
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
