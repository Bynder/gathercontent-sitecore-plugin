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
        private const string SitecoreTemplateId = "{6E9920D6-B60E-4518-822E-4D57CD426148}";//"{3C1715FE-6A13-4FCF-845F-DE308BA9741D}";

        private readonly MappingManager _mappingManager;

        public MappingsController()
        {
            _mappingManager = new MappingManager();
        }


        public List<MappingModel> Get(string id)
        {
            var model = new List<MappingModel>();
          
            var db = Sitecore.Configuration.Factory.GetDatabase("master");
            var item = db.GetItem(id);
            var mappings = item.Axes.GetDescendants().Where(i => i.TemplateName == "GC Template Mapping").ToList();
            var projects = item.Axes.GetDescendants().Where(i => i.TemplateName == "GC Project");

            foreach (var project in projects)
            {
                var templates = project.Axes.GetDescendants().Where(i => i.TemplateName == "GC Template Proxy").ToList();
                if (templates.Count() > 0)
                {
                    foreach (var template in templates)
                    {
                        var mapping = new MappingModel
                        {
                            GcProjectName = project.Name,
                            GcTemplateId = template["Temaplate Id"],
                            GcTemplateName = template.Name,
                        };

                        var m = mappings.FirstOrDefault(map => map["GC Template"] == template["Temaplate Id"]);
                        if (m != null)
                        {
                            var scTemplate = db.GetItem(m["Sitecore Template"]);
                            mapping.ScTemplateName = scTemplate != null ? scTemplate.Name : m["Sitecore Template"];
                            mapping.LastUpdatedDate = m["Last Updated in GC"];
                            mapping.LastMappedDateTime = m["Last Mapped Date"];
                            mapping.EditButtonTitle = "Edit";
                            mapping.IsMapped = true;
                        }
                        else
                        {
                            mapping.ScTemplateName = "Not mapped";
                            mapping.LastMappedDateTime = "never";
                            mapping.EditButtonTitle = "Setup mapping";
                            mapping.IsMapped = false;
                        }

                        model.Add(mapping);
                    }
                }
            }

            return model;

        }

        public TemplateMapModel GetMapping(string id)
        {
            var model = _mappingManager.GetMappingModel(id);
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

