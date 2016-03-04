using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.IRepositories.Models.New.Import;
using GatherContent.Connector.IRepositories.Models.New.Mapping;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.SitecoreRepositories.Repositories;

namespace GatherContent.Connector.Managers.Managers
{
  

    public class MappingManager : BaseManager
    {

        #region Constants
        public const string FieldGcContentId = "{955A4DD9-6A01-458E-9791-3C99F5E076A8}";
        public const string FieldLastSyncDate = "{F9D2EA57-86A2-45CF-9C28-8D8CA72A2669}";
        #endregion


        private readonly MappingRepository _mappingRepository;

        private readonly TemplatesService _templateService;
        private readonly ProjectsService _projectService;
        
        private readonly GCAccountSettings _accountSettings;

        public MappingManager()
        {
            var accountsRepository = new AccountsRepository();
            _accountSettings = accountsRepository.GetAccountSettings();

            _mappingRepository = new MappingRepository();

            _templateService = new TemplatesService(_accountSettings);
            _projectService = new ProjectsService(_accountSettings);
        }

        #region Utilities



        private DateTime ConvertMsecToDate(double date)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var gcUpdateDate =
                posixTime.AddMilliseconds(date * 1000);
            return gcUpdateDate;
        }


        private IEnumerable<CmsTemplateModel> MapSitecoreTemplates(IEnumerable<CmsTemplate> scTemplates)
        {
            var templates = new List<CmsTemplateModel>();

            foreach (var cmsTemplate in scTemplates)
            {
                var templateModel = new CmsTemplateModel
                {
                    Name = cmsTemplate.TemplateName,
                    Id = cmsTemplate.TemplateId
                };
                templateModel.Fields.Add(new CmsTemplateFieldModel { Id = "0", Name = "Do not map" });
                foreach (var field in cmsTemplate.TemplateFields)
                {
                    if (field.FieldId != FieldGcContentId &&
                        field.FieldId != FieldLastSyncDate)
                    {
                        var scField = new CmsTemplateFieldModel
                        {
                            Name = field.FieldName,
                            Id = field.FieldId,
                            Type = field.FieldType

                        };
                        templateModel.Fields.Add(scField);
                    }
                }
                templates.Add(templateModel);
            }
            return templates;
        }




        private AddMappingModel MapAddMappingModel(TemplateMapping templateMapping, TemplateEntity gcTemplate)
        {
            var addSitecoreMappingModel = new AddMappingModel
            {
                GcTemplateId = templateMapping.GcTemplate.GcTemplateId,
                CmsTemplateId = templateMapping.CmsTemplate.TemplateId,
                GcMappingTitle = templateMapping.MappingTitle,
                DefaultLocation = templateMapping.DefaultLocationId,
                DefaultLocationTitle = templateMapping.DefaultLocationTitle
            };

            foreach (var config in gcTemplate.Data.Config)
            {
                var tab = new TemplateTab { TabName = config.Label };
                foreach (var element in config.Elements)
                {
                    var tm = new TemplateField
                    {
                        Name = element.Label,
                        Id = element.Name,
                        Type = element.Type
                    };

                    if (templateMapping.FieldMappings != null)
                    {
                        var scField = templateMapping.FieldMappings.FirstOrDefault(item => item.GcField.Id == element.Name);
                        if (scField != null)
                        {
                            tm.SelectedFieldId = scField.CmsField.TemplateField.FieldId;
                        }
                    }

                    tab.Fields.Add(tm);
                }
                addSitecoreMappingModel.Tabs.Add(tab);
            }

            return addSitecoreMappingModel;
        }



        private static List<FieldMapping> ConvertToFieldMappings(IEnumerable<FieldMappingModel> list)
        {
            var fieldMappings = new List<FieldMapping>();
            foreach (var item in list)
            {
                var fieldMapping = new FieldMapping
                {
                    CmsField = new CmsField
                    {
                        TemplateField = new CmsTemplateField
                        {
                            FieldId = item.CmsTemplateId
                        }
                    },
                    GcField = new GcField
                    {
                        Id = item.GcFieldId,
                        Name = item.GcFieldName
                    }
                };
                fieldMappings.Add(fieldMapping);

            }
            return fieldMappings;
        }

        #endregion


        public List<MappingModel> GetMappingModel()
        {
            var mappings = _mappingRepository.GetMappings();

            var model = new List<MappingModel>();

            foreach (var templateMapping in mappings)
            {
                var mappingModel = new MappingModel
                {
                    GcProjectName = templateMapping.GcProjectName,
                    GcTemplateId = templateMapping.GcTemplate.GcTemplateId,
                    GcTemplateName = templateMapping.GcTemplate.GcTemplateName,
                    ScTemplateName = templateMapping.CmsTemplate.TemplateName,
                    ScMappingId = templateMapping.MappingId,
                    MappingTitle = templateMapping.MappingTitle,
                    LastMappedDateTime = templateMapping.LastMappedDateTime,
                    LastUpdatedDate = templateMapping.LastUpdatedDate,
                };
                model.Add(mappingModel);
            }

            foreach (var mapping in model)
            {
                try
                {
                    var template = GetGcTemplateEntity(mapping.GcTemplateId);
                    if (template == null)
                    {
                        mapping.LastUpdatedDate = "Removed from GatherContent";
                    }
                    else
                    {
                        var gcUpdateDate = ConvertMsecToDate((double)template.Data.Updated);
                        var dateFormat = _accountSettings.DateFormat;
                        if (string.IsNullOrEmpty(dateFormat))
                        {
                            dateFormat = Constants.DateFormat;
                        }
                        mapping.LastUpdatedDate = gcUpdateDate.ToString(dateFormat);
                    }
                }
                catch (Exception)
                {
                    mapping.LastUpdatedDate = "Removed from GatherContent";
                }

            }

            return model;
        }


        public TemplateMapModel GetSingleMappingModel(string gcTemplateId, string cmsMappingId)
        {
            var model = new TemplateMapModel();

            if (!string.IsNullOrEmpty(gcTemplateId))
            {
                var gcTemplate = GetGcTemplateEntity(gcTemplateId);
                var gcProject = GetGcProjectEntity(gcTemplate.Data.ProjectId.ToString());

                model.GcProjectName = gcProject.Data.Name;
                model.GcTemplateName = gcTemplate.Data.Name;


                if (!string.IsNullOrEmpty(cmsMappingId))
                {
                    model.MappingId = cmsMappingId;

                    var addMappingModel = _mappingRepository.GetMappingById(cmsMappingId);
                    var addCmsMappingModel = MapAddMappingModel(addMappingModel, gcTemplate);
                    model.AddMappingModel = addCmsMappingModel;
                }
            
            }


            var availableTemplates = _mappingRepository.GetAvailableCmsTemplates();
            if (availableTemplates.Count == 0)
            {
                throw new Exception("Template folder is empty");
            }
            var templates = MapSitecoreTemplates(availableTemplates);
            model.AvailableCmsTemplates.AddRange(templates);



            return model;
        }


        public void CreateMapping(PostMappingModel model)
        {
            var template = _templateService.GetSingleTemplate(model.GcTemplateId);
            var project = _projectService.GetSingleProject(template.Data.ProjectId.ToString());

            var templateMapping = new TemplateMapping
            {
                MappingId = model.MappingId,
                MappingTitle = model.MappingTitle,
                DefaultLocationId = model.DefaultLocation,
                LastUpdatedDate = template.Data.Updated.ToString(),
                GcProjectId = project.Data.Id.ToString(),
                CmsTemplate = new CmsTemplate
                {
                    TemplateId = model.CmsTemplateId
                },
                GcTemplate = new GcTemplate
                {
                    GcTemplateId = template.Data.Id.ToString(),
                    GcTemplateName = template.Data.Name
                },
            };

            var fieldMappings = ConvertToFieldMappings(model.FieldMappings);

            templateMapping.FieldMappings = fieldMappings;
            _mappingRepository.CreateMapping(templateMapping);
        }


        public void UpdateMapping(PostMappingModel model)
        {
            var template = _templateService.GetSingleTemplate(model.GcTemplateId);
            var project = _projectService.GetSingleProject(template.Data.ProjectId.ToString());

            var templateMapping = new TemplateMapping
            {
                MappingId = model.MappingId,
                MappingTitle = model.MappingTitle,
                DefaultLocationId = model.DefaultLocation,
                GcProjectId = project.Data.Id.ToString(),
                CmsTemplate = new CmsTemplate
                {
                    TemplateId = model.CmsTemplateId
                },
                GcTemplate = new GcTemplate
                {
                    GcTemplateId = template.Data.Id.ToString(),
                    GcTemplateName = template.Data.Name
                },
            };

            var fieldMappings = ConvertToFieldMappings(model.FieldMappings);

            templateMapping.FieldMappings = fieldMappings;
            _mappingRepository.UpdateMapping(templateMapping);
        }


        public void DeleteMapping(string scMappingId)
        {
            _mappingRepository.DeleteMapping(scMappingId);
        }

    }
}
