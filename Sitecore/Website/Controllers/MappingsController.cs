using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.Website.Extensions;
using GatherContent.Connector.Website.Managers;
using GatherContent.Connector.Website.Models;
using GatherContent.Connector.Website.Models.Mapping;
using GatherContent.Connector.Website.Models.Template;
using Sitecore.Configuration;
using Sitecore.Data.Managers;
using Sitecore.Services.Infrastructure.Web.Http;

namespace GatherContent.Connector.Website.Controllers
{
    public class MappingsController : ServicesApiController
    {
        private const string AccountItemId = "{B99D89BD-56AB-4F41-BB02-121D116E5145}";
        private const string SitecoreTemplateId = "{6E9920D6-B60E-4518-822E-4D57CD426148}";//"{3C1715FE-6A13-4FCF-845F-DE308BA9741D}";

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
                            LastUpdatedDate = DateTime.Now
                        };

                        var m = mappings.FirstOrDefault(map => map["GC Template"] == template["Temaplate Id"]);
                        if (m != null)
                        {
                            mapping.ScTemplateName = m["Sitecore Template"];
                            mapping.LastMappedDateTime = m["Last Updated in GC"];
                            mapping.EditButtonTitle = "Edit";
                        }
                        else
                        {
                            mapping.ScTemplateName = "Not mapped";
                            mapping.LastMappedDateTime = "never";
                            mapping.EditButtonTitle = "Setup mapping";
                        }

                        model.Add(mapping);
                    }
                }
            }

            return model;

        }

        public GcScMapModel GetMapping(string id, string projectName)
        {
            var model = new GcScMapModel { GcProjectName = projectName };

            var db = Sitecore.Configuration.Factory.GetDatabase("master");
            var accountItem = db.GetItem(AccountItemId);
            var gcSettings = GcAccountExtension.GetSettings(accountItem);
            var service = new GatherContentService.GatherContentService(gcSettings.ApiUrl, gcSettings.Username, gcSettings.ApiKey);
            var template = service.GetSingleTemplate(id);
            var scTemplates = db.GetItem(SitecoreTemplateId).Axes.GetDescendants().Where(item => item.TemplateName == "Template").ToList();
            
            model.GcTemplateName = template.Data.Name;
            model.GcTemplateId = template.Data.Id;
            
            foreach (var config in template.Data.Config)
            {
                var tab = new TemplateTab { TabName = config.Label };
                foreach (var element in config.Elements)
                {
                    tab.Fields.Add(new TemplateField { FieldName = element.Label});
                }
                model.Tabs.Add(tab);

            }
            foreach (var scTemplate in scTemplates)
            {
                var sitecoreTemplate = new SitecoreTemplate
                {
                    SitrecoreTemplateName = scTemplate.Name,
                    SitrecoreTemplateId = scTemplate.ID.ToString()
                };


                var fields = TemplateManager.GetTemplate(scTemplate.ID, Factory.GetDatabase("master")).GetFields();
                sitecoreTemplate.SitecoreFields.AddRange(
                    from f in fields
                    where !f.Name.StartsWith("__")
                    select new SitecoreTemplateField
                    {
                        SitrecoreFieldName = f.Name,
                        SitecoreFieldId = f.ID.ToString()
                    });
           

                model.SitecoreTemplates.Add(sitecoreTemplate);
            }


            return model;

        }



        public HttpResponseMessage Post(AddMappingModel model)
        {
            var db = Sitecore.Configuration.Factory.GetDatabase("master");

            var accountItem = db.GetItem(AccountItemId);

            var manager = new SitecoreDataManager(accountItem.Database, accountItem.Language);
            var gcSettings = GcAccountExtension.GetSettings(accountItem);
            var service = new GatherContentService.GatherContentService(gcSettings.ApiUrl, gcSettings.Username, gcSettings.ApiKey);
            var template = service.GetSingleTemplate(model.GcTemplateId);
            var project = service.GetSingleProject(template.Data.ProjectId.ToString());
            var scProject = manager.AddProjectFolder(accountItem.ID.ToString(), new Project
            {
                Id = project.Data.Id,
                Name = project.Data.Name
            });

           var templateMapping = manager.CreateTemplateMapping(scProject.ID.ToString(), new TemplateMapping
            {
                GcProjectId = project.Data.Id.ToString(),
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

 

            var response = Request.CreateResponse(HttpStatusCode.OK, model);
            return response;
        }


     
    }
}

