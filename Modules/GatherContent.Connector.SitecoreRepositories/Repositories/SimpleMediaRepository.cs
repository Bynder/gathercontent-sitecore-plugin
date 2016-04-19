using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using File = GatherContent.Connector.IRepositories.Models.Import.File;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    public class SimpleMediaRepository : BaseSitecoreRepository, IMediaRepository<Item>
    {
        public Item UploadFile(string targetPath, File fileInfo)
        {
            string uri = fileInfo.Url.StartsWith("http") ? fileInfo.Url : "https://gathercontent.s3.amazonaws.com/" + fileInfo.Url;

            string extension = string.Empty;
            if (fileInfo.FileName.Contains("."))
            {
                extension = fileInfo.FileName.Substring(fileInfo.FileName.LastIndexOf('.') + 1);
            }

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
                    var media = CreateMedia(targetPath, fileInfo, extension, memoryStream);
                    return media;
                }

            }

            return null;
        }

        public virtual string ResolveMediaPath(CmsItem item, Item createdItem, CmsField cmsField)
        {
            Field scField = createdItem.Fields[new ID(cmsField.TemplateField.FieldId)];
            string dataSourcePath = GetItem(scField.ID.ToString())["Source"];
            string path;
            if (string.IsNullOrEmpty(dataSourcePath))
            {
                path = string.IsNullOrEmpty(cmsField.TemplateField.FieldName)
                    ? string.Format("/sitecore/media library/GatherContent/{0}/", item.Title)
                    : string.Format("/sitecore/media library/GatherContent/{0}/{1}/", item.Title, cmsField.TemplateField.FieldName);
                
                SetDatasourcePath(createdItem, cmsField.TemplateField.FieldId, path);
            }
            else
            {
                path = dataSourcePath;
            }
            return path;
        }

        protected virtual Item CreateMedia(string rootPath, File mediaFile, string extension, Stream mediaStream)
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
                    Versioned = false,
                    Destination = String.Concat(rootPath, "/", validItemName)
                };

                var previewImgItem = MediaManager.Creator.CreateFromStream(mediaStream, validItemName + "." + extension, mediaOptions);
                return previewImgItem;
            }
        }

        protected void SetDatasourcePath(Item updatedItem, string fieldId, string path)
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
    }
}
