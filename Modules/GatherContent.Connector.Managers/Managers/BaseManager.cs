using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.Managers.Interfaces;

namespace GatherContent.Connector.Managers.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class BaseManager : IManager
    {
        protected IAccountsService AccountsService;
        protected IProjectsService ProjectsService;
        protected ITemplatesService TemplatesService;

        protected ICacheManager _cacheManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountsService"></param>
        /// <param name="projectsService"></param>
        /// <param name="templateService"></param>
        /// <param name="cacheManager"></param>
        public BaseManager(IAccountsService accountsService, IProjectsService projectsService, ITemplatesService templateService, ICacheManager cacheManager)
        {
            AccountsService = accountsService;
            ProjectsService = projectsService;
            TemplatesService = templateService;

            _cacheManager = cacheManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected Account GetAccount()
        {
            var accounts = AccountsService.GetAccounts();
            return accounts.Data.FirstOrDefault();
           
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        protected List<Project> GetProjects(int accountId)
        {
            var projects = ProjectsService.GetProjects(accountId);
            var activeProjects = projects.Data.Where(p => p.Active).ToList();
            return activeProjects;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
                template = TemplatesService.GetSingleTemplate(id);
                _cacheManager.Set(key, template, 60);
            }
            return template;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
                project = ProjectsService.GetSingleProject(id);
                _cacheManager.Set(key, project, 60);
            }
            return project;
        }
    }
}
