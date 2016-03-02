using System;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>
    /// 
    /// </summary>
    public class MappingRepository : BaseSitecoreRepository, IMappingRepository
    {
        protected IAccountsRepository _accountsRepository;

        public MappingRepository(IAccountsRepository accountsRepository)
            : base()
        {
            _accountsRepository = accountsRepository;
        }


        #region Utilities
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectFolder"></param>
        /// <returns></returns>
        private Item GetMappingFolder(Item projectFolder)
        {
            Item mappingFolder = projectFolder.Children.FirstOrDefault(i => i.Name == Constants.MappingFolderName);
            return mappingFolder;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mappingFolder"></param>
        /// <returns></returns>
        private IEnumerable<Item> GetTemplateMappings(Item mappingFolder)
        {
            return mappingFolder.Children;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcProjectId"></param>
        /// <param name="gcTemplateId"></param>
        /// <returns></returns>
        private List<Item> GetTemplateMappings(string gcProjectId, string gcTemplateId)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateMappingId"></param>
        /// <param name="name"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <param name="sitecoreFieldId"></param>
        private void UpdateFieldMapping(Item field, string sitecoreFieldId)
        {
            using (new SecurityDisabler())
            {
                field.Editing.BeginEdit();
                field.Fields["Sitecore Field"].Value = sitecoreFieldId;
                field.Editing.EndEdit();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        private void DeleteItem(Item template)
        {
            using (new SecurityDisabler())
            {
                template.Delete();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templates"></param>
        /// <returns></returns>
        private IEnumerable<MappingTemplateModel> ConvertSitecoreTemplatesToModel(IEnumerable<Item> templates)
        {
            IEnumerable<MappingTemplateModel> result = templates.Select(ConvertSitecoreTemplateToModel);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        private MappingTemplateModel ConvertSitecoreTemplateToModel(Item template)
        {
            IEnumerable<MappingFieldModel> fields = ConvertSitecoreFieldsToModel(template.Children);
            var result = new MappingTemplateModel(template["Sitecore Template"], template["GC Template"], fields.ToList());

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        private IEnumerable<MappingFieldModel> ConvertSitecoreFieldsToModel(IEnumerable<Item> fields)
        {
            IEnumerable<MappingFieldModel> result = fields.Select(ConvertSitecoreFieldToModel);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        private MappingFieldModel ConvertSitecoreFieldToModel(Item field)
        {
            var result = new MappingFieldModel(field["Sitecore Field"], field["GC Field Id"]);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapping"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateMapping"></param>
        private void SetupLinkedGCTemplate(TemplateMapping templateMapping)
        {
            var linkedGCTemplate = ContextDatabase.GetTemplate(new ID(Constants.GCLinkItemTemplateID));
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <param name="templateMapping"></param>
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

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<CmsMappingModel> GetMappings()
        {
            //TODO remove m["Last Updated in GC"]
            var model = new List<CmsMappingModel>();
            var scProjects = GetAllProjects();

            foreach (var project in scProjects)
            {
                var templates = project.Axes.GetDescendants().Where(i => i.TemplateName == Constants.TemplateMappingName).ToList();
                if (templates.Any())
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

	                    var accountSettings = _accountsRepository.GetAccountSettings();

                        var dateFormat = accountSettings.DateFormat;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public List<MappingTemplateModel> GetTemplateMappings(string projectId)
        {
            Item projectFolder = GetProject(projectId);
            if (projectFolder == null) return null;
            Item mappingFolder = GetMappingFolder(projectFolder);

            IEnumerable<Item> mappings = GetTemplateMappings(mappingFolder);
            IEnumerable<MappingTemplateModel> result = ConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateId"></param>
        /// <returns></returns>
        public MappingTemplateModel GetTemplateMappingsByTemplateId(string templateId)
        {
            var template = GetItem(templateId);
            if (template == null) return null;
            var result = ConvertSitecoreTemplateToModel(template);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<MappingTemplateModel> GetAllTemplateMappings()
        {
            IEnumerable<Item> mappings = GetAllMappings();
            IEnumerable<MappingTemplateModel> result = ConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcProjectId"></param>
        /// <param name="gcTemplateId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="template"></param>
        /// <param name="scMappingId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmsMappingId"></param>
        public void DeleteMapping(string cmsMappingId)
        {
            var templateMapping = GetItem(cmsMappingId);
            if (templateMapping != null)
            {
                DeleteItem(templateMapping);
            }
        }
    }
}
