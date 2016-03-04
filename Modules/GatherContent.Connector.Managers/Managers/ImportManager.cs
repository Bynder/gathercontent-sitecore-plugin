using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.SitecoreRepositories.Repositories;

namespace GatherContent.Connector.Managers.Managers
{
    public enum TryMapItemState
    {
        Success = 0,
        TemplateError = 1,
        FieldError = 2
    }

    public class ImportManager : BaseManager
    {
        private readonly ItemsRepository _itemsRepository;
        private readonly MappingRepository _mappingRepository;
        private readonly ItemsService _itemsService;
        private readonly ProjectsService _projectsService;
        private readonly TemplatesService _templatesService;

        private readonly MappingManager _mappingManager;
        private readonly GCAccountSettings _gcAccountSettings;

        public ImportManager()
        {
            _itemsRepository = new ItemsRepository();
            _mappingRepository = new MappingRepository();

            var accountsRepository = new AccountsRepository();
            _gcAccountSettings = accountsRepository.GetAccountSettings();

            _itemsService = new ItemsService(_gcAccountSettings);
            _projectsService = new ProjectsService(_gcAccountSettings);
            _templatesService = new TemplatesService(_gcAccountSettings);

            _mappingManager = new MappingManager();
        }


        #region Utilities

        private string ConvertMsecToFormattedDate(double date)
        {
            var posixTime = DateTime.SpecifyKind(new DateTime(1970, 1, 1), DateTimeKind.Utc);
            var dateFormat = _gcAccountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }
            var gcUpdateDate =
                posixTime.AddMilliseconds(date * 1000).ToString(dateFormat);
            return gcUpdateDate;
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
                gcTemplate = _templatesService.GetSingleTemplate(templateId.ToString()).Data;
                templatesDictionary.Add(templateId, gcTemplate);
            }

            return gcTemplate;
        }


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

            var files = new List<FileOld>();
            if (gcItem.Config.SelectMany(config => config.Elements).Select(element => element.Type).Contains("files"))
            {

                foreach (var file in _itemsService.GetItemFiles(gcItem.Id.ToString()).Data)
                {
                    files.Add(new FileOld
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

            var files = new List<FileOld>();
            if (item.Config.SelectMany(config => config.Elements).Select(element => element.Type).Contains("files"))
            {

                foreach (var file in _itemsService.GetItemFiles(item.Id.ToString()).Data)
                {
                    files.Add(new FileOld
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

        private TryMapItemState TryMapField(List<Element> gcFields, IGrouping<string, MappingFieldModel> fieldsMappig, List<FileOld> files, out ImportCMSField importCMSField)
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

        private TryMapItemState TryMapFields(List<Element> gcFields, IEnumerable<IGrouping<string, MappingFieldModel>> fieldsMappig, List<FileOld> files, out List<ImportCMSField> result)
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


        private Project GetProject(List<Project> projects, string projectIdStr)
        {
            int projectId;
            int.TryParse(projectIdStr, out projectId);

            Project project = projectId != 0 ? projects.FirstOrDefault(i => i.Id == projectId) : projects.FirstOrDefault();

            return project;
        }

        private List<GCStatus> GetStatuses(int projectId)
        {
            StatusesEntity statuses = _projectsService.GetAllStatuses(projectId.ToString());
            return statuses.Data;
        }

        private List<GCItem> GetItems(int projectId)
        {
            ItemsEntity items = _itemsService.GetItems(projectId.ToString());
            return items.Data;
        }

        private List<GCTemplate> GetTemplates(int projectId)
        {
            return GetTemplates(projectId.ToString());
        }

        private List<GCTemplate> GetTemplates(string projectId)
        {
            TemplatesEntity templates = _templatesService.GetTemplates(projectId);
            return templates.Data;
        }

        private List<ImportListItem> MapItems(List<GCItem> items, List<GCTemplate> templates)
        {
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();
            var dateFormat = _gcAccountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }

            var result = new List<ImportListItem>();
            foreach (var mappedItem in mappedItems)
            {
                var mappings = _mappingRepository.GetAllMappingsForGcTemplate(mappedItem.ProjectId.ToString(),
                    mappedItem.TemplateId.ToString());
                var availableMappings = mappings.Select(availableMappingModel => new AvailableMapping
                {
                    Id = availableMappingModel.Id,
                    Title = !string.IsNullOrEmpty(availableMappingModel.Title) ?
                        availableMappingModel.Title : string.Format("[{0}]", availableMappingModel.Name),
                }).ToList();

                result.Add(new ImportListItem(mappedItem, templates.FirstOrDefault(templ => templ.Id == mappedItem.TemplateId), items, dateFormat, availableMappings));
            }

            return result.ToList();
        }

        private List<ImportItembyLocation> MapItemsByLocation(List<GCItem> items, List<GCTemplate> templates)
        {
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();
            var dateFormat = _gcAccountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = Constants.DateFormat;
            }

            var result = new List<ImportItembyLocation>();
            foreach (var mappedItem in mappedItems)
            {
                var mappings = _mappingRepository.GetAllMappingsForGcTemplate(mappedItem.ProjectId.ToString(),
                    mappedItem.TemplateId.ToString());
                var availableMappings = mappings.Select(availableMappingModel => new AvailableMappingByLocation
                {
                    Id = availableMappingModel.Id,
                    Title = !string.IsNullOrEmpty(availableMappingModel.Title) ?
                        availableMappingModel.Title : string.Format("[{0}]", availableMappingModel.Name),
                    OpenerId = "drop-tree" + Guid.NewGuid(),
                    ScTemplate = availableMappingModel.ScTemplate,
                    IsShowing = false,
                    DefaultLocation = availableMappingModel.DefaultLocation,
                    DefaultLocationTitle = availableMappingModel.DefaultLocationTitle,
                }).ToList();

                result.Add(new ImportItembyLocation(mappedItem, templates.FirstOrDefault(templ => templ.Id == mappedItem.TemplateId), items, dateFormat, availableMappings));
            }

            return result.ToList();
        }

        private List<GCItem> MapItems(List<string> items)
        {
            List<GCItem> result = items.Select(GetGcItemByModel).ToList();
            return result;
        }

        private GCItem GetGcItemByModel(string id)
        {
            ItemEntity result = _itemsService.GetSingleItem(id);

            return result.Data;
        }

        private List<MappingResultModel> GetSuccessfulImportedItems(List<MappingResultModel> importResult)
        {
            return importResult.Where(i => i.IsImportSuccessful).ToList();
        }

        private void PostNewStatusesForItems(List<MappingResultModel> items, string statusId, string projectId)
        {
            foreach (MappingResultModel item in items)
            {
                _itemsService.ChooseStatusForItem(item.GCItemId, statusId);
                var status = _projectsService.GetSingleStatus(statusId, projectId);
                item.Status.Color = status.Data.Color;
                item.Status.Name = status.Data.Name;
            }
        }

        #endregion


        public SelectItemsForImportModel GetModelForSelectImportItemsDialog(string itemId, string projectId)
        {
            Account account = GetAccount();

            List<Project> projects = GetProjects(account.Id);

            Project project = GetProject(projects, projectId);

            List<GCTemplate> templates = GetTemplates(project.Id);
            List<GCStatus> statuses = GetStatuses(project.Id);
            List<GCItem> items = GetItems(project.Id);
            items = items.OrderBy(item => item.Status.Data.Name).ToList();

            List<ImportListItem> mappedItems = MapItems(items, templates);

            var result = new SelectItemsForImportModel(mappedItems, project, projects, statuses, templates);

            return result;
        }

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



        public ImportResultModel ImportItems(string itemId, List<ImportItemModel> items, string projectId, string statusId, string language)
        {
            List<MappingResultModel> cmsItems = MapItems(items);

            if (cmsItems == null) return null;
            List<MappingResultModel> successfulImportedItems = GetSuccessfulImportedItems(cmsItems);
            successfulImportedItems = _itemsRepository.ImportItems(itemId, language, successfulImportedItems);

            if (!string.IsNullOrEmpty(statusId))
            {
                PostNewStatusesForItems(successfulImportedItems, statusId, projectId);
            }

            var result = new ImportResultModel(cmsItems);

            return result;
        }

        public ImportResultModel ImportItemsWithLocation(List<LocationImportItemModel> items,
            string projectId, string statusId, string language)
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

            successfulImportedItems = _itemsRepository.ImportItemsWithLocation(language, successfulImportedItems);

            if (!string.IsNullOrEmpty(statusId))
            {
                PostNewStatusesForItems(successfulImportedItems, statusId, projectId);
            }

            var result = new ImportResultModel(cmsItems);

            return result;
        }


        public List<MappingResultModel> MapItems(List<GCItem> items, string projectId)
        {
            List<MappingTemplateModel> templates = _mappingRepository.GetTemplateMappingsByProjectId(projectId);

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
                var gcItem = _itemsService.GetSingleItem(importItem.Id);

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
