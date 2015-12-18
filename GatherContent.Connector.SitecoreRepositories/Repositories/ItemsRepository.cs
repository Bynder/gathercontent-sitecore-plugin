using System;
using System.IO;
using System.Net;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using Sitecore;
using System.Linq;
using Sitecore.ApplicationCenter.Applications;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Update;
using File = GatherContent.Connector.IRepositories.Models.Import.File;

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

        public void ImportItems(string itemId, ref List<MappingResultModel> items)
        {
            Item parentItem = GetItem(itemId);

            AddItems(parentItem, ref items);
        }

        private void AddItems(Item parent, ref List<MappingResultModel> items)
        {
            items.ForEach(i => AddItem(parent, ref i));
        }

        private void AddItem(Item parent, ref MappingResultModel item)
        {
            using (new SecurityDisabler())
            {
                TemplateItem template = ContextDatabase.GetTemplate(new ID(item.CMSTemplateId));
                string validName = ItemUtil.ProposeValidItemName(item.Title);
                Item createdItem = parent.Add(validName, template);

                if (!string.IsNullOrEmpty(_accountSettings.GatherContentUrl))
                {
                    item.GcLink = _accountSettings.GatherContentUrl + "/item/" + item.GCItemId;
                }
                var cmsLink = string.Format("{0}/sitecore/shell/Applications/Content Editor?fo={1}&sc_content=master", Sitecore.Context.Site.HostName, createdItem.ID);
                item.CmsLink = cmsLink;
                SetupFields(createdItem, item);
            }
        }

        private Item GetDatasource(Item updatedItem, string fieldId, string label)
        {
            var scField = updatedItem.Fields[new ID(fieldId)];
            var dataSourcePath = GetItem(scField.ID.ToString())["Source"];
            var dataSourceItem = GetItemByPath(dataSourcePath);
            if (dataSourceItem == null) return null;
            var children = dataSourceItem.GetChildren().InnerChildren.FirstOrDefault(c => c.Name == label);
            return children;
        }


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
                                switch (updatedItem.Fields[new ID(field.Name)].Type)
                                {
                                    case "Droptree":
                                    {
                                        var file = field.Files.FirstOrDefault();
                                        if (file != null)
                                        {
                                            var media = UploadFile(item.Title, field.Label, file);
                                            updatedItem.Fields[new ID(field.Name)].Value = media.ID.ToString();
                                        }

                                    }
                                        break;
                                    case "Image":
                                    {
                                        var file = field.Files.FirstOrDefault();
                                        if (file != null)
                                        {
                                            var media = UploadFile(item.Title, field.Label, file);
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
                                            var media = UploadFile(item.Title, field.Label, file);

                                            var mediaUrl = MediaManager.GetMediaUrl(media,
                                                new MediaUrlOptions {UseItemPath = false, AbsolutePath = false});
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
                                                var media = UploadFile(item.Title, field.Label, file);
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
                                updatedItem.Fields[new ID(field.Name)].Value = field.Value;
                            }
                            break;
                    }
                }
                try
                {
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



        private Item UploadFile(string itemTitle, string fieldTitle, File file)
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
                    var path = string.IsNullOrEmpty(fieldTitle) ?
                        string.Format("/sitecore/media library/GatherContent/{0}/", itemTitle) :
                        string.Format("/sitecore/media library/GatherContent/{0}/{1}/", itemTitle, fieldTitle);
                    var media = CreateMedia(path, file.FileName, "jpg", memoryStream);
                    return media;
                }

            }

            return null;
        }


        public Item CreateMedia(string rootPath, string name, string extension, Stream mediaStream)
        {
            using (new SecurityDisabler())
            {

                var validItemName = ItemUtil.ProposeValidItemName(name);

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




        public List<CMSUpdateItem> GetItemsForUpdate(string targetItemId)
        {
            Item parentItem = GetItem(targetItemId);
            var templatId = new ID(Constants.GCLinkItemTemplate);
            IEnumerable<Item> items = parentItem.Axes.GetDescendants().Where(i => IsItemHasTemplate(templatId, i));
            List<CMSUpdateItem> result = items.Select(GetCMSItem).ToList();

            return result;
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


        public void UpdateItems(List<MappingResultModel> items)
        {
            foreach (var item in items)
            {
                var scItem = GetItem(item.CMSId);
                if (!string.IsNullOrEmpty(_accountSettings.GatherContentUrl))
                {
                    item.GcLink = _accountSettings.GatherContentUrl + "/item/" + item.GCItemId;
                }
                var cmsLink = string.Format("{0}/sitecore/shell/Applications/Content Editor?fo={1}&sc_content=master", Sitecore.Context.Site.HostName, scItem.ID);
                item.CmsLink = cmsLink;
                SetupFields(scItem, item);
            }
        }

    }
}
