using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.GatherContentService.Interfaces
{
    public interface IAccountsService : IService
    {
	    AccountEntity GetAccounts();
    }
}
