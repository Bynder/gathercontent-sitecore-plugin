namespace Bynder.Content.SitecoreConnector.SitecoreRepositories.Repositories
{
    using Sitecore;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Linq;
    using Sitecore.ContentSearch.SearchTypes;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.Resources.Media;
    using Sitecore.SecurityModel;
    using Sitecore.Web.UI.HtmlControls.Data;
    
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Core.DependencyInjection;
    using Core.Interfaces;
    using Core.Models.Import;

    using Sitecore.Links.UrlBuilders;
    using Sitecore.Pipelines.HasPresentation;

    [Service(typeof(IItemsRepository))]
    public class ItemsRepository : BaseSitecoreRepository, IItemsRepository
    {
        private const string CwbContentId = "CWB Content Id";
        private const string LastSyncDate = "Last Sync Date";
        private const string CwbPath = "CWBPath";
        private const string MappingId = "MappingId";
        private const string IndexName = "sitecore_master_index";
        
        private readonly IAccountsRepository accountsRepository;
        private readonly IMediaRepository<Item> mediaRepository;

        private readonly Dictionary<int, string> linkedUrlsCache = new Dictionary<int, string>();

        public ItemsRepository(IAccountsRepository accountsRepository, IMediaRepository<Item> mediaRepository)
        {
            this.mediaRepository = mediaRepository;
            this.accountsRepository = accountsRepository;
        }

        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public IList<CmsItem> GetItems(string parentId, string language)
        {
            var parentItem = GetItem(parentId);
            var templatId = new ID(Constants.CwbLinkItemTemplateID);
            var items = new List<Item>();
            if (IsItemHasTemplate(templatId, parentItem))
            {
                items.Add(parentItem);
            }
            items.AddRange(parentItem.Axes.GetDescendants().Where(i => IsItemHasTemplate(templatId, i)).ToList());

            var result = new List<CmsItem>();

            foreach (var item in items)
            {
                var cmsItem = GetItem(item.ID.ToString(), item.Language.ToString());
                result.Add(cmsItem);
            }

            return result;
        }

        /// <summary>
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="language"></param>
        /// <param name="readAllFields"></param>
        /// <returns></returns>
        public CmsItem GetItem(string itemId, string language, bool readAllFields = false)
        {
            var item = ContextDatabase.GetItem(itemId);
            if (item != null)
            {
                var cmsItem = new CmsItem
                {
                    Id = item.ID.ToString(),
                    Title = item.Name,
                    Language = item.Language.ToString(),
                };

                cmsItem.Fields.Add(new CmsField
                {
                    TemplateField = new CmsTemplateField { FieldName = "CWB Content Id" },
                    Value = item[CwbContentId]
                });

                cmsItem.Fields.Add(new CmsField
                {
                    TemplateField = new CmsTemplateField { FieldName = "Last Sync Date" },
                    Value = DateUtil.IsoDateToDateTime(item[LastSyncDate])
                });

                cmsItem.Fields.Add(new CmsField
                {
                    TemplateField = new CmsTemplateField { FieldName = "Template" },
                    Value = item.TemplateName
                });


                if (readAllFields)
                {
                    //cmsItem.Fields  
                }
                return cmsItem;
            }
            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <param name="mappingId"></param>
        /// <param name="cwbPath"></param>
        public string CreateMappedItem(string parentId, CmsItem cmsItem, string mappingId, string cwbPath)
        {
            if (parentId != null)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(cmsItem.Language))
                    {
                        using (new EnforceVersionPresenceDisabler())
                        {
                            var template = ContextDatabase.GetTemplate(new ID(cmsItem.Template.TemplateId));
                            var validName = ItemUtil.ProposeValidItemName(cmsItem.Title);
                            var parent = ContextDatabase.GetItem(new ID(parentId));
                            if (parent != null)
                            {
                                var createdItem = parent.Add(validName, template);
                                
                                try
                                {
                                    EnsureMetaTemplateInherited(createdItem.Template);
                                    var idField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == CwbContentId);
                                    if (idField != null)
                                    {
                                        createdItem.Editing.BeginEdit();
                                        createdItem.Fields[CwbContentId].Value = idField.Value.ToString();
                                        var isoDate = DateUtil.ToIsoDate(DateTime.UtcNow);
                                        createdItem.Fields[LastSyncDate].Value = isoDate;
                                        createdItem.Fields[MappingId].Value = mappingId;
                                        createdItem.Fields[CwbPath].Value = cwbPath;
                                        createdItem.Editing.EndEdit();
                                    }
                                    return createdItem.ID.ToString();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error("cannot create mapped item.", ex, this);
                                    throw new Exception(string.Format("Your template({0}) is not inherited from the CWB Linked Item.", createdItem.TemplateName));
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <returns></returns>
        public string CreateNotMappedItem(string parentId, CmsItem cmsItem)
        {
            if (parentId != null)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(cmsItem.Language))
                    {
                        var templateItem = ContextDatabase.GetItem("/sitecore/templates/GatherContent/Not Mapped Item");
                        if (templateItem != null)
                        {
                            var template = ContextDatabase.GetTemplate(new ID(templateItem.ID.Guid));
                            var validName = ItemUtil.ProposeValidItemName(cmsItem.Title);
                            var parent = ContextDatabase.GetItem(new ID(parentId));
                            if (parent != null)
                            {
                                try
                                {
                                    var createdItem = parent.Add(validName, template);
                                    return createdItem.ID.ToString();
                                }
                                catch (Exception)
                                {
                                    throw new Exception(string.Format("Item cannot be created: parent='{0}l cmsItem.Title='{1}'", parent.Name, cmsItem.Title));
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cmsField"></param>
        public void ResolveAttachmentMapping(CmsItem item, CmsField cmsField)
        {
            switch (cmsField.TemplateField.FieldType)
            {
                case "Droptree":
                    MapDropTree(item, cmsField);
                    break;
                case "Image":
                case "Advance Image":
                    MapImage(item, cmsField);
                    break;
                case "File":
                    MapFile(item, cmsField);
                    break;
                case "General Link":
                    MapLink(item, cmsField);
                    break;
                default:
                    MapChoice(item, cmsField, null);
                    break;
            }
        }


        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cmsField"></param>
        public void MapText(CmsItem item, CmsField cmsField)
        {
            Item createdItem = GetItem(item.Id, Sitecore.Data.Managers.LanguageManager.GetLanguage(item.Language));
            if (createdItem == null)
            {
                return;
            }

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(item.Language))
                {
                    createdItem.Editing.BeginEdit();

                    string value;
                    switch (cmsField.TemplateField.FieldType)
                    {
                        case "Rich Text":
                            value = cmsField.Value.ToString();
                            break;
                        case "General Link":
                            {
                                value = GeneralLinkExternal(StringUtil.RemoveTags(cmsField.Value.ToString()).Trim());
                            }
                            break;
                        default:
                            value = StringUtil.RemoveTags(cmsField.Value.ToString()).Trim();
                            break;
                    }

                    createdItem[cmsField.TemplateField.FieldName] = value;

                    createdItem.Editing.EndEdit();
                }
            }
        }

        private static string GeneralLinkExternal(string url, string description = "")
        {
            return string.Format("<link text=\"{0}\" linktype=\"external\" url=\"{1}\" anchor=\"\" target=\"\" />",
                description, url);
        }

        private static string GeneralLinkInternal(ID contentId, string description = "")
        {
            return string.Format("<link text=\"{0}\" linktype=\"internal\" class=\"\" title=\"\" target='Active Browser' querystring=\"\" id=\"{1}\" />",
                description, contentId);
        }

        private static string GeneralLinkMedia(ID mediaId, string description = "")
        {
            return string.Format("<link text=\"{0}\" linktype=\"media\" target=\"\" id=\"{1}\" />",
                description, mediaId);
        }


        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cmsField"></param>
        public void MapChoice(CmsItem item, CmsField cmsField, List<Item> finalListOptions)
        {
            Item createdItem = GetItem(item.Id, Sitecore.Data.Managers.LanguageManager.GetLanguage(item.Language));

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(item.Language))
                {
                    createdItem.Editing.BeginEdit();
                    if (cmsField.Files != null && cmsField.Files.Any())
                    {
                        var value = string.Empty;
                        foreach (var file in cmsField.Files)
                        {
                            if (file != null)
                            {
                                // var media = UploadFile(path, file);
                                var path = mediaRepository.ResolveMediaPath(item, createdItem, cmsField);
                                Item media = mediaRepository.UploadFile(path, file);
                                if (media != null)
                                {
                                    value += media.ID + "|";
                                }
                            }
                        }
                        value = value.TrimEnd('|');
                        if (!string.IsNullOrEmpty(value))
                        {
                            createdItem[cmsField.TemplateField.FieldName] = value;
                        }
                    }
                    else if (cmsField.Options != null && cmsField.TemplateField.FieldType == "TreelistEx")
                    {
                        //Add new value for option type
                        var values = createdItem[cmsField.TemplateField.FieldName].Split('|').ToList();

                        foreach (var option in cmsField.Options)
                        {
                            var children = GetDatasource(createdItem, cmsField.TemplateField.FieldId, option);
                            //option = CWB option.Label
                            if (children != null && values.All(x => x != children.ID.ToString()))
                            {
                                values.Add(children.ID.ToString());
                            }
                        }
                        if (values.Count != 0)
                        {
                            createdItem[cmsField.TemplateField.FieldName] = string.Join("|", values.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct());
                        }
                    }
                    else if (cmsField.Options != null && cmsField.Options.Any())
                    {
                        var value = string.Empty;

                        foreach (var option in cmsField.Options)
                        {
                            var children = GetDatasourceOfChoise(option, finalListOptions);
                            //option = CWB option.Label
                            if (children != null)
                            {
                                value += children.ID + "|";
                            }
                        }
                        value = value.TrimEnd('|');
                        if (!string.IsNullOrEmpty(value))
                        {
                            createdItem[cmsField.TemplateField.FieldName] = value;
                        }
                    }
                    else
                    {
                        createdItem[cmsField.TemplateField.FieldName] = string.Empty;
                    }

                    createdItem.Editing.EndEdit();
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cmsField"></param>
        public void MapLink(CmsItem item, CmsField cmsField)
        {
            Item createdItem = GetItem(item.Id, Sitecore.Data.Managers.LanguageManager.GetLanguage(item.Language));

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(item.Language))
                {
                    createdItem.Editing.BeginEdit();

                    if (cmsField.Files != null && cmsField.Files.Any())
                    {
                        var path = mediaRepository.ResolveMediaPath(item, createdItem, cmsField);
                        for (int i = 0; i < cmsField.Files.Count; i++)
                        {
                            Item media = mediaRepository.UploadFile(path, cmsField.Files[i]);

                            if (i == 0)
                            {
                                var value = string.Format("<link linktype=\"media\" id=\"{0}\" />", media.ID);

                                createdItem[cmsField.TemplateField.FieldName] = value;
                            }
                        }
                    }
                    else
                    {
                        createdItem[cmsField.TemplateField.FieldName] = string.Empty;
                    }

                    createdItem.Editing.EndEdit();
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cmsField"></param>
        public void MapFile(CmsItem item, CmsField cmsField)
        {
            Item createdItem = GetItem(item.Id, Sitecore.Data.Managers.LanguageManager.GetLanguage(item.Language));

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(item.Language))
                {
                    if (cmsField.TemplateField != null)
                    {
                        var file = cmsField.Files.FirstOrDefault();
                        if (file != null)
                        {
                            // var media = UploadFile(path, file);
                            var path = mediaRepository.ResolveMediaPath(item, createdItem, cmsField);
                            Item media = mediaRepository.UploadFile(path, file);

                            var mediaUrl = MediaManager.GetMediaUrl(media, new MediaUrlBuilderOptions { UseItemPath = false, AbsolutePath = false });
                            var value = string.Format("<file mediaid=\"{0}\" src=\"{1}\" />", media.ID, mediaUrl);

                            createdItem.Editing.BeginEdit();
                            createdItem[cmsField.TemplateField.FieldName] = value;
                            createdItem.Editing.EndEdit();
                        }
                    }
                }
            }
        }

        public void MapImage(CmsItem item, CmsField cmsField)
        {
            Item createdItem = GetItem(item.Id, Sitecore.Data.Managers.LanguageManager.GetLanguage(item.Language));

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(item.Language))
                {
                    if (cmsField.TemplateField != null)
                    {
                        if (cmsField.Files.Count != 0)
                        {
                            var path = mediaRepository.ResolveMediaPath(item, createdItem, cmsField);
                            for (int i = 0; i < cmsField.Files.Count; i++)
                            {
                                Item media = mediaRepository.UploadFile(path, cmsField.Files[i]);

                                if (i == 0)
                                {
                                    var value = string.Format("<image mediaid=\"{0}\"  />", media.ID);

                                    createdItem.Editing.BeginEdit();
                                    createdItem[cmsField.TemplateField.FieldName] = value;
                                    createdItem.Editing.EndEdit();
                                }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(createdItem[cmsField.TemplateField.FieldName]))
                            {
                                createdItem.Editing.BeginEdit();
                                createdItem[cmsField.TemplateField.FieldName] = string.Empty;
                                createdItem.Editing.EndEdit();
                            }
                        }
                    }
                }
            }
        }

        public void MapDropTree(CmsItem item, CmsField cmsField)
        {
            Item createdItem = GetItem(item.Id, Sitecore.Data.Managers.LanguageManager.GetLanguage(item.Language));
            var path = mediaRepository.ResolveMediaPath(item, createdItem, cmsField);

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(item.Language))
                {
                    if (cmsField.TemplateField != null)
                    {
                        var file = cmsField.Files.FirstOrDefault();
                        if (file != null)
                        {
                            Item media = mediaRepository.UploadFile(path, file);

                            createdItem.Editing.BeginEdit();
                            createdItem[cmsField.TemplateField.FieldName] = media.ID.ToString();
                            createdItem.Editing.EndEdit();
                        }
                    }
                }
            }
        }

        public void MapDateTime(CmsItem item, CmsField cmsField)
        {
            Item createdItem = GetItem(item.Id, Sitecore.Data.Managers.LanguageManager.GetLanguage(item.Language));
            if (createdItem == null)
            {
                return;
            }

            string stringValue = StringUtil.RemoveTags(cmsField.Value.ToString()).Trim();
            string format = accountsRepository.GetAccountSettings().DateTimeParseFormat;

            if (string.IsNullOrWhiteSpace(format))
            {
                format = Constants.DateParseFormat;
            }

            DateTime dateTimeValue;

            bool parseSuccessfull = DateTime.TryParseExact(stringValue, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateTimeValue);

            if (!parseSuccessfull)
            {
                return;
            }

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(item.Language))
                {
                    createdItem.Editing.BeginEdit();

                    createdItem[cmsField.TemplateField.FieldName] = DateUtil.ToIsoDate(dateTimeValue);

                    createdItem.Editing.EndEdit();
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <param name="mappingId"></param>
        /// <param name="cwbPath"></param>
        /// <returns></returns>
        public bool IfMappedItemExists(string parentId, CmsItem cmsItem, string mappingId, string cwbPath)
        {
            if (parentId != null)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(cmsItem.Language))
                    {
                        var validName = ItemUtil.ProposeValidItemName(cmsItem.Title);
                        var parent = ContextDatabase.GetItem(new ID(parentId));
                        if (parent != null)
                        {
                            var items = parent.Axes.SelectItems(string.Format(".//*[@@name='{0}']", validName));
                            if (items != null && items.Any(item => item["CWBPath"] == cwbPath))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <param name="mappingId"></param>
        /// <param name="cwbPath"></param>
        /// <param name="cwbId"></param>
        /// <returns></returns>
        public bool IfMappedItemExists(string parentId, CmsItem cmsItem, string mappingId, string cwbPath, string cwbId)
        {
            if (parentId != null)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(cmsItem.Language))
                    {
                        var parent = ContextDatabase.GetItem(new ID(parentId));
                        if (parent != null)
                        {
                            var items = parent.Axes.SelectItems(string.Format(".//*[@CWB Content Id='{0}']", cwbId));
                            if (items != null)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <returns></returns>
        public bool IfMappedItemExists(string parentId, CmsItem cmsItem)
        {
            if (parentId != null)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(cmsItem.Language))
                    {
                        var validName = ItemUtil.ProposeValidItemName(cmsItem.Title);
                        var parent = ContextDatabase.GetItem(new ID(parentId));
                        if (parent != null)
                        {
                            var items = parent.Axes.SelectItems(string.Format(".//*[@@name='{0}' and @CWBPath!='']", validName));
                            if (items != null && items.Any())
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <returns></returns>
        public bool IfNotMappedItemExists(string parentId, CmsItem cmsItem)
        {
            if (parentId != null)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(cmsItem.Language))
                    {
                        var validName = ItemUtil.ProposeValidItemName(cmsItem.Title);
                        var parent = ContextDatabase.GetItem(new ID(parentId));
                        if (parent != null)
                        {
                            var items = parent.Axes.SelectItems(string.Format(".//*[@@name='{0}']", validName));
                            if (items != null && items.Any())
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <param name="mappingId"></param>
        /// <param name="cwbPath"></param>
        /// <param name="cwbId"></param>
        public string AddNewVersion(string parentId, CmsItem cmsItem, string mappingId, string cwbPath, string cwbId)
        {
            if (parentId == null)
            {
                return null;
            }

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(cmsItem.Language))
                {
                    var parent = ContextDatabase.GetItem(new ID(parentId));
                    if (parent == null)
                    {
                        return null;
                    }

                    var items = parent.Axes.SelectItems(string.Format(".//*[@CWB Content Id='{0}']", cwbId));
                    if (items == null)
                    {
                        return null;
                    }

                    //items = items.Where(item => item["CWBPath"] == cwbPath).ToArray();

                    foreach (var item in items)
                    {
                        var newVersion = item.Versions.AddVersion();

                        try
                        {
                            EnsureMetaTemplateInherited(newVersion.Template);
                            var idField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == CwbContentId);
                            if (idField != null)
                            {
                                newVersion.Editing.BeginEdit();

                                newVersion.Fields[CwbContentId].Value = idField.Value.ToString();
                                var isoDate = DateUtil.ToIsoDate(DateTime.UtcNow);
                                newVersion.Fields[LastSyncDate].Value = isoDate;
                                newVersion.Fields[MappingId].Value = mappingId;
                                newVersion.Fields[CwbPath].Value = cwbPath;

                                newVersion.Editing.EndEdit();
                            }
                            return newVersion.ID.ToString();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("cannot update mapped item.", ex, this);
                            throw new Exception(string.Format("Your template({0}) is not inherited from the CWB Linked Item.", newVersion.TemplateName));
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <param name="mappingId"></param>
        /// <param name="cwbPath"></param>
        public string AddNewVersion(string parentId, CmsItem cmsItem, string mappingId, string cwbPath)
        {
            if (parentId == null)
            {
                return null;
            }

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(cmsItem.Language))
                {
                    var validName = ItemUtil.ProposeValidItemName(cmsItem.Title);
                    var parent = ContextDatabase.GetItem(new ID(parentId));
                    if (parent == null)
                    {
                        return null;
                    }

                    var items = parent.Axes.SelectItems(string.Format(".//*[@@name='{0}']", validName));
                    if (items == null)
                    {
                        return null;
                    }

                    items = items.Where(item => item["CWBPath"] == cwbPath).ToArray();

                    foreach (var item in items)
                    {
                        var newVersion = item.Versions.AddVersion();

                        try
                        {
                            EnsureMetaTemplateInherited(newVersion.Template);
                            var idField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == CwbContentId);
                            if (idField != null)
                            {
                                newVersion.Editing.BeginEdit();

                                newVersion.Fields[CwbContentId].Value = idField.Value.ToString();
                                var isoDate = DateUtil.ToIsoDate(DateTime.UtcNow);
                                newVersion.Fields[LastSyncDate].Value = isoDate;
                                newVersion.Fields[MappingId].Value = mappingId;
                                newVersion.Fields[CwbPath].Value = cwbPath;

                                newVersion.Editing.EndEdit();
                            }
                            return newVersion.ID.ToString();
                        }
                        catch (Exception ex)
                        {
                            Log.Error("cannot update mapped item.", ex, this);
                            throw new Exception(string.Format("Your template({0}) is not inherited from the CWB Linked Item.", newVersion.TemplateName));
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// </summary>
        /// <param name="scheme"></param>
        /// <param name="host"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public string GetCmsItemLink(string scheme, string host, string itemId)
        {
            return string.Format("{2}://{0}/sitecore/shell/Applications/Content Editor?fo={1}&sc_content=master&sc_bw=1", host, itemId, scheme);
        }

        /// <summary>
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <returns></returns>
        public string GetItemId(string parentId, CmsItem cmsItem)
        {
            if (parentId == null)
            {
                return null;
            }

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(cmsItem.Language))
                {
                    var validName = ItemUtil.ProposeValidItemName(cmsItem.Title);
                    var parent = ContextDatabase.GetItem(new ID(parentId));
                    if (parent != null)
                    {
                        var items = parent.Axes.SelectItems(string.Format(".//*[@@name='{0}']", validName));
                        if (items != null && items.Any())
                        {
                            return items.First().ID.ToString();
                        }
                    }
                }
            }

            return null;
        }

        public string GetLinkedItemUrl(int cwbId)
        {
            if (linkedUrlsCache.ContainsKey(cwbId))
            {
                return linkedUrlsCache[cwbId];
            }

            IEnumerable<Item> items = GetLinkedSitecoreItems(cwbId.ToString());

            Item linkedItem = items
                .OrderBy(i => i.Paths.FullPath)
                .FirstOrDefault(i => HasPresentationPipeline.Run(i));

            if (linkedItem != null)
            {
                string url = "~/link.aspx?_id=" + linkedItem.ID.Guid.ToString("N").ToUpper() + "&amp;_z=z";

                if (!string.IsNullOrEmpty(url))
                {
                    linkedUrlsCache[cwbId] = url;

                    return url;
                }
            }

            return null;
        }

        public void ExpandLinksInText(string cmsRootId, bool includeDescendants)
        {
            Item root = ContextDatabase.GetItem(ID.Parse(cmsRootId));

            if (root == null)
            {
                return;
            }

            var baseUrl = accountsRepository.GetAccountSettings().GatherContentUrl;
            string pattern = StringUtil.EnsurePostfix('/', baseUrl.Replace("/", "\\/")) + "item\\/";
            Regex rgx = new Regex(pattern + "(\\d+)");

            ExpandLinksInTextRecursive(root, rgx, pattern, includeDescendants);
        }

        private void ExpandLinksInTextRecursive(Item rootItem, Regex rgx, string pattern, bool includeDescendants)
        {
            rootItem.Fields.ReadAll();

            foreach (Field field in rootItem.Fields.Where(f => f.HasValue && f.TypeKey == "rich text"))
            {
                string fieldValue = field.Value;
                string newFieldValue = field.Value;
                foreach (Match match in rgx.Matches(fieldValue))
                {
                    string cwbId = match.Groups[1].Value;

                    string url = GetLinkedItemUrl(int.Parse(cwbId));

                    if (!string.IsNullOrEmpty(url))
                    {
                        newFieldValue = Regex.Replace(newFieldValue, pattern + cwbId, url);
                    }
                }

                if (newFieldValue != fieldValue)
                {
                    using (new SecurityDisabler())
                    {
                        rootItem.Editing.BeginEdit();
                        rootItem.Fields[field.ID].Value = newFieldValue;
                        rootItem.Editing.EndEdit();
                    }
                }
            }

            if (includeDescendants)
            {
                foreach (Item child in rootItem.GetChildren())
                {
                    ExpandLinksInTextRecursive(child, rgx, pattern, includeDescendants);
                }
            }
        }

        private List<Item> GetLinkedSitecoreItems(string cwbId)
        {
            var result = new List<Item>();

            ISearchIndex index = ContentSearchManager.Indexes.SingleOrDefault(i => i.Name.Equals(IndexName));

            if (index == null)
            {
                Log.Warn("Index " + IndexName + " not found!", this);
                return result;
            }

            using (IProviderSearchContext context = index.CreateSearchContext())
            {
                var query = context.GetQueryable<SearchResultItem>()
                    .Where(i => i["cwb_content_id"] == cwbId);

                query = query.Filter(i => i.Language == Context.Language.Name);

                var res = query.GetResults();
                if (res.TotalSearchResults == 0)
                {
                    return result;
                }

                result = res.Hits
                    .Select(h => h.Document.GetItem())
                    .Where(i => i != null)
                    .ToList();

                return result;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="updatedItem"></param>
        /// <param name="fieldId"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        private Item GetDatasource(Item updatedItem, string fieldId, KeyValuePair<string, string> option)
        {
            var dataSourcePath = GetDatasourcePath(updatedItem, fieldId);

            Item[] datasourceItems = null;
            if (updatedItem.Fields[new ID(fieldId)].TypeKey == "treelist" || updatedItem.Fields[new ID(fieldId)].TypeKey == "treelistex")
            {
                if (dataSourcePath.IndexOf("IncludeTemplatesForSelection", StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    var templatesStr = dataSourcePath.Substring(dataSourcePath.IndexOf("IncludeTemplatesForSelection",
                        StringComparison.InvariantCultureIgnoreCase));
                    templatesStr = templatesStr.Substring(templatesStr.IndexOf("=", StringComparison.Ordinal) + 1)
                        .Trim();
                    if (templatesStr.IndexOf("&", StringComparison.Ordinal) != -1)
                    {
                        templatesStr = templatesStr.Substring(0, templatesStr.IndexOf("&", StringComparison.Ordinal));
                    }
                    if (!string.IsNullOrWhiteSpace(templatesStr))
                    {
                        var startPath = dataSourcePath.Trim();
                        startPath = startPath
                            .Substring(startPath.IndexOf("=", StringComparison.InvariantCultureIgnoreCase) + 1).Trim();
                        startPath = startPath.Substring(0,
                            startPath.IndexOf("&", StringComparison.InvariantCultureIgnoreCase));
                        if (!string.IsNullOrWhiteSpace(startPath))
                        {
                            var startItem = updatedItem.Database.SelectSingleItem(startPath);
                            if (startItem != null)
                            {
                                var templates = templatesStr.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                                    .ToList();
                                datasourceItems = GetDescendantsByTemplateNamesWithFallback(startItem, templates)
                                    .ToArray();
                            }
                        }
                    }
                }
                else
                {
                    var startItem = updatedItem.Database.SelectSingleItem(dataSourcePath);
                    datasourceItems = startItem.Axes.GetDescendants();
                }
            }
            else
            {
                datasourceItems = LookupSources.GetItems(updatedItem, dataSourcePath);
            }

            if (datasourceItems == null)
            {
                return null;
            }

            var label = option.Value.Trim();
            if (label.Contains('&'))
            {
                label = System.Web.HttpUtility.HtmlDecode(label);
            }

            return datasourceItems.FirstOrDefault(c =>
                label.Equals(c.Name, StringComparison.InvariantCultureIgnoreCase) ||
                label.Equals(c.DisplayName, StringComparison.InvariantCultureIgnoreCase) ||
                option.Key.Equals(c.Fields["CWB Option Id"]?.Value, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// </summary>
        /// <param name="updatedItem"></param>
        /// <param name="fieldId"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        private Item GetDatasourceOfChoise(KeyValuePair<string, string> option, List<Item> finalListOptions)
        {
            var label = option.Value.Trim();
            if (label.Contains('&'))
            {
                label = System.Web.HttpUtility.HtmlDecode(label);
            }

            return finalListOptions.FirstOrDefault(c =>
                label.Equals(c.Name, StringComparison.InvariantCultureIgnoreCase) ||
                label.Equals(c.DisplayName, StringComparison.InvariantCultureIgnoreCase) ||
                option.Key.Equals(c.Fields["CWB Option Id"]?.Value, StringComparison.InvariantCultureIgnoreCase));
        }

        private static IEnumerable<Item> GetDescendantsByTemplateNamesWithFallback(Item rootItem, List<string> templateNames)
        {
            if (rootItem == null || !templateNames.Any())
            {
                return Enumerable.Empty<Item>();
            }
            // todo: checkEnsureFallbackVersion // return rootItem.EnsureFallbackVersion().Axes.GetDescendants().Where(i =>
            return rootItem.Axes.GetDescendants().Where(i =>
                templateNames.Any(tn => tn.Equals(i.TemplateName, StringComparison.InvariantCultureIgnoreCase)));
        }

        /// <summary>
        /// </summary>
        /// <param name="updatedItem"></param>
        /// <param name="fieldId"></param>
        /// <returns></returns>
        private string GetDatasourcePath(Item updatedItem, string fieldId)
        {
            var scField = updatedItem.Fields[new ID(fieldId)];
            var dataSourcePath = GetItem(scField.ID.ToString())["Source"];
            return dataSourcePath;
        }


        /// <summary>
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool IsItemHasTemplate(ID templateId, Item item)
        {
            return item.Template.BaseTemplates.Any(i => i.ID == templateId);
        }

        /// <summary>
        /// </summary>
        /// <param name="templateItem"></param>
        /// <returns></returns>
        private bool EnsureMetaTemplateInherited(TemplateItem templateItem)
        {
            if (templateItem != null)
            {
                bool cwbLinkTemplate = templateItem.BaseTemplates.Any(bt => bt.Name == Constants.CWBLinkItemTemplateName);
                string baseTemplates = templateItem.InnerItem[FieldIDs.BaseTemplate];

                if (!cwbLinkTemplate)
                {
                    using (new SecurityDisabler())
                    {
                        templateItem.InnerItem.Editing.BeginEdit();
                        templateItem.InnerItem[FieldIDs.BaseTemplate] = string.Format("{0}|{1}", baseTemplates, Constants.CwbLinkItemTemplateID);
                        templateItem.InnerItem.Editing.EndEdit();
                    }
                }
                return true;
            }
            return false;
        }
    }
}
