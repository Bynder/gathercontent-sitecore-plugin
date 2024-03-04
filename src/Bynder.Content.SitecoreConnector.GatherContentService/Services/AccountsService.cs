namespace Bynder.Content.SitecoreConnector.GatherContentService.Services
{
    using System.Net;

    using Interfaces;
    using Abstract;
    using Core.DependencyInjection;
    using Core.Entities;
    using Core.Interfaces;

    [Service(typeof(IAccountsService))]
    public class AccountsService : BaseService, IAccountsService
    {
        protected override string ServiceUrl => "accounts";

        public AccountsService(IAccountsRepository accountsRepository) : base(accountsRepository)
        {
        }

        public AccountEntity GetAccounts()
        {
            WebRequest webrequest = CreateRequest(ServiceUrl);
            webrequest.Method = WebRequestMethods.Http.Get;

            AccountEntity result = ReadResponse<AccountEntity>(webrequest);

            return result;
        }
    }
}
