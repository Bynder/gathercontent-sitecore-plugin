namespace Bynder.Content.SitecoreConnector.SitecoreRepositories.Repositories
{
    using System.IO;
    using System.Linq;
    
    using Sitecore;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Resources.Media;
    using Sitecore.SecurityModel;

    using Extensions;
    using Core.Interfaces;
    using Core.Models.Import;
    using File = Core.Models.Import.File;
    using Core.DependencyInjection;

    using GatherContentService.Interfaces;

    [Service(typeof(IMediaRepository<Item>))]
    public class SimpleMediaRepository : BaseSitecoreRepository, IMediaRepository<Item>
    {
        protected readonly IItemsService ItemsService;

        public SimpleMediaRepository(IItemsService itemsService)
        {
            this.ItemsService = itemsService;
        }

        public Item UploadFile(string targetPath, File fileInfo)
        {
            string extension = string.Empty;
            if (fileInfo.FileName.Contains("."))
            {
                extension = fileInfo.FileName.Substring(fileInfo.FileName.LastIndexOf('.') + 1);
            }

            var memoryStream = ItemsService.DownloadFile(fileInfo.FileId) as MemoryStream;
            try
            {
                if (memoryStream.Length > 0)
                {
                    var media = CreateMedia(targetPath, fileInfo, extension, memoryStream);
                    return media;
                }
            }
            finally
            {
                memoryStream.Close();
            }

            return null;
        }

        public virtual string ResolveMediaPath(CmsItem item, Item createdItem, CmsField cmsField)
        {
            var itemName = StringExtensions.GetUrlOptimisedItemName(ItemUtil.ProposeValidItemName(item.Title));
            var fieldName =
                StringExtensions.GetUrlOptimisedItemName(
                    ItemUtil.ProposeValidItemName(cmsField.TemplateField.FieldName));
            var folderName = itemName
                .Where(char.IsLetterOrDigit)
                .First();

            string path = string.IsNullOrEmpty(cmsField.TemplateField.FieldName)
                    ? string.Format("/sitecore/media library/GatherContent/{0}/{1}/{2}/", item.Template.TemplateName, folderName, itemName)
                    : string.Format("/sitecore/media library/GatherContent/{0}/{1}/{2}/{3}/", item.Template.TemplateName, folderName, itemName, fieldName);

            return path;
        }

        protected virtual Item CreateMedia(string rootPath, File mediaFile, string extension, Stream mediaStream)
        {
            using (new SecurityDisabler())
            {
                var validItemName = StringExtensions.GetUrlOptimisedItemName(ItemUtil.ProposeValidItemName(mediaFile.FileName));

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
                    OverwriteExisting = false,
                    Versioned = false,
                    Destination = string.Concat(rootPath, "/", validItemName)
                };

                var previewImgItem = MediaManager.Creator.CreateFromStream(mediaStream, validItemName + "." + extension, mediaOptions);
                return previewImgItem;
            }
        }

        protected void SetDatasourcePath(Item updatedItem, string fieldId, string path)
        {
            var scField = updatedItem.Fields[new ID(fieldId)];
            var scItem = GetItem(scField.ID.ToString());

            if (string.IsNullOrEmpty(scItem["Source"]))
            {
                using (new SecurityDisabler())
                {
                    scItem.Editing.BeginEdit();
                    scItem["Source"] = path;
                    scItem.Editing.EndEdit();
                }
            }
        }
    }
}
