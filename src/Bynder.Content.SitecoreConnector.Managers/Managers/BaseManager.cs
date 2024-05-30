namespace Bynder.Content.SitecoreConnector.Managers.Managers
{
    using System.Collections.Generic;
    using System.Linq;

    using Bynder.Content.SitecoreConnector.GatherContentService.Interfaces;
    using Interfaces;
    using Core;
    using Core.Entities;
    using Core.Interfaces;

    public class BaseManager
    {
        protected IAccountsService AccountsService;
        protected IProjectsService ProjectsService;
        protected ITemplatesService TemplatesService;
	    protected CWBAccountSettings CwbAccountSettings;
        protected ICacheManager CacheManager;

        public BaseManager(IAccountsService accountsService, IProjectsService projectsService, ITemplatesService templateService, ICacheManager cacheManager, IAccountsRepository accountsRepository)
        {
            AccountsService = accountsService;
            ProjectsService = projectsService;
            TemplatesService = templateService;
			CwbAccountSettings = accountsRepository.GetAccountSettings();
			CacheManager = cacheManager;
        }

		public Account GetAccount()
		{
			{
				var accounts = AccountsService.GetAccounts();
				return accounts.Data.FirstOrDefault(acc => acc.Slug.ToLower() == CwbAccountSettings.TenantName);
			}
		}

		protected List<Project> GetProjects(int accountId)
        {
            var projects = ProjectsService.GetProjects(accountId);
            var activeProjects = projects.Data.Where(p => p.Active).ToList();
            return activeProjects;
        }

        protected TemplateEntity GetCwbTemplateEntity(string id)
        {
            TemplateEntity template;
            var key = "template_" + id;
            if (CacheManager.IsSet(key))
            {
                template = CacheManager.Get<TemplateEntity>(key);
            }
            else
            {
                template = TemplatesService.GetSingleTemplate(id);
                CacheManager.Set(key, template, 60);
            }
            return template;
        }

        protected ProjectEntity GetCwbProjectEntity(string id)
        {
            ProjectEntity project;
            var key = "project_" + id;
            if (CacheManager.IsSet(key))
            {
                project = CacheManager.Get<ProjectEntity>(key);
            }
            else
            {
                project = ProjectsService.GetSingleProject(id);
                CacheManager.Set(key, project, 60);
            }
            return project;
        }
    }
}
