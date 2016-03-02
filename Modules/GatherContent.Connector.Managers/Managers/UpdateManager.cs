using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Update;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.SitecoreRepositories.Repositories;
using Sitecore.Diagnostics;

namespace GatherContent.Connector.Managers.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class UpdateManager : BaseManager, IUpdateManager
    {
        protected IItemsRepository _itemsRepository;

        protected IItemsService _itemsService;
        
        protected IMappingManager _mappingManager;

        protected GCAccountSettings _gcAccountSettings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemsRepository"></param>
        /// <param name="itemsService"></param>
        /// <param name="mappingManager"></param>
        /// <param name="accountsService"></param>
        /// <param name="projectsService"></param>
        /// <param name="templateService"></param>
        /// <param name="cacheManager"></param>
        /// <param name="gcAccountSettings"></param>
        public UpdateManager(
            IItemsRepository itemsRepository,
            IItemsService itemsService,
            IMappingManager mappingManager,
            IAccountsService accountsService,
            IProjectsService projectsService,
            ITemplatesService templateService,
            ICacheManager cacheManager,
            GCAccountSettings gcAccountSettings) : base(accountsService, projectsService, templateService, cacheManager)
        {
            _itemsRepository = itemsRepository;

            _itemsService = itemsService;

            _mappingManager = mappingManager;

            _gcAccountSettings = gcAccountSettings;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public SelectItemsForUpdateModel GetItemsForUpdate(string itemId)
        {
            List<CMSUpdateItem> cmsItems = _itemsRepository.GetItemsForUpdate(itemId);

            List<GCTemplate> templates;
            List<GCStatus> statuses;
            List<UpdateListItem> models;
            List<Project> projects;
            TryToGetModelData(cmsItems, out templates, out statuses, out models, out projects);

            var result = new SelectItemsForUpdateModel(models, statuses, templates, projects);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmsItems"></param>
        /// <param name="templates"></param>
        /// <param name="statuses"></param>
        /// <param name="items"></param>
        /// <param name="projects"></param>
        /// <returns></returns>
        private bool TryToGetModelData(List<CMSUpdateItem> cmsItems, out List<GCTemplate> templates, out List<GCStatus> statuses, out List<UpdateListItem> items, out List<Project> projects)
        {
            var projectsDictionary = new Dictionary<int, Project>();
            var templatesDictionary = new Dictionary<int, GCTemplate>();

            statuses = new List<GCStatus>();
            items = new List<UpdateListItem>();

            foreach (CMSUpdateItem cmsItem in cmsItems)
            {
                if (!string.IsNullOrEmpty(cmsItem.GCItemId))
                {
                    ItemEntity entity = null;
                    try
                    {
                        entity = _itemsService.GetSingleItem(cmsItem.GCItemId);
                    }
                    catch (WebException exception)
                    {
                        Log.Error("GatherContent message. Api Server error has happened during getting Item with id = " + cmsItem.GCItemId, exception);
                        using (var response = exception.Response)
                        {
                            var httpResponse = (HttpWebResponse)response;
                            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                throw;
                            }
                        }
                    }
                    if (entity != null)
                    {
                        GCItem gcItem = entity.Data;
                        Project project = GetProject(projectsDictionary, gcItem.ProjectId);
                        if (gcItem.TemplateId.HasValue)
                        {
                            GCTemplate template = GetTemplate(templatesDictionary, gcItem.TemplateId.Value);

                            string gcLink = null;
                            if (!string.IsNullOrEmpty(_gcAccountSettings.GatherContentUrl))
                            {
                                gcLink = _gcAccountSettings.GatherContentUrl + "/item/" + gcItem.Id;
                            }
                            var dateFormat = _gcAccountSettings.DateFormat;
                            if (string.IsNullOrEmpty(dateFormat))
                            {
                                dateFormat = Constants.DateFormat;
                            }
                            var cmsLink =
                                string.Format(
                                    "http://{0}/sitecore/shell/Applications/Content Editor?fo={1}&sc_content=master&sc_bw=1",
                                    HttpContext.Current.Request.Url.Host, cmsItem.CMSId);
                            var listItem = new UpdateListItem(gcItem, template, cmsItem, dateFormat, project.Name,
                                cmsLink, gcLink);
                            items.Add(listItem);

                            GCStatus status = gcItem.Status.Data;
                            if (statuses.All(i => i.Id != status.Id))
                            {
                                statuses.Add(status);
                            }
                        }

                    }

                }
            }

            items = items.OrderBy(item => item.Status.Name).ToList();
            templates = templatesDictionary.Values.ToList();
            projects = projectsDictionary.Values.ToList();

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templates"></param>
        /// <param name="templateId"></param>
        /// <returns></returns>
        private GCTemplate GetTemplate(Dictionary<int, GCTemplate> templates, int templateId)
        {
            GCTemplate template;
            templates.TryGetValue(templateId, out template);

            if (template == null)
            {
                template = GetGcTemplateEntity(templateId.ToString()).Data;
                templates.Add(templateId, template);
            }

            return template;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private Project GetProject(Dictionary<int, Project> projects, int projectId)
        {
            Project project;
            projects.TryGetValue(projectId, out project);

            if (project == null)
            {
                project = GetGcProjectEntity(projectId.ToString()).Data;
                projects.Add(projectId, project);
            }

            return project;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="models"></param>
        /// <returns></returns>
        public UpdateResultModel UpdateItems(string itemId, List<UpdateListIds> models)
        {
            List<GCItem> gcItems = GetGCItemsByModels(models);
            List<MappingResultModel> resultItems = _mappingManager.MapItems(gcItems);
            List<MappingResultModel> successfulyUpdated = resultItems.Where(i => i.IsImportSuccessful).ToList();

            _itemsRepository.UpdateItems(successfulyUpdated);

            var result = new UpdateResultModel(resultItems);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        private List<GCItem> GetGCItemsByModels(List<UpdateListIds> models)
        {
            var result = new List<GCItem>();

            foreach (var item in models)
            {
                GCItem gcItem = _itemsService.GetSingleItem(item.GCId).Data;
                var gcItemWithCmsId = new UpdateGCItem(gcItem, item.CMSId);
                result.Add(gcItemWithCmsId);
            }

            return result;
        }
    }
}
