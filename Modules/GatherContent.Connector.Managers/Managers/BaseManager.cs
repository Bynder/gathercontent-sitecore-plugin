using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.SitecoreRepositories.Repositories;

namespace GatherContent.Connector.Managers.Managers
{
    public class BaseManager
    {
        private readonly AccountsService _accountsService;
        private readonly ProjectsService _projectsService;
        private readonly TemplatesService _templateService;

        private readonly CacheManager _cacheManager;


        public BaseManager()
        {
            var accountsRepository = new AccountsRepository();
            var accountSettings = accountsRepository.GetAccountSettings();

            _accountsService = new AccountsService(accountSettings);
            _projectsService = new ProjectsService(accountSettings);
            _templateService = new TemplatesService(accountSettings);

            _cacheManager = new CacheManager();
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

        protected TemplateEntity GetGcTemplateEntity(string id)
        {
            TemplateEntity template;
            var key = "template_" + id;
            if (_cacheManager.IsSet(key))
            {
                template = _cacheManager.Get<TemplateEntity>(key);
            }
            else
            {
                template = _templateService.GetSingleTemplate(id);
                _cacheManager.Set(key, template, 60);
            }
            return template;
        }


        protected ProjectEntity GetGcProjectEntity(string id)
        {
            ProjectEntity project;
            var key = "project_" + id;
            if (_cacheManager.IsSet(key))
            {
                project = _cacheManager.Get<ProjectEntity>(key);
            }
            else
            {
                project = _projectsService.GetSingleProject(id);
                _cacheManager.Set(key, project, 60);
            }
            return project;
        }

    }
}
