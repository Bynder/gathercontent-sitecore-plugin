using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Mapping;
using GatherContent.Connector.IRepositories.Models.New.Import;
using GatherContent.Connector.IRepositories.Models.New.Mapping;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.SecurityModel;
using CmsTemplateField = GatherContent.Connector.IRepositories.Models.Mapping.CmsTemplateField;
using TemplateMapping = GatherContent.Connector.IRepositories.Models.Mapping.TemplateMapping;

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


        #region Old Methods

        #region Old Utilities

        private IEnumerable<CmsMapping> ConvertSitecoreTemplatesToModel1(IEnumerable<Item> templates)
        {
            IEnumerable<CmsMapping> result = templates.Select(ConvertSitecoreTemplateToModel1);
            return result;
        }

        private IEnumerable<MappingTemplateModel> OldConvertSitecoreTemplatesToModel(IEnumerable<Item> templates)
        {
            IEnumerable<MappingTemplateModel> result = templates.Select(OldConvertSitecoreTemplateToModel);
            return result;
        }

        private CmsMapping ConvertSitecoreTemplateToModel1(Item template)
        {
            //IEnumerable<CmsField> fields = ConvertSitecoreFieldsToModel(template.Children);
            //var result = new CmsMapping
            //{
            //    CmsTemplateId = template["Sitecore Template"],
            //    GcTemplateId = template["GC Template"],
            //    CmsFields = fields.ToList()
            //};

            //return result;


            return null;
        }

        private MappingTemplateModel OldConvertSitecoreTemplateToModel(Item template)
        {
            IEnumerable<GatherContent.Connector.IRepositories.Models.Import.MappingFieldModel> fields = OldConvertSitecoreFieldsToModel(template.Children);
            var result = new MappingTemplateModel(template["Sitecore Template"], template["GC Template"], fields.ToList());

            return result;
        }

        private GatherContent.Connector.IRepositories.Models.Import.MappingFieldModel OldConvertSitecoreFieldToModel(Item field)
        {
            var result = new GatherContent.Connector.IRepositories.Models.Import.MappingFieldModel(field["Sitecore Field"], field["GC Field Id"]);
            return result;
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
                    using (new SecurityDisabler())
                    {
                        var mapping = ContextDatabase.GetTemplate(new ID(Constants.GcTemplateMapping));

                        SetupLinkedGcTemplate(templateMapping);

                        var validFolderName = ItemUtil.ProposeValidItemName(templateMapping.Name);
                        var createdItem = mappingsFolder.Add(validFolderName, mapping);
                        using (new SecurityDisabler())
                        {
                            createdItem.Editing.BeginEdit();
                            createdItem.Fields["Sitecore Template"].Value = templateMapping.SitecoreTemplateId;
                            createdItem.Fields["Default Location"].Value = templateMapping.DefaultLocation;
                            if (!string.IsNullOrEmpty(templateMapping.GcMappingTitle))
                            {
                                createdItem.Fields["Template mapping title"].Value = templateMapping.GcMappingTitle;
                            }
                            createdItem.Fields["GC Template"].Value = templateMapping.GcTemplateId;
                            createdItem.Fields["Last Mapped Date"].Value = DateUtil.ToIsoDate(DateTime.Now);
                            createdItem.Fields["Last Updated in GC"].Value = templateMapping.LastUpdated;
                            createdItem.Editing.EndEdit();
                        }

                        return createdItem;
                    }
                }
                return null;
            }
            return null;
        }

        private void SetupLinkedGcTemplate(TemplateMapping templateMapping)
        {
            var linkedGcTemplate = ContextDatabase.GetTemplate(new ID(Constants.GCLinkItemTemplateID));
            var sitecoreTemplate = GetItem(templateMapping.SitecoreTemplateId);
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


        #endregion





        public List<CmsMappingModel> OldGetMappings()
        {
            //TODO remove m["Last Updated in GC"]
            var model = new List<CmsMappingModel>();
            var scProjects = GetAllProjects();

            foreach (var project in scProjects)
            {
                var templates = project.Axes.GetDescendants().Where(i => i.TemplateName == Constants.TemplateMappingName).ToList();
                if (templates.Count() > 0)
                {
                    foreach (var templateMapping in templates)
                    {
                        var mapping = new CmsMappingModel
                        {
                            GcProjectName = project.Name,
                            GcTemplateId = templateMapping["GC Template"],
                            GcTemplateName = templateMapping.Name,
                            CmsMappingId = templateMapping.ID.ToString()
                        };

                        var dateFormat = _accountSettings.DateFormat;
                        if (string.IsNullOrEmpty(dateFormat))
                        {
                            dateFormat = Constants.DateFormat;
                        }
                        double d;
                        var gcUpdateDate = string.Empty;
                        var isHighlightingDate = true;
                        if (Double.TryParse(templateMapping["Last Updated in GC"], out d))
                        {
                            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
                            gcUpdateDate = posixTime.AddMilliseconds(d * 1000).ToString(dateFormat);
                        }

                        var scTemplate = GetItem(templateMapping["Sitecore Template"]);
                        if (scTemplate != null)
                        {
                            mapping.CmsTemplateName = scTemplate.Name;
                            mapping.LastUpdatedDate = gcUpdateDate;
                            mapping.CmsMappingTitle = templateMapping["Template mapping title"];
                            mapping.LastMappedDateTime =
                                DateUtil.IsoDateToDateTime(templateMapping["Last Mapped Date"])
                                    .ToString(dateFormat);
                            mapping.EditButtonTitle = "Edit";
                            mapping.IsMapped = true;
                            mapping.IsHighlightingDate = isHighlightingDate;
                        }
                        else
                        {
                            mapping.CmsTemplateName = "Not mapped";
                            mapping.CmsMappingTitle = "Not mapped";
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

        List<CmsMapping> GetAllMappings1()
        {
            IEnumerable<Item> mappings = GetAllMappings();
            IEnumerable<CmsMapping> result = ConvertSitecoreTemplatesToModel1(mappings);

            return result.ToList();
        }

        public List<MappingTemplateModel> GetAllTemplateMappings()
        {
            IEnumerable<Item> mappings = GetAllMappings();
            IEnumerable<MappingTemplateModel> result = OldConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }

        public List<MappingTemplateModel> GetTemplateMappingsByProjectId(string projectId)
        {
            Item projectFolder = GetProject(projectId);
            if (projectFolder == null) return null;
            Item mappingFolder = GetMappingFolder(projectFolder);

            IEnumerable<Item> mappings = GetTemplateMappings(mappingFolder);
            IEnumerable<MappingTemplateModel> result = OldConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }

        public MappingTemplateModel GetTemplateMappingsByTemplateId(string templateId)
        {
            var template = GetItem(templateId);
            if (template == null) return null;
            var result = OldConvertSitecoreTemplateToModel(template);
            return result;
        }

        public List<AvailableMappingModel> GetAllMappingsForGcTemplate(string gcProjectId, string gcTemplateId)
        {
            var mappings = GetTemplateMappings(gcProjectId, gcTemplateId);

            var result = mappings.Select(mapping => new AvailableMappingModel
            {
                Id = mapping.ID.ToString(),
                Title = mapping["Template mapping title"],
                DefaultLocation = mapping["Default Location"],
                DefaultLocationTitle = GetItem(mapping["Default Location"]) != null ? GetItem(mapping["Default Location"]).Name : "",
                Name = mapping.Name,
                ScTemplate = GetItem(mapping["Sitecore Template"]) != null ? GetItem(mapping["Sitecore Template"]).Name : "",
            }).ToList();


            return result.ToList();
        }

        public AddMapping GetAddMappingModel(string projectId, TemplateEntity template, string scMappingId)
        {
            var model = new AddMapping { GcTemplateId = template.Data.Id.ToString(), IsEdit = false };

            var scMapping = GetItem(scMappingId);
            if (scMapping != null)
            {
                model.GcMappingTitle = scMapping["Template mapping title"];
                model.DefaultLocation = scMapping["Default Location"];
                var defaultLocation = GetItem(scMapping["Default Location"]);
                if (defaultLocation != null)
                {
                    model.DefaultLocationTitle = defaultLocation.Name;
                }
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
        /// Create mapping
        /// </summary>
        /// <param name="projectId">Gc project ID</param>
        /// <param name="templateMapping"></param>
        public void CreateMapping(string projectId, TemplateMapping templateMapping)
        {
            var scProject = GetProject(projectId);
            if (scProject != null)
            {
                var mappingsFolder = GetMappingFolder(scProject);
                if (mappingsFolder != null)
                {
                    using (new SecurityDisabler())
                    {
                        var mapping = ContextDatabase.GetTemplate(new ID(Constants.GcTemplateMapping));
                        var validFolderName = ItemUtil.ProposeValidItemName(templateMapping.Name);
                        var createdItem = mappingsFolder.Add(validFolderName, mapping);
                        using (new SecurityDisabler())
                        {
                            createdItem.Editing.BeginEdit();
                            createdItem.Fields["GC Template Name"].Value = templateMapping.Name;
                            createdItem.Fields["GC Template"].Value = templateMapping.GcTemplateId;
                            createdItem.Editing.EndEdit();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update mappings
        /// </summary>
        /// <param name="projectId">Gc project ID</param>
        /// <param name="templateMappingModel"></param>
        /// <param name="fields"></param>
        public void UpdateMapping(int projectId, TemplateMapping templateMappingModel, List<CmsTemplateField> fields)
        {
            var templateMapping = GetItem(templateMappingModel.CmsMappingId);

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
            else
            {
                CreateMapping(projectId, templateMappingModel, fields);
            }
        }

        #endregion




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

        private Item GetFieldMappingItem(string templateMappingId, string name)
        {
            var parentItem = GetItem(templateMappingId);
            return parentItem.Axes.GetDescendants().FirstOrDefault(item => item["GC Field Id"] == name);
        }

        private List<Item> GetFieldMappingItems(string templateMappingId)
        {
            var parentItem = GetItem(templateMappingId);
            return parentItem.Axes.GetDescendants().ToList();
        }

        private IEnumerable<GatherContent.Connector.IRepositories.Models.New.Mapping.TemplateMapping> ConvertSitecoreTemplatesToModel(IEnumerable<Item> templates)
        {
            return ConvertSitecoreTemplatesToModel(templates, null);
        }

        private IEnumerable<GatherContent.Connector.IRepositories.Models.New.Mapping.TemplateMapping> ConvertSitecoreTemplatesToModel(IEnumerable<Item> templates, string projectName = null)
        {
            var result = new List<GatherContent.Connector.IRepositories.Models.New.Mapping.TemplateMapping>();
            foreach (var templateMapping in templates)
            {
                var mapping = ConvertSitecoreTemplateToModel(templateMapping, projectName);
                result.Add(mapping);
            }

            return result;
        }

        private GatherContent.Connector.IRepositories.Models.New.Mapping.TemplateMapping ConvertSitecoreTemplateToModel(Item templateMapping)
        {
            return ConvertSitecoreTemplateToModel(templateMapping, null);
        }

        private IRepositories.Models.New.Mapping.TemplateMapping ConvertSitecoreTemplateToModel(Item templateMapping, string projectName = null)
        {
            var fields = ConvertSitecoreFieldsToModel(templateMapping.Children);
            var mapping = new IRepositories.Models.New.Mapping.TemplateMapping
            {
                MappingId = templateMapping.ID.ToString(),
                FieldMappings = fields.ToList(),
                GcTemplate = new GcTemplate
                {
                    GcTemplateId = templateMapping["GC Template"],
                    GcTemplateName = templateMapping.Name,
                },

            };

            var dateFormat = _accountSettings.DateFormat;
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
                mapping.CmsTemplate = new GatherContent.Connector.IRepositories.Models.New.Mapping.CmsTemplate
                {
                    TemplateName = scTemplate.Name,
                    TemplateId = scTemplate.ID.ToString(),
                    TemplateFields = GetTemplateFields(scTemplate)
                };
                mapping.LastUpdatedDate = gcUpdateDate;
                mapping.MappingTitle = templateMapping["Template mapping title"];
                mapping.LastMappedDateTime =
                    DateUtil.IsoDateToDateTime(templateMapping["Last Mapped Date"])
                        .ToString(dateFormat);
            }
            else
            {
                mapping.CmsTemplate = new GatherContent.Connector.IRepositories.Models.New.Mapping.CmsTemplate
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

        private List<GatherContent.Connector.IRepositories.Models.New.Import.CmsTemplateField> GetTemplateFields(Item scTemplate)
        {
            var result = new List<GatherContent.Connector.IRepositories.Models.New.Import.CmsTemplateField>();
            var fields = GetFields(scTemplate);
            result.AddRange(
                from f in fields
                where !f.Name.StartsWith("__")
                select new GatherContent.Connector.IRepositories.Models.New.Import.CmsTemplateField
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

        private IEnumerable<GatherContent.Connector.IRepositories.Models.Import.MappingFieldModel> OldConvertSitecoreFieldsToModel(IEnumerable<Item> fields)
        {
            IEnumerable<GatherContent.Connector.IRepositories.Models.Import.MappingFieldModel> result = fields.Select(OldConvertSitecoreFieldToModel);
            return result;
        }

        private FieldMapping ConvertSitecoreFieldToModel(Item field)
        {
            var result = new FieldMapping
            {
                CmsField = new IRepositories.Models.New.Import.CmsField
                {
                    TemplateField = new IRepositories.Models.New.Import.CmsTemplateField
                    {
                        FieldId = field["Sitecore Field"]
                    }
                },
                GcField = new GcField
               {

                   Id = field["GC Field Id"]
               }
            };
            return result;
        }

        private Item GetFieldMapping(Item mapping, string fieldName)
        {
            return mapping.Axes.GetDescendants().FirstOrDefault(item => item["GC Field Id"] == fieldName);
        }

        private void SetupLinkedGcTemplate(IRepositories.Models.New.Mapping.TemplateMapping templateMapping)
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

        private void UpdateTemplateMapping(Item template, TemplateMapping templateMapping)
        {
            using (new SecurityDisabler())
            {
                template.Editing.BeginEdit();
                template.Fields["Sitecore Template"].Value = templateMapping.SitecoreTemplateId;
                template.Fields["Default Location"].Value = templateMapping.DefaultLocation;
                if (!string.IsNullOrEmpty(templateMapping.GcMappingTitle))
                {
                    template.Fields["Template mapping title"].Value = templateMapping.GcMappingTitle;
                }
                template.Fields["Last Mapped Date"].Value = DateUtil.ToIsoDate(DateTime.Now);
                template.Editing.EndEdit();
            }
        }

        private Item CreateTemplateMapping(IRepositories.Models.New.Mapping.TemplateMapping templateMapping)
        {
            var scProject = GetProject(templateMapping.GcProjectId);
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

        private void UpdateTemplateMapping(Item item, IRepositories.Models.New.Mapping.TemplateMapping templateMapping)
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


        public List<IRepositories.Models.New.Mapping.TemplateMapping> GetMappings()
        {
            var model = new List<IRepositories.Models.New.Mapping.TemplateMapping>();
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

        public List<IRepositories.Models.New.Mapping.TemplateMapping> GetMappingsByGcProjectId(string projectId)
        {
            var projectFolder = GetProject(projectId);
            if (projectFolder == null) return null;
            var mappingFolder = GetMappingFolder(projectFolder);
            var mappings = GetTemplateMappings(mappingFolder);
            var result = ConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }

        public List<IRepositories.Models.New.Mapping.TemplateMapping> GetMappingsByGcTemplateId(string gcTemplateId)
        {
            var mappings = GetTemplateMappingsByGcTemplateId(gcTemplateId);
            var result = ConvertSitecoreTemplatesToModel(mappings);
            return result.ToList();
        }

        public IRepositories.Models.New.Mapping.TemplateMapping GetMappingById(string id)
        {
            var mapping = GetItem(id);
            var result = ConvertSitecoreTemplateToModel(mapping);
            return result;
        }

        public void CreateMapping(IRepositories.Models.New.Mapping.TemplateMapping templateMapping)
        {
            var templateMappingItem = CreateTemplateMapping(templateMapping);

            foreach (var templateField in templateMapping.FieldMappings)
            {
                CreateFieldMapping(templateMappingItem, templateField);
            }
        }

        public void UpdateMapping(IRepositories.Models.New.Mapping.TemplateMapping templateMapping)
        {
            var templateMappingItem = GetItem(templateMapping.MappingId);

            if (templateMappingItem != null)
            {
                UpdateTemplateMapping(templateMappingItem, templateMapping);

                var fields = GetFieldMappingItems(templateMapping.CmsTemplate.TemplateId);

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
    }
}
