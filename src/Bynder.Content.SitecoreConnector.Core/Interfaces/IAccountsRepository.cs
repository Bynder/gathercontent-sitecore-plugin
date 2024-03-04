namespace Bynder.Content.SitecoreConnector.Core.Interfaces
{
    public interface IAccountsRepository : IRepository
    {
        CWBAccountSettings GetAccountSettings();
    }
}
