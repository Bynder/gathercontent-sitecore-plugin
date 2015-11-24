using System.Net;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services.Abstract;

namespace GatherContent.Connector.GatherContentService.Services
{
    public class AccountsService : BaseService
    {
        protected override string ServiceUrl
        {
            get { return "accounts"; }
        }

        public AccountsService(GCAccountSettings accountSettings)
            : base(accountSettings)
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
