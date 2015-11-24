using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.Website.Extensions;
using GatherContent.Connector.Website.Models.Import;
using Sitecore.Services.Infrastructure.Web.Http;

namespace GatherContent.Connector.Website.Controllers
{
    public class ImportController : ServicesApiController
    {
        public ImportResponseModel Get(string id, string projectId)
        {
            var db = Sitecore.Configuration.Factory.GetDatabase("master");
            var item = db.GetItem(id);

            var gcSettings = GcAccountExtension.GetSettings(item);
            var service = new GatherContentService.GatherContentService(gcSettings.ApiUrl, gcSettings.Username, gcSettings.ApiKey);

            var accounts = service.GetAccounts();
            var account = accounts.Data.FirstOrDefault();
            var projects = service.GetProjects(account.Id);

            int intProjectId = Convert.ToInt32(projectId);
            Project project;
            if (intProjectId != 0)
            {
                project = projects.Data.FirstOrDefault(i => i.Id == intProjectId);
            }
            else
            {
                project = projects.Data.FirstOrDefault();
            }

            var items = service.GetItems(project.Id.ToString());

            var templates = service.GetTemplates(project.Id.ToString());

            ImportResponseModel result = new ImportResponseModel(project, items, projects, templates);

            return result;

        }

        public class ResImportModel
        {
            public List<ImportResultItem> Items { get; set; }
        }

        public class ImportResultItem
        {
            public bool IsImportSuccess { get; set; }

            public StatusModel Status { get; set; }

            public string Title { get; set; }

            public TemplateModel Template { get; set; }

        }

        public ResImportModel ImportItems(string id, string statusId, List<ItemModel> items)
        {
            return new ResImportModel
            {
                Items = items.Select(i => new ImportResultItem
                    {
                        IsImportSuccess = true,
                        Status = i.Status,
                        Template = i.Template,
                        Title = i.Title
                    }).ToList()
            };
        }
    }
}