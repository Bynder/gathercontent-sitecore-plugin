using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
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

        public ImportManager()
        {
            _itemsRepository = new ItemsRepository();

            var accountsRepository = new AccountsRepository();
            var accountSettings = accountsRepository.GetAccountSettings();

            _itemsService = new ItemsService(accountSettings);
            _accountsService = new AccountsService(accountSettings);
            _projectsService = new ProjectsService(accountSettings);
            _templatesService = new TemplatesService(accountSettings);
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
            TemplatesEntity templates = _templatesService.GetTemplates(projectId.ToString());
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
            var result = new List<GCItem>();

            return result;
        }

        public ImportResultItemModel ImportItems(string itemId, List<ItemModel> items, string statusId)
        {
            List<GCItem> gcItems = MapItems(items);
            ImportNewItems(itemId, gcItems);
            PostNewStatusesForItems(gcItems, statusId);
            return GetImportResultModel(gcItems);
        }

        private void ImportNewItems(string itemId, List<GCItem> items)
        {
            Dictionary<GCItem, bool> importResult = _itemsRepository.ImportItems(itemId, items);
        }

        private void PostNewStatusesForItems(List<GCItem> items, string statusId)
        {

        }

        private ImportResultItemModel GetImportResultModel(List<GCItem> items)
        {
            var result = new ImportResultItemModel();

            return result;
        }
    }
}
