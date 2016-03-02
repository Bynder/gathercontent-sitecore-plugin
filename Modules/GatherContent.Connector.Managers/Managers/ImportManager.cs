using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.SitecoreRepositories.Repositories;

namespace GatherContent.Connector.Managers.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class ImportManager : BaseManager, IImportManager
    {
        protected IItemsRepository _itemsRepository;
        protected IMappingRepository _mappingRepository;

        protected IItemsService _itemsService;

        protected IMappingManager _mappingManager;
        protected GCAccountSettings _gcAccountSettings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemsRepository"></param>
        /// <param name="mappingRepository"></param>
        /// <param name="itemsService"></param>
        /// <param name="accountsService"></param>
        /// <param name="projectsService"></param>
        /// <param name="templateService"></param>
        /// <param name="cacheManager"></param>
        /// <param name="mappingManager"></param>
        /// <param name="gcAccountSettings"></param>
        public ImportManager(
            IItemsRepository itemsRepository,
            IMappingRepository mappingRepository,
            IItemsService itemsService,
            IAccountsService accountsService,
            IProjectsService projectsService,
            ITemplatesService templateService,
            ICacheManager cacheManager,
            IMappingManager mappingManager,
            GCAccountSettings gcAccountSettings) : base(accountsService, projectsService, templateService, cacheManager)
        {
            _itemsRepository = itemsRepository;
            _mappingRepository = mappingRepository;
            
            _itemsService = itemsService;

            _mappingManager = mappingManager;

            _gcAccountSettings = gcAccountSettings;
        }


        #region Utilities

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="projectIdStr"></param>
        /// <returns></returns>
        private Project GetProject(List<Project> projects, string projectIdStr)
        {
            int projectId;
            int.TryParse(projectIdStr, out projectId);

            Project project = projectId != 0 ? projects.FirstOrDefault(i => i.Id == projectId) : projects.FirstOrDefault();

            return project;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private List<GCStatus> GetStatuses(int projectId)
        {
            StatusesEntity statuses = ProjectsService.GetAllStatuses(projectId.ToString());
            return statuses.Data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private List<GCItem> GetItems(int projectId)
        {
            ItemsEntity items = _itemsService.GetItems(projectId.ToString());
            return items.Data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private List<GCTemplate> GetTemplates(int projectId)
        {
            return GetTemplates(projectId.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private List<GCTemplate> GetTemplates(string projectId)
        {
            TemplatesEntity templates = TemplatesService.GetTemplates(projectId);
            return templates.Data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        private List<ImportListItem> MapItems(List<GCItem> items, List<GCTemplate> templates)
        {
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();
            var dateFormat = _gcAccountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }

            var result = new List<ImportListItem>();
            foreach (var mappedItem in mappedItems)
            {
                var mappings = _mappingRepository.GetAllMappingsForGcTemplate(mappedItem.ProjectId.ToString(),
                    mappedItem.TemplateId.ToString());
                var availableMappings = mappings.Select(availableMappingModel => new AvailableMapping
                {
                    Id = availableMappingModel.Id,
                    Title = !string.IsNullOrEmpty(availableMappingModel.Title) ?
                        availableMappingModel.Title : string.Format("[{0}]", availableMappingModel.Name),
                }).ToList();

                result.Add(new ImportListItem(mappedItem, templates.FirstOrDefault(templ => templ.Id == mappedItem.TemplateId), items, dateFormat, availableMappings));
            }

            return result.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        private List<ImportItembyLocation> MapItemsByLocation(List<GCItem> items, List<GCTemplate> templates)
        {
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();
            var dateFormat = _gcAccountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }

            var result = new List<ImportItembyLocation>();
            foreach (var mappedItem in mappedItems)
            {
                var mappings = _mappingRepository.GetAllMappingsForGcTemplate(mappedItem.ProjectId.ToString(), mappedItem.TemplateId.ToString());

                var availableMappings = mappings.Select(availableMappingModel => new AvailableMappingByLocation
                {
                    Id = availableMappingModel.Id,
                    Title = !string.IsNullOrEmpty(availableMappingModel.Title) ?
                        availableMappingModel.Title : string.Format("[{0}]", availableMappingModel.Name),
                    OpenerId = "drop-tree" + Guid.NewGuid(),
                    ScTemplate = availableMappingModel.ScTemplate,
                    IsShowing = false,
                    DefaultLocation = availableMappingModel.DefaultLocation,
                    DefaultLocationTitle = availableMappingModel.DefaultLocationTitle,
                }).ToList();

                result.Add(new ImportItembyLocation(mappedItem, templates.FirstOrDefault(templ => templ.Id == mappedItem.TemplateId), items, dateFormat, availableMappings));
            }

            return result.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private List<GCItem> MapItems(List<string> items)
        {
            List<GCItem> result = items.Select(GetGcItemByModel).ToList();
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private GCItem GetGcItemByModel(string id)
        {
            ItemEntity result = _itemsService.GetSingleItem(id);

            return result.Data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="importResult"></param>
        /// <returns></returns>
        private List<MappingResultModel> GetSuccessfulImportedItems(List<MappingResultModel> importResult)
        {
            return importResult.Where(i => i.IsImportSuccessful).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="statusId"></param>
        /// <param name="projectId"></param>
        private void PostNewStatusesForItems(List<MappingResultModel> items, string statusId, string projectId)
        {
            foreach (MappingResultModel item in items)
            {
                _itemsService.ChooseStatusForItem(item.GCItemId, statusId);
                var status = ProjectsService.GetSingleStatus(statusId, projectId);
                item.Status.Color = status.Data.Color;
                item.Status.Name = status.Data.Name;
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public SelectItemsForImportModel GetModelForSelectImportItemsDialog(string itemId, string projectId)
        {
            Account account = GetAccount();

            List<Project> projects = GetProjects(account.Id);

            Project project = GetProject(projects, projectId);

            List<GCTemplate> templates = GetTemplates(project.Id);
            List<GCStatus> statuses = GetStatuses(project.Id);
            List<GCItem> items = GetItems(project.Id);
            items = items.OrderBy(item => item.Status.Data.Name).ToList();

            List<ImportListItem> mappedItems = MapItems(items, templates);

            var result = new SelectItemsForImportModel(mappedItems, project, projects, statuses, templates);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public SelectItemsForImportWithLocation GetDialogModelWithLocation(string itemId, string projectId)
        {
            Account account = GetAccount();

            List<Project> projects = GetProjects(account.Id);

            Project project = GetProject(projects, projectId);

            List<GCTemplate> templates = GetTemplates(project.Id);
            List<GCStatus> statuses = GetStatuses(project.Id);
            List<GCItem> items = GetItems(project.Id);
            items = items.OrderBy(item => item.Status.Data.Name).ToList();

            List<ImportItembyLocation> mappedItems = MapItemsByLocation(items, templates);

            var result = new SelectItemsForImportWithLocation(mappedItems, project, projects, statuses, templates);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="items"></param>
        /// <param name="projectId"></param>
        /// <param name="statusId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public ImportResultModel ImportItems(string itemId, List<ImportItemModel> items, string projectId, string statusId, string language)
        {
            List<MappingResultModel> cmsItems = _mappingManager.MapItems(items);

            if (cmsItems == null) return null;
            List<MappingResultModel> successfulImportedItems = GetSuccessfulImportedItems(cmsItems);
            successfulImportedItems = _itemsRepository.ImportItems(itemId, language, successfulImportedItems);

            if (!string.IsNullOrEmpty(statusId))
            {
                PostNewStatusesForItems(successfulImportedItems, statusId, projectId);
            }

            var result = new ImportResultModel(cmsItems);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="projectId"></param>
        /// <param name="statusId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public ImportResultModel ImportItemsWithLocation(List<LocationImportItemModel> items, string projectId, string statusId, string language)
        {
            var importItems = new List<ImportItemModel>();

            if (items == null) return null;
            
            foreach (var item in items)
            {
                if (item.IsImport)
                {
                    importItems.Add(new ImportItemModel
                    {
                        Id = item.Id,
                        SelectedMappingId = item.SelectedMappingId,
                        DefaultLocation = item.SelectedLocation
                    });
                }
            }

            List<MappingResultModel> cmsItems = _mappingManager.MapItems(importItems);
            if (cmsItems == null) return null;
            List<MappingResultModel> successfulImportedItems = GetSuccessfulImportedItems(cmsItems);

            successfulImportedItems = _itemsRepository.ImportItemsWithLocation(language, successfulImportedItems);

            if (!string.IsNullOrEmpty(statusId))
            {
                PostNewStatusesForItems(successfulImportedItems, statusId, projectId);
            }

            var result = new ImportResultModel(cmsItems);

            return result;
        }
    }
}
