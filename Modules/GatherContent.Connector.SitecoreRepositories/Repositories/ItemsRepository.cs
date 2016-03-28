using System;
using System.IO;
using System.Net;
using GatherContent.Connector.IRepositories.Models.Import;
using Sitecore;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Interfaces;
using File = GatherContent.Connector.IRepositories.Models.Import.File;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    /// <summary>
    /// 
    /// </summary>
    public class ItemsRepository : BaseSitecoreRepository, IItemsRepository
    {
        private const string GC_CONTENT_ID = "GC Content Id";
        private const string LAST_SYNC_DATE = "Last Sync Date";
        private const string GC_PATH = "GCPath";
        private const string MAPPING_ID = "MappingId";

        protected IAccountsRepository AccountsRepository;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountsRepository"></param>
        public ItemsRepository(IAccountsRepository accountsRepository) : base()
        {
            AccountsRepository = accountsRepository;
        }

        
        #region Utilities


        /// <summary>
        /// 
        /// </summary>
        /// <param name="updatedItem"></param>
        /// <param name="fieldId"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        private Item GetDatasource(Item updatedItem, string fieldId, string label)
        {
            var dataSourcePath = GetDatasourcePath(updatedItem, fieldId);
            var dataSourceItem = GetItemByPath(dataSourcePath);
            if (dataSourceItem == null) return null;
            var children = dataSourceItem.GetChildren().InnerChildren.FirstOrDefault(c => c.Name.ToLower() == label.ToLower());
            return children;
        }

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        /// <param name="updatedItem"></param>
        /// <param name="fieldId"></param>
        /// <param name="path"></param>
        private void SetDatasourcePath(Item updatedItem, string fieldId, string path)
        {
            var scField = updatedItem.Fields[new ID(fieldId)];
            var scItem = GetItem(scField.ID.ToString());
            using (new SecurityDisabler())
            {
                scItem.Editing.BeginEdit();
                scItem["Source"] = path;
                scItem.Editing.EndEdit();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmsItem"></param>
        /// <param name="createdItem"></param>
        private void SetupFields(CmsItem cmsItem, Item createdItem)
        {
            foreach (var cmsField in cmsItem.Fields)
            {
                var path = GetMediaItemPath(cmsItem.Title, createdItem, cmsField);
                switch (cmsField.TemplateField.FieldType)
                {
                    case "Single-Line Text":
                    case "Multi-Line Text":
                    case "Rich Text":
                        {
                            var value = StringUtil.RemoveTags(cmsField.Value.ToString()).Trim();
                            createdItem[cmsField.TemplateField.FieldName] = value;
                        }
                        break;

                    case "Image":
                        {
                            if (cmsField.TemplateField != null)
                            {
                                var filesValue = (FieldValueFiles)cmsField.Value;
                                var file = filesValue.Files.FirstOrDefault();
                                if (file != null)
                                {
                                    var media = UploadFile(path, file);
                                    var value = "<image mediaid=\"" + media.ID + "\" />";

                                    createdItem[cmsField.TemplateField.FieldName] = value;
                                }
                            }
                        }
                        break;
                    case "File":
                        {
                            if (cmsField.TemplateField != null)
                            {
                                var filesValue = (FieldValueFiles)cmsField.Value;
                                var file = filesValue.Files.FirstOrDefault();
                                if (file != null)
                                {
                                    var media = UploadFile(path, file);

                                    var mediaUrl = MediaManager.GetMediaUrl(media,
                                        new MediaUrlOptions { UseItemPath = false, AbsolutePath = false });
                                    var value = "<file mediaid=\"" + media.ID + "\" src=\"" + mediaUrl + "\" />";

                                    createdItem[cmsField.TemplateField.FieldName] = value;
                                }
                            }
                        }
                        break;

                    case "Droptree":
                        {
                            if (cmsField.TemplateField != null)
                            {
                                if (cmsField.Value.GetType() == typeof(FieldValueFiles))
                                {
                                    var filesValue = (FieldValueFiles)cmsField.Value;
                                    var file = filesValue.Files.FirstOrDefault();
                                    if (file != null)
                                    {
                                        var media = UploadFile(path, file);
                                        createdItem[cmsField.TemplateField.FieldName] = media.ID.ToString();
                                    }
                                }
                                else if (cmsField.Value.GetType() == typeof(FieldValueOptions))
                                {
                                    var value = string.Empty;
                                    var optionsValue = (FieldValueOptions)cmsField.Value;
                                    var option = optionsValue.Options.FirstOrDefault();
                                    if (option != null)
                                    {
                                        var children = GetDatasource(createdItem,
                                            cmsField.TemplateField.FieldName, option); //option = GC option.Label
                                        if (children != null) value = children.ID.ToString();
                                        if (!string.IsNullOrEmpty(value))
                                            createdItem[cmsField.TemplateField.FieldName] = value;
                                    }
                                }
                            }
                        }
                        break;
                    case "Checklist":
                    case "Multilist":
                    case "Multilist with Search":
                    case "Treelist":
                    case "TreelistEx":
                        {
                            if (cmsField.Value.GetType() == typeof(FieldValueFiles))
                            {
                                var value = string.Empty;
                                var filesValue = (FieldValueFiles)cmsField.Value;
                                foreach (var file in filesValue.Files)
                                {
                                    if (file != null)
                                    {
                                        var media = UploadFile(path, file);
                                        if (media != null) value += media.ID.ToString() + "|";
                                    }
                                }
                                value = value.TrimEnd('|');
                                if (!string.IsNullOrEmpty(value))
                                    createdItem[cmsField.TemplateField.FieldName] = value;
                            }
                            else if (cmsField.Value.GetType() == typeof(FieldValueOptions))
                            {
                                var value = string.Empty;
                                var optionsValue = (FieldValueOptions)cmsField.Value;
                                foreach (var option in optionsValue.Options)
                                {
                                    var children = GetDatasource(createdItem, cmsField.TemplateField.FieldName, option);
                                    //option = GC option.Label
                                    if (children != null) value += children.ID.ToString() + "|";
                                }
                                value = value.TrimEnd('|');
                                if (!string.IsNullOrEmpty(value)) createdItem[cmsField.TemplateField.FieldName] = value;
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        private Item UploadFile(string path, File file)
        {
            var uri = file.Url.StartsWith("http") ? file.Url : "https://gathercontent.s3.amazonaws.com/" + file.Url;
            var request = (HttpWebRequest)WebRequest.Create(uri);
            var resp = (HttpWebResponse)request.GetResponse();
            var stream = resp.GetResponseStream();

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                if (stream != null)
                {
                    stream.CopyTo(memoryStream);
                }

                if (memoryStream.Length > 0)
                {
                    var media = CreateMedia(path, file, "jpg", memoryStream);
                    return media;
                }

            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootPath"></param>
        /// <param name="mediaFile"></param>
        /// <param name="extension"></param>
        /// <param name="mediaStream"></param>
        /// <returns></returns>
        private Item CreateMedia(string rootPath, File mediaFile, string extension, Stream mediaStream)
        {
            using (new SecurityDisabler())
            {
                var validItemName = ItemUtil.ProposeValidItemName(mediaFile.FileName);

                var filesFolder = GetItemByPath(rootPath);
                if (filesFolder != null)
                {
                    var files = filesFolder.Children;
                    var item = files.FirstOrDefault(f => f.Name == validItemName &&
                                                         DateUtil.IsoDateToDateTime(f.Fields["__Created"].Value) >=
                                                         mediaFile.UpdatedDate);
                    if (item != null)
                    {
                        return item;
                    }
                }

                var mediaOptions = new MediaCreatorOptions
                {
                    Database = ContextDatabase,
                    FileBased = false,
                    IncludeExtensionInItemName = false,
                    KeepExisting = true,
                    Versioned = true,
                    Destination = String.Concat(rootPath, "/", validItemName)
                };

                var previewImgItem = MediaManager.Creator.CreateFromStream(mediaStream, validItemName + "." + extension, mediaOptions);
                return previewImgItem;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemTitle"></param>
        /// <param name="createdItem"></param>
        /// <param name="cmsField"></param>
        /// <returns></returns>
        private string GetMediaItemPath(string itemTitle, Item createdItem, CmsField cmsField)
        {
            var dataSourcePath = GetDatasourcePath(createdItem,
                cmsField.TemplateField.FieldId);
            string path;
            if (string.IsNullOrEmpty(dataSourcePath))
            {
                path = string.IsNullOrEmpty(cmsField.TemplateField.FieldName)
                    ? string.Format("/sitecore/media library/GatherContent/{0}/",
                        itemTitle)
                    : string.Format("/sitecore/media library/GatherContent/{0}/{1}/",
                        itemTitle,
                        cmsField.TemplateField.FieldName);
                SetDatasourcePath(createdItem, cmsField.TemplateField.FieldId, path);
            }
            else
            {
                path = dataSourcePath;
            }
            return path;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool IsItemHasTemplate(ID templateId, Item item)
        {
            return item.Template.BaseTemplates.Any(i => i.ID == templateId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateItem"></param>
        /// <returns></returns>
        private bool EnsureMetaTemplateInherited(TemplateItem templateItem)
        {
            if (templateItem != null)
            {
                var gcLinkTemplate = templateItem.BaseTemplates.Any(bt => bt.Name == Constants.GCLinkItemTemplateName);
                var baseTemplates = templateItem.InnerItem[FieldIDs.BaseTemplate];

                if (!gcLinkTemplate)
                {
                    using (new SecurityDisabler())
                    {
                        templateItem.InnerItem.Editing.BeginEdit();
                        templateItem.InnerItem[FieldIDs.BaseTemplate] = string.Format("{0}|{1}", baseTemplates, Constants.GCLinkItemTemplateID);
                        templateItem.InnerItem.Editing.EndEdit();
                    }
                }
                return true;
            }
            return false;
        }



        #endregion




        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public IList<CmsItem> GetItems(string parentId, string language)
        {
            var parentItem = GetItem(parentId);
            var templatId = new ID(Constants.GCLinkItemTemplateID);
            var items = new List<Item>();
            if (IsItemHasTemplate(templatId, parentItem))
            {
                items.Add(parentItem);
            }
            items.AddRange(parentItem.Axes.GetDescendants().Where(i => IsItemHasTemplate(templatId, i)).ToList());

            var result = new List<CmsItem>();

            foreach (var item in items)
            {
                var cmsItem = GetItem(item.ID.ToString(), item.Language.ToString(), false);
                result.Add(cmsItem);
            }

            return result;
        }

        /// <summary>
        /// 
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
                    Language = item.Language.ToString()
                };

                cmsItem.Fields.Add(new CmsField
                {
                    TemplateField = new CmsTemplateField { FieldName = "GC Content Id" },
                    Value = item[GC_CONTENT_ID]
                });

                cmsItem.Fields.Add(new CmsField
                {
                    TemplateField = new CmsTemplateField { FieldName = "Last Sync Date" },
                    Value = DateUtil.IsoDateToDateTime(item[LAST_SYNC_DATE])
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
        /// 
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <param name="mappingId"></param>
        /// <param name="gcPath"></param>
        public string CreateMappedItem(string parentId, CmsItem cmsItem, string mappingId, string gcPath)
        {
            if (parentId != null)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(cmsItem.Language))
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
                                var idField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == GC_CONTENT_ID);
                                if (idField != null)
                                {
                                    createdItem.Editing.BeginEdit();
                                    createdItem.Fields[GC_CONTENT_ID].Value = idField.Value.ToString();
                                    var isoDate = DateUtil.ToIsoDate(DateTime.UtcNow);
                                    createdItem.Fields[LAST_SYNC_DATE].Value = isoDate;
                                    createdItem.Fields[MAPPING_ID].Value = mappingId;
                                    createdItem.Fields[GC_PATH].Value = gcPath;
                                    createdItem.Editing.EndEdit();
                                }
                                return createdItem.ID.ToString();
                            }
                            catch (Exception)
                            {
                                throw new Exception(string.Format("Your template({0}) is not inherited from the GC Linked Item.", createdItem.TemplateName));
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        /// <param name="item"></param>
        public void UpdateItem(CmsItem item)
        {
            var scItem = GetItem(item.Id);
            var validName = ItemUtil.ProposeValidItemName(item.Title);
            scItem.Editing.BeginEdit();
            scItem.Name = validName;
            scItem.Editing.EndEdit();
            SetupFields(item, scItem);
        }

        /// <summary>
        /// 
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

                    var value = StringUtil.RemoveTags(cmsField.Value.ToString()).Trim();
                    createdItem[cmsField.TemplateField.FieldName] = value;

                    createdItem.Editing.EndEdit();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cmsField"></param>
        public void MapChoice(CmsItem item, CmsField cmsField)
        {
            Item createdItem = GetItem(item.Id, Sitecore.Data.Managers.LanguageManager.GetLanguage(item.Language));
            var path = GetMediaItemPath(item.Title, createdItem, cmsField);

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
                                var media = UploadFile(path, file);
                                if (media != null) value += media.ID + "|";
                            }
                        }
                        value = value.TrimEnd('|');
                        if (!string.IsNullOrEmpty(value))
                        {
                            createdItem[cmsField.TemplateField.FieldName] = value;
                        }
                    }
                    else if (cmsField.Options != null && cmsField.Options.Any())
                    {
                        var value = string.Empty;
                        foreach (var option in cmsField.Options)
                        {
                            var children = GetDatasource(createdItem, cmsField.TemplateField.FieldId, option);
                            //option = GC option.Label
                            if (children != null) value += children.ID + "|";
                        }
                        value = value.TrimEnd('|');
                        if (!string.IsNullOrEmpty(value)) createdItem[cmsField.TemplateField.FieldName] = value;
                    }

                    createdItem.Editing.EndEdit();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cmsField"></param>
        public void MapFile(CmsItem item, CmsField cmsField)
        {
            Item createdItem = GetItem(item.Id, Sitecore.Data.Managers.LanguageManager.GetLanguage(item.Language));
            var path = GetMediaItemPath(item.Title, createdItem, cmsField);

            using (new SecurityDisabler())
            {
                using (new LanguageSwitcher(item.Language))
                {
                    if (cmsField.TemplateField != null)
                    {
                        var file = cmsField.Files.FirstOrDefault();
                        if (file != null)
                        {
                            var media = UploadFile(path, file);

                            var mediaUrl = MediaManager.GetMediaUrl(media, new MediaUrlOptions {UseItemPath = false, AbsolutePath = false});
                            var value = string.Format("<file mediaid=\"{0}\" src=\"{1}\" />", media.ID, mediaUrl);

                            createdItem.Editing.BeginEdit();
                            createdItem[cmsField.TemplateField.FieldName] = value;
                            createdItem.Editing.EndEdit();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <param name="mappingId"></param>
        /// <param name="gcPath"></param>
        /// <returns></returns>
        public bool IfMappedItemExists(string parentId, CmsItem cmsItem, string mappingId, string gcPath)
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
                            var items = parent.Axes.SelectItems(string.Format("./*[@GCPath='{0}' and @@name='{1}']", gcPath, validName));
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
        /// 
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
                            var items = parent.Axes.SelectItems(string.Format("./*[@@name='{0}']", validName));
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
        /// 
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <param name="mappingId"></param>
        /// <param name="gcPath"></param>
        public string AddNewVersion(string parentId, CmsItem cmsItem, string mappingId, string gcPath)
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
                            var items = parent.Axes.SelectItems(string.Format("./*[@GCPath='{0}' and @@name='{1}']", gcPath, validName));

                            foreach (var item in items)
                            {
                                //TODO: add new method for update and create
                                var newVersion = item.Versions.AddVersion();

                                try
                                {
                                    EnsureMetaTemplateInherited(newVersion.Template);
                                    var idField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == GC_CONTENT_ID);
                                    if (idField != null)
                                    {
                                        newVersion.Editing.BeginEdit();

                                        newVersion.Fields[GC_CONTENT_ID].Value = idField.Value.ToString();
                                        var isoDate = DateUtil.ToIsoDate(DateTime.UtcNow);
                                        newVersion.Fields[LAST_SYNC_DATE].Value = isoDate;
                                        newVersion.Fields[MAPPING_ID].Value = mappingId;
                                        newVersion.Fields[GC_PATH].Value = gcPath;

                                        newVersion.Editing.BeginEdit();
                                    }
                                    return newVersion.ID.ToString();
                                }
                                catch (Exception)
                                {
                                    throw new Exception(string.Format("Your template({0}) is not inherited from the GC Linked Item.", newVersion.TemplateName));
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public string GetCmsItemLink(string host, string itemId)
        {
            return string.Format("http://{0}/sitecore/shell/Applications/Content Editor?fo={1}&sc_content=master&sc_bw=1", host, itemId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="cmsItem"></param>
        /// <returns></returns>
        public string GetItemId(string parentId, CmsItem cmsItem)
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
                            var items = parent.Axes.SelectItems(string.Format("./*[@@name='{0}']", validName));
                            if (items != null && items.Any())
                            {
                                return items.First().ID.ToString();
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}
