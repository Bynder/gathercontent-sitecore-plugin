using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.SitecoreRepositories;

namespace GatherContent.Connector.Managers.Managers
{
    public class UpdateManager : BaseManager
    {
         private readonly ItemsRepository _itemsRepository;
        private readonly ItemsService _itemsService;
        private readonly ProjectsService _projectsService;
        private readonly TemplatesService _templatesService;
        private readonly GCAccountSettings _gcAccountSettings;
        private readonly MappingManager _mappingManager;

        public UpdateManager()
        {
            _itemsRepository = new ItemsRepository();

            var accountsRepository = new AccountsRepository();
            _gcAccountSettings = accountsRepository.GetAccountSettings();

            _itemsService = new ItemsService(_gcAccountSettings);
            _projectsService = new ProjectsService(_gcAccountSettings);
            _templatesService = new TemplatesService(_gcAccountSettings);

            _mappingManager = new MappingManager();
        }

        private List<GCItem> MapItems(List<CMSUpdateItem> items)
        {
            List<GCItem> result = items.Select(GetGCItemByModel).ToList();
            return result;
        }

        private GCItem GetGCItemByModel(CMSUpdateItem model)
        {
            ItemEntity result = _itemsService.GetSingleItem(model.GSItemId);

            return result.Data;
        }

        public SelectImportItemsModel GetItemsForUpdate(string itemId, string projectId)
        {
            Account account = GetAccount();

            List<Project> projects = GetProjects(account.Id);

            Project project = GetProject(projects, projectId);

            List<Template> templates = GetTemplates(project.Id);
            List<Status> statuses = GetStatuses(project.Id);
            
            var resultitems = new List<ItemModel>();
            List<CMSUpdateItem> cmsItems = _itemsRepository.GetItems(itemId);
            List<GCItem> items = MapItems(cmsItems);

            foreach (var gcItem in items)
            {
                var template = _templatesService.GetSingleTemplate(gcItem.TemplateId.ToString()).Data;
                resultitems.Add(new ItemModel(gcItem, template, cmsItems.FirstOrDefault(i => i.GSItemId == gcItem.Id.ToString()), _gcAccountSettings.DateFormat));
            }

            var result = new SelectImportItemsModel(resultitems, project, projects, statuses, templates);
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
        
        private List<Template> GetTemplates(int projectId)
        {
            return GetTemplates(projectId.ToString());
        }

        private List<Template> GetTemplates(string projectId)
        {
            TemplatesEntity templates = _templatesService.GetTemplates(projectId);
            return templates.Data;
        }

        public ImportResultModel UpdateItems(string itemId, List<ItemModel> items, string projectId)
        {
            var resultItems = new List<ImportItemsResponseModel>();
            Thread.Sleep(2000);
            foreach (var gcItem in items)
            {
                resultItems.Add(new ImportItemsResponseModel(gcItem.Id, gcItem.Status, gcItem.Title,
                    gcItem.Template.Name, "View in Sitecore", true, null, null));
            }

            var result = new ImportResultModel(resultItems);

            return result;
        }
    }
}
