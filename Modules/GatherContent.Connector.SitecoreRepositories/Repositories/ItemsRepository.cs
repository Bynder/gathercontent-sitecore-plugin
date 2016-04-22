using System;
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
using Sitecore.Diagnostics;

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
        private readonly IMediaRepository<Item> _mediaRepository;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountsRepository"></param>
        /// <param name="mediaRepository"></param>
        public ItemsRepository(IAccountsRepository accountsRepository, IMediaRepository<Item> mediaRepository) : base()
        {
            AccountsRepository = accountsRepository;
            _mediaRepository = mediaRepository;
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
                bool gcLinkTemplate = templateItem.BaseTemplates.Any(bt => bt.Name == Constants.GCLinkItemTemplateName);
                string baseTemplates = templateItem.InnerItem[FieldIDs.BaseTemplate];

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
                    Title = item.Name,
                    Language = item.Language.ToString(),
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
                            catch (Exception ex)
                            {
                                Log.Error("cannot create mapped item.", ex, this);
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
            var path = _mediaRepository.ResolveMediaPath(item, createdItem, cmsField);

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
                                Item media = _mediaRepository.UploadFile(path, file);
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
            var path = _mediaRepository.ResolveMediaPath(item, createdItem, cmsField);

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
                            Item media = _mediaRepository.UploadFile(path, file);

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
                            var items = parent.Axes.SelectItems(string.Format("./*[@@name='{0}' and @GCPath!='']", validName));
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

                                        newVersion.Editing.EndEdit();
                                    }
                                    return newVersion.ID.ToString();
                                }
                                catch (Exception ex)
                                {
                                    Log.Error("cannot update mapped item.", ex, this);
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
