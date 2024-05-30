namespace Bynder.Content.SitecoreConnector.SitecoreRepositories.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Data.Managers;
    using Sitecore.Data.Templates;
    using Sitecore.SecurityModel;

    using Core.DependencyInjection;
    using Core.Interfaces;
    using Core.Models.Import;
    using Core.Models.Mapping;
    using Configuration;
    using System.Web.UI.WebControls;
    using System.Xml.Linq;
    using ProtoBuf;

    [Service(typeof(IMappingRepository))]
    public class MappingRepository : BaseSitecoreRepository, IMappingRepository
    {
        private readonly IAccountsRepository accountsRepository;

        protected Dictionary<string, string> DefaultAllowedMappings = new Dictionary<string, string>
            {
                {"text", "Single-Line Text, Multi-Line Text, Rich Text, Datetime, Date"},
                {"section", "Single-Line Text, Multi-Line Text, Rich Text"},
                {"choice_radio", "Droptree, Checklist, Multilist, Multilist with Search, Treelist, TreelistEx"},
                {"choice_checkbox", "Checklist, Multilist, Multilist with Search, Treelist, TreelistEx"},
                {"files", "Image, File, Droptree, Multilist, Multilist with Search, Treelist, TreelistEx"}
            };

        public MappingRepository(IAccountsRepository accountsRepository)
        {
            this.accountsRepository = accountsRepository;
        }

        public List<TemplateMapping> GetMappings()
        {
            var model = new List<TemplateMapping>();
            var scProjects = GetAllProjects();

            foreach (var project in scProjects)
            {
                var templates = project.Axes.GetDescendants().Where(i => i.TemplateName == Constants.TemplateMappingName || i.TemplateName == Constants.TemplateRelatedMappingName).ToList();
                if (templates.Any())
                {
                    model.AddRange(ConvertSitecoreTemplatesToModel(templates, project.Name));
                }
            }

            return model;
        }

        public TemplateMapping GetMappingByItemId(string itemId, string language)
        {
            var mapping = new TemplateMapping();

            var item = GetItem(itemId, Sitecore.Data.Managers.LanguageManager.GetLanguage(language));

            if (item != null)
            {
                var mappingId = item["MappingId"];

                var mappingItem = GetItem(mappingId);

                if (mappingItem != null)
                {
                    MapSitecoreTemplateToModel(mappingItem, mapping);
                }
            }

            return mapping;
        }

        public List<TemplateMapping> GetMappingsByCwbProjectId(string projectId)
        {
            var projectFolder = GetProject(projectId);
            if (projectFolder == null) return null;
            var mappingFolder = GetMappingFolder(projectFolder);
            var mappings = GetTemplateMappings(mappingFolder);
            var result = ConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }

        public List<TemplateMapping> GetMappingsByCwbTemplateId(string cwbTemplateId)
        {
            var mappings = GetTemplateMappingsByCwbTemplateId(cwbTemplateId);
            var result = ConvertSitecoreTemplatesToModel(mappings);
            return result.ToList();
        }

        public TemplateMapping GetMappingById(string id)
        {
            var mapping = GetItem(id);
            if (mapping == null)
            {
                return null;
            }

            var result = new TemplateMapping();
            MapSitecoreTemplateToModel(mapping, result);
            return result;
        }

        public RelatedTemplateMapping GetRelatedMappingById(string id)
        {
            var mapping = GetItem(id);
            if (mapping == null)
            {
                return null;
            }
            var result = new RelatedTemplateMapping();
            MapSitecoreRelatedTemplateMappingToModel(mapping, result);
            return result;
        }

        public List<RelatedTemplateMapping> GetRelatedMappingCollection(TemplateMapping mainMapping)
        {
            var mainMappingItem = ContextDatabase.GetItem(mainMapping.MappingId, ContextLanguage);
            var mappingCollection = mainMappingItem.Parent.Axes.SelectItems($"./*[@@templatename='{Constants.TemplateRelatedMappingName}' and @{Constants.MainMappingFieldName} = '{mainMapping.MappingId}']");
            var result = new List<RelatedTemplateMapping>();
            if (mappingCollection != null)
            {
                foreach (var mapping in mappingCollection)
                {
                    var relatedMapping = new RelatedTemplateMapping();
                    MapSitecoreRelatedTemplateMappingToModel(mapping, relatedMapping);
                    result.Add(relatedMapping);
                }
            }

            return result;
        }

        public void CreateMapping(TemplateMapping templateMapping)
        {
            var templateMappingItem = CreateTemplateMapping(templateMapping);

            int sortOrder = 1;
            foreach (var templateField in templateMapping.FieldMappings)
            {
                CreateFieldMapping(templateMappingItem, templateField, sortOrder);
                sortOrder++;
            }
        }

        public void CreateRelatedMapping(RelatedTemplateMapping templateMapping)
        {
            var mainMapping = GetItem(templateMapping.CmsMainMappingId);
            if (mainMapping != null)
            {
                var templateMappingItem = CreateRelatedTemplateMapping(mainMapping, templateMapping);

                int sortOrder = 1;
                foreach (var templateField in templateMapping.FieldMappings)
                {
                    CreateFieldMapping(templateMappingItem, templateField, sortOrder);
                    sortOrder++;
                }
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
                    if (!templateMapping.FieldMappings.Select(tm => tm.CwbField.Id).Contains(field["CWB Field Id"]))
                    {
                        DeleteItem(field);
                    }
                }

                int sortOrder = 1;
                foreach (var templateField in templateMapping.FieldMappings)
                {
                    var field = fields.FirstOrDefault(fm => fm["CWB Field Id"] == templateField.CwbField.Id);
                    if (field != null)
                    {
                        UpdateFieldMapping(field, templateField.CmsField, sortOrder);
                    }
                    else
                    {
                        CreateFieldMapping(templateMappingItem, templateField, sortOrder);
                    }

                    sortOrder++;
                }
            }
        }

        public void UpdateRelatedMapping(RelatedTemplateMapping templateMapping)
        {
            var templateMappingItem = GetItem(templateMapping.MappingId);

            if (templateMappingItem != null)
            {
                UpdateRelatedTemplateMapping(templateMappingItem, templateMapping);

                var fields = GetFieldMappingItems(templateMapping.MappingId);

                foreach (var field in fields)
                {
                    if (!templateMapping.FieldMappings.Select(tm => tm.CwbField.Id).Contains(field["CWB Field Id"]))
                    {
                        DeleteItem(field);
                    }
                }

                int sortOrder = 1;
                foreach (var templateField in templateMapping.FieldMappings)
                {
                    var field = fields.FirstOrDefault(fm => fm["CWB Field Id"] == templateField.CwbField.Id);
                    if (field != null)
                    {
                        UpdateFieldMapping(field, templateField.CmsField, sortOrder);
                    }
                    else
                    {
                        CreateFieldMapping(templateMappingItem, templateField, sortOrder);
                    }

                    sortOrder++;
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
            var accountSettings = accountsRepository.GetAccountSettings();

            var templateFolderId = accountSettings.TemplatesRootFolderId;
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

        public List<CmsTemplate> GetAvailableOptionsContentFolder()
        {
            var accountSettings = accountsRepository.GetAccountSettings();
            var optionsContentFolderId = accountSettings.OptionsContentFolderId;

            if (string.IsNullOrEmpty(optionsContentFolderId))
            {
                optionsContentFolderId = Constants.TemplateFolderId;
            }

            var model = new List<CmsTemplate>();
            var optionContentFolders = GetOptionContentFolders(optionsContentFolderId);

            foreach (var optionContentFolder in optionContentFolders)
            {
                var cmsTemplate = new CmsTemplate
                {
                    TemplateName = optionContentFolder.Name,
                    TemplateId = optionContentFolder.ID.ToString()
                };

                model.Add(cmsTemplate);
            }

            return model;
        }

        public List<CmsTemplate> GetAvailableOptionTemplates()
        {
            var accountSettings = accountsRepository.GetAccountSettings();
            var optionsTemplateId = accountSettings.OptionsTemplateId;

            if (string.IsNullOrEmpty(optionsTemplateId))
            {
                optionsTemplateId = Constants.TemplateFolderId;
            }

            var model = new List<CmsTemplate>();
            var optionTemplates = GetOptionTemplates(optionsTemplateId);

            foreach (var optionTemplate in optionTemplates)
            {
                var cmsTemplate = new CmsTemplate
                {
                    TemplateName = optionTemplate.Name,
                    TemplateId = optionTemplate.ID.ToString()
                };

                model.Add(cmsTemplate);
            }

            return model;
        }

        public Dictionary<string, string> GetAllowedMappings()
        {
            var settings = Factory.CreateObject("Bynder.Content.SitecoreConnector/mappings", false) as MappingSettings;

            if (settings == null || settings.AllowedMappings == null || settings.AllowedMappings.Count == 0)
            {
                return DefaultAllowedMappings;
            }

            return settings.AllowedMappings;
        }

        public string GetOrCreatePageComponentsFolder(string parentId)
        {
            string result = null;
            if (parentId != null)
            {
                using (new SecurityDisabler())
                {
                    var template = ContextDatabase.GetTemplate(new ID(Constants.PageComponentsId));
                    if (template != null)
                    {
                        var parent = ContextDatabase.GetItem(new ID(parentId));
                        if (parent != null)
                        {
                            try
                            {
                                var folderItem = parent.Children.FirstOrDefault(x => x.Name == Constants.PageComponents);
                                if (folderItem == null)
                                {
                                    folderItem = parent.Add(Constants.PageComponents, template);
                                }
                                result = folderItem.ID.ToString();
                            }
                            catch (Exception)
                            {
                                throw new Exception(string.Format("Item cannot be created: parent='{0}, Item name='{1}'", parent.Name, Constants.PageComponents));
                            }
                        }
                    }
                }
            }

            return result;
        }

        public string GetOrCreateContainer(string parentId, string templateId)
        {
            string result = null;
            if (parentId != null)
            {
                var parent = ContextDatabase.GetItem(new ID(parentId));
                if (parent != null)
                {
                    var containerItem = parent.Children.FirstOrDefault(x => x.TemplateID == new ID(templateId));
                    if (containerItem != null)
                    {
                        result = containerItem.ID.ToString();
                    }
                    else
                    {
                        var template = ContextDatabase.GetTemplate(new ID(templateId));
                        if (template != null)
                        {
                            var newContainerItem = parent.Add(template.Name, template);
                            if (newContainerItem != null)
                            {
                                result = newContainerItem.ID.ToString();
                            }
                        }
                    }
                }
            }

            return result;
        }

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

        private IEnumerable<Item> GetOptionRootFolders(string id)
        {
            var item = GetItem(id);
            return item != null ? item.Axes.GetDescendants().Where(t => t.TemplateName == "Options Folder").ToList() : Enumerable.Empty<Item>();
        }

        private IEnumerable<Item> GetOptionContentFolders(string id)
        {
            var item = GetItem(id);
            return item != null ? item.Axes.GetDescendants().Where(t => t.TemplateName == "System Folder").ToList() : Enumerable.Empty<Item>();
        }

        private IEnumerable<Item> GetOptionTemplates(string id)
        {
            var item = GetItem(id);
            return item != null ? item.Axes.GetDescendants().Where(t => t.TemplateName == "Template").ToList() : Enumerable.Empty<Item>();
        }

        private IEnumerable<Item> GetTemplateMappingsByCwbTemplateId(string cwbTemplateId)
        {
            var homeItem = GetItem(Constants.AccountItemId, ContextLanguage);
            if (homeItem != null)
            {
                return homeItem.Axes.GetDescendants()
                    .Where(item => item["CWB Template"] == cwbTemplateId &
                                   item.TemplateID == new ID(Constants.CwbTemplateMapping)).ToList();
            }
            return null;
        }

        private List<Item> GetFieldMappingItems(string templateMappingId)
        {
            var parentItem = GetItem(templateMappingId);
            return parentItem.Axes.GetDescendants().ToList();
        }

        private IEnumerable<TemplateMapping> ConvertSitecoreTemplatesToModel(IEnumerable<Item> templates, string projectName = null)
        {
            var result = new List<TemplateMapping>();
            foreach (var templateMapping in templates)
            {
                var mapping = new TemplateMapping();
                MapSitecoreTemplateToModel(templateMapping, mapping, projectName);
                result.Add(mapping);
            }

            return result;
        }

        private void MapSitecoreTemplateToModel(Item templateMapping, TemplateMapping mapping, string projectName = null)
        {
            if (mapping == null)
            {
                mapping = new TemplateMapping();
            }

            var fields = ConvertSitecoreFieldsToModel(templateMapping.Children);
            mapping.MappingId = templateMapping.ID.ToString();
            mapping.FieldMappings = fields.ToList();
            mapping.CwbTemplate = new CwbTemplate
            {
                CwbTemplateId = templateMapping["CWB Template"],
                CwbTemplateName = templateMapping["CWB Template Name"],
            };

            var accountSettings = accountsRepository.GetAccountSettings();
            var dateFormat = accountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }

            mapping.IsRelated = templateMapping.TemplateName == Constants.TemplateRelatedMappingName;
            var relatedMapping = templateMapping.Fields[Constants.MainMappingFieldName];
            mapping.MappingGroupId = relatedMapping != null && relatedMapping.HasValue
                ? ((Sitecore.Data.Fields.ReferenceField)relatedMapping).TargetItem["Template mapping title"]
                : mapping.MappingGroupId = templateMapping["Template mapping title"];


            var scTemplate = GetItem(templateMapping["Sitecore Template"]);
            if (scTemplate != null)
            {
                mapping.CmsTemplate = new CmsTemplate
                {
                    TemplateName = scTemplate.Name,
                    TemplateId = scTemplate.ID.ToString(),
                    TemplateFields = GetTemplateFields(scTemplate)
                };

                mapping.MappingTitle = templateMapping["Template mapping title"];
                mapping.DefaultLocationId = templateMapping["Default Location"];
                mapping.DefaultLocationTitle = GetItem(templateMapping["Default Location"]) != null
                    ? GetItem(templateMapping["Default Location"]).Paths.Path
                    : "";
                mapping.LastMappedDateTime = DateUtil.IsoDateToDateTime(templateMapping["Last Mapped Date"]).ToString(dateFormat);
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
                mapping.CwbProjectName = projectName;
            }
        }

        private void MapSitecoreRelatedTemplateMappingToModel(Item templateMapping, RelatedTemplateMapping mapping, string projectName = null)
        {
            MapSitecoreTemplateToModel(templateMapping, mapping, projectName);
            mapping.CmsMainMappingId = templateMapping[Constants.MainMappingFieldName];
            mapping.CmsContainerTemplateId = templateMapping[Constants.ContainerFieldName];

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

        private IEnumerable<Sitecore.Data.Templates.TemplateField> GetFields(Item template)
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
            if (field == null)
            {
                return null;
            }

            var result = new FieldMapping
            {
                CmsField = new CmsField
                {
                    TemplateField = new CmsTemplateField
                    {
                        FieldId = fieldMapping["Sitecore Field"],
                        FieldName = field.Name,
                        FieldType = field["Type"],
                        OptionsContentFolderId = fieldMapping["Options Content Folder"],
                        OptionsTemplateId = fieldMapping["Options Template"]
                    }
                },
                CwbField = new CwbField
                {
                    Id = fieldMapping["CWB Field Id"],
                    Name = fieldMapping["CWB Field"]
                }
            };
            return result;
        }

        private void SetupLinkedCwbTemplate(TemplateMapping templateMapping)
        {
            var linkedCwbTemplate = ContextDatabase.GetTemplate(new ID(Constants.CwbLinkItemTemplateID));
            if (templateMapping.CmsTemplate == null)
            {
                return;
            }

            var sitecoreTemplate = GetItem(templateMapping.CmsTemplate.TemplateId);
            if (sitecoreTemplate == null)
            {
                return;
            }

            var baseTemplates = sitecoreTemplate[FieldIDs.BaseTemplate];
            if (!baseTemplates.ToLower().Contains(linkedCwbTemplate.ID.ToString().ToLower()))
            {
                using (new SecurityDisabler())
                {
                    sitecoreTemplate.Editing.BeginEdit();
                    sitecoreTemplate[Sitecore.FieldIDs.BaseTemplate] = string.Format("{0}|{1}", baseTemplates, linkedCwbTemplate.ID);
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
                    var project = ContextDatabase.GetTemplate(new ID(Constants.CwbFolderId));
                    var validFolderName = ItemUtil.ProposeValidItemName(name);
                    folder.Add(validFolderName, project);
                }
            }
        }

        private Item CreateOrGetProject(string cwbProjectId, string cwbProjectName)
        {
            var projectItem = GetProject(cwbProjectId);

            if (projectItem != null)
            {
                return projectItem;
            }

            var accountSettings = accountsRepository.GetAccountSettings();

            var parentItem = GetItem(accountSettings.AccountItemId);

            using (new SecurityDisabler())
            {
                var template = ContextDatabase.GetTemplate(new ID(Constants.CwbProject));
                var validFolderName = ItemUtil.ProposeValidItemName(cwbProjectName);
                projectItem = parentItem.Add(validFolderName, template);
                using (new SecurityDisabler())
                {
                    projectItem.Editing.BeginEdit();
                    projectItem.Fields["Id"].Value = cwbProjectId;
                    projectItem.Fields["Name"].Value = cwbProjectName;
                    projectItem.Editing.EndEdit();
                }
                CreateProjectFolders(projectItem.ID.ToString());
            }

            return projectItem;
        }

        private Item CreateTemplateMapping(TemplateMapping templateMapping)
        {
            var scProject = CreateOrGetProject(templateMapping.CwbProjectId, templateMapping.CwbProjectName);

            if (scProject == null)
            {
                return null;
            }

            var mappingsFolder = GetMappingFolder(scProject);
            if (mappingsFolder == null)
            {
                return null;
            }

            using (new SecurityDisabler())
            {
                var mapping = ContextDatabase.GetTemplate(new ID(Constants.CwbTemplateMapping));
                var accountSettings = accountsRepository.GetAccountSettings();

                SetupLinkedCwbTemplate(templateMapping);

                var validFolderName = ItemUtil.ProposeValidItemName(templateMapping.MappingTitle);
                var createdItem = mappingsFolder.Add(validFolderName, mapping);
                using (new SecurityDisabler())
                {
                    createdItem.Editing.BeginEdit();
                    createdItem.Fields["Sitecore Template"].Value = templateMapping.CmsTemplate.TemplateId;
                    createdItem.Fields["Default Location"].Value = templateMapping.DefaultLocationId;
                    createdItem.Fields["CWB Template Name"].Value = templateMapping.CwbTemplate.CwbTemplateName;

                    if (!string.IsNullOrEmpty(templateMapping.MappingTitle))
                    {
                        createdItem.Fields["Template mapping title"].Value = templateMapping.MappingTitle;
                    }
                    createdItem.Fields["CWB Template"].Value = templateMapping.CwbTemplate.CwbTemplateId;
                    createdItem.Fields["Last Mapped Date"].Value = DateUtil.ToIsoDate(DateTime.Now);

                    double d;
                    DateTime cwbUpdateDate = DateTime.MinValue;
                    if (Double.TryParse(templateMapping.LastUpdatedDate, out d))
                    {
                        var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
                        cwbUpdateDate = TimeZoneInfo.ConvertTimeFromUtc(posixTime.AddMilliseconds(d * 1000), TimeZoneInfo.Local);
                    }

                    createdItem.Fields["Last Updated in CWB"].Value = DateUtil.ToIsoDate(cwbUpdateDate);
                    createdItem.Editing.EndEdit();
                }

                return createdItem;
            }
        }

        private Item CreateRelatedTemplateMapping(Item relatedMappingItem, RelatedTemplateMapping templateMapping)
        {
            if (relatedMappingItem == null) return null;

            using (new SecurityDisabler())
            {
                var mapping = ContextDatabase.GetTemplate(new ID(Constants.CwbRelatedTemplateMapping));
                var accountSettings = accountsRepository.GetAccountSettings();

                SetupLinkedCwbTemplate(templateMapping);

                var validFolderName = ItemUtil.ProposeValidItemName(templateMapping.MappingTitle);
                var createdItem = relatedMappingItem.Parent.Add(validFolderName, mapping);

                using (new SecurityDisabler())
                {
                    createdItem.Editing.BeginEdit();
                    createdItem.Fields["Sitecore Template"].Value = templateMapping.CmsTemplate.TemplateId;
                    createdItem.Fields["Default Location"].Value = templateMapping.DefaultLocationId;
                    createdItem.Fields["CWB Template Name"].Value = templateMapping.CwbTemplate.CwbTemplateName;

                    if (!string.IsNullOrEmpty(templateMapping.MappingTitle))
                    {
                        createdItem.Fields["Template mapping title"].Value = templateMapping.MappingTitle;
                    }
                    createdItem.Fields["CWB Template"].Value = templateMapping.CwbTemplate.CwbTemplateId;
                    createdItem.Fields["Last Mapped Date"].Value = DateUtil.ToIsoDate(DateTime.Now);

                    double d;
                    DateTime cwbUpdateDate = DateTime.MinValue;
                    if (Double.TryParse(templateMapping.LastUpdatedDate, out d))
                    {
                        var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
                        cwbUpdateDate = TimeZoneInfo.ConvertTimeFromUtc(posixTime.AddMilliseconds(d * 1000), TimeZoneInfo.Local);
                    }

                    createdItem.Fields["Last Updated in CWB"].Value = DateUtil.ToIsoDate(cwbUpdateDate);
                    createdItem.Fields[Constants.MainMappingFieldName].Value = templateMapping.CmsMainMappingId;
                    createdItem.Fields[Constants.ContainerFieldName].Value = templateMapping.CmsContainerTemplateId;
                    createdItem.Editing.EndEdit();
                }

                return createdItem;
            }
        }
        private void CreateFieldMapping(Item templateMappingItem, FieldMapping fieldMapping, int sortOrder)
        {
            var validName = ItemUtil.ProposeValidItemName(fieldMapping.CwbField.Name + " " + fieldMapping.CwbField.Id);
            using (new SecurityDisabler())
            {
                var mapping = ContextDatabase.GetTemplate(new ID(Constants.CwbFieldMapping));

                var createdItem = templateMappingItem.Add(validName, mapping);
                using (new SecurityDisabler())
                {
                    createdItem.Editing.BeginEdit();
                    createdItem.Fields["CWB Field"].Value = fieldMapping.CwbField.Name;
                    createdItem.Fields["CWB Field Id"].Value = fieldMapping.CwbField.Id;
                    createdItem.Fields["Sitecore Field"].Value = fieldMapping.CmsField.TemplateField.FieldId;
                    createdItem.Fields["Options Content Folder"].Value = fieldMapping.CmsField.TemplateField.OptionsContentFolderId;
                    createdItem.Fields["Options Template"].Value = fieldMapping.CmsField.TemplateField.OptionsTemplateId;
                    createdItem.Fields[FieldIDs.Sortorder].Value = sortOrder.ToString();
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

                double d;
                DateTime cwbUpdateDate = DateTime.MinValue;
                if (Double.TryParse(templateMapping.LastUpdatedDate, out d))
                {
                    var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
                    cwbUpdateDate = TimeZoneInfo.ConvertTimeFromUtc(posixTime.AddMilliseconds(d * 1000), TimeZoneInfo.Local);
                }
                
                item.Fields["Last Updated in CWB"].Value = DateUtil.ToIsoDate(cwbUpdateDate);

                item.Editing.EndEdit();
            }
        }

        private void UpdateRelatedTemplateMapping(Item item, RelatedTemplateMapping templateMapping)
        {
            UpdateTemplateMapping(item, templateMapping);

            using (new SecurityDisabler())
            {
                item.Editing.BeginEdit();
                if (templateMapping.CmsMainMappingId != null)
                {
                    item.Fields[Constants.MainMappingFieldName].Value = templateMapping.CmsMainMappingId;
                    item.Fields[Constants.ContainerFieldName].Value = templateMapping.CmsContainerTemplateId;
                }
                item.Editing.EndEdit();
            }
        }

        private void UpdateFieldMapping(Item item, CmsField cmsField, int sortOrder)
        {
            using (new SecurityDisabler())
            {
                item.Editing.BeginEdit();
                item.Fields["Sitecore Field"].Value = cmsField.TemplateField.FieldId;
                item.Fields["Options Content Folder"].Value = cmsField.TemplateField.OptionsContentFolderId;
                item.Fields["Options Template"].Value = cmsField.TemplateField.OptionsTemplateId;
                item.Fields[FieldIDs.Sortorder].Value = sortOrder.ToString();
                item.Editing.EndEdit();
            }
        }

        private void DeleteItem(Item template)
        {
            using (new SecurityDisabler())
            {
                template.Delete();
            }
        }
    }
}
