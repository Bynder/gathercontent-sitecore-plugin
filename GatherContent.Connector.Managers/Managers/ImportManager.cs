using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.SitecoreRepositories.Repositories;

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

        
        public SelectItemsForImportModel GetModelForSelectImportItemsDialog(string itemId, string projectId)
        {
            Account account = GetAccount();

            List<Project> projects = GetProjects(account.Id);

            Project project = GetProject(projects, projectId);

            List<GCTemplate> templates = GetTemplates(project.Id);
            List<GCStatus> statuses = GetStatuses(project.Id);
            List<GCItem> items = GetItems(project.Id);

            List<ImportListItem> mappedItems = MapItems(items, templates);

            var result = new SelectItemsForImportModel(mappedItems, project, projects, statuses, templates);

            return result;
        }

        private Project GetProject(List<Project> projects, string projectIdStr)
        {
            int projectId;
            int.TryParse(projectIdStr, out projectId);

            Project project = projectId != 0 ? projects.FirstOrDefault(i => i.Id == projectId) : projects.FirstOrDefault();

            return project;
        }

        private List<GCStatus> GetStatuses(int projectId)
        {
            StatusesEntity statuses = _projectsService.GetAllStatuses(projectId.ToString());
            return statuses.Data;
        }

        private List<GCItem> GetItems(int projectId)
        {
            ItemsEntity items = _itemsService.GetItems(projectId.ToString());
            return items.Data;
        }

        private List<GCTemplate> GetTemplates(int projectId)
        {
            return GetTemplates(projectId.ToString());
        }

        private List<GCTemplate> GetTemplates(string projectId)
        {
            TemplatesEntity templates = _templatesService.GetTemplates(projectId);
            return templates.Data;
        }

        private List<ImportListItem> MapItems(List<GCItem> items, List<GCTemplate> templates)
        {
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();

            var result = mappedItems.Select(i => new ImportListItem(i, templates.FirstOrDefault(templ => templ.Id == i.TemplateId), items, _gcAccountSettings.DateFormat));

            return result.ToList();
        }


        public ImportResultModel ImportItems(string itemId, List<ImportListItem> items, string projectId, string statusId)
        {
            List<GCItem> gcItems = MapItems(items);
            List<MappingResultModel> cmsItems = _mappingManager.MapItems(gcItems, projectId);

            List<MappingResultModel> successfulImportedItems = GetSuccessfulImportedItems(cmsItems);
            _itemsRepository.ImportItems(itemId, successfulImportedItems);

            if (!string.IsNullOrEmpty(statusId))
            {
                //PostNewStatusesForItems(successfulImportedItems, statusId);
            }

            List<MappingResultModel> importedWithErrorItems = cmsItems.Except(successfulImportedItems).ToList();

            var result = new ImportResultModel(importedWithErrorItems);

            return result;
        }

        private List<GCItem> MapItems(List<ImportListItem> items)
        {
            List<GCItem> result = items.Select(GetGCItemByModel).ToList();
            return result;
        }

        private GCItem GetGCItemByModel(ImportListItem model)
        {
            ItemEntity result = _itemsService.GetSingleItem(model.Id);

            return result.Data;
        }

        private List<MappingResultModel> GetSuccessfulImportedItems(List<MappingResultModel> importResult)
        {
            return importResult.Where(i => i.IsImportSuccessful).ToList();
        }

        private void PostNewStatusesForItems(List<MappingResultModel> items, string statusId)
        {
            foreach (MappingResultModel item in items)
            {
                _itemsService.ChooseStatusForItem(item.GCItemId, statusId);
            }
        }
    }
}
