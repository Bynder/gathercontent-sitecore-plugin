using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.SitecoreRepositories;

namespace GatherContent.Connector.Managers.Managers
{
    public class BaseManager
    {
        private readonly AccountsService _accountsService;
        private readonly ProjectsService _projectsService;


        public BaseManager()
        {
            var accountsRepository = new AccountsRepository();
            var accountSettings = accountsRepository.GetAccountSettings();

            _accountsService = new AccountsService(accountSettings);
            _projectsService = new ProjectsService(accountSettings);

        }

        protected Account GetAccount()
        {
            var accounts = _accountsService.GetAccounts();
            return accounts.Data.FirstOrDefault();
           
        }

        protected List<Project> GetProjects(int accountId)
        {
            var projects = _projectsService.GetProjects(accountId);
            return projects.Data;
        }

    }
}
