using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Mapping;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.SitecoreRepositories;
using GatherContent.Connector.SitecoreRepositories.Repositories;

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
        private readonly MappingRepository _mappingRepository;
        private readonly TemplatesRepository _templatesRepository;

        private readonly TemplatesService _templateService;
        private readonly ProjectsService _projectService;

        private readonly GCAccountSettings _accountSettings;

        public MappingManager()
        {
            var accountsRepository = new AccountsRepository();
            _accountSettings = accountsRepository.GetAccountSettings();

            _mappingRepository = new MappingRepository();
            _templatesRepository = new TemplatesRepository();

            _templateService = new TemplatesService(_accountSettings);
            _projectService = new ProjectsService(_accountSettings);
        }


        #region Utilities

        private string ConvertMsecToDate(double date)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var gcUpdateDate =
                posixTime.AddMilliseconds(date * 1000).ToString(_accountSettings.DateFormat);
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
                    var scField = new SitecoreTemplateField
                    {
                        SitrecoreFieldName = field.CmsFieldName,
                        SitecoreFieldId = field.CmsFieldId,
                        SitecoreFieldType = field.CmsFieldType

                    };
                    st.SitecoreFields.Add(scField);
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
                                        SelectedField = t.SelectedField
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
                cmsMappingModel.GcTemplateName, cmsMappingModel.CmsTemplateName, cmsMappingModel.LastMappedDateTime,
                cmsMappingModel.LastUpdatedDate, cmsMappingModel.EditButtonTitle, cmsMappingModel.IsMapped)).ToList();
            foreach (var mapping in model)
            {
                try
                {
                    var template = _templateService.GetSingleTemplate(mapping.GcTemplateId);
                    if (template == null)
                    {
                        mapping.LastUpdatedDate = "Removed from GC";
                        mapping.RemovedFromGc = true;
                    }
                    else
                    {
                        var gcUpdateDate = ConvertMsecToDate((double)template.Data.Updated);

                        mapping.LastUpdatedDate = gcUpdateDate;
                        mapping.RemovedFromGc = false;
                    }
                }
                catch
                {
                    mapping.LastUpdatedDate = "Removed from GC";
                    mapping.RemovedFromGc = true;
                }

            }

            return model;
        }


        public TemplateMapModel GetTemplateMappingModel(string id)
        {
            var model = new TemplateMapModel();

            var template = _templateService.GetSingleTemplate(id);
            var project = _projectService.GetSingleProject(template.Data.ProjectId.ToString());

            model.GcProjectName = project.Data.Name;
            model.GcTemplateName = template.Data.Name;

            var scTemplates = _templatesRepository.GetTemplatesModel(_accountSettings.TemplateFolderId);
            var addMappingModel = _mappingRepository.GetAddMappingModel(project.Data.Id.ToString(), template);

            var templates = MapSitecoreTemplates(scTemplates);
            var addSitecoreMappingModel = MapAddMappingModel(addMappingModel);

            model.SitecoreTemplates.AddRange(templates);
            model.AddMappingModel = addSitecoreMappingModel;

            return model;
        }

        public void DeleteMapping(string id)
        {
            var template = _templateService.GetSingleTemplate(id);

            _mappingRepository.DeleteTemplate(template);
            _mappingRepository.DeleteMapping(template);

        }

        public void PostMapping(AddMappingModel model)
        {
            var template = _templateService.GetSingleTemplate(model.GcTemplateId);
            var project = _projectService.GetSingleProject(template.Data.ProjectId.ToString());

            var list = (from tab in model.Tabs
                        from templateField in tab.Fields
                        select new CmsTemplateField
                        {
                            FieldName = templateField.FieldName,
                            FieldId =  templateField.FieldId,
                            SelectedField = templateField.SelectedField,
                        }).ToList();

            if (model.IsEdit)
            {

                _mappingRepository.UpdateMapping(project.Data.Id, template.Data.Id, new TemplateMapping
                {
                    SitecoreTemplateId = model.SelectedTemplateId,
                    Name = template.Data.Name,
                    GcTemplateId = template.Data.Id.ToString()
                }, list);

            }
            else
            {
                _mappingRepository.CreateMapping(project.Data.Id, new TemplateMapping
                {
                    SitecoreTemplateId = model.SelectedTemplateId,
                    Name = template.Data.Name,
                    GcTemplateId = template.Data.Id.ToString(),
                    LastUpdated = template.Data.Updated.ToString()
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
            List<Element> gcFields = item.Config.SelectMany(i => i.Elements).ToList();

            MappingTemplateModel template;
            TryMapItemState templateMapState = TryGetTemplate(templates, item.TemplateId.ToString(), out template);

            if (templateMapState == TryMapItemState.TemplateError)
            {
                result = new MappingResultModel(item, null, gcTemplate.Name, null, string.Empty, "Template not mapped", false);
                return;
            }

            List<ImportCMSField> fields;
            IEnumerable<IGrouping<string, MappingFieldModel>> groupedFields = template.Fields.GroupBy(i => i.CMSField);

            TryMapItemState mapState = TryMapFields(gcFields, groupedFields, out fields);
            if (mapState == TryMapItemState.FieldError)
            {
                result = new MappingResultModel(item, null, gcTemplate.Name, null, string.Empty, "Template fields mismatch", false);
                return;
            }

            string cmsId = string.Empty;
            if (item is UpdateGCItem)
                cmsId = (item as UpdateGCItem).CMSId;

            result = new MappingResultModel(item, fields, gcTemplate.Name, template.CMSTemplateId, cmsId);
        }

        private TryMapItemState TryGetTemplate(List<MappingTemplateModel> templates, string templateId, out MappingTemplateModel result)
        {
            result = templates.FirstOrDefault(i => templateId == i.GCTemplateId);
            if (result == null)
                return TryMapItemState.TemplateError;

            return TryMapItemState.Success;
        }

        private TryMapItemState TryMapFields(List<Element> gcFields, IEnumerable<IGrouping<string, MappingFieldModel>> fieldsMappig, out List<ImportCMSField> result)
        {
            result = new List<ImportCMSField>();
            foreach (IGrouping<string, MappingFieldModel> grouping in fieldsMappig)
            {
                ImportCMSField cmsField;
                TryMapItemState mapState = TryMapField(gcFields, grouping, out cmsField);
                if (mapState == TryMapItemState.FieldError)
                    return mapState;
                result.Add(cmsField);
            }

            return TryMapItemState.Success;
        }

        private TryMapItemState TryMapField(List<Element> gcFields, IGrouping<string, MappingFieldModel> fieldsMappig, out ImportCMSField importCMSField)
        {
            string cmsFieldName = fieldsMappig.Key;
            List<Element> gcFieldsForMapping = GetFieldsForMapping(fieldsMappig, gcFields);

            Element field = gcFieldsForMapping.FirstOrDefault();
            if (field == null)
            {
                importCMSField = new ImportCMSField(string.Empty, cmsFieldName, string.Empty, null);
                return TryMapItemState.FieldError;
            }

            if (IsMappedFieldsHaveDifrentTypes(gcFieldsForMapping))
            {
                importCMSField = new ImportCMSField(string.Empty, cmsFieldName, string.Empty, null);
                return TryMapItemState.FieldError;
            }

            string value = GetValue(gcFieldsForMapping);
            List<Option> options = GetOptions(gcFieldsForMapping);

            importCMSField = new ImportCMSField(field.Type, cmsFieldName, value, options);

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
            IEnumerable<Element> gcFieldsForMapping = gcFields.Where(i => gsFiledNames.Contains(i.Label));

            return gcFieldsForMapping.ToList();
        }
        
    }
}
