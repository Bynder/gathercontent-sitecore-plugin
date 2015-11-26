using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.IRepositories.Models.Mapping;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class MappingRepository : BaseSitecoreRepository, IMappingRepository
    {
        // private readonly ProjectsRepository _projectsRepository;


        public MappingRepository()
            : base()
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

        private Item GetFieldMappingItem(string templateMappingId, string name)
        {
            var parentItem = GetItem(templateMappingId);
            return parentItem.Axes.GetDescendant(name);
        }

        private void UpdateFieldMapping(Item field, string sitecoreFieldId)
        {
            using (new SecurityDisabler())
            {
                field.Editing.BeginEdit();
                field.Fields["Sitecore Field"].Value = sitecoreFieldId;
                field.Editing.EndEdit();
            }
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

        private void UpdateTemplateMapping(Item template, TemplateMapping templateMapping)
        {
            using (new SecurityDisabler())
            {
                template.Editing.BeginEdit();
                template.Fields["Sitecore Template"].Value = templateMapping.SitecoreTemplateId;
                template.Fields["Last Mapped Date"].Value = DateTime.Now.ToString();
                template.Editing.EndEdit();
            }
        }

        #endregion


        public List<CmsMappingModel> GetMappings()
        {
            var model = new List<CmsMappingModel>();
            var scProjects = GetAllProjects();
            var scMappings = GetAllMappings();


            foreach (var project in scProjects)
            {
                var templates = project.Axes.GetDescendants().Where(i => i.TemplateName == Constants.TemplateProxyName).ToList();
                if (templates.Count() > 0)
                {
                    foreach (var template in templates)
                    {
                        var mapping = new CmsMappingModel
                        {
                            GcProjectName = project.Name,
                            GcTemplateId = template["Temaplate Id"],
                            GcTemplateName = template.Name,
                        };

                        var m = scMappings.FirstOrDefault(map => map["GC Template"] == template["Temaplate Id"]);
                        if (m != null)
                        {
                            var scTemplate = GetItem(m["Sitecore Template"]);
                            mapping.CmsTemplateName = scTemplate != null ? scTemplate.Name : m["Sitecore Template"];
                            mapping.LastUpdatedDate = m["Last Updated in GC"];
                            mapping.LastMappedDateTime = m["Last Mapped Date"];
                            mapping.EditButtonTitle = "Edit";
                            mapping.IsMapped = true;
                        }
                        else
                        {
                            mapping.CmsTemplateName = "Not mapped";
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


        public object CreateMapping(TemplateMapping templateMapping)
        {
            throw new NotImplementedException();
        }



        public void UpdateMapping(int projectId, int templateId, TemplateMapping templateMappingModel, List<CmsTemplateField> fields)
        {
            var scProject = GetProjectFolder(projectId.ToString());

            var templateMapping = GetTemplateMapping(scProject.ID.ToString(), templateId.ToString());

            if (templateMapping != null)
            {
                UpdateTemplateMapping(templateMapping, templateMappingModel);

                foreach (var templateField in fields)
                {               
                        var field = GetFieldMappingItem(templateMapping.ID.ToString(), templateField.FieldName);
                        if (field != null)
                        {
                            UpdateFieldMapping(field, templateField.SelectedField);
                        }
                }
            }
        }

    }
}
