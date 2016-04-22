using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Mapping;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.SecurityModel;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    /// <summary>
    /// 
    /// </summary>
    public class MappingRepository : BaseSitecoreRepository, IMappingRepository
    {
        protected IAccountsRepository AccountsRepository;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountsRepository"></param>
        public MappingRepository(IAccountsRepository accountsRepository)
            : base()
        {
            AccountsRepository = accountsRepository;
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

        private IEnumerable<Item> GetTemplates(string id)
        {
            var item = GetItem(id);
            return item != null ? item.Axes.GetDescendants().Where(t => t.TemplateName == "Template").ToList() : Enumerable.Empty<Item>();
        }

        private IEnumerable<Item> GetTemplateMappings(string gcProjectId, string gcTemplateId)
        {
            var project = GetProject(gcProjectId);

            if (project != null)
            {
                return project.Axes.GetDescendants()
                    .Where(item => item["GC Template"] == gcTemplateId &
                                            item.TemplateID ==
                                            new ID(Constants.GcTemplateMapping)).ToList();

            }
            return null;
        }

        private IEnumerable<Item> GetTemplateMappingsByGcTemplateId(string gcTemplateId)
        {
            var homeItem = GetItem(Constants.AccountItemId, ContextLanguage);
            if (homeItem != null)
            {
                return homeItem.Axes.GetDescendants()
                    .Where(item => item["GC Template"] == gcTemplateId &
                                            item.TemplateID ==
                                            new ID(Constants.GcTemplateMapping)).ToList();

            }
            return null;
        }

        private List<Item> GetFieldMappingItems(string templateMappingId)
        {
            var parentItem = GetItem(templateMappingId);
            return parentItem.Axes.GetDescendants().ToList();
        }

        private IEnumerable<TemplateMapping> ConvertSitecoreTemplatesToModel(IEnumerable<Item> templates)
        {
            return ConvertSitecoreTemplatesToModel(templates, null);
        }

        private IEnumerable<TemplateMapping> ConvertSitecoreTemplatesToModel(IEnumerable<Item> templates, string projectName = null)
        {
            var result = new List<TemplateMapping>();
            foreach (var templateMapping in templates)
            {
                var mapping = ConvertSitecoreTemplateToModel(templateMapping, projectName);
                result.Add(mapping);
            }

            return result;
        }

        private TemplateMapping ConvertSitecoreTemplateToModel(Item templateMapping)
        {
            return ConvertSitecoreTemplateToModel(templateMapping, null);
        }

        private TemplateMapping ConvertSitecoreTemplateToModel(Item templateMapping, string projectName = null)
        {
            var fields = ConvertSitecoreFieldsToModel(templateMapping.Children);
            var mapping = new TemplateMapping
            {
                MappingId = templateMapping.ID.ToString(),
                FieldMappings = fields.ToList(),
                GcTemplate = new GcTemplate
                {
                    GcTemplateId = templateMapping["GC Template"],
                    GcTemplateName = templateMapping.Name,
                },

            };

            var accountSettings = AccountsRepository.GetAccountSettings();
            var dateFormat = accountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }
            double d;
            var gcUpdateDate = string.Empty;
            if (Double.TryParse(templateMapping["Last Updated in GC"], out d))
            {
                var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
                gcUpdateDate = posixTime.AddMilliseconds(d * 1000).ToString(dateFormat);
            }

            var scTemplate = GetItem(templateMapping["Sitecore Template"]);
            if (scTemplate != null)
            {
                mapping.CmsTemplate = new CmsTemplate
                {
                    TemplateName = scTemplate.Name,
                    TemplateId = scTemplate.ID.ToString(),
                    TemplateFields = GetTemplateFields(scTemplate)
                };
                mapping.LastUpdatedDate = gcUpdateDate;
                mapping.MappingTitle = templateMapping["Template mapping title"];
                mapping.DefaultLocationId = templateMapping["Default Location"];
                mapping.DefaultLocationTitle = GetItem(templateMapping["Default Location"]) != null
                    ? GetItem(templateMapping["Default Location"]).Name
                    : "";
                mapping.LastMappedDateTime =
                    DateUtil.IsoDateToDateTime(templateMapping["Last Mapped Date"])
                        .ToString(dateFormat);
            }
            else
            {
                mapping.CmsTemplate = new CmsTemplate
                {
                    TemplateName = "Not mapped"
                };
                mapping.MappingTitle = "Not mapped";
                mapping.LastMappedDateTime = "never";
            }

            if (projectName != null)
            {
                mapping.GcProjectName = projectName;
            }

            return mapping;
        }

        private List<CmsTemplateField> GetTemplateFields(Item scTemplate)
        {
            var result = new List<CmsTemplateField>();
            var fields = GetFields(scTemplate);
            result.AddRange(
                from f in fields
                where !f.Name.StartsWith("__")
                select new CmsTemplateField
                {
                    FieldName = f.Name,
                    FieldId = f.ID.ToString(),
                    FieldType = f.Type
                });

            return result;
        }

        private IEnumerable<TemplateField> GetFields(Item template)
        {
            return TemplateManager.GetTemplate(template.ID, ContextDatabase).GetFields();
        }

        private IEnumerable<FieldMapping> ConvertSitecoreFieldsToModel(IEnumerable<Item> fields)
        {
            var result = fields.Select(ConvertSitecoreFieldToModel);
            return result;
        }


        private FieldMapping ConvertSitecoreFieldToModel(Item fieldMapping)
        {
            var field = GetItem(fieldMapping["Sitecore Field"]);
            var result = new FieldMapping
            {
                CmsField = new CmsField
                {
                    TemplateField = new CmsTemplateField
                    {
                        FieldId = fieldMapping["Sitecore Field"],
                        FieldName = field.Name,
                        FieldType = "todo"
                    }
                },
                GcField = new GcField
               {
                   Id = fieldMapping["GC Field Id"],
                   Name = fieldMapping["GC Field"]
               }
            };
            return result;
        }

        private void SetupLinkedGcTemplate(TemplateMapping templateMapping)
        {
            var linkedGcTemplate = ContextDatabase.GetTemplate(new ID(Constants.GCLinkItemTemplateID));
            if (templateMapping.CmsTemplate == null) return;
            var sitecoreTemplate = GetItem(templateMapping.CmsTemplate.TemplateId);
            if (sitecoreTemplate == null) return;
            var baseTemplates = sitecoreTemplate[FieldIDs.BaseTemplate];
            if (!baseTemplates.ToLower().Contains(linkedGcTemplate.ID.ToString().ToLower()))
            {
                using (new SecurityDisabler())
                {
                    sitecoreTemplate.Editing.BeginEdit();
                    sitecoreTemplate[Sitecore.FieldIDs.BaseTemplate] = string.Format("{0}|{1}", baseTemplates,
                        linkedGcTemplate.ID);
                    sitecoreTemplate.Editing.EndEdit();
                }
            }
        }

        private void CreateProjectFolders(string id)
        {
            var names = new[] { Constants.MappingFolderName };
            foreach (var name in names)
            {
                using (new SecurityDisabler())
                {
                    var folder = GetItem(id);
                    var project = ContextDatabase.GetTemplate(new ID(Constants.GcFolderId));
                    var validFolderName = ItemUtil.ProposeValidItemName(name);
                    folder.Add(validFolderName, project);
                }
            }
        }

        private Item CreateOrGetProject(string gcProjectId, string gcProjectName)
        {
            var projectItem = GetProject(gcProjectId);

            if (projectItem == null)
            {
                var accountSettings = AccountsRepository.GetAccountSettings();

                var parentItem = GetItem(accountSettings.AccountItemId);

                using (new SecurityDisabler())
                {
                    var template = ContextDatabase.GetTemplate(new ID(Constants.GcProject));
                    var validFolderName = ItemUtil.ProposeValidItemName(gcProjectName);
                    projectItem = parentItem.Add(validFolderName, template);
                    using (new SecurityDisabler())
                    {
                        projectItem.Editing.BeginEdit();
                        projectItem.Fields["Id"].Value = gcProjectId;
                        projectItem.Fields["Name"].Value = gcProjectName;
                        projectItem.Editing.EndEdit();
                    }
                    CreateProjectFolders(projectItem.ID.ToString());
                }
            }
            return projectItem;
        }

        private Item CreateTemplateMapping(TemplateMapping templateMapping)
        {
            var scProject = CreateOrGetProject(templateMapping.GcProjectId, templateMapping.GcProjectName);
            
            if (scProject != null)
            {
                var mappingsFolder = GetMappingFolder(scProject);
                if (mappingsFolder != null)
                {
                    using (new SecurityDisabler())
                    {
                        var mapping = ContextDatabase.GetTemplate(new ID(Constants.GcTemplateMapping));

                        SetupLinkedGcTemplate(templateMapping);

                        var validFolderName = ItemUtil.ProposeValidItemName(templateMapping.GcTemplate.GcTemplateName);
                        var createdItem = mappingsFolder.Add(validFolderName, mapping);
                        using (new SecurityDisabler())
                        {
                            createdItem.Editing.BeginEdit();
                            createdItem.Fields["Sitecore Template"].Value = templateMapping.CmsTemplate.TemplateId;
                            createdItem.Fields["Default Location"].Value = templateMapping.DefaultLocationId;
                            if (!string.IsNullOrEmpty(templateMapping.MappingTitle))
                            {
                                createdItem.Fields["Template mapping title"].Value = templateMapping.MappingTitle;
                            }
                            createdItem.Fields["GC Template"].Value = templateMapping.GcTemplate.GcTemplateId;
                            createdItem.Fields["Last Mapped Date"].Value = DateUtil.ToIsoDate(DateTime.Now);
                            createdItem.Fields["Last Updated in GC"].Value = templateMapping.LastUpdatedDate;
                            createdItem.Editing.EndEdit();
                        }

                        return createdItem;
                    }
                }
                return null;
            }
            return null;
        }

        private void CreateFieldMapping(Item templateMappingItem, FieldMapping fieldMapping)
        {
            var validName = ItemUtil.ProposeValidItemName(fieldMapping.GcField.Name + " " + fieldMapping.GcField.Id);
            using (new SecurityDisabler())
            {
                var mapping = ContextDatabase.GetTemplate(new ID(Constants.GcFieldMapping));

                var createdItem = templateMappingItem.Add(validName, mapping);
                using (new SecurityDisabler())
                {
                    createdItem.Editing.BeginEdit();
                    createdItem.Fields["GC Field"].Value = fieldMapping.GcField.Name;
                    createdItem.Fields["GC Field Id"].Value = fieldMapping.GcField.Id;
                    createdItem.Fields["Sitecore Field"].Value = fieldMapping.CmsField.TemplateField.FieldId;
                    createdItem.Editing.EndEdit();
                }
            }
        }

        private void UpdateTemplateMapping(Item item, TemplateMapping templateMapping)
        {
            using (new SecurityDisabler())
            {
                item.Editing.BeginEdit();
                if (templateMapping.CmsTemplate != null)
                {
                    item.Fields["Sitecore Template"].Value = templateMapping.CmsTemplate.TemplateId;
                }
                item.Fields["Default Location"].Value = templateMapping.DefaultLocationId;
                if (!string.IsNullOrEmpty(templateMapping.MappingTitle))
                {
                    item.Fields["Template mapping title"].Value = templateMapping.MappingTitle;
                }
                item.Fields["Last Mapped Date"].Value = DateUtil.ToIsoDate(DateTime.Now);
                item.Editing.EndEdit();
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

        #endregion


        public List<TemplateMapping> GetMappings()
        {
            var model = new List<TemplateMapping>();
            var scProjects = GetAllProjects();

            foreach (var project in scProjects)
            {
                var templates = project.Axes.GetDescendants().Where(i => i.TemplateName == Constants.TemplateMappingName).ToList();
                if (templates.Count() > 0)
                {
                    model.AddRange(ConvertSitecoreTemplatesToModel(templates, project.Name));
                }
            }

            return model;
        }

        public List<TemplateMapping> GetMappingsByGcProjectId(string projectId)
        {
            var projectFolder = GetProject(projectId);
            if (projectFolder == null) return null;
            var mappingFolder = GetMappingFolder(projectFolder);
            var mappings = GetTemplateMappings(mappingFolder);
            var result = ConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }

        public List<TemplateMapping> GetMappingsByGcTemplateId(string gcTemplateId)
        {
            var mappings = GetTemplateMappingsByGcTemplateId(gcTemplateId);
            var result = ConvertSitecoreTemplatesToModel(mappings);
            return result.ToList();
        }

        public TemplateMapping GetMappingById(string id)
        {
            var mapping = GetItem(id);
            if (mapping == null) return null;
            var result = ConvertSitecoreTemplateToModel(mapping);
            return result;
        }

        public void CreateMapping(TemplateMapping templateMapping)
        {
            var templateMappingItem = CreateTemplateMapping(templateMapping);

            foreach (var templateField in templateMapping.FieldMappings)
            {
                CreateFieldMapping(templateMappingItem, templateField);
            }
        }

        public void UpdateMapping(TemplateMapping templateMapping)
        {
            var templateMappingItem = GetItem(templateMapping.MappingId);

            if (templateMappingItem != null)
            {
                UpdateTemplateMapping(templateMappingItem, templateMapping);

                var fields = GetFieldMappingItems(templateMapping.MappingId);

                foreach (var field in fields)
                {
                    if (!templateMapping.FieldMappings.Select(tm => tm.GcField.Id).Contains(field["GC Field Id"]))
                    {
                        DeleteItem(field);
                    }
                }         

                foreach (var templateField in templateMapping.FieldMappings)
                {
                    var field = fields.FirstOrDefault(fm => fm["GC Field Id"] == templateField.GcField.Id);
                    if (field != null)
                    {
                        UpdateFieldMapping(field, templateField.CmsField.TemplateField.FieldId);
                    }
                    else
                    {
                        CreateFieldMapping(templateMappingItem, templateField);
                    }
                }
            }
        }

        public void DeleteMapping(string mappingId)
        {
            var templateMapping = GetItem(mappingId);
            if (templateMapping != null)
            {
                DeleteItem(templateMapping);
            }
        }

        public List<CmsTemplate> GetAvailableCmsTemplates()
        {
            var accountSettings = AccountsRepository.GetAccountSettings();

            var templateFolderId = accountSettings.TemplateFolderId;
            if (string.IsNullOrEmpty(templateFolderId))
            {
                templateFolderId = Constants.TemplateFolderId;
            }
            var model = new List<CmsTemplate>();
            var cmsTemplates = GetTemplates(templateFolderId);
            foreach (var template in cmsTemplates)
            {
                var cmsTemplate = new CmsTemplate
                {
                    TemplateName = template.Name,
                    TemplateId = template.ID.ToString()
                };

                var fields = GetFields(template);
                cmsTemplate.TemplateFields.AddRange(
                    fields.Where(f => !f.Name.StartsWith("__")).Select(f => new CmsTemplateField
                    {
                        FieldName = f.Name,
                        FieldId = f.ID.ToString(),
                        FieldType = f.Type
                    }));

                model.Add(cmsTemplate);
            }
            return model;
        }
    }
}
