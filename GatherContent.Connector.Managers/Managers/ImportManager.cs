using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.SitecoreRepositories;

namespace GatherContent.Connector.Managers.Managers
{
    public class ImportManager
    {
        private readonly ItemsRepository _itemsRepository;
        private readonly ItemsService _itemsService;
        private readonly AccountsService _accountsService;
        private readonly ProjectsService _projectsService;
        private readonly TemplatesService _templatesService;

        private readonly MappingManager _mappingManager;

        public ImportManager()
        {
            _itemsRepository = new ItemsRepository();

            var accountsRepository = new AccountsRepository();
            var accountSettings = accountsRepository.GetAccountSettings();

            _itemsService = new ItemsService(accountSettings);
            _accountsService = new AccountsService(accountSettings);
            _projectsService = new ProjectsService(accountSettings);
            _templatesService = new TemplatesService(accountSettings);

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

        private Account GetAccount()
        {
            AccountEntity accounts = _accountsService.GetAccounts();
            Account account = accounts.Data.FirstOrDefault();

            return account;
        }

        private List<Project> GetProjects(int accountId)
        {
            ProjectsEntity projects = _projectsService.GetProjects(accountId);

            return projects.Data;
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

            var result = mappedItems.Select(i => new ItemModel(i, templates.FirstOrDefault(templ => templ.Id == i.TemplateId), items));

            return result.ToList();
        }

        private List<GCItem> MapItems(List<ItemModel> items)
        {
            List<GCItem> result = items.Select(i => _itemsService.GetSingleItem(i.Id.ToString()).Data).ToList();
            return result;
        }

        public ImportResultModel ImportItems(string itemId, List<ItemModel> items, string projectId, string statusId)
        {
            //List<GCItem> gcItems = MapItems(items);
            //List<ImportItemsResponseModel> importResult = ImportNewItems(itemId, projectId, gcItems);

            //if (!string.IsNullOrEmpty(statusId))
            //{
            //    List<GCItem> successfulImportedItems = GetSuccessfulImportedItems(importResult);
            //    //PostNewStatusesForItems(successfulImportedItems, statusId);
            //}

            //return GetImportResultModel(importResult, projectId);

            return new ImportResultModel(new List<ImportResultItemModel>());
        }

        private List<ImportItemsResponseModel> ImportNewItems(string itemId, string projectId, List<GCItem> items)
        {
            List<ImportCMSItem> cmsItems = _mappingManager.MappingItems(items, projectId);

            List<ImportItemsResponseModel> importResult = _itemsRepository.ImportItems(itemId, cmsItems);
            
            return importResult;
        }

        private List<GCItem> GetSuccessfulImportedItems(List<ImportItemsResponseModel> importResult)
        {
            return importResult.Where(i => i.IsImportSuccessful).Select(i => i.Item).ToList();
        }

        private void PostNewStatusesForItems(List<GCItem> items, string statusId)
        {
            foreach (GCItem item in items)
            {
                _itemsService.ChooseStatusForItem(item.Id.ToString(), statusId);
            }
        }

        private ImportResultModel GetImportResultModel(List<ImportItemsResponseModel> importResult, string projectId)
        {
            var importedItemResult = new List<ImportResultItemModel>();

            List<Template> templates = GetTemplates(projectId);

            foreach (ImportItemsResponseModel model in importResult)
            {
                Template template = templates.FirstOrDefault(templ => templ.Id == model.Item.TemplateId);

                var itemRes = new ImportResultItemModel(model.Item, template, model.IsImportSuccessful, model.Message);
                importedItemResult.Add(itemRes);
            }

            var result = new ImportResultModel(importedItemResult);

            return result;
        }
    }
}
