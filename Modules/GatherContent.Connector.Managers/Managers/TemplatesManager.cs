using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.TemplateModel;

namespace GatherContent.Connector.Managers.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class TemplatesManager : BaseManager, ITemplatesManager
    {
        protected IMappingRepository _mappingRepository;
        protected ITemplatesRepository _templatesRepository;
        protected IProjectsRepository _projectsRepository;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mappingRepository"></param>
        /// <param name="templatesRepository"></param>
        /// <param name="projectsRepository"></param>
        /// <param name="accountsService"></param>
        /// <param name="projectsService"></param>
        /// <param name="templateService"></param>
        /// <param name="cacheManager"></param>
        public TemplatesManager(
            IMappingRepository mappingRepository,
            ITemplatesRepository templatesRepository,
            IProjectsRepository projectsRepository,
            IAccountsService accountsService,
            IProjectsService projectsService,
            ITemplatesService templateService,
            ICacheManager cacheManager) : base(accountsService, projectsService, templateService, cacheManager)
        {
            _mappingRepository = mappingRepository;
            _projectsRepository = projectsRepository;
            _templatesRepository = templatesRepository;
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
        /// <returns></returns>
        public TemplateMappingModel GetTemplateMappingModel()
        {
            var model = new TemplateMappingModel();
            var account = GetAccount();

            var projects = GetProjects(account.Id);
            foreach (var project in projects)
            {
                var p = new GcProjectModel
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name
                };
                var templates = TemplatesService.GetTemplates(project.Id.ToString());
                if (templates.Data.Count > 0)
                {
                    foreach (var template in templates.Data)
                    {
                        p.Templates.Add(new GcTemplateModel
                        {
                            TemplateName = template.Name,
                            TemplateId = template.Id,
                        });
                    }

                    model.Projects.Add(p);
                }
            }
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public void PostTemplate(TemplateMappingModel model)
        {
            //TODO: uses sitecore item
            //foreach (var project in model.Projects)
            //{
            //    var gcSelectedTemplates = project.Templates.Where(gcTemplateModel => model.Selected.Contains(gcTemplateModel.TemplateId)).ToList();
            //    if (gcSelectedTemplates.Any())
            //    {
            //        _projectsRepository.CreateOrGetProject(_accountSettings.AccountItemId, new Project { Name = project.ProjectName, Id = project.ProjectId });

            //        foreach (var gcTemplate in gcSelectedTemplates)
            //        {
            //            _mappingRepository.CreateMapping(project.ProjectId.ToString(), new TemplateMapping
            //            {
            //                Name = gcTemplate.TemplateName,
            //                GcTemplateId = gcTemplate.TemplateId.ToString()
            //            });
            //        }
            //    }
            //}
        }
    }
}
