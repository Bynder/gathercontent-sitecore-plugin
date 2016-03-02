using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Mapping;
using GatherContent.Connector.Managers.Enums;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.SitecoreRepositories.Repositories;
using GatherContent.Connector.Managers.Models.ImportItems;

namespace GatherContent.Connector.Managers.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class MappingManager : BaseManager, IMappingManager
    {
        #region Constants

        public const string FieldGcContentId = "{955A4DD9-6A01-458E-9791-3C99F5E076A8}";
        public const string FieldLastSyncDate = "{F9D2EA57-86A2-45CF-9C28-8D8CA72A2669}";

        #endregion

        protected IMappingRepository _mappingRepository;
        protected ITemplatesRepository _templatesRepository;
        
        protected IItemsService _itemService;

        protected GCAccountSettings _accountSettings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mappingRepository"></param>
        /// <param name="templatesRepository"></param>
        /// <param name="accountsService"></param>
        /// <param name="projectsService"></param>
        /// <param name="templateService"></param>
        /// <param name="itemService"></param>
        /// <param name="cacheManager"></param>
        /// <param name="accountSettings"></param>
        public MappingManager(
            IMappingRepository mappingRepository,
            ITemplatesRepository templatesRepository,
            IAccountsService accountsService,
            IProjectsService projectsService,
            ITemplatesService templateService,
            IItemsService itemService,
            ICacheManager cacheManager,
            GCAccountSettings accountSettings) : base(accountsService, projectsService, templateService, cacheManager)
        {
	        _accountSettings = accountSettings;

            _mappingRepository = mappingRepository;
            _templatesRepository = templatesRepository;

            _itemService = itemService;
        }


        #region Utilities

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetMapRules()
        {
            return new Dictionary<string, string>
            {
                {"text", "Single-Line Text, Multi-Line Text, Rich Text"},
                {"section", "Single-Line Text, Multi-Line Text, Rich Text"},
                {"choice_radio", "Droptree, Checklist, Multilist, Multilist with Search, Treelist, TreelistEx"},
                {"choice_checkbox", "Checklist, Multilist, Multilist with Search, Treelist, TreelistEx"},
                {"files", "Image, File, Droptree, Multilist, Multilist with Search, Treelist, TreelistEx"}
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private DateTime ConvertMsecToDate(double date)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var gcUpdateDate =
                posixTime.AddMilliseconds(date * 1000);
            return gcUpdateDate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private string ConvertMsecToFormattedDate(double date)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var dateFormat = _accountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }
            var gcUpdateDate =
                posixTime.AddMilliseconds(date * 1000).ToString(dateFormat);
            return gcUpdateDate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scTemplates"></param>
        /// <returns></returns>
        private IEnumerable<SitecoreTemplate> MapSitecoreTemplates(IEnumerable<CmsTemplate> scTemplates)
        {
            var templates = new List<SitecoreTemplate>();

            foreach (var cmsTemplate in scTemplates)
            {
                var st = new SitecoreTemplate
                {
                    SitrecoreTemplateName = cmsTemplate.CmsTemplateName,
                    SitrecoreTemplateId = cmsTemplate.CmsTemplateId
                };
                st.SitecoreFields.Add(new SitecoreTemplateField { SitecoreFieldId = "0", SitrecoreFieldName = "Do not map" });
                foreach (var field in cmsTemplate.CmsFields)
                {
                    if (field.CmsFieldId != FieldGcContentId &&
                        field.CmsFieldId != FieldLastSyncDate)
                    {
                        var scField = new SitecoreTemplateField
                        {
                            SitrecoreFieldName = field.CmsFieldName,
                            SitecoreFieldId = field.CmsFieldId,
                            SitecoreFieldType = field.CmsFieldType

                        };
                        st.SitecoreFields.Add(scField);
                    }
                }
                templates.Add(st);
            }
            return templates;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="addMappingModel"></param>
        /// <returns></returns>
        private AddMappingModel MapAddMappingModel(AddMapping addMappingModel)
        {
            var addSitecoreMappingModel = new AddMappingModel
            {
                IsEdit = addMappingModel.IsEdit,
                GcTemplateId = addMappingModel.GcTemplateId,
                SelectedTemplateId = addMappingModel.SelectedTemplateId,
                GcMappingTitle = addMappingModel.GcMappingTitle,
                OpenerId = "drop-tree" + Guid.NewGuid(),
                DefaultLocation = addMappingModel.DefaultLocation,
                DefaultLocationTitle = addMappingModel.DefaultLocationTitle
            };

            foreach (var cmsTab in addMappingModel.Tabs)
            {
                var tab = new TemplateTab
                {
                    TabName = cmsTab.TabName
                };

                tab.Fields.AddRange(from t in cmsTab.Fields
                                    select new TemplateField
                                    {
                                        FieldName = t.FieldName,
                                        FieldId = t.FieldId,
                                        FieldType = t.FieldType,
                                        SelectedField = t.SelectedField,
                                    });
                addSitecoreMappingModel.Tabs.Add(tab);
            }
            return addSitecoreMappingModel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        private List<MappingResultModel> TryMapItems(List<GCItem> items, List<MappingTemplateModel> templates)
        {
            var result = new List<MappingResultModel>();
            var templatesDictionary = new Dictionary<int, GCTemplate>();

            foreach (GCItem gcItem in items)
            {
                GCTemplate gcTemplate = GetTemplate(gcItem.TemplateId.Value, templatesDictionary);

                MappingResultModel cmsItem;
                TryMapItem(gcItem, gcTemplate, templates, out cmsItem);
                result.Add(cmsItem);
            }

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="templatesDictionary"></param>
        /// <returns></returns>
        private GCTemplate GetTemplate(int templateId, Dictionary<int, GCTemplate> templatesDictionary)
        {
            GCTemplate gcTemplate;
            templatesDictionary.TryGetValue(templateId, out gcTemplate);
            if (gcTemplate == null)
            {
                gcTemplate = TemplatesService.GetSingleTemplate(templateId.ToString()).Data;
                templatesDictionary.Add(templateId, gcTemplate);
            }

            return gcTemplate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcItem"></param>
        /// <param name="gcTemplate"></param>
        /// <param name="selectedMappingId"></param>
        /// <param name="result"></param>
        /// <param name="selectedLocationId"></param>
        private void TryMapItem(GCItem gcItem, GCTemplate gcTemplate, string selectedMappingId, out MappingResultModel result, string selectedLocationId = null)
        {
            bool isUpdate = gcItem is UpdateGCItem;

            List<Element> gcFields = gcItem.Config.SelectMany(i => i.Elements).ToList();
            MappingTemplateModel template = _mappingRepository.GetTemplateMappingsByTemplateId(selectedMappingId);


            if (template == null)
            {
                string errorMessage = isUpdate ? "Update failed: Template not mapped" : "Import failed: Template not mapped";
                result = new MappingResultModel(gcItem, null, gcTemplate.Name, null, string.Empty, errorMessage, false, selectedLocationId);
                return;
            }

            List<ImportCMSField> fields;
            IEnumerable<IGrouping<string, MappingFieldModel>> groupedFields = template.Fields.GroupBy(i => i.CMSField);

            var files = new List<File>();
            if (gcItem.Config.SelectMany(config => config.Elements).Select(element => element.Type).Contains("files"))
            {

                foreach (var file in _itemService.GetItemFiles(gcItem.Id.ToString()).Data)
                {
                    files.Add(new File
                    {
                        FileName = file.FileName,
                        Url = file.Url,
                        FieldId = file.Field,
                        UpdatedDate = file.Updated
                    });


                }
            }


            TryMapItemState mapState = TryMapFields(gcFields, groupedFields, files, out fields);
            if (mapState == TryMapItemState.FieldError)
            {
                string errorMessage = isUpdate ? "Update failed: Template fields mismatch" : "Import failed: Template fields mismatch";
                result = new MappingResultModel(gcItem, null, gcTemplate.Name, null, string.Empty, errorMessage, false, selectedLocationId);
                return;
            }

            string cmsId = string.Empty;
            string message = "Import Successful";
            if (isUpdate)
            {
                cmsId = (gcItem as UpdateGCItem).CMSId;
                message = "Update Successful";
            }

            result = new MappingResultModel(gcItem, fields, gcTemplate.Name, template.CMSTemplateId, cmsId, message, true, selectedLocationId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="gcTemplate"></param>
        /// <param name="templates"></param>
        /// <param name="result"></param>
        private void TryMapItem(GCItem item, GCTemplate gcTemplate, List<MappingTemplateModel> templates, out MappingResultModel result)
        {
            bool isUpdate = item is UpdateGCItem;

            List<Element> gcFields = item.Config.SelectMany(i => i.Elements).ToList();


            MappingTemplateModel template;
            TryMapItemState templateMapState = TryGetTemplate(templates, item.TemplateId.ToString(), out template);

            if (templateMapState == TryMapItemState.TemplateError)
            {
                string errorMessage = isUpdate ? "Update failed: Template not mapped" : "Import failed: Template not mapped";
                result = new MappingResultModel(item, null, gcTemplate.Name, null, string.Empty, errorMessage, false);
                return;
            }
            
            List<ImportCMSField> fields;
            IEnumerable<IGrouping<string, MappingFieldModel>> groupedFields = template.Fields.GroupBy(i => i.CMSField);

            var files = new List<File>();
            if (item.Config.SelectMany(config => config.Elements).Select(element => element.Type).Contains("files"))
            {
                foreach (var file in _itemService.GetItemFiles(item.Id.ToString()).Data)
                {
                    files.Add(new File
                    {
                        FileName = file.FileName,
                        Url = file.Url,
                        FieldId = file.Field,
                        UpdatedDate = file.Updated
                    });
                }
            }
            
            TryMapItemState mapState = TryMapFields(gcFields, groupedFields, files, out fields);
            if (mapState == TryMapItemState.FieldError)
            {
                string errorMessage = isUpdate ? "Update failed: Template fields mismatch" : "Import failed: Template fields mismatch";
                result = new MappingResultModel(item, null, gcTemplate.Name, null, string.Empty, errorMessage, false);
                return;
            }

            string cmsId = string.Empty;
            string message = "Import Successful";
            if (isUpdate)
            {
                cmsId = (item as UpdateGCItem).CMSId;
                message = "Update Successful";
            }

            result = new MappingResultModel(item, fields, gcTemplate.Name, template.CMSTemplateId, cmsId, message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcFields"></param>
        /// <param name="fieldsMappig"></param>
        /// <param name="files"></param>
        /// <param name="importCMSField"></param>
        /// <returns></returns>
        private TryMapItemState TryMapField(List<Element> gcFields, IGrouping<string, MappingFieldModel> fieldsMappig, List<File> files, out ImportCMSField importCMSField)
        {
            var cmsFieldName = fieldsMappig.Key;

            var gcFieldsForMapping = GetFieldsForMapping(fieldsMappig, gcFields);


            var field = gcFieldsForMapping.FirstOrDefault();

            if (field == null)
            {
                importCMSField = new ImportCMSField(string.Empty, cmsFieldName, null, string.Empty, null, null);
                return TryMapItemState.FieldError;
            }
            
            if (IsMappedFieldsHaveDifrentTypes(gcFieldsForMapping))
            {
                importCMSField = new ImportCMSField(string.Empty, cmsFieldName, field.Label, string.Empty, null, null);
                return TryMapItemState.FieldError;
            }

            var value = GetValue(gcFieldsForMapping);
            var options = GetOptions(gcFieldsForMapping);
            files = files.Where(item => item.FieldId == field.Name).ToList();

            importCMSField = new ImportCMSField(field.Type, cmsFieldName, field.Label, value, options, files);

            return TryMapItemState.Success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templates"></param>
        /// <param name="templateId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private TryMapItemState TryGetTemplate(List<MappingTemplateModel> templates, string templateId, out MappingTemplateModel result)
        {
            if (templates == null)
            {
                result = null;
                return TryMapItemState.TemplateError;
            }

            result = templates.FirstOrDefault(i => templateId == i.GCTemplateId);
            if (result == null)
                return TryMapItemState.TemplateError;

            return TryMapItemState.Success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcFields"></param>
        /// <param name="fieldsMappig"></param>
        /// <param name="files"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private TryMapItemState TryMapFields(List<Element> gcFields, IEnumerable<IGrouping<string, MappingFieldModel>> fieldsMappig, List<File> files, out List<ImportCMSField> result)
        {
            result = new List<ImportCMSField>();
            foreach (IGrouping<string, MappingFieldModel> grouping in fieldsMappig)
            {
                ImportCMSField cmsField;
                TryMapItemState mapState = TryMapField(gcFields, grouping, files, out cmsField);
                if (mapState == TryMapItemState.FieldError)
                    return mapState;
                result.Add(cmsField);
            }

            return TryMapItemState.Success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        private string GetValue(List<Element> fields)
        {
            string value = string.Join("", fields.Select(i => i.Value));
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        private List<Option> GetOptions(List<Element> fields)
        {
            var result = new List<Option>();
            foreach (Element field in fields)
            {
                if (field.Options != null)
                    result.AddRange(field.Options);
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        private bool IsMappedFieldsHaveDifrentTypes(List<Element> fields)
        {
            return fields.Select(i => i.Type).Distinct().Count() > 1;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldsMappig"></param>
        /// <param name="gcFields"></param>
        /// <returns></returns>
        private List<Element> GetFieldsForMapping(IGrouping<string, MappingFieldModel> fieldsMappig, List<Element> gcFields)
        {
            IEnumerable<string> gsFiledNames = fieldsMappig.Select(i => i.GCField);
            IEnumerable<Element> gcFieldsForMapping = gcFields.Where(i => gsFiledNames.Contains(i.Name));

            return gcFieldsForMapping.ToList();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<MappingModel> GetMappingModel()
        {
            var mappings = _mappingRepository.GetMappings();

            var model = mappings.Select(cmsMappingModel => new MappingModel(cmsMappingModel.GcProjectName, cmsMappingModel.GcTemplateId,
                cmsMappingModel.GcTemplateName, cmsMappingModel.CmsMappingId, cmsMappingModel.CmsTemplateName, cmsMappingModel.CmsMappingTitle, cmsMappingModel.LastMappedDateTime,
                cmsMappingModel.LastUpdatedDate, cmsMappingModel.EditButtonTitle, cmsMappingModel.IsMapped, cmsMappingModel.IsHighlightingDate)).ToList();
            foreach (var mapping in model)
            {
                try
                {
                    var template = GetGcTemplateEntity(mapping.GcTemplateId);
                    if (template == null)
                    {
                        mapping.LastUpdatedDate = "Removed from GatherContent ";
                        mapping.RemovedFromGc = true;
                    }
                    else
                    {
                        var gcUpdateDate = ConvertMsecToDate((double)template.Data.Updated);
                        var dateFormat = _accountSettings.DateFormat;
                        if (string.IsNullOrEmpty(dateFormat))
                        {
                            dateFormat = Constants.DateFormat;
                        }
                        if (mapping.IsMapped)
                        {
                            mapping.IsHighlightingDate =
                                DateTime.ParseExact(mapping.LastMappedDateTime, dateFormat,
                                    CultureInfo.InvariantCulture) < gcUpdateDate;
                        }

                        mapping.LastUpdatedDate = gcUpdateDate.ToString(dateFormat);
                        mapping.RemovedFromGc = false;

                    }
                }
                catch (Exception)
                {
                    mapping.LastUpdatedDate = "Removed from GatherContent";
                    mapping.RemovedFromGc = true;
                }

            }

            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="scMappingId"></param>
        /// <returns></returns>
        public TemplateMapModel GetTemplateMappingModel(string id, string scMappingId)
        {
            var model = new TemplateMapModel();

            var template = GetGcTemplateEntity(id);
            var project = GetGcProjectEntity(template.Data.ProjectId.ToString());

            model.GcProjectName = project.Data.Name;
            model.GcTemplateName = template.Data.Name;
            model.ScMappingId = scMappingId;

            var templateFolderId = _accountSettings.TemplateFolderId;
            if (string.IsNullOrEmpty(templateFolderId))
            {
                templateFolderId = Constants.TemplateFolderId;
            }
            var scTemplates = _templatesRepository.GetTemplatesModel(templateFolderId);
            if (scTemplates.Count == 0)
            {
                throw new Exception("Template folder is empty");
            }
            var addMappingModel = _mappingRepository.GetAddMappingModel(project.Data.Id.ToString(), template, scMappingId);

            var templates = MapSitecoreTemplates(scTemplates);
            var addSitecoreMappingModel = MapAddMappingModel(addMappingModel);

            model.SitecoreTemplates.AddRange(templates);
            model.AddMappingModel = addSitecoreMappingModel;
            model.Rules = GetMapRules();

            return model;
        }



        public void PostMapping(PostMappingModel model)
        {
            var template = TemplatesService.GetSingleTemplate(model.TemplateId);
            var project = ProjectsService.GetSingleProject(template.Data.ProjectId.ToString());

            var list = (from tab in model.TemplateTabs
                        from templateField in tab.Fields
                        select new CmsTemplateField
                        {
                            FieldName = templateField.FieldName,
                            FieldId = templateField.FieldId,
                            SelectedField = templateField.SelectedField,
                        }).ToList();

            if (model.IsEdit)
            {
                _mappingRepository.UpdateMapping(project.Data.Id, new TemplateMapping
                {
                    SitecoreTemplateId = model.SelectedTemplateId,
                    Name = template.Data.Name,
                    GcTemplateId = template.Data.Id.ToString(),
                    GcMappingTitle = model.GcMappingTitle,
                    CmsMappingId = model.ScMappingId,
                    DefaultLocation = model.DefaultLocation
                }, list);

            }
            else
            {
                _mappingRepository.CreateMapping(project.Data.Id, new TemplateMapping
                {
                    SitecoreTemplateId = model.SelectedTemplateId,
                    Name = template.Data.Name,
                    GcTemplateId = template.Data.Id.ToString(),
                    LastUpdated = template.Data.Updated.ToString(),
                    GcMappingTitle = model.GcMappingTitle,
                    CmsMappingId = model.ScMappingId,
                    DefaultLocation = model.DefaultLocation
                }, list);

            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scMappingId"></param>
        public void DeleteMapping(string scMappingId)
        {
            _mappingRepository.DeleteMapping(scMappingId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public List<MappingResultModel> MapItems(List<GCItem> items, string projectId)
        {
            List<MappingTemplateModel> templates = new List<MappingTemplateModel>(); //TODO:_mappingRepository.GetTemplateMappings(projectId);

            List<MappingResultModel> result = TryMapItems(items, templates);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public List<MappingResultModel> MapItems(List<GCItem> items)
        {
            List<MappingTemplateModel> templates = new List<MappingTemplateModel>(); //TODO:_mappingRepository.GetAllTemplateMappings();
            List<MappingResultModel> result = TryMapItems(items, templates);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public List<MappingResultModel> MapItems(List<ImportItemModel> items)
        {
            var result = new List<MappingResultModel>();
            var templatesDictionary = new Dictionary<int, GCTemplate>();

            foreach (var importItem in items)
            {
                var gcItem = _itemService.GetSingleItem(importItem.Id);

                if (gcItem != null && gcItem.Data != null && gcItem.Data.TemplateId != null)
                {
                    GCTemplate gcTemplate = GetTemplate(gcItem.Data.TemplateId.Value, templatesDictionary);

                    MappingResultModel cmsItem;
                    TryMapItem(gcItem.Data, gcTemplate, importItem.SelectedMappingId, out cmsItem, importItem.DefaultLocation);
                    result.Add(cmsItem);
                }
            }

            return result;
        }
    }
}
