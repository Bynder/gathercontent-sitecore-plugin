namespace Bynder.Content.SitecoreConnector.Managers.Managers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using Core.DependencyInjection;
    using Core.Entities;
    using Core.Interfaces;
    using Core.Models.Import;
    using Core.Models.Mapping;
    using Enums;
    using Interfaces;
    using Models.ImportItems;
    using Models.Mapping;
    using Models.UpdateItems;
    using SitecoreRepositories.Repositories;
    using Models.ImportItems.New;
    using Bynder.Content.SitecoreConnector.GatherContentService.Interfaces;
    using Sitecore.Data;
    using Sitecore.Configuration;
    using Sitecore.Data.Query;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.SecurityModel;
    using Sitecore;
    using System.Security.Cryptography;

    [Service(typeof(IImportManager))]
    public class ImportManager : BaseManager, IImportManager
    {
        protected IItemsRepository ItemsRepository;
        protected IMappingRepository MappingRepository;
        protected IItemsService ItemsService;
        protected readonly Database ContextDatabase;

        public ImportManager(
            IItemsRepository itemsRepository,
            IMappingRepository mappingRepository,
            IItemsService itemsService,
            IAccountsService accountsService,
            IProjectsService projectsService,
            ITemplatesService templateService,
            ICacheManager cacheManager,
            IAccountsRepository accountsRepository)
            : base(accountsService, projectsService, templateService, cacheManager, accountsRepository)
        {
            ItemsRepository = itemsRepository;
            MappingRepository = mappingRepository;
            ItemsService = itemsService;
            ContextDatabase = Factory.GetDatabase("master");
        }

        public List<ItemModel> GetImportDialogModel(string itemId, string projectId)
        {
            var model = new List<ItemModel>();
            if (projectId == "0")
            {
                return null;
            }

            var project = GetCwbProjectEntity(projectId);

            if (project != null)
            {
                List<CWBTemplate> templates = GetTemplates(project.Data.Id);

                List<CWBItem> items = GetItems(project.Data.Id);
                items = items.OrderBy(item => item.Status.Data.Name).ToList();
                model = MapImportItems(items, templates);

                // do not show items without mappings
                model = model.Where(item => item.AvailableMappings.Mappings.Any()).ToList();
                return model;
            }

            return model;
        }

        public Models.ImportItems.New.FiltersModel GetFilters(string projectId)
        {
            Account account = GetAccount();

            List<Project> cwbProjects = GetProjectsWithData(account.Id);

            var projects = new List<CwbProjectModel>();
            foreach (var cwbProject in cwbProjects)
            {
                projects.Add(new CwbProjectModel
                {
                    Id = cwbProject.Id.ToString(),
                    Name = cwbProject.Name
                });
            }

            if (projectId != "0")
            {
                Project cwbProject = GetProject(cwbProjects, projectId);

                List<CWBTemplate> cwbTemplates = GetTemplates(cwbProject.Id);
                var templates = new List<CwbTemplateModel>();
                foreach (var cwbTemplate in cwbTemplates)
                {
                    templates.Add(new CwbTemplateModel
                    {
                        Id = cwbTemplate.Id.ToString(),
                        Name = cwbTemplate.Name

                    });
                }
                List<CWBStatus> cwbStatuses = GetStatuses(cwbProject.Id);

                var statuses = new List<CwbStatusModel>();
                foreach (var cwbStatus in cwbStatuses)
                {
                    statuses.Add(new CwbStatusModel
                    {
                        Id = cwbStatus.Id,
                        Name = cwbStatus.Name,
                        Color = cwbStatus.Color
                    });
                }

                return new Models.ImportItems.New.FiltersModel
                {
                    CurrentProject = new CwbProjectModel
                    {
                        Id = cwbProject.Id.ToString(),
                        Name = cwbProject.Name
                    },
                    Projects = projects,
                    Statuses = statuses,
                    Templates = templates
                };

            }

            return new Models.ImportItems.New.FiltersModel
            {
                Projects = projects
            };
        }

        private List<Item> CreateChildOptions(Item parentItem, List<string> itemNames, ID templateID)
        {
            if (parentItem != null && itemNames.Any() && !templateID.IsNull)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(parentItem.Language))
                    {
                        using (new EnforceVersionPresenceDisabler())
                        {
                            foreach (var itemName in itemNames)
                            {
                                Item createdOption = null;
                                try
                                {
                                    var templateOption = ContextDatabase.GetTemplate(templateID);
                                    var validName = ItemUtil.ProposeValidItemName(itemName);
                                    createdOption = parentItem.Add(validName, templateOption);
                                }
                                catch (Exception ex)
                                {
                                    Log.Error("Cannot create options item.", ex, this);
                                    throw new Exception(string.Format("Your template({0}) is not inherited from the CWB Linked Item.", createdOption.TemplateName));
                                }
                            }
                        }
                    }
                }
            }

            return parentItem.Axes.GetDescendants().ToList();
        }

        private List<ItemResultModel> Import(string itemId, List<ImportItemModel> items, string projectId, string statusId, string language)
        {
            var model = new List<ItemResultModel>();

            //get all paths
            var fullCwbPaths = GetItemsMap(projectId, items.Select(x => x.Id));
            var pathItemsToBeRemoved = new List<int>();

            Dictionary<string, List<ItemEntity>> shortPaths = new Dictionary<string, List<ItemEntity>>();
            if (fullCwbPaths.Count() > 1)
            {
                var firstPath = fullCwbPaths.First();
                foreach (var path in firstPath.Value)
                {
                    //if all paths start with same item and this item is not selected
                    if (fullCwbPaths.Select(x => x.Value).All(x => x.First().Data.Id == path.Data.Id) && !items.Select(x => x.Id).Contains(path.Data.Id.ToString()))
                    {
                        pathItemsToBeRemoved.Add(path.Data.Id);
                    }
                }
            }

            foreach (var item in fullCwbPaths)
            {
                List<ItemEntity> itemsToAdd = new List<ItemEntity>();

                foreach (var cwbPathItem in item.Value)
                {
                    if (!pathItemsToBeRemoved.Contains(cwbPathItem.Data.Id))
                    {
                        itemsToAdd.Add(cwbPathItem);
                    }
                }
                shortPaths.Add(item.Key, itemsToAdd);
            }

            var firstItem = items.FirstOrDefault();
            var templateMappingSc = MappingRepository.GetMappingById(firstItem.SelectedMappingId);
            var sorted = shortPaths.OrderBy(x => x.Value.Count).ThenBy(x => x.Value.First().Data.Name);//sort to start from shortest and alphabetically asc

            var itemEntity = sorted.FirstOrDefault().Value.FirstOrDefault(); //!important
            var listOptions = new List<string>();
            var finalListOptions = new List<Item>();

            foreach (var configs in itemEntity.Data.Config)
            {
                foreach (var element in configs.Elements)
                {
                    if (element.Type.Equals("choice_checkbox") || element.Type.Equals("choice_radio"))
                    {
                        var currentFieldMapping = templateMappingSc.FieldMappings.Where(m => m.CwbField.Id.Equals(element.Name)).FirstOrDefault();

                        if (currentFieldMapping != null)
                        {
                            var currentOptionsTemplateId = currentFieldMapping.CmsField.TemplateField.OptionsTemplateId;
                            var currentOptionsContentFolderId = currentFieldMapping.CmsField.TemplateField.OptionsContentFolderId;

                            if (!string.IsNullOrEmpty(currentOptionsTemplateId) && !string.IsNullOrEmpty(currentOptionsContentFolderId))
                            {
                                var optionsTemplateItem = ContextDatabase.GetItem(new ID(currentOptionsTemplateId));
                                var optionsContentFolderItem = ContextDatabase.GetItem(new ID(currentOptionsContentFolderId));

                                var existingOptionList = optionsContentFolderItem.Axes.GetDescendants()
                                    .Where(t => t.TemplateName == optionsTemplateItem.Name)
                                    .Select(x => x.Name);

                                var cwbOptionList = new List<string>();

                                foreach (var option in element.Options)
                                {
                                    if (!string.IsNullOrEmpty(option.Label))
                                    {
                                        cwbOptionList.Add(option.Label);
                                    }
                                }

                                var needsToAdd = cwbOptionList.Where(x => !existingOptionList.Contains(x)).ToList();

                                var createdOptions = CreateChildOptions(optionsContentFolderItem, needsToAdd, optionsTemplateItem.ID);
                                if (createdOptions != null && createdOptions.Any())
                                {
                                    finalListOptions.AddRange(createdOptions);
                                }
                            }
                        }
                    }
                }
            }

            foreach (var path in sorted)
            {
                var itemResponseModel = new ItemResultModel
                {
                    IsImportSuccessful = true,
                    ImportMessage = "Import Successful"
                };

                var cwbItem = path.Value.Last(); //this is the item we selected to import
                var item = items.FirstOrDefault(x => x.Id == cwbItem.Data.Id.ToString()); //item coming from UI; selected mapping and other info

                if (cwbItem != null && cwbItem.Data != null && cwbItem.Data.TemplateId != null)
                {
                    if (!string.IsNullOrEmpty(CwbAccountSettings.GatherContentUrl))
                    {
                        itemResponseModel.CwbLink = string.Concat(CwbAccountSettings.GatherContentUrl, "/item/", cwbItem.Data.Id);
                    }
                    itemResponseModel.CwbItem = new CwbItemModel
                    {
                        Id = cwbItem.Data.Id.ToString(),
                        Title = cwbItem.Data.Name
                    };

                    itemResponseModel.Status = new CwbStatusModel
                    {
                        Color = cwbItem.Data.Status.Data.Color,
                        Name = cwbItem.Data.Status.Data.Name,
                    };

                    var cwbTemplate = TemplatesService.GetSingleTemplate(cwbItem.Data.TemplateId.ToString());
                    itemResponseModel.CwbTemplate = new CwbTemplateModel
                    {
                        Id = cwbTemplate.Data.Id.ToString(),
                        Name = cwbTemplate.Data.Name
                    };

                    //element that corresponds to item in CMS that holds mappings
                    TemplateMapping templateMapping = MappingRepository.GetMappingById(item.SelectedMappingId);

                    List<Element> cwbFields = cwbItem.Data.Config.SelectMany(i => i.Elements).ToList();

                    if (templateMapping != null) // template found, now map fields here
                    {
                        var files = new List<File>();
                        if (cwbItem.Data.Config.SelectMany(config => config.Elements).Select(element => element.Type).Contains("files"))
                        {
                            foreach (var file in ItemsService.GetItemFiles(cwbItem.Data.Id.ToString()).Data)
                            {
                                files.Add(new File
                                {
                                    FileName = file.FileName,
                                    Url = file.Url,
                                    FieldId = file.Field,
                                    UpdatedDate = file.Updated,
                                    FileId = file.Id
                                });
                            }
                        }

                        bool fieldError = CheckFieldError(templateMapping, cwbFields, files, itemResponseModel);

                        if (!fieldError)
                        {
                            var cmsContentIdField = new FieldMapping
                            {
                                CmsField = new CmsField
                                {
                                    TemplateField = new CmsTemplateField { FieldName = "CWB Content Id" },
                                    Value = cwbItem.Data.Id.ToString()
                                }
                            };
                            templateMapping.FieldMappings.Add(cmsContentIdField);

                            var cmsItem = new CmsItem
                            {
                                Template = templateMapping.CmsTemplate,
                                Title = cwbItem.Data.Name,
                                Fields = templateMapping.FieldMappings.Select(x => x.CmsField).ToList(),
                                Language = language
                            };

                            var cwbPath = string.Join("/", path.Value.Select(x => x.Data.Name));
                            var cwbId = cwbItem.Data.Id.ToString();

                            var parentId = itemId;

                            bool alreadyMappedItemInPath = false;
                            //for each mapping which is fact CWB Item => Sitecore/Umbraco item - get CWB Path and run through its each item
                            for (int i = 0; i < path.Value.Count; i++)
                            {
                                //for each path item check if it exists already in CMS and if yes - skip; otherwise - add not mapped item
                                if (i == path.Value.Count - 1)
                                {
                                    //if we at the last item in the path - import mapped item
                                    if (ItemsRepository.IfMappedItemExists(parentId, cmsItem, templateMapping.MappingId, cwbPath, cwbId))
                                    {
                                        cmsItem.Id = ItemsRepository.AddNewVersion(parentId, cmsItem, templateMapping.MappingId, cwbPath, cwbId);
                                    }
                                    else
                                    {
                                        cmsItem.Id = ItemsRepository.CreateMappedItem(parentId, cmsItem, templateMapping.MappingId, cwbPath);
                                    }
                                    parentId = cmsItem.Id;

                                    MapCwbField(cmsItem, templateMapping.FieldMappings, finalListOptions);

                                    //set CMS link after we got out CMS Id
                                    var cmsLink = ItemsRepository.GetCmsItemLink(HttpContext.Current.Request.Url.Scheme, HttpContext.Current.Request.Url.Host, cmsItem.Id);
                                    itemResponseModel.CmsLink = cmsLink;
                                    itemResponseModel.CmsId = cmsItem.Id;

                                    if (!string.IsNullOrEmpty(statusId))
                                    {
                                        var status = PostNewItemStatus(cwbItem.Data.Id.ToString(), statusId, projectId);
                                        itemResponseModel.Status.Color = status.Color;
                                        itemResponseModel.Status.Name = status.Name;
                                    }
                                }
                                else
                                {
                                    var currentCmsItem = new CmsItem
                                    {
                                        Title = path.Value[i].Data.Name,
                                        Language = language
                                    };
                                    //if we are not at the selected item, somewhere in the middle
                                    //1. åñëè çàìàïëåííûé àéòåì ñóùåñòâóåò (òàêîå æå íàçâàíèå è òàêîé æå gc path?), òî òîãäà âûñòàâëÿåì alreadyMappedItemInPath = òðó
                                    //è ïðîïóñêàåì âñÿêîå ñîçäàíèå, ñåòòèì òîëüêî ïàðåíò id
                                    //2. èíà÷å åñëè åñòü íåçàìàïëåííûé àéòåì:
                                    // - alreadyMappedItemInPath == true = - ñêèïóåì ñîçäàíèå, âûñòàâëÿåì åãî êàê ïàðåíò àéäè
                                    // - alreadyMappedItemInPath == false, - ñêèïóåì âñ¸, ïàðåíò àéäè íå ìåíÿåì
                                    //3. àéòåìà íèêàêîãî íåò
                                    // - alreadyMappedItemInPath == true = - ñîçäà¸ì íåçàìàïëåííûé àéòåì, âûñòàâëÿåì åãî êàê ïàðåíò àéäè
                                    // - alreadyMappedItemInPath == false, - ñêèïóåì âñ¸, ïàðåíò àéäè íå ìåíÿåì
                                    if (ItemsRepository.IfMappedItemExists(parentId, currentCmsItem))
                                    {
                                        //cmsItem.Id = ItemsRepository.CreateNotMappedItem(parentId, notMappedCmsItem);
                                        //parentId = cmsItem.Id;
                                        alreadyMappedItemInPath = true;
                                        parentId = ItemsRepository.GetItemId(parentId, currentCmsItem);
                                    }
                                    else if (ItemsRepository.IfNotMappedItemExists(parentId, currentCmsItem))
                                    {
                                        if (alreadyMappedItemInPath)
                                        {
                                            parentId = ItemsRepository.GetItemId(parentId, currentCmsItem);
                                        }
                                    }
                                    else
                                    {
                                        if (alreadyMappedItemInPath)
                                        {
                                            parentId = ItemsRepository.CreateNotMappedItem(parentId, currentCmsItem);
                                        }
                                    }
                                }

                                //related mapping collection for main mapping
                                List<RelatedTemplateMapping> relatedTemplateMappingCollection = MappingRepository.GetRelatedMappingCollection(templateMapping);

                                if (relatedTemplateMappingCollection != null && relatedTemplateMappingCollection.Any())
                                {
                                    var folderId = MappingRepository.GetOrCreatePageComponentsFolder(cmsItem.Id);
                                    if (!string.IsNullOrWhiteSpace(folderId))
                                    {
                                        foreach (var relatedMapping in relatedTemplateMappingCollection)
                                        {
                                            bool relateMappingFieldError = CheckFieldError(relatedMapping, cwbFields, files, itemResponseModel);
                                            if (!relateMappingFieldError)
                                            {
                                                MapRelatedItems(relatedMapping, cwbItem, folderId, language, cwbPath);
                                            }
                                        }
                                    }
                                }

                            }
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

            return model;
        }

        private void MapRelatedItems(RelatedTemplateMapping templateMapping,
            ItemEntity cwbItem,
            string parentId,
            string language,
            string cwbPath
            )
        {
            var cmsContentIdField = new FieldMapping
            {
                CmsField = new CmsField
                {
                    TemplateField = new CmsTemplateField { FieldName = "CWB Content Id" },
                    Value = cwbItem.Data.Id.ToString()
                }
            };
            templateMapping.FieldMappings.Add(cmsContentIdField);

            var cmsItem = new CmsItem
            {
                Template = templateMapping.CmsTemplate,
                Title = templateMapping.MappingTitle,
                Fields = templateMapping.FieldMappings.Select(x => x.CmsField).ToList(),
                Language = language
            };

            if (!string.IsNullOrWhiteSpace(templateMapping.CmsContainerTemplateId))
            {
                var containerId = MappingRepository.GetOrCreateContainer(parentId, templateMapping.CmsContainerTemplateId);
                if (containerId != null)
                {
                    parentId = containerId;
                }
            }

            if (ItemsRepository.IfMappedItemExists(parentId, cmsItem, templateMapping.MappingId, cwbPath))
            {
                cmsItem.Id = ItemsRepository.AddNewVersion(parentId, cmsItem, templateMapping.MappingId, cwbPath);
            }
            else
            {
                cmsItem.Id = ItemsRepository.CreateMappedItem(parentId, cmsItem, templateMapping.MappingId, cwbPath);
            }

            var fieldMappingCollection = templateMapping.FieldMappings;

            MapCwbField(cmsItem, fieldMappingCollection, null);
        }

        private void MapCwbField(CmsItem cmsItem, IList<FieldMapping> fieldMappingCollection, List<Item> finalListOptions)
        {
            // one CMS text field can be mapped to several CWB fields
            // in this case we concatenate their texts and put into one CMS field
            foreach (IGrouping<string, FieldMapping> fields in fieldMappingCollection.GroupBy(f => f.CmsField.TemplateField.FieldName))
            {
                FieldMapping field = fields.First();
                if (field.CwbField != null)
                {
                    switch (field.CwbField.Type)
                    {
                        case "choice_radio":
                        case "choice_checkbox":
                            {
                                foreach (var f in fields)
                                {
                                    ItemsRepository.MapChoice(cmsItem, f.CmsField, finalListOptions);
                                }
                            }
                            break;
                        case "files":
                            {
                                ItemsRepository.ResolveAttachmentMapping(cmsItem, field.CmsField);
                            }
                            break;
                        default:
                            {
                                if (field.CmsField.TemplateField.FieldType == "Datetime" || field.CmsField.TemplateField.FieldType == "Date")
                                {
                                    ItemsRepository.MapDateTime(cmsItem, field.CmsField);
                                }
                                else
                                {
                                    if (fields.Count() > 1)
                                    {
                                        field.CmsField.Value = string.Join("\r\n", fields.Select(f => f.CmsField.Value.ToString()));
                                    }

                                    ItemsRepository.MapText(cmsItem, field.CmsField);
                                }
                            }
                            break;
                    }
                }
            }
        }

        private bool CheckFieldError(TemplateMapping templateMapping, List<Element> cwbFields, List<File> files, ItemResultModel itemResponseModel)
        {
            bool fieldError = false;

            var groupedFields = templateMapping.FieldMappings.GroupBy(i => i.CmsField);

            foreach (var grouping in groupedFields)
            {
                CmsField cmsField = grouping.Key;

                var cwbFieldIds = grouping.Select(i => i.CwbField.Id);
                var cwbFieldsToMap = grouping.Select(i => i.CwbField);

                IEnumerable<Element> cwbFieldsForMapping = cwbFields.Where(i => cwbFieldIds.Contains(i.Name)).ToList();

                var cwbField = cwbFieldsForMapping.FirstOrDefault();

                if (cwbField != null)
                {
                    var value = GetValue(cwbFieldsForMapping);
                    var options = GetOptions(cwbFieldsForMapping);

                    cmsField.Files = files.Where(x => x.FieldId == cwbField.Name).ToList();
                    cmsField.Value = value;
                    cmsField.Options = options;

                    //update CWB fields' type
                    foreach (var field in cwbFieldsToMap)
                    {
                        field.Type = cwbField.Type;
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

            return fieldError;
        }

        public List<ItemResultModel> ImportItems(string itemId, List<ImportItemModel> items, string projectId, string statusId, string language)
        {
            return Import(itemId, items, projectId, statusId, language);
        }

        private Dictionary<string, List<ItemEntity>> GetItemsMap(string projectId, IEnumerable<string> cwbItemIds)
        {
            List<ItemEntity> items = new List<ItemEntity>();
            Dictionary<string, List<ItemEntity>> paths = new Dictionary<string, List<ItemEntity>>();
            var account = GetAccount();

            if (account != null)
            {
                var project = ProjectsService.GetProjects(account.Id).Data.FirstOrDefault(p => p.Active && p.Id.ToString() == projectId);

                if (project != null)
                {
                    foreach (var cwbItemId in cwbItemIds)
                    {
                        string itemInPathId = cwbItemId;
                        ItemEntity item = null;
                        List<ItemEntity> path = new List<ItemEntity>();
                        while (true)
                        {
                            if (items.All(x => x.Data.Id.ToString() != itemInPathId)) //if we've not requested it yet
                            {
                                var cwbItem = ItemsService.GetSingleItem(itemInPathId);
                                if (cwbItem != null)
                                {
                                    item = cwbItem;
                                    items.Add(item);
                                }
                            }
                            else
                            {
                                item = items.First(x => x.Data.Id.ToString() == itemInPathId);
                            }

                            if (item != null)
                            {
                                path.Add(item);
                                if (item.Data.ParentId != 0)
                                {
                                    itemInPathId = item.Data.ParentId.ToString();
                                    continue;
                                }
                            }
                            break;
                        }
                        path.Reverse();
                        paths.Add(cwbItemId, path);
                    }

                    return paths;
                }
            }

            return null;
        }

        public List<ItemResultModel> ImportItemsWithLocation(List<LocationImportItemModel> items, string projectId, string statusId, string language)
        {
            var importItems = new List<ImportItemModel>();

            if (items == null)
            {
                return null;
            }

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

            var result = new List<ItemResultModel>();
            var groupByLocation = importItems.GroupBy(x => x.DefaultLocation);

            foreach (var locationGroup in groupByLocation)
            {
                var importedItems = ImportItems(locationGroup.Key, locationGroup.ToList(), projectId, statusId, language);
                result.AddRange(importedItems);
            }

            return result;
        }

        public List<MappingResultModel> MapItems(List<CWBItem> items)
        {
            var templates = MappingRepository.GetMappings();
            List<MappingResultModel> result = TryMapItems(items, templates);

            return result;
        }

        public List<MappingResultModel> MapItems(List<ImportItemModel> items)
        {
            var result = new List<MappingResultModel>();
            var templatesDictionary = new Dictionary<int, CWBTemplate>();

            foreach (var importItem in items)
            {
                var cwbItem = ItemsService.GetSingleItem(importItem.Id);

                if (cwbItem != null && cwbItem.Data != null && cwbItem.Data.TemplateId != null)
                {
                    CWBTemplate cwbTemplate = GetTemplate(cwbItem.Data.TemplateId.Value, templatesDictionary);

                    MappingResultModel cmsItem;
                    TryMapItem(cwbItem.Data, cwbTemplate, importItem.SelectedMappingId, out cmsItem, importItem.DefaultLocation);
                    result.Add(cmsItem);
                }
            }

            return result;
        }

        protected List<Project> GetProjectsWithData(int accountId)
        {
            string cacheKey = "ProjectsWithData_" + accountId;
            List<Project> activeProjects = HttpContext.Current.Cache[cacheKey] as List<Project>;
            //List<Project> activeProjects = null;
            if (activeProjects == null)
            {
                var projects = ProjectsService.GetProjects(accountId);
                activeProjects = projects.Data.Where(p => p.Active).ToList();

                //to debug
                //var activeProjects2 = new List<Project>();
                //foreach(var p in activeProjects)
                //{
                //    List<CWBTemplate> templates = GetTemplates(p.Id);

                //    List<CWBItem> items = GetItems(p.Id);
                //    var mappedItems = MapImportItems(items, templates);

                //    mappedItems = mappedItems.Where(item => item.AvailableMappings.Mappings.Any()).ToList();

                //    if (mappedItems.Any())
                //        activeProjects2.Add(p);
                //}

                //activeProjects = activeProjects2;
                activeProjects = activeProjects.Where(p =>
                    {
                        List<CWBTemplate> templates = GetTemplates(p.Id);

                        List<CWBItem> items = GetItems(p.Id);
                        var mappedItems = MapImportItems(items, templates);

                        mappedItems = mappedItems.Where(item => item.AvailableMappings.Mappings.Any()).ToList();

                        return mappedItems.Any();
                    }
                ).ToList();

                if (activeProjects.Count > 0)
                {
                    HttpContext.Current.Cache.Insert(cacheKey, activeProjects, null, DateTime.Now.AddMinutes(15), TimeSpan.Zero);
                }
            }

            return activeProjects;
        }

        private List<MappingResultModel> TryMapItems(List<CWBItem> items, List<TemplateMapping> templates)
        {
            var result = new List<MappingResultModel>();
            var templatesDictionary = new Dictionary<int, CWBTemplate>();

            foreach (CWBItem cwbItem in items)
            {
                CWBTemplate cwbTemplate = GetTemplate(cwbItem.TemplateId.Value, templatesDictionary);

                MappingResultModel cmsItem;
                TryMapItem(cwbItem, cwbTemplate, templates, out cmsItem);
                result.Add(cmsItem);
            }

            return result;
        }

        private CWBTemplate GetTemplate(int templateId, Dictionary<int, CWBTemplate> templatesDictionary)
        {
            CWBTemplate cwbTemplate;
            templatesDictionary.TryGetValue(templateId, out cwbTemplate);
            if (cwbTemplate == null)
            {
                cwbTemplate = TemplatesService.GetSingleTemplate(templateId.ToString()).Data;
                templatesDictionary.Add(templateId, cwbTemplate);
            }

            return cwbTemplate;
        }

        private void TryMapItem(CWBItem cwbItem, CWBTemplate cwbTemplate, string selectedMappingId, out MappingResultModel result, string selectedLocationId = null)
        {
            bool isUpdate = cwbItem is UpdateCWBItem;

            List<Element> cwbFields = cwbItem.Config.SelectMany(i => i.Elements).ToList();
            var template = MappingRepository.GetMappingById(selectedMappingId);

            if (template == null)
            {
                string errorMessage = isUpdate ? "Update failed: Template not mapped" : "Import failed: Template not mapped";
                result = new MappingResultModel(cwbItem, null, cwbTemplate.Name, null, string.Empty, errorMessage, false, selectedLocationId);
                return;
            }

            List<ImportCMSField> fields;

            IEnumerable<IGrouping<string, FieldMapping>> groupedFields = template.FieldMappings.GroupBy(i => i.CmsField.TemplateField.FieldId);

            var files = new List<File>();
            if (cwbItem.Config.SelectMany(config => config.Elements).Select(element => element.Type).Contains("files"))
            {
                foreach (var file in ItemsService.GetItemFiles(cwbItem.Id.ToString()).Data)
                {
                    files.Add(new File
                    {
                        FileName = file.FileName,
                        Url = file.Url,

                        UpdatedDate = file.Updated
                    });
                }
            }

            TryMapItemState mapState = TryMapFields(cwbFields, groupedFields, files, out fields);
            if (mapState == TryMapItemState.FieldError)
            {
                string errorMessage = isUpdate ? "Update failed: Template fields mismatch" : "Import failed: Template fields mismatch";
                result = new MappingResultModel(cwbItem, null, cwbTemplate.Name, null, string.Empty, errorMessage, false, selectedLocationId);
                return;
            }

            string cmsId = string.Empty;
            string message = "Import Successful";
            if (isUpdate)
            {
                cmsId = (cwbItem as UpdateCWBItem).CMSId;
                message = "Update Successful";
            }

            result = new MappingResultModel(cwbItem, fields, cwbTemplate.Name, template.CmsTemplate.TemplateId, cmsId, message, true, selectedLocationId);
        }

        private void TryMapItem(CWBItem item, CWBTemplate cwbTemplate, List<TemplateMapping> templates, out MappingResultModel result)
        {
            bool isUpdate = item is UpdateCWBItem;

            List<Element> cwbFields = item.Config.SelectMany(i => i.Elements).ToList();

            TemplateMapping template;
            TryMapItemState templateMapState = TryGetTemplate(templates, item.TemplateId.ToString(), out template);

            if (templateMapState == TryMapItemState.TemplateError)
            {
                string errorMessage = isUpdate ? "Update failed: Template not mapped" : "Import failed: Template not mapped";
                result = new MappingResultModel(item, null, cwbTemplate.Name, null, string.Empty, errorMessage, false);
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

            TryMapItemState mapState = TryMapFields(cwbFields, groupedFields, files, out fields);
            if (mapState == TryMapItemState.FieldError)
            {
                string errorMessage = isUpdate ? "Update failed: Template fields mismatch" : "Import failed: Template fields mismatch";
                result = new MappingResultModel(item, null, cwbTemplate.Name, null, string.Empty, errorMessage, false);
                return;
            }

            string cmsId = string.Empty;
            string message = "Import Successful";
            if (isUpdate)
            {
                cmsId = (item as UpdateCWBItem).CMSId;
                message = "Update Successful";
            }

            result = new MappingResultModel(item, fields, cwbTemplate.Name, template.CmsTemplate.TemplateId, cmsId, message);
        }

        private TryMapItemState TryMapField(List<Element> cwbFields, IGrouping<string, FieldMapping> fieldsMappig, List<File> files, out ImportCMSField importCMSField)
        {
            var cmsFieldName = fieldsMappig.Key;

            var cwbFieldsForMapping = GetFieldsForMapping(fieldsMappig, cwbFields);


            var field = cwbFieldsForMapping.FirstOrDefault();

            if (field == null)
            {
                importCMSField = new ImportCMSField(string.Empty, cmsFieldName, null, string.Empty, null, null);
                return TryMapItemState.FieldError;
            }


            if (IsMappedFieldsHaveDifrentTypes(cwbFieldsForMapping))
            {
                importCMSField = new ImportCMSField(string.Empty, cmsFieldName, field.Label, string.Empty, null, null);
                return TryMapItemState.FieldError;
            }

            var value = GetValue(cwbFieldsForMapping);
            var options = GetOptions(cwbFieldsForMapping);
            //files = files.Where(item => item.FieldId == field.Name).ToList();
            //TODO: used new List<Option>() here to build; will be removed soon
            importCMSField = new ImportCMSField(field.Type, cmsFieldName, field.Label, value.ToString(), new List<Option>(), files);

            return TryMapItemState.Success;
        }

        private TryMapItemState TryGetTemplate(List<TemplateMapping> templates, string templateId, out TemplateMapping result)
        {
            if (templates == null)
            {
                result = null;
                return TryMapItemState.TemplateError;
            }

            result = templates.FirstOrDefault(i => templateId == i.CwbTemplate.CwbTemplateId);
            if (result == null)
            {
                return TryMapItemState.TemplateError;
            }

            return TryMapItemState.Success;
        }

        private TryMapItemState TryMapFields(List<Element> cwbFields, IEnumerable<IGrouping<string, FieldMapping>> fieldsMappig, List<File> files, out List<ImportCMSField> result)
        {
            result = new List<ImportCMSField>();
            foreach (IGrouping<string, FieldMapping> grouping in fieldsMappig)
            {
                ImportCMSField cmsField;
                TryMapItemState mapState = TryMapField(cwbFields, grouping, files, out cmsField);
                if (mapState == TryMapItemState.FieldError)
                {
                    return mapState;
                }

                result.Add(cmsField);
            }

            return TryMapItemState.Success;
        }

        private object GetValue(IEnumerable<Element> fields)
        {
            string value = string.Join("", fields.Select(i => i.Value));
            return value;
        }

        private Dictionary<string, string> GetOptions(IEnumerable<Element> fields)
        {
            var result = new Dictionary<string, string>();
            foreach (Element field in fields)
            {
                if (field.Options != null)
                {
                    foreach (var option in field.Options.Where(x => x.Selected))
                    {
                        result.Add(option.Name, option.Label);
                    }
                }
            }
            return result;
        }

        private bool IsMappedFieldsHaveDifrentTypes(List<Element> fields)
        {
            return fields.Select(i => i.Type).Distinct().Count() > 1;
        }

        private List<Element> GetFieldsForMapping(IGrouping<string, FieldMapping> fieldsMappig, List<Element> cwbFields)
        {
            IEnumerable<string> cwbFiledNames = fieldsMappig.Select(i => i.CwbField.Id);
            IEnumerable<Element> cwbFieldsForMapping = cwbFields.Where(i => cwbFiledNames.Contains(i.Name));

            return cwbFieldsForMapping.ToList();
        }

        private Project GetProject(List<Project> projects, string projectIdStr)
        {
            int projectId;
            int.TryParse(projectIdStr, out projectId);

            Project project = projectId != 0 ? projects.FirstOrDefault(i => i.Id == projectId) : projects.FirstOrDefault();

            return project;
        }

        private List<CWBStatus> GetStatuses(int projectId)
        {
            StatusesEntity statuses = ProjectsService.GetAllStatuses(projectId.ToString());
            return statuses.Data;
        }

        private List<CWBItem> GetItems(int projectId)
        {
            ItemsEntity items = ItemsService.GetItems(projectId.ToString());
            return items.Data;
        }

        private List<CWBTemplate> GetTemplates(int projectId)
        {
            return GetTemplates(projectId.ToString());
        }

        private List<CWBTemplate> GetTemplates(string projectId)
        {
            TemplatesEntity templates = TemplatesService.GetTemplates(projectId);
            return templates.Data;
        }

        private string GetBreadcrumb(CWBItem item, List<CWBItem> items)
        {
            var names = new List<string>();
            string result = BuildBreadcrumb(item, items, names);
            return result;
        }

        private string BuildBreadcrumb(CWBItem item, List<CWBItem> items, List<string> names)
        {
            names.Add(item.Name);

            if (item.ParentId != 0)
            {
                CWBItem next = items.FirstOrDefault(i => i.Id == item.ParentId);
                return BuildBreadcrumb(next, items, names);
            }

            names.Reverse();

            string url = string.Join("/", names);

            return string.Format("/{0}", url);
        }

        private List<ItemModel> MapImportItems(List<CWBItem> items, List<CWBTemplate> templates)
        {
            var model = new List<ItemModel>();
            var mappedItems = items.Where(i => i.TemplateId != null).ToList();
            var dateFormat = CwbAccountSettings.DateFormat;
            if (string.IsNullOrEmpty(dateFormat))
            {
                dateFormat = SitecoreRepositories.Repositories.Constants.DateFormat;
            }

            foreach (var mappedItem in mappedItems)
            {
                var template = templates.FirstOrDefault(templ => templ.Id == mappedItem.TemplateId);
                var mappings = MappingRepository.GetMappingsByCwbTemplateId(mappedItem.TemplateId.ToString());
                var availableMappings = mappings.Select(availableMappingModel => new AvailableMapping
                {
                    Id = availableMappingModel.MappingId,
                    Title = availableMappingModel.MappingTitle,
                    DefaultLocationId = availableMappingModel.DefaultLocationId,
                    DefaultLocationTitle = availableMappingModel.DefaultLocationTitle,
                    CmsTemplateName = availableMappingModel.CmsTemplate.TemplateName

                }).ToList();

                model.Add(new ItemModel
                {
                    CwbItem = new CwbItemModel
                    {
                        Id = mappedItem.Id.ToString(),
                        Title = mappedItem.Name,
                        LastUpdatedInCwb = TimeZoneInfo.ConvertTime(mappedItem.Updated.Date, TimeZoneInfo.Utc, TimeZoneInfo.Local).ToString(dateFormat)
                    },
                    CwbTemplate = new CwbTemplateModel
                    {
                        Name = template != null ? template.Name : "",
                        Id = template != null ? template.Id.ToString() : ""
                    },
                    Status = new CwbStatusModel
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

        private CwbStatusModel PostNewItemStatus(string cwbItemId, string statusId, string projectId)
        {
            ItemsService.ChooseStatusForItem(cwbItemId, statusId);
            var status = ProjectsService.GetSingleStatus(statusId, projectId);
            var statusModel = new CwbStatusModel { Color = status.Data.Color, Name = status.Data.Name };
            return statusModel;
        }
    }
}
