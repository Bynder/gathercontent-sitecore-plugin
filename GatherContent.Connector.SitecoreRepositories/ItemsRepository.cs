using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.IRepositories.Models.Import;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class ItemsRepository : BaseSitecoreRepository, IItemsRepository
    {
        public ItemsRepository() : base() { }

        public void ImportItems(string itemId, List<ImportItemsResponseModel> items)
        {
            Item parentItem = GetItem(itemId);
         
            AddItems(parentItem, items);
            
        }

        private void AddItems(Item parent, List<ImportItemsResponseModel> items)
        {
            items.ForEach(i => AddItem(parent, i));
        }

        private void AddItem(Item parent, ImportItemsResponseModel item)
        {
            using (new SecurityDisabler())
            {
                TemplateItem template = ContextDatabase.GetTemplate(new ID(item.CMSTemplate));
                string validName = ItemUtil.ProposeValidItemName(item.Title);
                Item createdItem = parent.Add(validName, template);
                using (new SecurityDisabler())
                {
                    createdItem.Editing.BeginEdit();
                    
                    foreach (ImportCMSFiled field in item.Fields)
                    {
                        createdItem.Fields[field.Name].Value = field.Value;
                    }

                    createdItem.Fields["GC Content Id"].Value = item.GCItemId;

                    var isoDate = DateUtil.ToIsoDate(DateTime.UtcNow);
                    createdItem.Fields["Last Sync Date"].Value = isoDate;

                    createdItem.Editing.EndEdit();
                }
            }
        }

        public List<CMSUpdateItem> GetItems(string targetItemId)
        {
            Item parentItem = GetItem(targetItemId);
            var items = parentItem.Axes.SelectItems(String.Format("./descendant::*[@@templatename='{0}']", Constants.GCLinkItemTemplate));
            var result = items.Select(i => new CMSUpdateItem { GSItemId = i["GC Content Id"], LastUpdatedTime = DateTime.Now.AddDays(-1) }).ToList();

            return result;
        }
    }
}
