using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.SitecoreRepositories;

namespace GatherContent.Connector.Managers.Managers
{
    public class ImportManager : BaseManager
    {
        private readonly ItemsRepository _itemsRepository;
        private readonly ItemsService _itemsService;
        private readonly ProjectsService _projectsService;
        private readonly TemplatesService _templatesService;

        private readonly MappingManager _mappingManager;
        private readonly GCAccountSettings _gcAccountSettings;

        public ImportManager()
        {
            _itemsRepository = new ItemsRepository();

            var accountsRepository = new AccountsRepository();
            _gcAccountSettings = accountsRepository.GetAccountSettings();

            _itemsService = new ItemsService(_gcAccountSettings);
            _projectsService = new ProjectsService(_gcAccountSettings);
            _templatesService = new TemplatesService(_gcAccountSettings);

            _mappingManager = new MappingManager();
        }

        public SelectImportItemsModel GetModelForSelectImportItemsDialog(string itemId, string projectId)
        {
            Account account = GetAccount();

            List<Project> projects = GetProjects(account.Id);

            Project project = GetProject(projects, projectId);

            List<Template> templates = GetTemplates(project.Id);
            List<Status> statuses = GetStatuses(project.Id);
            List<GCItem> items = GetItems(project.Id);

            List<ItemModel> mappedItems = MapItems(items, templates);

            var result = new SelectImportItemsModel(mappedItems, project, projects, statuses, templates);

            return result;
        }

        private Project GetProject(List<Project> projects, string projectIdStr)
        {
            int projectId;
            int.TryParse(projectIdStr, out projectId);

            Project project = projectId != 0 ? projects.FirstOrDefault(i => i.Id == projectId) : projects.FirstOrDefault();

            return project;
        }

        private List<Status> GetStatuses(int projectId)
        {
            StatusesEntity statuses = _projectsService.GetAllStatuses(projectId.ToString());
            return statuses.Data;
        }

        private List<GCItem> GetItems(int projectId)
        {
            ItemsEntity items = _itemsService.GetItems(projectId.ToString());
            return items.Data;
        }

        private List<Template> GetTemplates(int projectId)
        {
            return GetTemplates(projectId.ToString());
        }

        private List<Template> GetTemplates(string projectId)
        {
            TemplatesEntity templates = _templatesService.GetTemplates(projectId);
            return templates.Data;
        }

        private List<ItemModel> MapItems(List<GCItem> items, List<Template> templates)
        {
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();

            var result = mappedItems.Select(i => new ItemModel(i, templates.FirstOrDefault(templ => templ.Id == i.TemplateId), items, _gcAccountSettings.DateFormat));

            return result.ToList();
        }


        public ImportResultModel ImportItems(string itemId, List<ItemModel> items, string projectId, string statusId)
        {
            List<GCItem> gcItems = MapItems(items);
            List<ImportItemsResponseModel> importResult = ImportNewItems(itemId, projectId, gcItems);

            if (!string.IsNullOrEmpty(statusId))
            {
                //List<ImportItemsResponseModel> successfulImportedItems = GetSuccessfulImportedItems(importResult);
                //PostNewStatusesForItems(successfulImportedItems, statusId);
            }

            var result = new ImportResultModel(importResult);

            return result;
        }

        private List<GCItem> MapItems(List<ItemModel> items)
        {
            List<GCItem> result = items.Select(GetGCItemByModel).ToList();
            return result;
        }

        private GCItem GetGCItemByModel(ItemModel model)
        {
            ItemEntity result = _itemsService.GetSingleItem(model.Id);

            return result.Data;
        }

        private List<ImportItemsResponseModel> ImportNewItems(string itemId, string projectId, List<GCItem> items)
        {
            List<ImportItemsResponseModel> cmsItems = _mappingManager.MapItems(items, projectId);

            //List<ImportItemsResponseModel> importResult = _itemsRepository.ImportItems(itemId, cmsItems);

            return cmsItems;
        }

        private List<ImportItemsResponseModel> GetSuccessfulImportedItems(List<ImportItemsResponseModel> importResult)
        {
            return importResult.Where(i => i.IsImportSuccessful).ToList();
        }

        private void PostNewStatusesForItems(List<ImportItemsResponseModel> items, string statusId)
        {
            foreach (ImportItemsResponseModel item in items)
            {
                _itemsService.ChooseStatusForItem(item.GCItemId, statusId);
            }
        }
    }
}
