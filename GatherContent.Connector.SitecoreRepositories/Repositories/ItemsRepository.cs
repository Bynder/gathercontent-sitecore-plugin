using System;
using Sitecore;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;
using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.IRepositories.Models.Update;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    public class ItemsRepository : BaseSitecoreRepository, IItemsRepository
    {
        private const string GC_CONTENT_ID = "GC Content Id";
        private const string LAST_SYNC_DATE = "Last Sync Date";

        
        public void ImportItems(string itemId, List<MappingResultModel> items)
        {
            Item parentItem = GetItem(itemId);

            AddItems(parentItem, items);
        }

        private void AddItems(Item parent, List<MappingResultModel> items)
        {
            items.ForEach(i => AddItem(parent, i));
        }

        private void AddItem(Item parent, MappingResultModel item)
        {
            using (new SecurityDisabler())
            {
                TemplateItem template = ContextDatabase.GetTemplate(new ID(item.CMSTemplateId));
                string validName = ItemUtil.ProposeValidItemName(item.Title);
                Item createdItem = parent.Add(validName, template);
                SetupFields(createdItem, item);
            }
        }

        private void SetupFields(Item updatedItem, MappingResultModel item)
        {
            using (new SecurityDisabler())
            {
                updatedItem.Editing.BeginEdit();

                foreach (ImportCMSField field in item.Fields)
                {
                    updatedItem.Fields[new ID(field.Name)].Value = field.Value;
                }

                updatedItem.Fields[GC_CONTENT_ID].Value = item.GCItemId;

                var isoDate = DateUtil.ToIsoDate(DateTime.UtcNow);
                updatedItem.Fields[LAST_SYNC_DATE].Value = isoDate;

                updatedItem.Editing.EndEdit();
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
            foreach (MappingResultModel item in items)
            {
                Item scItem = GetItem(item.CMSId);
                SetupFields(scItem, item);
            }
        }

        public string GetItemId(string itemId, string gcItemId)
        {
            Item parentItem = GetItem(itemId);

            if (parentItem != null)
            {
                var gcItem = parentItem.Axes.GetDescendants()
                    .FirstOrDefault(item => item[GC_CONTENT_ID] == gcItemId);
                if (gcItem != null)
                    return gcItem.ID.ToString();
            }
            return null;
        }


    }
}
