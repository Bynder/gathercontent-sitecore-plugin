using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Mapping;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    public class MappingRepository : BaseSitecoreRepository, IMappingRepository
    {

        private readonly GCAccountSettings _accountSettings;

        public MappingRepository()
            : base()
        {
            var accountsRepository = new AccountsRepository();
            _accountSettings = accountsRepository.GetAccountSettings();
        }


        #region Utilities




        private Item GetMappingFolder(Item projectFolder)
        {
            Item mappingFolder = projectFolder.Children.FirstOrDefault(i => i.Name == Constants.MappingFolderName);
            return mappingFolder;
        }

        private IEnumerable<Item> GetTemplateMappings(Item mappingFolder)
        {
            return mappingFolder.Children;
        }

        /// <summary>
        /// Get template mapping by GC project ID and GC template ID
        /// </summary>
        /// <param name="gcProjectId">GC project ID</param>
        /// <param name="gcTemplateId">GC template ID</param>
        /// <returns>Template mapping Item</returns>
        private Item GetTemplateMapping(string gcProjectId, string gcTemplateId)
        {
            var project = GetProject(gcProjectId);

            if (project != null)
            {
                return project.Axes.GetDescendants()
                    .FirstOrDefault(item => item["GC Template"] == gcTemplateId &
                                            item.TemplateID ==
                                            new ID(Constants.GcTemplateMapping));

            }
            return null;
        }

        private Item GetTemplate(string gcProjectId, string gcTemplateId)
        {
            var project = GetProject(gcProjectId);

            if (project != null)
            {
                return project.Axes.GetDescendants()
                    .FirstOrDefault(item => item["Temaplate Id"] == gcTemplateId &
                                            item.TemplateID ==
                                            new ID(Constants.GcTemplate));

            }
            return null;
        }

        private Item GetFieldMappingItem(string templateMappingId, string name)
        {
            var parentItem = GetItem(templateMappingId);
            return parentItem.Axes.GetDescendants().FirstOrDefault(item => item["GC Field Id"] == name);
        }

        /// <summary>
        /// Create filed mapping
        /// </summary>
        /// <param name="field">Template mapping Item</param>
        /// <param name="fieldMapping"></param>
        private void CreateFieldMapping(Item field, CmsTemplateField fieldMapping)
        {
            var mappings = field.Axes.GetDescendants();
            var validName = ItemUtil.ProposeValidItemName(fieldMapping.FieldName + " " + fieldMapping.FieldId);
            if (!mappings.Select(item => item.Name).ToList().Contains(validName))
            {
                using (new SecurityDisabler())
                {
                    var mapping = ContextDatabase.GetTemplate(new ID(Constants.GcFieldMapping));

                    var createdItem = field.Add(validName, mapping);
                    using (new SecurityDisabler())
                    {
                        createdItem.Editing.BeginEdit();
                        createdItem.Fields["GC Field"].Value = fieldMapping.FieldName;
                        createdItem.Fields["GC Field Id"].Value = fieldMapping.FieldId;
                        createdItem.Fields["Sitecore Field"].Value = fieldMapping.SelectedField;
                        createdItem.Editing.EndEdit();
                    }
                }
            }
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

        private void DeleteItem(Item template)
        {
            using (new SecurityDisabler())
            {
                template.Delete();
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
            var result = new MappingFieldModel(field["Sitecore Field"], field["GC Field Id"]);
            return result;
        }



        private Item GetFieldMapping(Item mapping, string fieldName)
        {
            return mapping.Axes.GetDescendants().FirstOrDefault(item => item["GC Field Id"] == fieldName);
        }

        /// <summary>
        /// Create template mapping
        /// </summary>
        /// <param name="projectId">Gather content project Id</param>
        /// <param name="templateMapping"></param>
        /// <returns>Template mapping item</returns>
        private Item CreateTemplateMapping(string projectId, TemplateMapping templateMapping)
        {
            var scProject = GetProject(projectId);
            if (scProject != null)
            {
                var mappingsFolder = GetMappingFolder(scProject);
                if (mappingsFolder != null)
                {
                    var mappings = mappingsFolder.Axes.GetDescendants();
                    if (!mappings.Select(item => item.Name).ToList().Contains(templateMapping.Name))
                    {
                        using (new SecurityDisabler())
                        {
                            var mapping = ContextDatabase.GetTemplate(new ID(Constants.GcTemplateMapping));

                            SetupLinkedGCTemplate(templateMapping);

                            var validFolderName = ItemUtil.ProposeValidItemName(templateMapping.Name);
                            var createdItem = mappingsFolder.Add(validFolderName, mapping);
                            using (new SecurityDisabler())
                            {
                                createdItem.Editing.BeginEdit();
                                createdItem.Fields["Sitecore Template"].Value = templateMapping.SitecoreTemplateId;
                                createdItem.Fields["GC Template"].Value = templateMapping.GcTemplateId;
                                createdItem.Fields["Last Mapped Date"].Value = DateUtil.ToIsoDate(DateTime.Now);
                                createdItem.Fields["Last Updated in GC"].Value = templateMapping.LastUpdated;
                                createdItem.Editing.EndEdit();
                            }

                            return createdItem;
                        }
                    }
                    return mappings.FirstOrDefault(item => item.Name == templateMapping.Name);
                }
                return null;
            }
            return null;
        }

        private void SetupLinkedGCTemplate(TemplateMapping templateMapping)
        {
            var linkedGCTemplate = ContextDatabase.GetTemplate(new ID(Constants.GCLinkItemTemplate));
            var sitecoreTemplate = GetItem(templateMapping.SitecoreTemplateId);
            var baseTemplates = sitecoreTemplate[FieldIDs.BaseTemplate];
            if (!baseTemplates.ToLower().Contains(linkedGCTemplate.ID.ToString().ToLower()))
            {
                using (new SecurityDisabler())
                {
                    sitecoreTemplate.Editing.BeginEdit();
                    sitecoreTemplate[Sitecore.FieldIDs.BaseTemplate] = string.Format("{0}|{1}", baseTemplates,
                        linkedGCTemplate.ID);
                    sitecoreTemplate.Editing.EndEdit();
                }
            }
        }

        private void UpdateTemplateMapping(Item template, TemplateMapping templateMapping)
        {
            using (new SecurityDisabler())
            {
                template.Editing.BeginEdit();
                template.Fields["Sitecore Template"].Value = templateMapping.SitecoreTemplateId;
                template.Fields["Last Mapped Date"].Value = DateUtil.ToIsoDate(DateTime.Now);
                template.Editing.EndEdit();
            }
        }

        #endregion


        public List<CmsMappingModel> GetMappings()
        {
            //TODO remove m["Last Updated in GC"]
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
                            var dateFormat = _accountSettings.DateFormat;
                            if (string.IsNullOrEmpty(dateFormat))
                            {
                                dateFormat = Constants.DateFormat;
                            }
                            double d;
                            var gcUpdateDate = string.Empty;
                            var isHighlightingDate = true;
                            if (Double.TryParse(m["Last Updated in GC"], out d))
                            {
                                var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);                                                         
                                gcUpdateDate = posixTime.AddMilliseconds(d * 1000).ToString(dateFormat);
                            }


                            var scTemplate = GetItem(m["Sitecore Template"]);

                            
                            if (scTemplate != null)
                            {
                                mapping.CmsTemplateName = scTemplate.Name;
                                mapping.LastUpdatedDate = gcUpdateDate;
                                mapping.LastMappedDateTime =
                                    DateUtil.IsoDateToDateTime(m["Last Mapped Date"])
                                        .ToString(dateFormat);
                                mapping.EditButtonTitle = "Edit";
                                mapping.IsMapped = true;
                                mapping.IsHighlightingDate = isHighlightingDate;
                            }
                            else
                            {
                                mapping.CmsTemplateName = "Not mapped";
                                mapping.LastMappedDateTime = "never";
                                mapping.EditButtonTitle = "Setup";
                                mapping.IsMapped = false;
                            }
                        }
                        else
                        {
                            mapping.CmsTemplateName = "Not mapped";
                            mapping.LastMappedDateTime = "never";
                            mapping.EditButtonTitle = "Setup";
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
            Item projectFolder = GetProject(projectId);
            if (projectFolder == null) return null;
            Item mappingFolder = GetMappingFolder(projectFolder);

            IEnumerable<Item> mappings = GetTemplateMappings(mappingFolder);
            IEnumerable<MappingTemplateModel> result = ConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }

        public List<MappingTemplateModel> GetAllTemplateMappings()
        {
            IEnumerable<Item> mappings = GetAllMappings();
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
                    var tm = new CmsTemplateField
                    {
                        FieldName = element.Label,
                        FieldId = element.Name,
                        FieldType = element.Type
                    };

                    if (scMapping != null)
                    {
                        var scField = GetFieldMapping(scMapping, element.Name);
                        if (scField != null)
                        {
                            tm.SelectedField = scField["Sitecore Field"];
                        }
                    }

                    tab.Fields.Add(tm);
                }
                model.Tabs.Add(tab);
            }
            return model;
        }


        /// <summary>
        /// Create mappings
        /// </summary>
        /// <param name="projectId">Gc project ID</param>
        /// <param name="templateMappingModel"></param>
        /// <param name="fields"></param>
        public void CreateMapping(int projectId, TemplateMapping templateMappingModel, List<CmsTemplateField> fields)
        {
            var templateMapping = CreateTemplateMapping(projectId.ToString(), templateMappingModel);

            foreach (var templateField in fields)
            {
                if (templateField.SelectedField != "0")
                {
                    CreateFieldMapping(templateMapping, templateField);
                }
            }
        }


        /// <summary>
        /// Update mappings
        /// </summary>
        /// <param name="projectId">Gc project ID</param>
        /// <param name="templateId">Gc template ID</param>
        /// <param name="templateMappingModel"></param>
        /// <param name="fields"></param>
        public void UpdateMapping(int projectId, int templateId, TemplateMapping templateMappingModel, List<CmsTemplateField> fields)
        {
            var templateMapping = GetTemplateMapping(projectId.ToString(), templateId.ToString());

            if (templateMapping != null)
            {
                UpdateTemplateMapping(templateMapping, templateMappingModel);

                foreach (var templateField in fields)
                {
                    var field = GetFieldMappingItem(templateMapping.ID.ToString(), templateField.FieldId);
                    if (field != null)
                    {
                        if (templateField.SelectedField == "0")
                        {
                            DeleteItem(field);
                        }
                        else
                        {
                            UpdateFieldMapping(field, templateField.SelectedField);
                        }
                    }
                    else
                    {
                        if (templateField.SelectedField != "0")
                        {
                            CreateFieldMapping(templateMapping, templateField);
                        }
                    }
                }
            }
        }


        public void DeleteMapping(TemplateEntity template)
        {
            var templateMapping = GetTemplateMapping(template.Data.ProjectId.ToString(), template.Data.Id.ToString());
            if (templateMapping != null)
            {
                DeleteItem(templateMapping);
            }
        }

        public void DeleteTemplate(TemplateEntity template)
        {
            var scTemplate = GetTemplate(template.Data.ProjectId.ToString(), template.Data.Id.ToString());
            if (scTemplate != null)
            {
                DeleteItem(scTemplate);
            }
        }
    }
}
