using System;
using System.IO;
using System.Net;
using System.Web;
using GatherContent.Connector.Entities;
using GatherContent.Connector.IRepositories.Models.New.Import;
using Sitecore;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Update;
using Sitecore.Data.Managers;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    public class ItemsRepository : BaseSitecoreRepository, IItemsRepository
    {
        private const string GC_CONTENT_ID = "GC Content Id";
        private const string LAST_SYNC_DATE = "Last Sync Date";
        private readonly GCAccountSettings _accountSettings;

        public ItemsRepository()
            : base()
        {
            var accountsRepository = new AccountsRepository();
            _accountSettings = accountsRepository.GetAccountSettings();
        }



        #region Old Utilities

        private void SetupFields(Item updatedItem, MappingResultModel item)
        {
            using (new SecurityDisabler())
            {
                updatedItem.Editing.BeginEdit();

                foreach (var field in item.Fields)
                {
                    switch (field.Type)
                    {
                        case "choice_radio":
                        case "choice_checkbox":
                            {
                                var value = string.Empty;
                                foreach (var option in field.Options)
                                {
                                    if (option.Selected)
                                    {
                                        var children = GetDatasource(updatedItem, field.Name, option.Label);
                                        if (children != null) value += children.ID.ToString() + "|";
                                    }
                                }
                                value = value.TrimEnd('|');
                                if (!string.IsNullOrEmpty(value)) updatedItem.Fields[new ID(field.Name)].Value = value;
                            }
                            break;
                        case "files":
                            {
                                var dataSourcePath = GetDatasourcePath(updatedItem, field.Name);
                                string path;
                                if (string.IsNullOrEmpty(dataSourcePath))
                                {
                                    path = string.IsNullOrEmpty(field.Label)
                                        ? string.Format("/sitecore/media library/GatherContent/{0}/", item.Title)
                                        : string.Format("/sitecore/media library/GatherContent/{0}/{1}/", item.Title,
                                            field.Label);
                                    SetDatasourcePath(updatedItem, field.Name, path);
                                }
                                else
                                {
                                    path = dataSourcePath;
                                }
                                switch (updatedItem.Fields[new ID(field.Name)].Type)
                                {
                                    case "Droptree":
                                        {
                                            var file = field.Files.FirstOrDefault();
                                            if (file != null)
                                            {
                                                var media = UploadFile(path, file);
                                                updatedItem.Fields[new ID(field.Name)].Value = media.ID.ToString();
                                            }

                                        }
                                        break;
                                    case "Image":
                                        {
                                            var file = field.Files.FirstOrDefault();
                                            if (file != null)
                                            {

                                                var media = UploadFile(path, file);
                                                var val = "<image mediaid=\"" + media.ID + "\" />";
                                                updatedItem.Fields[new ID(field.Name)].Value = val;
                                            }
                                        }
                                        break;
                                    case "File":
                                        {
                                            var file = field.Files.FirstOrDefault();
                                            if (file != null)
                                            {
                                                var media = UploadFile(path, file);

                                                var mediaUrl = MediaManager.GetMediaUrl(media,
                                                    new MediaUrlOptions { UseItemPath = false, AbsolutePath = false });
                                                var val = "<file mediaid=\"" + media.ID + "\" src=\"" + mediaUrl + "\" />";
                                                updatedItem.Fields[new ID(field.Name)].Value = val;
                                            }
                                        }
                                        break;
                                    default:
                                        {
                                            var value = string.Empty;
                                            foreach (var file in field.Files)
                                            {
                                                if (file != null)
                                                {
                                                    var media = UploadFile(path, file);
                                                    if (media != null) value += media.ID.ToString() + "|";
                                                }
                                            }
                                            value = value.TrimEnd('|');
                                            if (!string.IsNullOrEmpty(value))
                                                updatedItem.Fields[new ID(field.Name)].Value = value;
                                        }
                                        break;
                                }
                            }
                            break;
                        default:
                            {
                                var targetFieldType = updatedItem.Fields[new ID(field.Name)].Type;
                                var valueToUpdate = field.Value.Trim();
                                if (targetFieldType == "Single-Line Text" || targetFieldType == "Multi-Line Text")
                                {
                                    valueToUpdate = StringUtil.RemoveTags(valueToUpdate).Trim();
                                }

                                updatedItem.Fields[new ID(field.Name)].Value = valueToUpdate;
                            }
                            break;
                    }
                }
                try
                {
                    EnsureMetaTemplateInherited(updatedItem.Template);
                    updatedItem.Fields[GC_CONTENT_ID].Value = item.GCItemId;
                    var isoDate = DateUtil.ToIsoDate(DateTime.UtcNow);
                    updatedItem.Fields[LAST_SYNC_DATE].Value = isoDate;
                }
                catch (Exception)
                {
                    throw new Exception("Your template(" + updatedItem.TemplateName + ") is not inherited from the GC Linked Item.");
                }

                updatedItem.Editing.EndEdit();
            }
        }

        private Item CreateMedia(string rootPath, FileOld mediaFile, string extension, Stream mediaStream)
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

        private Item UploadFile(string path, FileOld file)
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


        #endregion






        #region Utilities



        private List<MappingResultModel> AddItems(Item parent, string language, List<MappingResultModel> items)
        {
            var list = new List<MappingResultModel>();
            foreach (var successfulImportedItem in items)
            {
                var item = AddItem(parent, language, successfulImportedItem);
                list.Add(item);
            }
            return list;

        }

        private MappingResultModel AddItem(Item parent, string language, MappingResultModel item)
        {
            if (parent != null)
            {
                using (new SecurityDisabler())
                {
                    using (new LanguageSwitcher(language))
                    {

                        TemplateItem template = ContextDatabase.GetTemplate(new ID(item.CMSTemplateId));
                        string validName = ItemUtil.ProposeValidItemName(item.Title);
                        Item createdItem = parent.Add(validName, template);

                        if (!string.IsNullOrEmpty(_accountSettings.GatherContentUrl))
                        {
                            item.GcLink = _accountSettings.GatherContentUrl + "/item/" + item.GCItemId;
                        }
                        var cmsLink =
                            string.Format(
                                "http://{0}/sitecore/shell/Applications/Content Editor?fo={1}&sc_content=master&sc_bw=1",
                                HttpContext.Current.Request.Url.Host, createdItem.ID);
                        item.CmsLink = cmsLink;
                        SetupFields(createdItem, item);

                        return item;
                    }
                }
            }
            return item;
        }

        private Item GetDatasource(Item updatedItem, string fieldId, string label)
        {
            var dataSourcePath = GetDatasourcePath(updatedItem, fieldId);
            var dataSourceItem = GetItemByPath(dataSourcePath);
            if (dataSourceItem == null) return null;
            var children = dataSourceItem.GetChildren().InnerChildren.FirstOrDefault(c => c.Name.ToLower() == label.ToLower());
            return children;
        }

        private string GetDatasourcePath(Item updatedItem, string fieldId)
        {
            var scField = updatedItem.Fields[new ID(fieldId)];
            var dataSourcePath = GetItem(scField.ID.ToString())["Source"];
            return dataSourcePath;
        }

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


        private Item UploadFile(string path, IRepositories.Models.New.Import.File file)
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


        private Item CreateMedia(string rootPath, IRepositories.Models.New.Import.File mediaFile, string extension, Stream mediaStream)
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

        private string GetMediaItemPath(string itemTitle, Item createdItem, CmsField cmsField)
        {
            var dataSourcePath = GetDatasourcePath(createdItem,
                cmsField.TemplateField.FieldName);
            string path;
            if (string.IsNullOrEmpty(dataSourcePath))
            {
                path = string.IsNullOrEmpty(cmsField.TemplateField.FieldName)
                    ? string.Format("/sitecore/media library/GatherContent/{0}/",
                        itemTitle)
                    : string.Format("/sitecore/media library/GatherContent/{0}/{1}/",
                        itemTitle,
                        cmsField.TemplateField.FieldName);
                SetDatasourcePath(createdItem, cmsField.TemplateField.FieldName, path);
            }
            else
            {
                path = dataSourcePath;
            }
            return path;
        }


        private bool IsItemHasTemplate(ID templateId, Item item)
        {
            return item.Template.BaseTemplates.Any(i => i.ID == templateId);
        }

        private CMSUpdateItem GetCMSItem(Item item)
        {
            string gcItemId = item[GC_CONTENT_ID];
            DateTime lastSyncDate = DateUtil.IsoDateToDateTime(item[LAST_SYNC_DATE]);
            var result = new CMSUpdateItem(item.ID.ToString(), item.Name, item.TemplateName, gcItemId, lastSyncDate);
            return result;
        }


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


        #region Old Methods

        public List<MappingResultModel> ImportItems(string itemId, string languageName, List<MappingResultModel> items)
        {
            var language = LanguageManager.GetLanguage(languageName);
            Item parentItem = GetItem(itemId, language);

            var result = AddItems(parentItem, languageName, items);
            return result;
        }


        public List<MappingResultModel> ImportItemsWithLocation(string languageName, List<MappingResultModel> items)
        {
            var list = new List<MappingResultModel>();
            var language = LanguageManager.GetLanguage(languageName);
            foreach (var successfulImportedItem in items)
            {
                Item parentItem = GetItem(successfulImportedItem.DefaultLocation, language);
                var item = AddItem(parentItem, languageName, successfulImportedItem);
                list.Add(item);
            }
            return list;
        }


        public void UpdateItems(List<MappingResultModel> items)
        {
            foreach (var item in items)
            {
                var scItem = GetItem(item.CMSId);
                if (!string.IsNullOrEmpty(_accountSettings.GatherContentUrl))
                {
                    item.GcLink = _accountSettings.GatherContentUrl + "/item/" + item.GCItemId;
                }
                var cmsLink = string.Format("http://{0}/sitecore/shell/Applications/Content Editor?fo={1}&sc_content=master&sc_bw=1", HttpContext.Current.Request.Url.Host, scItem.ID);
                item.CmsLink = cmsLink;

                var validName = ItemUtil.ProposeValidItemName(item.Title);
                scItem.Editing.BeginEdit();
                scItem.Name = validName;
                scItem.Editing.EndEdit();
                SetupFields(scItem, item);
            }
        }


        public List<CMSUpdateItem> GetItemsForUpdate(string targetItemId)
        {
            Item parentItem = GetItem(targetItemId);
            var templatId = new ID(Constants.GCLinkItemTemplateID);
            var items = new List<Item>();
            if (IsItemHasTemplate(templatId, parentItem))
            {
                items.Add(parentItem);
            }
            items.AddRange(parentItem.Axes.GetDescendants().Where(i => IsItemHasTemplate(templatId, i)).ToList());
            List<CMSUpdateItem> result = items.Select(GetCMSItem).ToList();

            return result;
        }




        #endregion



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


        public void CreateItem(string parentId, CmsItem cmsItem)
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
                            SetupFields(cmsItem, createdItem);
                            try
                            {
                                EnsureMetaTemplateInherited(createdItem.Template);
                                var idField = cmsItem.Fields.FirstOrDefault(f => f.TemplateField.FieldName == "GC Content Id");
                                if (idField != null)
                                {
                                    createdItem.Fields[GC_CONTENT_ID].Value = idField.Value.ToString();
                                    var isoDate = DateUtil.ToIsoDate(DateTime.UtcNow);
                                    createdItem.Fields[LAST_SYNC_DATE].Value = isoDate;
                                }
                            }
                            catch (Exception)
                            {
                                throw new Exception("Your template(" + createdItem.TemplateName + ") is not inherited from the GC Linked Item.");
                            }
                        }
                    }
                }
            }
        }

    
        public void UpdateItem(CmsItem item)
        {
            var scItem = GetItem(item.Id);
            var validName = ItemUtil.ProposeValidItemName(item.Title);
            scItem.Editing.BeginEdit();
            scItem.Name = validName;
            scItem.Editing.EndEdit();
            SetupFields(item, scItem);
        }
    }
}
