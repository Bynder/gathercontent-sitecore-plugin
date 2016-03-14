using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.New.Import;
using GatherContent.Connector.IRepositories.Models.New.Mapping;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.SitecoreRepositories.Repositories;

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

        protected IMappingRepository MappingRepository;

        protected IItemsService ItemService;

        protected ITemplatesService TemplateService;

        protected GCAccountSettings AccountSettings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mappingRepository"></param>
        /// <param name="accountsService"></param>
        /// <param name="projectsService"></param>
        /// <param name="templateService"></param>
        /// <param name="itemService"></param>
        /// <param name="cacheManager"></param>
        /// <param name="accountSettings"></param>
        public MappingManager(
            IMappingRepository mappingRepository,
            IAccountsService accountsService,
            IProjectsService projectsService,
            ITemplatesService templateService,
            IItemsService itemService,
            ICacheManager cacheManager,
            GCAccountSettings accountSettings)  : base(accountsService, projectsService, templateService, cacheManager)
        {
            AccountSettings = accountSettings;

            MappingRepository = mappingRepository;

            ItemService = itemService;

            TemplateService = templateService;
        }

        #region Utilities

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
        /// <param name="scTemplates"></param>
        /// <returns></returns>
        private IEnumerable<CmsTemplateModel> MapCmsTemplates(IEnumerable<CmsTemplate> scTemplates)
        {
            var templates = new List<CmsTemplateModel>();

            foreach (var cmsTemplate in scTemplates)
            {
                var templateModel = new CmsTemplateModel
                {
                    Name = cmsTemplate.TemplateName,
                    Id = cmsTemplate.TemplateId
                };
                
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateMapping"></param>
        /// <returns></returns>
        private AddMappingModel MapAddMappingModel(TemplateMapping templateMapping)
        {
            var addCmsMappingModel = new AddMappingModel
            {
                GcTemplateId = templateMapping.GcTemplate.GcTemplateId,
                CmsTemplateId = templateMapping.CmsTemplate.TemplateId,
                MappingTitle = templateMapping.MappingTitle,
                DefaultLocation = templateMapping.DefaultLocationId,
                DefaultLocationTitle = templateMapping.DefaultLocationTitle
            };

            foreach (var fieldMapping in templateMapping.FieldMappings)
            {
                addCmsMappingModel.SelectedFields.Add(new FieldMappingModel
                {
                    CmsTemplateId = fieldMapping.CmsField.TemplateField.FieldId,
                    GcFieldId = fieldMapping.GcField.Id,
                    GcFieldName = fieldMapping.GcField.Name
                });
            }

            return addCmsMappingModel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<MappingModel> GetMappingModel()
        {
            var mappings = MappingRepository.GetMappings();

            var model = new List<MappingModel>();

            foreach (var templateMapping in mappings)
            {
                var mappingModel = new MappingModel
                {
                    GcProjectName = templateMapping.GcProjectName,
                    GcTemplateId = templateMapping.GcTemplate.GcTemplateId,
                    GcTemplateName = templateMapping.GcTemplate.GcTemplateName,
                    CmsTemplateName = templateMapping.CmsTemplate.TemplateName,
                    CmsMappingId = templateMapping.MappingId,
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
                        var dateFormat = AccountSettings.DateFormat;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcTemplateId"></param>
        /// <param name="cmsMappingId"></param>
        /// <returns></returns>
        public AddMappingModel GetSingleMappingModel(string gcTemplateId, string cmsMappingId)
        {
            if (!string.IsNullOrEmpty(gcTemplateId) && !string.IsNullOrEmpty(cmsMappingId))
            {
                var gcTemplate = GetGcTemplateEntity(gcTemplateId);
                var gcProject = GetGcProjectEntity(gcTemplate.Data.ProjectId.ToString());
                var addMappingModel = MappingRepository.GetMappingById(cmsMappingId);
                var model = MapAddMappingModel(addMappingModel);
                model.GcProjectName = gcProject.Data.Name;
                model.GcProjectId = gcProject.Data.Id.ToString();
                model.GcTemplateName = gcTemplate.Data.Name;
                model.GcTemplateId = gcTemplate.Data.Id.ToString();
                model.MappingId = cmsMappingId;
                return model;
            }

            return new AddMappingModel();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<CmsTemplateModel> GetAvailableTemplates()
        {
            var availableTemplates = MappingRepository.GetAvailableCmsTemplates();
            if (availableTemplates.Count == 0)
            {
                throw new Exception("Template folder is empty");
            }
            var templates = MapCmsTemplates(availableTemplates).ToList();

            return templates;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<GcProjectModel> GetAllGcProjects()
        {
            var account = GetAccount();
            var projects = GetProjects(account.Id);
            var model = new List<GcProjectModel>();

            foreach (var project in projects)
            {
                model.Add(new GcProjectModel
                {
                    Id = project.Id.ToString(),
                    Name = project.Name
                });
            }

            return model;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcProjectId"></param>
        /// <returns></returns>
        public List<GcTemplateModel> GetTemplatesByProjectId(string gcProjectId)
        {
            var model = new List<GcTemplateModel>();
            var templates = TemplateService.GetTemplates(gcProjectId);
            foreach (var template in templates.Data)
            {
                model.Add(new GcTemplateModel
                {
                    Id = template.Id.ToString(),
                    Name = template.Name
                });
            }
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcTemplateId"></param>
        /// <returns></returns>
        public List<TemplateTabModel> GetFieldsByTemplateId(string gcTemplateId)
        {
            var model = new List<TemplateTabModel>();
            
            var gcTemplate = TemplateService.GetSingleTemplate(gcTemplateId);
            foreach (var config in gcTemplate.Data.Config)
            {
                var tab = new TemplateTabModel { TabName = config.Label };
                foreach (var element in config.Elements)
                {
                    var tm = new TemplateField
                    {
                        Name = element.Label,
                        Id = element.Name,
                        Type = element.Type
                    };

                    tab.Fields.Add(tm);
                }
                model.Add(tab);
            }
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public void CreateMapping(PostMappingModel model)
        {
            var template = TemplateService.GetSingleTemplate(model.GcTemplateId);
            var project = ProjectsService.GetSingleProject(template.Data.ProjectId.ToString());

            var templateMapping = new TemplateMapping
            {
                MappingId = model.MappingId,
                MappingTitle = model.MappingTitle,
                DefaultLocationId = model.DefaultLocation,
                LastUpdatedDate = template.Data.Updated.ToString(),
                GcProjectId = project.Data.Id.ToString(),
                GcProjectName = project.Data.Name,
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
            MappingRepository.CreateMapping(templateMapping);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public void UpdateMapping(PostMappingModel model)
        {
            var template = TemplateService.GetSingleTemplate(model.GcTemplateId);
            var project = ProjectsService.GetSingleProject(template.Data.ProjectId.ToString());

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
            MappingRepository.UpdateMapping(templateMapping);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scMappingId"></param>
        public void DeleteMapping(string scMappingId)
        {
            MappingRepository.DeleteMapping(scMappingId);
        }
    }
}
