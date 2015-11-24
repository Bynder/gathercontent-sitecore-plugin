using System.Linq;
using System.Net;
using System.Net.Http;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.Website.Extensions;
using GatherContent.Connector.Website.Managers;
using GatherContent.Connector.Website.Models.Template;
using Sitecore.Services.Infrastructure.Web.Http;

namespace GatherContent.Connector.Website.Controllers
{
    public class TemplatesMappingController : ServicesApiController
    {
        private const string AccountItemId = "{B99D89BD-56AB-4F41-BB02-121D116E5145}";
        public TemplateMappingModel Get(string id)
        {
            var model = new TemplateMappingModel();

            var db = Sitecore.Configuration.Factory.GetDatabase("master");

            var item = db.GetItem(id);

            var gcSettings = GcAccountExtension.GetSettings(item);

            var service = new GatherContentService.GatherContentService(gcSettings.ApiUrl, gcSettings.Username, gcSettings.ApiKey);
            var accounts = service.GetAccounts();

            var account = accounts.Data.FirstOrDefault();
            if (account != null)
            {
                var projects = service.GetProjects(account.Id);
                foreach (var project in projects.Data)
                {
                    var p = new GcProjectModel
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name
                    };
                    var templates = service.GetTemplates(project.Id.ToString());
                    foreach (var template in templates.Data)
                    {
                        p.Templates.Add(new GcTemplateModel
                        {
                            TemplateName = template.Name,
                            TemplateId =  template.Id
                        });
                    }
                    model.Projects.Add(p);
                }
            }

            return model;

        }

        public HttpResponseMessage Post(TemplateMappingModel model)
        {
            var db = Sitecore.Configuration.Factory.GetDatabase("master");

            var accountItem = db.GetItem(AccountItemId);

            var manager = new SitecoreDataManager(accountItem.Database, accountItem.Language);
            

            foreach (var project in model.Projects)
            {
                var scProject = manager.AddProjectFolder(accountItem.ID.ToString(), new Project
                    {
                        Id = project.ProjectId,
                        Name = project.ProjectName
                    });

                foreach (var gcTemplateModel in project.Templates.Where(gcTemplateModel => model.Selected.Contains(gcTemplateModel.TemplateId)))
                {
                    manager.CreateTemplate(scProject.ID.ToString(), gcTemplateModel);
                }
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, model);
            return response;  
        }
    }

}

