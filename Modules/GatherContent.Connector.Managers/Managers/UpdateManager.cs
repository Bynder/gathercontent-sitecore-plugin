using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Mapping;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.Managers.Models.ImportItems.New;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.SitecoreRepositories.Repositories;
using Sitecore.Diagnostics;

namespace GatherContent.Connector.Managers.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class UpdateManager : BaseManager, IUpdateManager
    {
        protected IItemsRepository ItemsRepository;

        protected IMappingRepository MappingRepository;

        protected IItemsService ItemsService;

        protected IMappingManager MappingManager;

        protected IImportManager ImportManager;

        protected GCAccountSettings GcAccountSettings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemsRepository"></param>
        /// <param name="mappingRepository"></param>
        /// <param name="itemsService"></param>
        /// <param name="mappingManager"></param>
        /// <param name="importManager"></param>
        /// <param name="accountsService"></param>
        /// <param name="projectsService"></param>
        /// <param name="templateService"></param>
        /// <param name="cacheManager"></param>
        /// <param name="gcAccountSettings"></param>
        public UpdateManager(
            IItemsRepository itemsRepository,
            IMappingRepository mappingRepository,
            IItemsService itemsService,
            IMappingManager mappingManager,
            IImportManager importManager,
            IAccountsService accountsService,
            IProjectsService projectsService,
            ITemplatesService templateService,
            ICacheManager cacheManager,
            GCAccountSettings gcAccountSettings)
            : base(accountsService, projectsService, templateService, cacheManager)
        {
            ItemsRepository = itemsRepository;

            MappingRepository = mappingRepository;

            ItemsService = itemsService;

            MappingManager = mappingManager;

            ImportManager = importManager;

            GcAccountSettings = gcAccountSettings;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public SelectItemsForUpdateModel GetItemsForUpdate(string itemId)
        {
            var cmsItems = ItemsRepository.GetItems(itemId, "").ToList();

            List<GCTemplate> templates;
            List<GCStatus> statuses;
            List<UpdateListItem> models;
            List<Project> projects;
            TryToGetModelData(cmsItems, out templates, out statuses, out models, out projects);

            var result = new SelectItemsForUpdateModel(models, statuses, templates, projects);
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmsItems"></param>
        /// <param name="templates"></param>
        /// <param name="statuses"></param>
        /// <param name="items"></param>
        /// <param name="projects"></param>
        /// <returns></returns>
        private bool TryToGetModelData(List<CmsItem> cmsItems, out List<GCTemplate> templates, out List<GCStatus> statuses, out List<UpdateListItem> items, out List<Project> projects)
        {
            var projectsDictionary = new Dictionary<int, Project>();
            var templatesDictionary = new Dictionary<int, GCTemplate>();

            statuses = new List<GCStatus>();
            items = new List<UpdateListItem>();

            foreach (var cmsItem in cmsItems)
            {
                var idField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == "GC Content Id");
                if (idField != null && !string.IsNullOrEmpty(idField.Value.ToString()))
                {
                    ItemEntity entity = null;
                    try
                    {
                        entity = ItemsService.GetSingleItem(idField.Value.ToString());
                    }
                    catch (WebException exception)
                    {
                        Log.Error("GatherContent message. Api Server error has happened during getting Item with id = " + idField.Value.ToString(), exception);
                        using (var response = exception.Response)
                        {
                            var httpResponse = (HttpWebResponse)response;
                            if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                            {
                                throw;
                            }
                        }
                    }
                    if (entity != null)
                    {
                        GCItem gcItem = entity.Data;
                        Project project = GetProject(projectsDictionary, gcItem.ProjectId);
                        if (gcItem.TemplateId.HasValue)
                        {
                            GCTemplate template = GetTemplate(templatesDictionary, gcItem.TemplateId.Value);

                            string gcLink = null;
                            if (!string.IsNullOrEmpty(GcAccountSettings.GatherContentUrl))
                            {
                                gcLink = GcAccountSettings.GatherContentUrl + "/item/" + gcItem.Id;
                            }
                            var dateFormat = GcAccountSettings.DateFormat;
                            if (string.IsNullOrEmpty(dateFormat))
                            {
                                dateFormat = Constants.DateFormat;
                            }
                            var cmsLink =
                                string.Format(
                                    "http://{0}/sitecore/shell/Applications/Content Editor?fo={1}&sc_content=master&sc_bw=1",
                                    HttpContext.Current.Request.Url.Host, cmsItem.Id);


                            var lastUpdate = new DateTime();
                            string cmsTemplateName = null;
                            var lastUpdateField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == "Last Sync Date");
                            if (lastUpdateField != null)
                            {
                                lastUpdate = (DateTime)lastUpdateField.Value;
                            }

                            var cmsTemplateNameField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == "Template");
                            if (cmsTemplateNameField != null)
                            {
                                cmsTemplateName = cmsTemplateNameField.Value.ToString();
                            }

                            var cmsUpdateItem = new CMSUpdateItem(cmsItem.Id, cmsItem.Title, cmsTemplateName, idField.Value.ToString(), lastUpdate);
                            var listItem = new UpdateListItem(gcItem, template, cmsUpdateItem, dateFormat, project.Name,
                                cmsLink, gcLink);
                            items.Add(listItem);

                            GCStatus status = gcItem.Status.Data;
                            if (statuses.All(i => i.Id != status.Id))
                            {
                                statuses.Add(status);
                            }
                        }
                    }
                }
            }

            items = items.OrderBy(item => item.Status.Name).ToList();
            templates = templatesDictionary.Values.ToList();
            projects = projectsDictionary.Values.ToList();

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templates"></param>
        /// <param name="templateId"></param>
        /// <returns></returns>
        private GCTemplate GetTemplate(Dictionary<int, GCTemplate> templates, int templateId)
        {
            GCTemplate template;
            templates.TryGetValue(templateId, out template);

            if (template == null)
            {
                template = GetGcTemplateEntity(templateId.ToString()).Data;
                templates.Add(templateId, template);
            }

            return template;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        private Project GetProject(Dictionary<int, Project> projects, int projectId)
        {
            Project project;
            projects.TryGetValue(projectId, out project);

            if (project == null)
            {
                project = GetGcProjectEntity(projectId.ToString()).Data;
                projects.Add(projectId, project);
            }

            return project;
        }

        private object GetValue(IEnumerable<Element> fields)
        {
            string value = string.Join("", fields.Select(i => i.Value));
            return value;
        }

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
        /// <param name="itemId"></param>
        /// <param name="models"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public List<ItemResultModel> UpdateItems(string itemId, List<UpdateListIds> models, string language)
        {
            var model = new List<ItemResultModel>();

            var gcItems = new Dictionary<GCItem, string>();

            foreach (var item in models)
            {
                GCItem gcItem = ItemsService.GetSingleItem(item.GCId).Data;
                gcItems.Add(gcItem, item.CMSId);
            }

            var templates = MappingRepository.GetMappings();
            var templatesDictionary = new Dictionary<int, GCTemplate>();

            foreach (var item in gcItems)
            {
                var gcItem = item.Key; //gc item
                var cmsId = item.Value; // corresponding cms id
                var itemResponseModel = new ItemResultModel
                {
                    IsImportSuccessful = true,
                    ImportMessage = "Update Successful"
                };

                GCTemplate gcTemplate;
                var templateId = gcItem.TemplateId.Value;
                templatesDictionary.TryGetValue(templateId, out gcTemplate);
                if (gcTemplate == null)
                {
                    gcTemplate = TemplatesService.GetSingleTemplate(templateId.ToString()).Data;
                    templatesDictionary.Add(templateId, gcTemplate);
                }

                //MappingResultModel cmsItem;
                //TryMapItem(gcItem, gcTemplate, templates, out cmsItem);
                //result.Add(cmsItem);
                List<Element> gcFields = gcItem.Config.SelectMany(i => i.Elements).ToList();

                var templateMapping = templates.First(x => x.GcTemplate.GcTemplateId == gcItem.TemplateId.ToString());
                if (templateMapping != null) // template found, now map fields here
                {
                    var files = new List<File>();
                    if (
                        gcItem.Config.SelectMany(config => config.Elements)
                            .Select(element => element.Type)
                            .Contains("files"))
                    {
                        foreach (var file in ItemsService.GetItemFiles(gcItem.Id.ToString()).Data)
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

                        IEnumerable<Element> gcFieldsForMapping =
                            gcFields.Where(i => gcFieldIds.Contains(i.Name)).ToList();

                        var gcField = gcFieldsForMapping.FirstOrDefault();

                        if (gcField != null)
                        {
                            var value = GetValue(gcFieldsForMapping);
                            var options = GetOptions(gcFieldsForMapping);

                            files = files.Where(x => x.FieldId == gcField.Name).ToList();

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
                            itemResponseModel.ImportMessage = "Update failed: Template fields mismatch";
                            itemResponseModel.IsImportSuccessful = false;
                            fieldError = true;
                            break;
                        }
                    }

                    if (!fieldError)
                    {
                        var cmsContentIdField = new FieldMapping
                        {
                            CmsField = new CmsField
                            {
                                TemplateField = new CmsTemplateField {FieldName = "GC Content Id"},
                                Value = gcItem.Id.ToString()
                            }
                        };
                        templateMapping.FieldMappings.Add(cmsContentIdField);

                        var cmsItem = new CmsItem
                        {
                            Template = templateMapping.CmsTemplate,
                            Title = gcItem.Name,
                            Fields = templateMapping.FieldMappings.Select(x => x.CmsField).ToList(),
                            Language = language,
                            Id = cmsId
                        };

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

                        //ItemsRepository.UpdateItem(new CmsItem
                        //{


                        //});
                    }

                }
                else
                {
                    //no template mapping, set error message
                    itemResponseModel.ImportMessage = "Update failed: Template not mapped";
                    itemResponseModel.IsImportSuccessful = false;
                }
                model.Add(itemResponseModel);

            }

            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        private List<GCItem> GetGCItemsByModels(List<UpdateListIds> models)
        {
            var result = new List<GCItem>();

            foreach (var item in models)
            {
                GCItem gcItem = ItemsService.GetSingleItem(item.GCId).Data;
                var gcItemWithCmsId = new UpdateGCItem(gcItem, item.CMSId);
                result.Add(gcItemWithCmsId);
            }

            return result;
        }
    }
}
