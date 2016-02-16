using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Mapping;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.SitecoreRepositories.Repositories;
using GatherContent.Connector.Managers.Models.ImportItems;

namespace GatherContent.Connector.Managers.Managers
{
    public enum TryMapItemState
    {
        Success = 0,
        TemplateError = 1,
        FieldError = 2
    }

    public class MappingManager : BaseManager
    {
        #region Constants
        public const string FieldGcContentId = "{955A4DD9-6A01-458E-9791-3C99F5E076A8}";
        public const string FieldLastSyncDate = "{F9D2EA57-86A2-45CF-9C28-8D8CA72A2669}";
        #endregion


        private readonly MappingRepository _mappingRepository;
        private readonly TemplatesRepository _templatesRepository;
        
        private readonly TemplatesService _templateService;
        private readonly ProjectsService _projectService;
        private readonly ItemsService _itemService;

        private readonly GCAccountSettings _accountSettings;

        public MappingManager()
        {
            var accountsRepository = new AccountsRepository();
            _accountSettings = accountsRepository.GetAccountSettings();

            _mappingRepository = new MappingRepository();
            _templatesRepository = new TemplatesRepository();


            _templateService = new TemplatesService(_accountSettings);
            _itemService = new ItemsService(_accountSettings);
            _projectService = new ProjectsService(_accountSettings);
        }


        #region Utilities


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



        private DateTime ConvertMsecToDate(double date)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var gcUpdateDate =
                posixTime.AddMilliseconds(date * 1000);
            return gcUpdateDate;
        }

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

        private AddMappingModel MapAddMappingModel(AddMapping addMappingModel)
        {

            var addSitecoreMappingModel = new AddMappingModel
            {
                IsEdit = addMappingModel.IsEdit,
                GcTemplateId = addMappingModel.GcTemplateId,
                SelectedTemplateId = addMappingModel.SelectedTemplateId,
                GcMappingTitle = addMappingModel.GcMappingTitle
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


        #endregion


        public List<MappingModel> GetMappingModel()
        {
            var mappings = _mappingRepository.GetMappings();

            var model = mappings.Select(cmsMappingModel => new MappingModel(cmsMappingModel.GcProjectName, cmsMappingModel.GcTemplateId,
                cmsMappingModel.GcTemplateName, cmsMappingModel.GcTemplateProxy, cmsMappingModel.CmsTemplateName, cmsMappingModel.LastMappedDateTime,
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
                    //throw;
                }

            }

            return model;
        }




        public TemplateMapModel GetTemplateMappingModel(string id, string gcTemplateProxyId)
        {
            var model = new TemplateMapModel();

            var template = GetGcTemplateEntity(id);
            var project = GetGcProjectEntity(template.Data.ProjectId.ToString());

            model.GcProjectName = project.Data.Name;
            model.GcTemplateName = template.Data.Name;
            model.GcTemplateProxyId = gcTemplateProxyId;

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
            var addMappingModel = _mappingRepository.GetAddMappingModel(project.Data.Id.ToString(), template, gcTemplateProxyId);

            var templates = MapSitecoreTemplates(scTemplates);
            var addSitecoreMappingModel = MapAddMappingModel(addMappingModel);

            model.SitecoreTemplates.AddRange(templates);
            model.AddMappingModel = addSitecoreMappingModel;
            model.Rules = GetMapRules();

            return model;
        }

        public void DeleteMapping(string id, string gcTemplateProxyId)
        {
            var template = _templateService.GetSingleTemplate(id);

            _mappingRepository.DeleteMapping(template, gcTemplateProxyId);
            //_mappingRepository.DeleteTemplate(template);


        }

        public void PostMapping(List<TemplateTab> model, bool isEdit, string templateId, string selectedTemplateId, string gcMappingTitle, string gcTemplateProxyId)
        {
            var template = _templateService.GetSingleTemplate(templateId);
            var project = _projectService.GetSingleProject(template.Data.ProjectId.ToString());

            var list = (from tab in model
                        from templateField in tab.Fields
                        select new CmsTemplateField
                        {
                            FieldName = templateField.FieldName,
                            FieldId = templateField.FieldId,
                            SelectedField = templateField.SelectedField,
                        }).ToList();

            if (isEdit)
            {

                _mappingRepository.UpdateMapping(project.Data.Id, template.Data.Id, new TemplateMapping
                {
                    SitecoreTemplateId = selectedTemplateId,
                    Name = template.Data.Name,
                    GcTemplateId = template.Data.Id.ToString(),
                    GcMappingTitle = gcMappingTitle,
                    GcTemplateProxy = gcTemplateProxyId
                }, list);

            }
            else
            {
                _mappingRepository.CreateMapping(project.Data.Id, new TemplateMapping
                {
                    SitecoreTemplateId = selectedTemplateId,
                    Name = template.Data.Name,
                    GcTemplateId = template.Data.Id.ToString(),
                    LastUpdated = template.Data.Updated.ToString(),
                    GcMappingTitle = gcMappingTitle,
                    GcTemplateProxy = gcTemplateProxyId
                }, list);

            }

        }


        public List<MappingResultModel> MapItems(List<GCItem> items, string projectId)
        {
            List<MappingTemplateModel> templates = _mappingRepository.GetTemplateMappings(projectId);

            List<MappingResultModel> result = TryMapItems(items, templates);

            return result;
        }

        public List<MappingResultModel> MapItems(List<GCItem> items)
        {
            List<MappingTemplateModel> templates = _mappingRepository.GetAllTemplateMappings();
            List<MappingResultModel> result = TryMapItems(items, templates);

            return result;
        }


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
                    TryMapItem(gcItem.Data, gcTemplate, importItem.SelectedMappingId, out cmsItem);
                    result.Add(cmsItem);
                }
            }

            return result;
        }

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

        private GCTemplate GetTemplate(int templateId, Dictionary<int, GCTemplate> templatesDictionary)
        {
            GCTemplate gcTemplate;
            templatesDictionary.TryGetValue(templateId, out gcTemplate);
            if (gcTemplate == null)
            {
                gcTemplate = _templateService.GetSingleTemplate(templateId.ToString()).Data;
                templatesDictionary.Add(templateId, gcTemplate);
            }

            return gcTemplate;
        }

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

        private string GetValue(List<Element> fields)
        {
            string value = string.Join("", fields.Select(i => i.Value));
            return value;
        }

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

        private bool IsMappedFieldsHaveDifrentTypes(List<Element> fields)
        {
            return fields.Select(i => i.Type).Distinct().Count() > 1;
        }

        private List<Element> GetFieldsForMapping(IGrouping<string, MappingFieldModel> fieldsMappig, List<Element> gcFields)
        {
            IEnumerable<string> gsFiledNames = fieldsMappig.Select(i => i.GCField);
            IEnumerable<Element> gcFieldsForMapping = gcFields.Where(i => gsFiledNames.Contains(i.Name));

            return gcFieldsForMapping.ToList();
        }



        private void TryMapItem(GCItem gcItem, GCTemplate gcTemplate, string selectedMappingId, out MappingResultModel result)
        {
            bool isUpdate = gcItem is UpdateGCItem;

            List<Element> gcFields = gcItem.Config.SelectMany(i => i.Elements).ToList();
            MappingTemplateModel template = _mappingRepository.GetTemplateMappingsByTemplateId(selectedMappingId);


            if (template == null)
            {
                string errorMessage = isUpdate ? "Update failed: Template not mapped" : "Import failed: Template not mapped";
                result = new MappingResultModel(gcItem, null, gcTemplate.Name, null, string.Empty, errorMessage, false);
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
                result = new MappingResultModel(gcItem, null, gcTemplate.Name, null, string.Empty, errorMessage, false);
                return;
            }

            string cmsId = string.Empty;
            string message = "Import Successful";
            if (isUpdate)
            {
                cmsId = (gcItem as UpdateGCItem).CMSId;
                message = "Update Successful";
            }

            result = new MappingResultModel(gcItem, fields, gcTemplate.Name, template.CMSTemplateId, cmsId, message);
        }

    }
}
