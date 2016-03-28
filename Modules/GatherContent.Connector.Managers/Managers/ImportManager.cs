using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Mapping;
using GatherContent.Connector.Managers.Enums;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.SitecoreRepositories.Repositories;
using GatherContent.Connector.Managers.Models.ImportItems.New;

namespace GatherContent.Connector.Managers.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class ImportManager : BaseManager, IImportManager
    {
        protected IItemsRepository ItemsRepository;
        protected IMappingRepository MappingRepository;

        protected IItemsService ItemsService;

        protected IMappingManager MappingManager;
        protected GCAccountSettings GcAccountSettings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemsRepository"></param>
        /// <param name="mappingRepository"></param>
        /// <param name="itemsService"></param>
        /// <param name="accountsService"></param>
        /// <param name="projectsService"></param>
        /// <param name="templateService"></param>
        /// <param name="cacheManager"></param>
        /// <param name="mappingManager"></param>
        /// <param name="gcAccountSettings"></param>
        public ImportManager(
            IItemsRepository itemsRepository,
            IMappingRepository mappingRepository,
            IItemsService itemsService,
            IAccountsService accountsService,
            IProjectsService projectsService,
            ITemplatesService templateService,
            ICacheManager cacheManager,
            IMappingManager mappingManager,
            GCAccountSettings gcAccountSettings)
            : base(accountsService, projectsService, templateService, cacheManager)
        {
            ItemsRepository = itemsRepository;
            MappingRepository = mappingRepository;

            ItemsService = itemsService;

            MappingManager = mappingManager;

            GcAccountSettings = gcAccountSettings;
        }


        #region Utilities

        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private string ConvertMsecToFormattedDate(double date)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var dateFormat = GcAccountSettings.DateFormat;
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
        /// <param name="items"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        private List<MappingResultModel> TryMapItems(List<GCItem> items, List<TemplateMapping> templates)
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
            var template = MappingRepository.GetMappingById(selectedMappingId);


            if (template == null)
            {
                string errorMessage = isUpdate ? "Update failed: Template not mapped" : "Import failed: Template not mapped";
                result = new MappingResultModel(gcItem, null, gcTemplate.Name, null, string.Empty, errorMessage, false, selectedLocationId);
                return;
            }

            List<ImportCMSField> fields;

            IEnumerable<IGrouping<string, FieldMapping>> groupedFields = template.FieldMappings.GroupBy(i => i.CmsField.TemplateField.FieldId);

            var files = new List<File>();
            if (gcItem.Config.SelectMany(config => config.Elements).Select(element => element.Type).Contains("files"))
            {

                foreach (var file in ItemsService.GetItemFiles(gcItem.Id.ToString()).Data)
                {
                    files.Add(new File
                    {
                        FileName = file.FileName,
                        Url = file.Url,

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

            result = new MappingResultModel(gcItem, fields, gcTemplate.Name, template.CmsTemplate.TemplateId, cmsId, message, true, selectedLocationId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="gcTemplate"></param>
        /// <param name="templates"></param>
        /// <param name="result"></param>
        private void TryMapItem(GCItem item, GCTemplate gcTemplate, List<TemplateMapping> templates, out MappingResultModel result)
        {
            bool isUpdate = item is UpdateGCItem;

            List<Element> gcFields = item.Config.SelectMany(i => i.Elements).ToList();

            TemplateMapping template;
            TryMapItemState templateMapState = TryGetTemplate(templates, item.TemplateId.ToString(), out template);

            if (templateMapState == TryMapItemState.TemplateError)
            {
                string errorMessage = isUpdate ? "Update failed: Template not mapped" : "Import failed: Template not mapped";
                result = new MappingResultModel(item, null, gcTemplate.Name, null, string.Empty, errorMessage, false);
                return;
            }

            List<ImportCMSField> fields;
            IEnumerable<IGrouping<string, FieldMapping>> groupedFields = template.FieldMappings.GroupBy(i => i.CmsField.TemplateField.FieldId);

            var files = new List<File>();
            if (item.Config.SelectMany(config => config.Elements).Select(element => element.Type).Contains("files"))
            {

                foreach (var file in ItemsService.GetItemFiles(item.Id.ToString()).Data)
                {
                    files.Add(new File
                    {
                        FileName = file.FileName,
                        Url = file.Url,
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

            result = new MappingResultModel(item, fields, gcTemplate.Name, template.CmsTemplate.TemplateId, cmsId, message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gcFields"></param>
        /// <param name="fieldsMappig"></param>
        /// <param name="files"></param>
        /// <param name="importCMSField"></param>
        /// <returns></returns>
        private TryMapItemState TryMapField(List<Element> gcFields, IGrouping<string, FieldMapping> fieldsMappig, List<File> files, out ImportCMSField importCMSField)
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
            //files = files.Where(item => item.FieldId == field.Name).ToList();
            //TODO: used new List<Option>() here to build; will be removed soon
            importCMSField = new ImportCMSField(field.Type, cmsFieldName, field.Label, value.ToString(), new List<Option>(), files);

            return TryMapItemState.Success;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templates"></param>
        /// <param name="templateId"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private TryMapItemState TryGetTemplate(List<TemplateMapping> templates, string templateId, out TemplateMapping result)
        {
            if (templates == null)
            {
                result = null;
                return TryMapItemState.TemplateError;
            }

            result = templates.FirstOrDefault(i => templateId == i.GcTemplate.GcTemplateId);
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
        private TryMapItemState TryMapFields(List<Element> gcFields, IEnumerable<IGrouping<string, FieldMapping>> fieldsMappig, List<File> files, out List<ImportCMSField> result)
        {
            result = new List<ImportCMSField>();
            foreach (IGrouping<string, FieldMapping> grouping in fieldsMappig)
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
        private object GetValue(IEnumerable<Element> fields)
        {
            string value = string.Join("", fields.Select(i => i.Value));
            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fields"></param>
        /// <returns></returns>
        private List<string> GetOptions(IEnumerable<Element> fields)
        {
            var result = new List<string>();
            foreach (Element field in fields)
            {
                if (field.Options != null)
                    result.AddRange(field.Options.Select(x => x.Label));
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
        private List<Element> GetFieldsForMapping(IGrouping<string, FieldMapping> fieldsMappig, List<Element> gcFields)
        {
            IEnumerable<string> gsFiledNames = fieldsMappig.Select(i => i.GcField.Id);
            IEnumerable<Element> gcFieldsForMapping = gcFields.Where(i => gsFiledNames.Contains(i.Name));

            return gcFieldsForMapping.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="projectIdStr"></param>
        /// <returns></returns>
        private Project GetProject(List<Project> projects, string projectIdStr)
        {
            int projectId;
            int.TryParse(projectIdStr, out projectId);

            Project project = projectId != 0 ? projects.FirstOrDefault(i => i.Id == projectId) : projects.FirstOrDefault();

            return project;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private List<GCStatus> GetStatuses(int projectId)
        {
            StatusesEntity statuses = ProjectsService.GetAllStatuses(projectId.ToString());
            return statuses.Data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private List<GCItem> GetItems(int projectId)
        {
            ItemsEntity items = ItemsService.GetItems(projectId.ToString());
            return items.Data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private List<GCTemplate> GetTemplates(int projectId)
        {
            return GetTemplates(projectId.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private List<GCTemplate> GetTemplates(string projectId)
        {
            TemplatesEntity templates = TemplatesService.GetTemplates(projectId);
            return templates.Data;
        }

        private string GetBreadcrumb(GCItem item, List<GCItem> items)
        {
            var names = new List<string>();
            string result = BuildBreadcrumb(item, items, names);
            return result;
        }

        private string BuildBreadcrumb(GCItem item, List<GCItem> items, List<string> names)
        {
            names.Add(item.Name);

            if (item.ParentId != 0)
            {
                GCItem next = items.FirstOrDefault(i => i.Id == item.ParentId);
                return BuildBreadcrumb(next, items, names);
            }

            names.Reverse();

            string url = string.Join("/", names);

            return string.Format("/{0}", url);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        private List<ImportListItem> MapItems(List<GCItem> items, List<GCTemplate> templates)
        {
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();
            var dateFormat = GcAccountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }

            var result = new List<ImportListItem>();
            foreach (var mappedItem in mappedItems)
            {
                var mappings = MappingRepository.GetMappingsByGcTemplateId(mappedItem.TemplateId.ToString());
                var availableMappings = mappings.Select(availableMappingModel => new AvailableMapping
                {
                    Id = availableMappingModel.MappingId,
                    Title = availableMappingModel.MappingTitle,
                }).ToList();

                result.Add(new ImportListItem(mappedItem, templates.FirstOrDefault(templ => templ.Id == mappedItem.TemplateId), items, dateFormat, availableMappings));
            }

            return result.ToList();
        }


        private List<ItemModel> MapImportItems(List<GCItem> items, List<GCTemplate> templates)
        {
            var model = new List<ItemModel>();
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();
            var dateFormat = GcAccountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }


            foreach (var mappedItem in mappedItems)
            {
                var template = templates.FirstOrDefault(templ => templ.Id == mappedItem.TemplateId);
                var mappings = MappingRepository.GetMappingsByGcTemplateId(mappedItem.TemplateId.ToString());
                var availableMappings = mappings.Select(availableMappingModel => new AvailableMapping
                {
                    Id = availableMappingModel.MappingId,
                    Title = availableMappingModel.MappingTitle,
                }).ToList();

                model.Add(new ItemModel
                {
                    GcItem = new GcItemModel
                    {
                        Id = mappedItem.Id.ToString(),
                        Title = mappedItem.Name,
                        LastUpdatedInGc = mappedItem.Updated.Date.ToString(dateFormat)
                    },
                    GcTemplate = new GcTemplateModel
                    {
                        Name = template != null ? template.Name : "",
                        Id = template != null ? template.Id.ToString() : ""
                    },
                    Status = new GcStatusModel
                    {
                        Id = mappedItem.Status.Data.Id,
                        Name = mappedItem.Status.Data.Name,
                        Color = mappedItem.Status.Data.Color
                    },
                    AvailableMappings = new AvailableMappings
                    {
                        Mappings = availableMappings
                    },
                    Breadcrumb = GetBreadcrumb(mappedItem, items),
                });
            }
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="templates"></param>
        /// <returns></returns>
        private List<ImportItembyLocation> MapItemsByLocation(List<GCItem> items, List<GCTemplate> templates)
        {
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();
            var dateFormat = GcAccountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }

            var result = new List<ImportItembyLocation>();
            foreach (var mappedItem in mappedItems)
            {
                var mappings = MappingRepository.GetMappingsByGcTemplateId(mappedItem.TemplateId.ToString());
                var availableMappings = mappings.Select(availableMappingModel => new AvailableMappingByLocation
                {
                    Id = availableMappingModel.MappingId,
                    Title = availableMappingModel.MappingTitle,
                    OpenerId = "drop-tree" + Guid.NewGuid(),
                    ScTemplate = availableMappingModel.CmsTemplate.TemplateId,
                    IsShowing = false,
                    DefaultLocation = availableMappingModel.DefaultLocationId,
                    DefaultLocationTitle = availableMappingModel.DefaultLocationTitle,
                }).ToList();

                result.Add(new ImportItembyLocation(mappedItem, templates.FirstOrDefault(templ => templ.Id == mappedItem.TemplateId), items, dateFormat, availableMappings));
            }

            return result.ToList();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="importResult"></param>
        /// <returns></returns>
        private List<MappingResultModel> GetSuccessfulImportedItems(List<MappingResultModel> importResult)
        {
            return importResult.Where(i => i.IsImportSuccessful).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="statusId"></param>
        /// <param name="projectId"></param>
        private void PostNewStatusesForItems(List<MappingResultModel> items, string statusId, string projectId)
        {
            foreach (MappingResultModel item in items)
            {
                ItemsService.ChooseStatusForItem(item.GCItemId, statusId);
                var status = ProjectsService.GetSingleStatus(statusId, projectId);
                item.Status.Color = status.Data.Color;
                item.Status.Name = status.Data.Name;
            }
        }

        private GcStatusModel PostNewItemStatus(string gcItemId, string statusId, string projectId)
        {
            ItemsService.ChooseStatusForItem(gcItemId, statusId);
            var status = ProjectsService.GetSingleStatus(statusId, projectId);
            var statusModel = new GcStatusModel { Color = status.Data.Color, Name = status.Data.Name };
            return statusModel;
        }

        #endregion

  


        public List<ItemModel> GetImportDialogModel(string itemId, string projectId)
        {
            var model = new List<ItemModel>();
            if (projectId == "0") return null;

            var project = GetGcProjectEntity(projectId);

            if (project != null)
            {
                List<GCTemplate> templates = GetTemplates(project.Data.Id);

                List<GCItem> items = GetItems(project.Data.Id);
                items = items.OrderBy(item => item.Status.Data.Name).ToList();
                model = MapImportItems(items, templates);
                return model;
            }

            return model;
        }

   

        public GatherContent.Connector.Managers.Models.ImportItems.New.FiltersModel GetFilters(string projectId)
        {
            Account account = GetAccount();

            List<Project> gcProjects = GetProjects(account.Id);

            var projects = new List<GcProjectModel>();
            foreach (var gcProject in gcProjects)
            {
                projects.Add(new GcProjectModel
                {
                    Id = gcProject.Id.ToString(),
                    Name = gcProject.Name
                });
            }

            if (projectId != "0")
            {
                Project gcProject = GetProject(gcProjects, projectId);

                List<GCTemplate> gcTemplates = GetTemplates(gcProject.Id);
                var templates = new List<GcTemplateModel>();
                foreach (var gcTemplate in gcTemplates)
                {
                    templates.Add(new GcTemplateModel
                    {
                        Id = gcTemplate.Id.ToString(),
                        Name = gcTemplate.Name

                    });
                }
                List<GCStatus> gcStatuses = GetStatuses(gcProject.Id);

                var statuses = new List<GcStatusModel>();
                foreach (var gcStatus in gcStatuses)
                {
                    statuses.Add(new GcStatusModel
                    {
                        Id = gcStatus.Id.ToString(),
                        Name = gcStatus.Name,
                        Color = gcStatus.Color
                    });
                }


                return new GatherContent.Connector.Managers.Models.ImportItems.New.FiltersModel
                {
                    CurrentProject = new GcProjectModel
                {
                    Id = gcProject.Id.ToString(),
                    Name = gcProject.Name
                },
                    Projects = projects,
                    Statuses = statuses,
                    Templates = templates
                };

            }

            return new GatherContent.Connector.Managers.Models.ImportItems.New.FiltersModel
            {
                Projects = projects
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public SelectItemsForImportWithLocation GetDialogModelWithLocation(string itemId, string projectId)
        {
            Account account = GetAccount();

            List<Project> projects = GetProjects(account.Id);

            Project project = GetProject(projects, projectId);

            List<GCTemplate> templates = GetTemplates(project.Id);
            List<GCStatus> statuses = GetStatuses(project.Id);
            List<GCItem> items = GetItems(project.Id);
            items = items.OrderBy(item => item.Status.Data.Name).ToList();

            List<ImportItembyLocation> mappedItems = MapItemsByLocation(items, templates);

            var result = new SelectItemsForImportWithLocation(mappedItems, project, projects, statuses, templates);

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="items"></param>
        /// <param name="projectId"></param>
        /// <param name="statusId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public List<ItemResultModel> ImportItems(string itemId, List<ImportItemModel> items, string projectId, string statusId, string language)
        {
            var model = new List<ItemResultModel>();
            var templateMappings = new List<TemplateMapping>();

            var gcDataDictionary = new Dictionary<string, string>(); //(GC Id, GC Name)

            foreach (var importItem in items)
            {
                var itemResponseModel = new ItemResultModel
                {
                    IsImportSuccessful = true,
                    ImportMessage = "Import Successful",
                };

                var gcItem = ItemsService.GetSingleItem(importItem.Id);

                if (gcItem != null && gcItem.Data != null && gcItem.Data.TemplateId != null)
                {
                    if (!string.IsNullOrEmpty(GcAccountSettings.GatherContentUrl))
                    {
                        itemResponseModel.GcLink = string.Concat(GcAccountSettings.GatherContentUrl, "/item/", gcItem.Data.Id);
                    }
                    itemResponseModel.GcItem = new GcItemModel
                    {
                        Id = gcItem.Data.Id.ToString(),
                        Title = gcItem.Data.Name
                    };

                    itemResponseModel.Status = new GcStatusModel
                    {
                        Color = gcItem.Data.Status.Data.Color,
                        Name = gcItem.Data.Status.Data.Name,
                    };

                    var gcTemplate = TemplatesService.GetSingleTemplate(gcItem.Data.TemplateId.ToString());
                    itemResponseModel.GcTemplate = new GcTemplateModel
                    {
                        Id = gcTemplate.Data.Id.ToString(),
                        Name = gcTemplate.Data.Name
                    };

                    //element that corresponds to item in CMS that holds mappings
                    TemplateMapping templateMapping = MappingRepository.GetMappingById(importItem.SelectedMappingId);

                    List<Element> gcFields = gcItem.Data.Config.SelectMany(i => i.Elements).ToList();

                    if (templateMapping != null) // template found, now map fields here
                    {
                        //save id and gc item name to use as item name later
                        gcDataDictionary.Add(gcItem.Data.Id.ToString(), gcItem.Data.Name);

                        var files = new List<File>();
                        if (gcItem.Data.Config.SelectMany(config => config.Elements).Select(element => element.Type).Contains("files"))
                        {
                            foreach (var file in ItemsService.GetItemFiles(gcItem.Data.Id.ToString()).Data)
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

                        bool fieldError = false;

                        var groupedFields = templateMapping.FieldMappings.GroupBy(i => i.CmsField);

                        foreach (var grouping in groupedFields)
                        {
                            CmsField cmsField = grouping.Key;

                            var gcFieldIds = grouping.Select(i => i.GcField.Id);
                            var gcFieldsToMap = grouping.Select(i => i.GcField);

                            IEnumerable<Element> gcFieldsForMapping = gcFields.Where(i => gcFieldIds.Contains(i.Name)).ToList();

                            var gcField = gcFieldsForMapping.FirstOrDefault();

                            if (gcField != null)
                            {
                                var value = GetValue(gcFieldsForMapping);
                                var options = GetOptions(gcFieldsForMapping);

                                files = files.Where(x => x.FieldId == gcField.Name).ToList(); //TODO: check files

                                cmsField.Files = files;
                                cmsField.Value = value;
                                cmsField.Options = options;

                                //update GC fields' type
                                foreach (var field in gcFieldsToMap)
                                {
                                    field.Type = gcField.Type;
                                }
                            }
                            else
                            {
                                //if field error, set error message
                                itemResponseModel.ImportMessage = "Import failed: Template fields mismatch";
                                itemResponseModel.IsImportSuccessful = false;
                                fieldError = true;
                                break;
                            }
                        }

                        templateMapping.FieldMappings.Add(new FieldMapping
                        {
                            CmsField = new CmsField
                            {
                                TemplateField = new CmsTemplateField { FieldName = "GC Content Id" },
                                Value = gcItem.Data.Id.ToString()
                            }
                        });

                        if (!fieldError)
                        {
                            templateMappings.Add(templateMapping);
                        }
                    }
                    else
                    {
                        //no template mapping, set error message
                        itemResponseModel.ImportMessage = "Import failed: Template not mapped";
                        itemResponseModel.IsImportSuccessful = false;
                    }
                }
                model.Add(itemResponseModel);
            }

            var itemsMap = GetItemsMap(projectId, model.Select(x => x.GcItem.Id));

            //make paths short as much as possible
            //get first path and remove all same parent items for all paths
            var pathItemsToBeRemoved = new List<string>();

            Dictionary<string, List<GcPathItem>> shortPaths = new Dictionary<string, List<GcPathItem>>();
            if (itemsMap.Count() > 1)
            {
                var firstPath = itemsMap.First();
                foreach (var path in firstPath.Value)
                {
                    //if all paths start with same item and this item is not selected
                    //TODO: check all cases work: add UT
                    if (itemsMap.Select(x => x.Value).All(x => x.First().Id == path.Id) && !items.Select(x => x.Id).Contains(path.Id))
                    {
                        pathItemsToBeRemoved.Add(path.Id);
                    }
                }
            }

            foreach (var item in itemsMap)
            {
                List<GcPathItem> itemsToAdd = new List<GcPathItem>();

                foreach (var gcPathItem in item.Value)
                {
                    if (!pathItemsToBeRemoved.Contains(gcPathItem.Id))
                    {
                        itemsToAdd.Add(gcPathItem);
                    }
                }
                itemsToAdd.Reverse();
                shortPaths.Add(item.Key, itemsToAdd);
            }

            //run through template mappings
            foreach (var templateMapping in templateMappings)
            {
                var gcContentId = templateMapping.FieldMappings.Select(x => x.CmsField).First(f => f.TemplateField.FieldName == "GC Content Id").Value.ToString();

                var cmsItem = new CmsItem
                {
                    Template = templateMapping.CmsTemplate,
                    Title = gcDataDictionary[gcContentId],
                    Fields = templateMapping.FieldMappings.Select(x => x.CmsField).ToList(),
                    Language = language
                };

                var path = shortPaths[gcContentId];//path for gc item we are going to import now
                var gcPath = string.Join("/", path.Select(x => x.Name));

                var parentId = itemId;

                //для каждого маппинга (что по сути gc айтем => sitecore/umbraco айтем) берём GC путь и бежим по нему.
                for (int i = 0; i < path.Count; i++)
                {
                    //для каждого айтема в пути смотрим, существует ли он уже в CMS (независимо от языка); если да - скипуем, если нет - добавляем фейковый(дефолтный)
                    if (i == path.Count - 1)
                    {
                        //когда доходим по последнего айтема в пути, то есть до выбранного импортировать - делаем уже нормальный айтем
                        if (ItemsRepository.IfMappedItemExists(parentId, cmsItem, templateMapping.MappingId, gcPath))
                        {
                            cmsItem.Id = ItemsRepository.AddNewVersion(parentId, cmsItem, templateMapping.MappingId, gcPath);
                        }
                        else
                        {
                            cmsItem.Id = ItemsRepository.CreateMappedItem(parentId, cmsItem, templateMapping.MappingId, gcPath);
                        }
                        parentId = cmsItem.Id;
                        //

                        var cmsLink = ItemsRepository.GetCmsItemLink(HttpContext.Current.Request.Url.Host, cmsItem.Id);

                        var idField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == "GC Content Id");
                        var itemResponseModel = model.FirstOrDefault(item => idField != null && item.GcItem.Id == idField.Value.ToString());
                        if (itemResponseModel != null)
                        {
                            itemResponseModel.CmsLink = cmsLink;
                        }

                        var fields = templateMapping.FieldMappings;

                        foreach (var field in fields)
                        {
                            if (field.GcField != null)
                            {
                                switch (field.GcField.Type)
                                {
                                    case "choice_radio":
                                    case "choice_checkbox":
                                        {
                                            ItemsRepository.MapChoice(cmsItem, field.CmsField);
                                        }
                                        break;
                                    case "files":
                                        {
                                            ItemsRepository.MapFile(cmsItem, field.CmsField);
                                        }
                                        break;
                                    default:
                                        {
                                            ItemsRepository.MapText(cmsItem, field.CmsField);
                                        }
                                        break;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(statusId))
                        {
                            if (idField != null)
                            {
                                var status = PostNewItemStatus(idField.Value.ToString(), statusId, projectId);
                                if (itemResponseModel == null) continue;
                                itemResponseModel.Status.Color = status.Color;
                                itemResponseModel.Status.Name = status.Name;
                            }
                        }
                    }
                    else
                    {
                        var notMappedCmsItem = new CmsItem
                        {
                            Title = path[i].Name,
                            Language = language
                        };
                        //if we are not at the selected item, somewhere in the middle
                        if (!ItemsRepository.IfNotMappedItemExists(parentId, notMappedCmsItem))
                        {
                            cmsItem.Id = ItemsRepository.CreateNotMappedItem(parentId, notMappedCmsItem);
                            parentId = cmsItem.Id;
                        }
                        else
                        {
                            parentId = ItemsRepository.GetItemId(parentId, notMappedCmsItem);
                        }
                    }
                }
            }
            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        //public string GetGcItemPath(string itemId, string projectId)
        //{
        //    var accounts = AccountsService.GetAccounts();
        //    var account = accounts.Data.FirstOrDefault();

        //    if (account != null)
        //    {
        //        var project = ProjectsService.GetProjects(account.Id).Data.FirstOrDefault(p => p.Active && p.Id.ToString() == projectId);

        //        if (project != null)
        //        {
        //            List<GCItem> items = GetItems(project.Id);
        //            var gcItem = items.FirstOrDefault(x => x.Id.ToString() == itemId);
        //            var path = new List<string>();

        //            while (true)
        //            {
        //                if (gcItem != null)
        //                {
        //                    path.Add(gcItem.Name);
        //                    if (gcItem.ParentId != 0)
        //                    {
        //                        gcItem = items.FirstOrDefault(x => x.Id == gcItem.ParentId);
        //                        continue;
        //                    }
        //                }
        //                break;
        //            }
        //            path.Reverse();

        //            return string.Join("/", path);
        //        }
        //    }

        //    return string.Empty;
        //}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="gcItemIds"></param>
        /// <returns></returns>
        public Dictionary<string, List<GcPathItem>> GetItemsMap(string projectId, IEnumerable<string> gcItemIds)
        {
            List<GcPathItem> items = new List<GcPathItem>();
            Dictionary<string, List<GcPathItem>> paths = new Dictionary<string, List<GcPathItem>>();
            var accounts = AccountsService.GetAccounts();
            var account = accounts.Data.FirstOrDefault();

            if (account != null)
            {
                var project = ProjectsService.GetProjects(account.Id).Data.FirstOrDefault(p => p.Active && p.Id.ToString() == projectId);

                if (project != null)
                {
                    foreach (var gcItemId in gcItemIds)
                    {
                        string itemInPathId = gcItemId;
                        GcPathItem item = null;
                        List<GcPathItem> path = new List<GcPathItem>();
                        while (true)
                        {
                            if (!items.Any(x => x.Id.Equals(itemInPathId))) //if we've not requested it yet
                            {
                                var gcItem = ItemsService.GetSingleItem(itemInPathId);
                                if (gcItem != null)
                                {
                                    item = new GcPathItem
                                    {
                                        Id = itemInPathId,
                                        Name = gcItem.Data.Name,
                                        ParentId = gcItem.Data.ParentId
                                    };

                                    items.Add(item);
                                }
                                else
                                {
                                }
                            }
                            else
                            {
                                item = items.First(x => x.Id == itemInPathId);
                            }


                            if (item != null)
                            {
                                path.Add(item);
                                if (item.ParentId != 0)
                                {
                                    itemInPathId = item.ParentId.ToString();
                                    continue;
                                }
                            }
                            break;
                        }
                        paths.Add(gcItemId, path);
                    }
                    return paths;
                }
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <param name="projectId"></param>
        /// <param name="statusId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public ImportResultModel ImportItemsWithLocation(List<LocationImportItemModel> items, string projectId, string statusId, string language)
        {
            var importItems = new List<ImportItemModel>();

            if (items == null) return null;

            foreach (var item in items)
            {
                if (item.IsImport)
                {
                    importItems.Add(new ImportItemModel
                    {
                        Id = item.Id,
                        SelectedMappingId = item.SelectedMappingId,
                        DefaultLocation = item.SelectedLocation
                    });
                }
            }

            List<MappingResultModel> cmsItems = MapItems(importItems);
            if (cmsItems == null) return null;
            List<MappingResultModel> successfulImportedItems = GetSuccessfulImportedItems(cmsItems);

            foreach (var successfulImportedItem in successfulImportedItems)
            {
                //ItemsRepository.CreateItem(successfulImportedItem.DefaultLocation, new CmsItem
                //{

                //});
            }

            if (!string.IsNullOrEmpty(statusId))
            {
                PostNewStatusesForItems(successfulImportedItems, statusId, projectId);
            }

            var result = new ImportResultModel(cmsItems);

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public List<MappingResultModel> MapItems(List<GCItem> items)
        {
            var templates = MappingRepository.GetMappings();
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
                var gcItem = ItemsService.GetSingleItem(importItem.Id);

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
