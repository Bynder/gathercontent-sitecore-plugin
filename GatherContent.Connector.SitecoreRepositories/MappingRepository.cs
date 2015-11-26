using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.IRepositories.Models.Mapping;
using Sitecore.Data.Items;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class MappingRepository : BaseSitecoreRepository, IMappingRepository
    {
       // private readonly ProjectsRepository _projectsRepository;
        

        public MappingRepository() : base()
        {
            //_projectsRepository = new ProjectsRepository();
        }


        #region Utilities

        private Item GetProjectFolder(string projectId)
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var projectsFolder = accountSettingItem.Axes.SelectSingleItem(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectFolderTemplateName));
            var project = projectsFolder.Axes.GetDescendants().FirstOrDefault(i => i["Id"] == projectId);
            return project;
        }


        private Item GetMappingFolder(Item projectFolder)
        {
            Item mappingFolder = projectFolder.Children.FirstOrDefault(i => i.Template.Name == Constants.MappingFolderTemplateName);
            return mappingFolder;
        }

        private IEnumerable<Item> GetTemplateMappings(Item mappingFolder)
        {
            return mappingFolder.Children;
        }

        private Item GetTemplateMapping(string projectId, string gcTemplateId)
        {
            var project = GetProjectFolder(projectId);
            
            if (project != null)
            {
                return project.Axes.GetDescendants()
                    .FirstOrDefault(item => item["GC Template"] == gcTemplateId &
                                            item.TemplateID ==
                                            new Sitecore.Data.ID(Constants.TemplateId));

            }
            return null;
        }

        private IEnumerable<MappingTemplateModel> ConvertSitecoreTemplatesToModel(IEnumerable<Item> templates)
        {
            IEnumerable<MappingTemplateModel> result = templates.Select(ConvertSitecoreTemplateToModel);
            return result;
        }

        private MappingTemplateModel ConvertSitecoreTemplateToModel(Item template)
        {
            IEnumerable<MappingFieldModel> fields = ConvertSitecoreFieldsToModel(template.Children);
            var result = new MappingTemplateModel(template["Sitecore Template"], template["GC Template"], fields.ToList());

            return result;
        }

        private IEnumerable<MappingFieldModel> ConvertSitecoreFieldsToModel(IEnumerable<Item> fields)
        {
            IEnumerable<MappingFieldModel> result = fields.Select(ConvertSitecoreFieldToModel);
            return result;
        }

        private MappingFieldModel ConvertSitecoreFieldToModel(Item field)
        {
            var result = new MappingFieldModel(field["Sitecore Field"], field["GC Field"]);
            return result;
        }



        private Item GetFieldMapping(Item mapping, string fieldName)
        {
            return mapping.Axes.GetDescendants().FirstOrDefault(item => item["GC Field"] == fieldName);
        }

        #endregion


        public List<MappingTemplateModel> GetTemplateMappings(string projectId)
        {
            Item projectFolder = GetProjectFolder(projectId);
            Item mappingFolder = GetMappingFolder(projectFolder);
            IEnumerable<Item> mappings = GetTemplateMappings(mappingFolder);

            IEnumerable<MappingTemplateModel> result = ConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }



        public AddMapping GetAddMappingModel(string projectId, TemplateEntity template)
        {
            var model = new AddMapping { GcTemplateId = template.Data.Id.ToString(), IsEdit = false };

            var scMapping = GetTemplateMapping(projectId, template.Data.Id.ToString());


            if (scMapping != null)
            {
                model.IsEdit = true;
                model.SelectedTemplateId = scMapping["Sitecore Template"];
            }


            foreach (var config in template.Data.Config)
            {
                var tab = new CmsTemplateTab { TabName = config.Label };
                foreach (var element in config.Elements)
                {
                    var tm = new CmsTemplateField { FieldName = element.Label };

                    if (scMapping != null)
                    {
                        var scField = GetFieldMapping(scMapping, element.Label);
                        if (scField != null) tm.SelectedField = scField["Sitecore Field"];
                    }

                    tab.Fields.Add(tm);
                }
                model.Tabs.Add(tab);
            }
            return model;
        }

    }
}
