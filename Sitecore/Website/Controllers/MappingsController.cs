using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.Website.Extensions;
using GatherContent.Connector.Website.Managers;
using GatherContent.Connector.Website.Models.Mapping;
using Sitecore.Services.Infrastructure.Web.Http;
using AddMappingModel = GatherContent.Connector.Website.Models.Mapping.AddMappingModel;

namespace GatherContent.Connector.Website.Controllers
{
    public class MappingsController : ServicesApiController
    {
        private const string AccountItemId = "{B99D89BD-56AB-4F41-BB02-121D116E5145}";
       
        private readonly MappingManager _mappingManager;

        public MappingsController()
        {
            _mappingManager = new MappingManager();
        }


        public List<MappingModel> Get()
        {
            var model = _mappingManager.GetMappingModel();
            return model;
        }



        public TemplateMapModel GetMapping(string id)
        {
            var model = _mappingManager.GetTemplateMappingModel(id);
            return model;
        }



        public HttpResponseMessage Post(AddMappingModel model)
        {
            var db = Sitecore.Configuration.Factory.GetDatabase("master");
            var accountItem = db.GetItem(AccountItemId);

            var manager = new SitecoreDataManager(accountItem.Database, accountItem.Language);
            var gcSettings = GcAccountExtension.GetSettings(accountItem);
            var service = new GatherContentService.GatherContentService(gcSettings.ApiUrl, gcSettings.Username,
                gcSettings.ApiKey);
            var template = service.GetSingleTemplate(model.GcTemplateId);
            var project = service.GetSingleProject(template.Data.ProjectId.ToString());
            var scProject = manager.GetOrCreateProjectFolder(accountItem.ID.ToString(), new Project
            {
                Id = project.Data.Id,
                Name = project.Data.Name
            });

            if (model.IsEdit)
            {
                var templateMapping = manager.GetTemplateMappingItem(scProject.ID.ToString(), template.Data.Id.ToString());
                if (templateMapping != null)
                {
                    manager.UpdateTemplateMapping(templateMapping, new TemplateMapping
                    {
                        //GcTemplateId = model.GcTemplateId,
                        SitecoreTemplateId = model.SelectedTemplateId,
                        Name = template.Data.Name,
                        //LastUpdated = template.Data.Updated.ToString()
                    });

                    foreach (var tab in model.Tabs)
                    {
                        foreach (var templateField in tab.Fields)
                        {
                            var field = manager.GetFieldMappingItem(templateMapping.ID.ToString(), templateField.FieldName);
                            if (field != null)
                            {
                                manager.UpdateFieldMapping(field, templateField.SelectedField);
                            }
                        }
                    }
                }
            }
            else
            {
                var templateMapping = manager.CreateTemplateMapping(scProject.ID.ToString(), new TemplateMapping
                {
                    GcTemplateId = model.GcTemplateId,
                    SitecoreTemplateId = model.SelectedTemplateId,
                    Name = template.Data.Name,
                    LastUpdated = template.Data.Updated.ToString()
                });


                foreach (var tab in model.Tabs)
                {
                    foreach (var templateField in tab.Fields)
                    {
                        manager.CreateFieldMapping(templateMapping.ID.ToString(), new FieldMapping
                        {
                            GcField = templateField.FieldName,
                            SitecoreFieldId = templateField.SelectedField,
                        });
                    }
                }
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, model);
            return response;
        }
    }
}

