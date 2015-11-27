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
        private readonly ProjectsRepository _projectsRepository;


        private readonly AccountsService _accountsService;
        private readonly TemplatesService _templateService;
        private readonly ProjectsService _projectService;


        private readonly GCAccountSettings _accountSettings;

        public MappingManager()
        {
            var accountsRepository = new AccountsRepository();
            _accountSettings = accountsRepository.GetAccountSettings();

            _mappingRepository = new MappingRepository();
            _projectsRepository = new ProjectsRepository();
            _templatesRepository = new TemplatesRepository();

            _accountsService = new AccountsService(_accountSettings);
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


        private List<ImportCMSItem> MapItems(List<GCItem> items, List<MappingTemplateModel> templates)
        {
            var result = new List<ImportCMSItem>();
            foreach (GCItem gcItem in items)
            {
                MappingTemplateModel template = templates.FirstOrDefault(i => gcItem.TemplateId.ToString() == i.GCTemplate);
                ImportCMSItem cmsItem = MapItem(gcItem, template);
                result.Add(cmsItem);
            }

            return result;
        }

        private ImportCMSItem MapItem(GCItem item, MappingTemplateModel template)
        {
            List<Element> gcFields = item.Config.SelectMany(i => i.Elements).ToList();

            List<ImportCMSFiled> fields = MapFields(gcFields, template);

            var result = new ImportCMSItem(item, item.Name, template.CMSTemplate, fields);

            return result;
        }

        private List<ImportCMSFiled> MapFields(List<Element> gcFields, MappingTemplateModel template)
        {
            List<ImportCMSFiled> result = gcFields.Select(i => MapField(i, template)).ToList();
            return result;
        }

        private ImportCMSFiled MapField(Element gcField, MappingTemplateModel template)
        {
            string cmsFieldName = GetCMSFieldNameForGCField(template.Fields, gcField);
            var result = new ImportCMSFiled(TEXT_TYPE, cmsFieldName, gcField.Value);
            return result;
        }

        private string GetCMSFieldNameForGCField(List<MappingFieldModel> fields, Element gcField)
        {
            MappingFieldModel field = fields.FirstOrDefault(i => i.GCField == gcField.Name);
            return field != null ? field.CMSField : string.Empty;
        }


        #endregion



        public List<ImportCMSItem> MappingItems(List<GCItem> items, string projectId)
        {
            List<MappingTemplateModel> templates = _mappingRepository.GetTemplateMappings(projectId);

            List<ImportCMSItem> result = MapItems(items, templates);

            return result;
        }

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

    }
}
