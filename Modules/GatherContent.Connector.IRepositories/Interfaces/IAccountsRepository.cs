using GatherContent.Connector.Entities;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    public interface IAccountsRepository
    {
        GCAccountSettings GetAccountSettings();
    }
}
