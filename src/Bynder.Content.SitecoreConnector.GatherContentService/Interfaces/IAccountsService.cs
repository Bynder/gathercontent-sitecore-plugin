namespace Bynder.Content.SitecoreConnector.GatherContentService.Interfaces
{
    using Core.Entities;

    /// <summary>
    /// 
    /// </summary>
    public interface IAccountsService : IService
    {
        AccountEntity GetAccounts();
    }
}
