namespace Bynder.Content.SitecoreConnector.Core.Interfaces
{
    using System.Collections.Generic;

    using Models.Import;

    using Sitecore.Data.Items;

    /// <summary>
    /// 
    /// </summary>
    public interface IItemsRepository : IRepository
    {
        IList<CmsItem> GetItems(string parentId, string language);

        CmsItem GetItem(string itemId, string language, bool readAllFields = false);

        string CreateMappedItem(string parentId, CmsItem cmsItem, string mappingId, string cwbPath);

        string CreateNotMappedItem(string parentId, CmsItem cmsItem);

        void ResolveAttachmentMapping(CmsItem item, CmsField field);

        void MapText(CmsItem item, CmsField field);

        void MapChoice(CmsItem item, CmsField field, List<Item> finalListOptions);

        void MapFile(CmsItem item, CmsField field);

        void MapImage(CmsItem item, CmsField field);

        void MapDropTree(CmsItem item, CmsField field);

        void MapDateTime(CmsItem item, CmsField field);

        bool IfMappedItemExists(string itemId, CmsItem cmsItem, string mappingId, string cwbPath);

        bool IfMappedItemExists(string parentId, CmsItem cmsItem, string mappingId, string cwbPath, string cwbId);

        bool IfMappedItemExists(string parentId, CmsItem cmsItem);

        bool IfNotMappedItemExists(string parentId, CmsItem cmsItem);

        string AddNewVersion(string itemId, CmsItem cmsItem, string mappingId, string cwbPath);
        string AddNewVersion(string parentId, CmsItem cmsItem, string mappingId, string cwbPath, string cwbId);

        string GetCmsItemLink(string scheme, string host, string itemId);

        string GetItemId(string parentId, CmsItem cmsItem);

        /// <summary>
        /// Gets the linked item URL in RTE format '~/link.aspx?_id=GUID&amp;_z=z'.
        /// </summary>
        /// <param name="cwbId">The cwb identifier.</param>
        /// <returns></returns>
        string GetLinkedItemUrl(int cwbId);

        void ExpandLinksInText(string cmsRootId, bool includeDescendants);
    }
}
