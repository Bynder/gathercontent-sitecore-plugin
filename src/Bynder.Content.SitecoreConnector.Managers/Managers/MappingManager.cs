namespace Bynder.Content.SitecoreConnector.Managers.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Bynder.Content.SitecoreConnector.GatherContentService.Interfaces;
    using Extensions;
    using Interfaces;
    using Models.Mapping;
    using SitecoreRepositories.Repositories;
    using Core.DependencyInjection;
    using Core;
    using Core.Interfaces;
    using Core.Models.Import;
    using Core.Models.Mapping;
    using Sitecore.Pipelines.InsertRenderings.Processors;
    using Sitecore.Configuration;
    using Sitecore;
    using Sitecore.Globalization;
    using Sitecore.Data;

    [Service(typeof(IMappingManager))]
    public class MappingManager : BaseManager, IMappingManager
    {
        public const string FieldCwbContentId = "{955A4DD9-6A01-458E-9791-3C99F5E076A8}";
        public const string FieldLastSyncDate = "{F9D2EA57-86A2-45CF-9C28-8D8CA72A2669}";
        public const string FieldGwbPath = "{3C8AB507-583A-47A4-9CAD-5BFB96059933}";
        public const string FieldMappingId = "{1101A6B0-E4A7-402A-B7FD-C4F71E4B036B}";

        protected IMappingRepository MappingRepository;
        protected IItemsService ItemService;
        protected readonly ILogger Log;
        protected ITemplatesService TemplateService;
        private readonly IAccountsRepository AccountsRepository;
        protected readonly Database ContextDatabase;
        protected readonly Language ContextLanguage;

        public MappingManager(
            IMappingRepository mappingRepository,
            IAccountsService accountsService,
            IProjectsService projectsService,
            ITemplatesService templateService,
            IItemsService itemService,
            ICacheManager cacheManager,
            ILogger logger,
            IAccountsRepository accountsRepository)
            : base(accountsService, projectsService, templateService, cacheManager, accountsRepository)
        {
            MappingRepository = mappingRepository;
            ItemService = itemService;
            Log = logger ?? new NullLogger();
            TemplateService = templateService;
            AccountsRepository = accountsRepository;
            ContextDatabase = Factory.GetDatabase("master");
            ContextLanguage = Context.Language;
        }

        public List<MappingModel> GetMappingModel()
        {
            var mappings = MappingRepository.GetMappings();

            var model = new List<MappingModel>();

            model.AddRange(mappings.Select(mapping => mapping.CastToMappingModel()));

            foreach (var mapping in model)
            {
                SetLastUpdatedDate(mapping);
            }

            return model;
        }

        public List<MappingModel> GetMappingModel(string cwbProjectId)
        {
            var mappings = MappingRepository.GetMappingsByCwbProjectId(cwbProjectId);
            if (mappings == null)
            {
                return null;
            }

            var model = new List<MappingModel>();
            model.AddRange(mappings.Select(mapping => mapping.CastToMappingModel()));
            return model;
        }

        public MappingModel GetSingleMappingModel(string cwbTemplateId, string cmsMappingId)
        {
            if (!string.IsNullOrEmpty(cwbTemplateId) && !string.IsNullOrEmpty(cmsMappingId))
            {
                var cwbTemplate = GetCwbTemplateEntity(cwbTemplateId);
                var cwbProject = GetCwbProjectEntity(cwbTemplate.Data.ProjectId.ToString());
                var addMappingModel = MappingRepository.GetMappingById(cmsMappingId);

                var model = new MappingModel();
                MapAddMappingModel(addMappingModel, model);
                model.CwbProject = new CwbProjectModel
                {
                    Name = cwbProject.Data.Name,
                    Id = cwbProject.Data.Id.ToString()
                };
                model.CwbTemplate = new CwbTemplateModel
                {
                    Name = cwbTemplate.Data.Name,
                    Id = cwbTemplate.Data.Id.ToString()
                };
                model.MappingId = cmsMappingId;
                return model;
            }

            return new MappingModel();
        }

        public RelatedMappingModel GetSingleRelatedMappingModel(string cwbTemplateId, string cmsMappingId)
        {
            if (!string.IsNullOrEmpty(cwbTemplateId) && !string.IsNullOrEmpty(cmsMappingId))
            {
                var cwbTemplate = GetCwbTemplateEntity(cwbTemplateId);
                var cwbProject = GetCwbProjectEntity(cwbTemplate.Data.ProjectId.ToString());
                var addMappingModel = MappingRepository.GetRelatedMappingById(cmsMappingId);

                var model = new RelatedMappingModel();
                MapAddRelatedMappingModel(addMappingModel, model);
                model.CwbProject = new CwbProjectModel
                {
                    Name = cwbProject.Data.Name,
                    Id = cwbProject.Data.Id.ToString()
                };
                model.CwbTemplate = new CwbTemplateModel
                {
                    Name = cwbTemplate.Data.Name,
                    Id = cwbTemplate.Data.Id.ToString()
                };
                model.MappingId = cmsMappingId;
                return model;
            }

            return new RelatedMappingModel();
        }

        public List<CmsTemplateModel> GetAvailableTemplates()
        {
            var availableTemplates = MappingRepository.GetAvailableCmsTemplates();
            if (availableTemplates.Count == 0)
            {
                Log.Warn("Template folder is empty", this);
                return new List<CmsTemplateModel>();
            }
            var templates = MapCmsTemplates(availableTemplates).ToList();

            return templates;
        }

        public List<CmsTemplateModel> GetAvailableOptionsContentFolder()
        {
            var availableOptionTemplates = MappingRepository.GetAvailableOptionsContentFolder();

            if (availableOptionTemplates.Count == 0)
            {
                Log.Warn("Template folder is empty", this);
                return new List<CmsTemplateModel>();
            }

            var optionTemplates = MapCmsTemplates(availableOptionTemplates).ToList();

            return optionTemplates;
        }

        public List<CmsTemplateModel> GetAvailableOptionTemplates()
        {
            var availableOptionTemplates = MappingRepository.GetAvailableOptionTemplates();

            if (availableOptionTemplates.Count == 0)
            {
                Log.Warn("Template folder is empty", this);
                return new List<CmsTemplateModel>();
            }

            var optionTemplates = MapCmsTemplates(availableOptionTemplates).ToList();

            return optionTemplates;
        }

        public List<CwbProjectModel> GetAllCwbProjects()
        {
            var account = GetAccount();
            var projects = GetProjects(account.Id);
            var model = new List<CwbProjectModel>();

            foreach (var project in projects)
            {
                model.Add(new CwbProjectModel
                {
                    Id = project.Id.ToString(),
                    Name = project.Name
                });
            }

            return model;
        }

        public List<CwbTemplateModel> GetTemplatesByProjectId(string cwbProjectId)
        {
            var model = new List<CwbTemplateModel>();
            var templates = TemplateService.GetTemplates(cwbProjectId);
            foreach (var template in templates.Data)
            {
                model.Add(new CwbTemplateModel
                {
                    Id = template.Id.ToString(),
                    Name = template.Name
                });
            }
            return model;
        }

        public List<CwbTabModel> GetFieldsByTemplateId(string cwbTemplateId)
        {
            var model = new List<CwbTabModel>();

            var cwbTemplate = TemplateService.GetSingleTemplate(cwbTemplateId);
            foreach (var config in cwbTemplate.Data.Config)
            {
                var tab = new CwbTabModel { TabName = config.Label };
                foreach (var element in config.Elements)
                {
                    var tm = new CwbFieldModel
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

        public void CreateMapping(MappingModel model)
        {
            var template = TemplateService.GetSingleTemplate(model.CwbTemplate.Id);
            var project = ProjectsService.GetSingleProject(template.Data.ProjectId.ToString());

            var templateMapping = new TemplateMapping
            {
                MappingId = model.MappingId,
                MappingTitle = model.MappingTitle,
                DefaultLocationId = model.DefaultLocationId,
                LastUpdatedDate = template.Data.Updated.ToString(),
                CwbProjectId = project.Data.Id.ToString(),
                CwbProjectName = project.Data.Name,
                CmsTemplate = new CmsTemplate
                {
                    TemplateId = model.CmsTemplate.Id
                },
                CwbTemplate = new CwbTemplate
                {
                    CwbTemplateId = template.Data.Id.ToString(),
                    CwbTemplateName = template.Data.Name
                },
            };

            var fieldMappings = ConvertToFieldMappings(model.FieldMappings);

            templateMapping.FieldMappings = fieldMappings;
            MappingRepository.CreateMapping(templateMapping);
        }

        public void CreateRelatedMapping(RelatedMappingModel model)
        {
            var template = TemplateService.GetSingleTemplate(model.CwbTemplate.Id);
            var project = ProjectsService.GetSingleProject(template.Data.ProjectId.ToString());

            var templateMapping = new RelatedTemplateMapping
            {
                MappingId = model.MappingId,
                MappingTitle = model.MappingTitle,
                DefaultLocationId = model.DefaultLocationId,
                LastUpdatedDate = template.Data.Updated.ToString(),
                CwbProjectId = project.Data.Id.ToString(),
                CwbProjectName = project.Data.Name,
                CmsTemplate = new CmsTemplate
                {
                    TemplateId = model.CmsTemplate.Id
                },
                CwbTemplate = new CwbTemplate
                {
                    CwbTemplateId = template.Data.Id.ToString(),
                    CwbTemplateName = template.Data.Name
                },
                CmsMainMappingId = model.CmsRelatedMappingId,
                CmsContainerTemplateId = model.CmsContainerTemplateId
            };

            var fieldMappings = ConvertToFieldMappings(model.FieldMappings);

            templateMapping.FieldMappings = fieldMappings;
            MappingRepository.CreateRelatedMapping(templateMapping);
        }

        public void UpdateMapping(MappingModel model)
        {
            var template = TemplateService.GetSingleTemplate(model.CwbTemplate.Id);
            var project = ProjectsService.GetSingleProject(template.Data.ProjectId.ToString());

            var templateMapping = new TemplateMapping
            {
                MappingId = model.MappingId,
                MappingTitle = model.MappingTitle,
                DefaultLocationId = model.DefaultLocationId,
                CwbProjectId = project.Data.Id.ToString(),
                CmsTemplate = new CmsTemplate
                {
                    TemplateId = model.CmsTemplate.Id
                },
                CwbTemplate = new CwbTemplate
                {
                    CwbTemplateId = template.Data.Id.ToString(),
                    CwbTemplateName = template.Data.Name
                },
                LastUpdatedDate = template.Data.Updated.ToString(),
            };

            var fieldMappings = ConvertToFieldMappings(model.FieldMappings);

            templateMapping.FieldMappings = fieldMappings;
            MappingRepository.UpdateMapping(templateMapping);
        }

        public void UpdateRelatedMapping(RelatedMappingModel model)
        {
            var template = TemplateService.GetSingleTemplate(model.CwbTemplate.Id);
            var project = ProjectsService.GetSingleProject(template.Data.ProjectId.ToString());

            var templateMapping = new RelatedTemplateMapping
            {
                MappingId = model.MappingId,
                MappingTitle = model.MappingTitle,
                DefaultLocationId = model.DefaultLocationId,
                CwbProjectId = project.Data.Id.ToString(),
                CmsTemplate = new CmsTemplate
                {
                    TemplateId = model.CmsTemplate.Id
                },
                CwbTemplate = new CwbTemplate
                {
                    CwbTemplateId = template.Data.Id.ToString(),
                    CwbTemplateName = template.Data.Name
                },
                CmsMainMappingId = model.CmsRelatedMappingId,
                CmsContainerTemplateId = model.CmsContainerTemplateId
            };

            var fieldMappings = ConvertToFieldMappings(model.FieldMappings);

            templateMapping.FieldMappings = fieldMappings;
            MappingRepository.UpdateRelatedMapping(templateMapping);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scMappingId"></param>
        public void DeleteMapping(string scMappingId)
        {
            MappingRepository.DeleteMapping(scMappingId);
        }

        private DateTime ConvertMsecToDate(double date)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var cwbUpdateDate = posixTime.AddMilliseconds(date * 1000);
            return cwbUpdateDate;
        }

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
                    if (field.FieldId != FieldCwbContentId &&
                        field.FieldId != FieldLastSyncDate &&
                        field.FieldId != FieldGwbPath &&
                        field.FieldId != FieldMappingId)
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

        private void MapAddMappingModel(TemplateMapping templateMapping, MappingModel addCmsMappingModel)
        {
            addCmsMappingModel.CwbTemplate = new CwbTemplateModel
            {
                Id = templateMapping.CwbTemplate.CwbTemplateId,
            };
            addCmsMappingModel.CmsTemplate = new CmsTemplateModel
            {
                Id = templateMapping.CmsTemplate.TemplateId,
            };
            addCmsMappingModel.MappingTitle = templateMapping.MappingTitle;
            addCmsMappingModel.DefaultLocationId = templateMapping.DefaultLocationId;
            addCmsMappingModel.DefaultLocationTitle = templateMapping.DefaultLocationTitle;
            

            foreach (var fieldMapping in templateMapping.FieldMappings)
            {
                addCmsMappingModel.FieldMappings.Add(new FieldMappingModel
                {
                    CmsTemplateId = fieldMapping.CmsField.TemplateField.FieldId,
                    CwbFieldId = fieldMapping.CwbField.Id,
                    CwbFieldName = fieldMapping.CwbField.Name,
                    OptionsContentFolderId = fieldMapping.CmsField.TemplateField.OptionsContentFolderId,
                    OptionsTemplateId = fieldMapping.CmsField.TemplateField.OptionsTemplateId
                });
            }
        }
        
        private void MapAddRelatedMappingModel(RelatedTemplateMapping templateMapping, RelatedMappingModel model)
        {
            MapAddMappingModel(templateMapping, model);
            model.CmsRelatedMappingId = templateMapping.CmsMainMappingId;
            model.CmsContainerTemplateId = templateMapping.CmsContainerTemplateId;
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
                            FieldId = item.CmsTemplateId,
                            OptionsContentFolderId = item.OptionsContentFolderId,
                            OptionsTemplateId = item.OptionsTemplateId
                        }
                    },
                    CwbField = new CwbField
                    {
                        Id = item.CwbFieldId,
                        Name = item.CwbFieldName
                    }
                };
                fieldMappings.Add(fieldMapping);

            }
            return fieldMappings;
        }


        private void SetLastUpdatedDate(MappingModel mapping)
        {
            try
            {
                var template = GetCwbTemplateEntity(mapping.CwbTemplate.Id);
                if (template == null)
                {
                    mapping.LastUpdatedDate = "Removed from ContentWorkflow";
                }
                else
                {
                    var cwbUpdateDate = ConvertMsecToDate(template.Data.Updated);
                    var dateFormat = CwbAccountSettings.DateFormat;
                    if (string.IsNullOrEmpty(dateFormat))
                    {
                        dateFormat = SitecoreRepositories.Repositories.Constants.DateFormat;
                    }
                    mapping.LastUpdatedDate = cwbUpdateDate.ToString(dateFormat);
                }
            }
            catch (Exception)
            {
                mapping.LastUpdatedDate = "Removed from ContentWorkflow";
            }
        }

        public Dictionary<string, string> GetAllowedMappings()
        {
            return MappingRepository.GetAllowedMappings();
        }
    }
}
