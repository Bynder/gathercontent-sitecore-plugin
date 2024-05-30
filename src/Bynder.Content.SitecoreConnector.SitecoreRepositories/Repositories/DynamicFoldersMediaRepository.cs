using System.Linq;

using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Bynder.Content.SitecoreConnector.SitecoreRepositories.Repositories
{
    using Core.Models.Import;

    using GatherContentService.Interfaces;

    public class DynamicFoldersMediaRepository : SimpleMediaRepository
    {
        public DynamicFoldersMediaRepository(IItemsService itemsService) : base(itemsService)
        {
        }

        public override string ResolveMediaPath(CmsItem item, Item createdItem, CmsField cmsField)
        {
            Field scField = createdItem.Fields[new ID(cmsField.TemplateField.FieldId)];
            string dataSourcePath = GetItem(scField.ID.ToString())["Source"];
            if (string.IsNullOrEmpty(dataSourcePath) || GetItem(dataSourcePath) == null)
            {
                dataSourcePath = "/sitecore/media library/GatherContent";
            }

            Item parent = createdItem.Parent;
            string subfolder = string.Empty;
            while (parent != null && ItemIsCwbLinked(parent))
            {
                subfolder = "/" + parent.Name + subfolder;

                parent = parent.Parent;
            }

            dataSourcePath = dataSourcePath + subfolder;

            var path = string.IsNullOrEmpty(cmsField.TemplateField.FieldName)
                ? string.Format("{0}/{1}/", dataSourcePath,item.Title)
                : string.Format("{0}/{1}/{2}/", dataSourcePath, item.Title, cmsField.TemplateField.FieldName);

            return path;
        }

        private bool ItemIsCwbLinked(Item item)
        {
            if (item.TemplateName == Constants.CWBNotMappedItemTemplateName || item.TemplateName == Constants.CWBLinkItemTemplateName)
            {
                return true;
            }

            if (item.Template.BaseTemplates.Any(i => (i.Name == Constants.CWBNotMappedItemTemplateName || i.Name == Constants.CWBLinkItemTemplateName)))
            {
                return true;
            }

            return false;
        }
    }
}
