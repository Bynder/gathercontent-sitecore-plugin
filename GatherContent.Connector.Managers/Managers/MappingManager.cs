using System.Collections.Generic;
using System.Linq;
using System.Net;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.IRepositories.Models.Mapping;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.SitecoreRepositories;
using TemplateMapModel = GatherContent.Connector.Managers.Models.Mapping.TemplateMapModel;
using TemplateTab = GatherContent.Connector.Managers.Models.Mapping.TemplateTab;

namespace GatherContent.Connector.Managers.Managers
{
    public class MappingManager : BaseManager
    {
        private const string TEXT_TYPE = "Single-Line Text";

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
                        SitecoreFieldId = field.CmsFieldId
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

            return mappings.Select(cmsMappingModel => new MappingModel(cmsMappingModel.GcProjectName, cmsMappingModel.GcTemplateId,
                cmsMappingModel.GcTemplateName, cmsMappingModel.CmsTemplateName, cmsMappingModel.LastMappedDateTime,
                cmsMappingModel.LastUpdatedDate, cmsMappingModel.EditButtonTitle, cmsMappingModel.IsMapped)).ToList();
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

        public void PostMapping(AddMappingModel model)
        {
            var template = _templateService.GetSingleTemplate(model.GcTemplateId);
            var project = _projectService.GetSingleProject(template.Data.ProjectId.ToString());

            var list = (from tab in model.Tabs
                        from templateField in tab.Fields
                        select new CmsTemplateField
                        {
                            FieldName = templateField.FieldName,
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


        public List<ImportItemsResponseModel> MapItems(List<GCItem> items, string projectId)
        {
            List<MappingTemplateModel> templates = _mappingRepository.GetTemplateMappings(projectId);

            List<ImportItemsResponseModel> result = TryMapItems(items, templates);

            return result;
        }

        private List<ImportItemsResponseModel> TryMapItems(List<GCItem> items, List<MappingTemplateModel> templates)
        {
            var result = new List<ImportItemsResponseModel>();

            foreach (GCItem gcItem in items)
            {
                ImportItemsResponseModel cmsItem;
                TryMapItem(gcItem, templates, out cmsItem);
                result.Add(cmsItem);
            }

            return result;
        }

        private void TryMapItem(GCItem item, List<MappingTemplateModel> templates, out ImportItemsResponseModel result)
        {
            List<Element> gcFields = item.Config.SelectMany(i => i.Elements).ToList();

            Template gcTemplate = _templateService.GetSingleTemplate(item.TemplateId.ToString()).Data;

            MappingTemplateModel template;
            TryMapItemState templateMapState = TryGetTemplate(templates, item.TemplateId.ToString(), out template);
            
            if (templateMapState == TryMapItemState.TemplateError)
            {
                result = new ImportItemsResponseModel(item.Id.ToString(), item.Status.Data, item.Name, gcTemplate.Name,
                    "Template not mapped", false, null, null);
                return;
            }

            List<ImportCMSFiled> fields;
            TryMapItemState mapState = TryMapFields(gcFields, template, out fields);
            if (mapState == TryMapItemState.FiledError)
            {
                result = new ImportItemsResponseModel(item.Id.ToString(), item.Status.Data, item.Name, gcTemplate.Name,
                    "Template fields mismatch", false, null, null);
                return;
            }

            result = new ImportItemsResponseModel(item.Id.ToString(), item.Status.Data, item.Name, gcTemplate.Name,
                    string.Empty, true, fields, template.CMSTemplate);
        }

        private TryMapItemState TryGetTemplate(List<MappingTemplateModel> templates, string templateId, out MappingTemplateModel result)
        {
            result = templates.FirstOrDefault(i => templateId == i.GCTemplate);
            if (result == null)
                return TryMapItemState.TemplateError;

            return TryMapItemState.Success;
        }

        private TryMapItemState TryMapFields(List<Element> gcFields, MappingTemplateModel template, out List<ImportCMSFiled> result)
        {
            result = new List<ImportCMSFiled>();
            foreach (var gcField in gcFields)
            {
                ImportCMSFiled cmsField;
                TryMapItemState mapState = TryMapField(gcField, template, out cmsField);
                if (mapState == TryMapItemState.FiledError)
                    return mapState;
                result.Add(cmsField);
            }

            return TryMapItemState.Success;
        }

        private TryMapItemState TryMapField(Element gcField, MappingTemplateModel template, out ImportCMSFiled importCMSField)
        {
            string cmsFieldName;
            TryMapItemState result = TryGetCMSFieldName(template.Fields, gcField, out cmsFieldName);

            importCMSField = new ImportCMSFiled(TEXT_TYPE, cmsFieldName, gcField.Value);

            return result;
        }

        private TryMapItemState TryGetCMSFieldName(List<MappingFieldModel> fields, Element gcField, out string name)
        {
            name = string.Empty;
            MappingFieldModel field = fields.FirstOrDefault(i => i.GCField == gcField.Name);
            if (field == null)
                return TryMapItemState.FiledError;

            name = field.CMSField;

            return TryMapItemState.Success;
        }

        public enum TryMapItemState
        {
            Success = 0,
            TemplateError = 1,
            FiledError = 2
        }
    }
}
