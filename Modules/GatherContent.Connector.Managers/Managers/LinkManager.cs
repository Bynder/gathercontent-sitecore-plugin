namespace GatherContent.Connector.Managers.Managers
{
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;
    using Sitecore.ContentSearch;
    using Sitecore.ContentSearch.Linq;
    using Sitecore.ContentSearch.SearchTypes;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;

    public class LinkManager : ILinkManager, IManager
    {
        private const string IndexName = "sitecore_master_index";

        public List<string> GetLinkedItemsIds(int gcId)
        {
            var items = GetLinkedSitecoreItems(gcId.ToString());

            return items.Select(i => i.ID.ToString()).ToList();
        }

        public string GetLinkedItemUrl(int gcId)
        {
            throw new System.NotImplementedException();
        }

        public void ExpandLinksInText(string cmsRootId)
        {
            throw new System.NotImplementedException();
        }

        private List<Item> GetLinkedSitecoreItems(string gcId)
        {
            var result = new List<Item>();

            ISearchIndex index = ContentSearchManager.Indexes.SingleOrDefault(i => i.Name.Equals(IndexName));

            if (index == null)
            {
                Log.Warn("Index " + IndexName + " not found!", this);
                return result;
            }

            using (IProviderSearchContext context = index.CreateSearchContext())
            {
                var query = context.GetQueryable<SearchResultItem>()
                    .Where(i => i.Path.StartsWith("/sitecore/content/") &&
                                i["gc_content_id"] == gcId
                                );

                query = query.Filter(i => i.Language == Sitecore.Context.Language.Name);

                var res = query.GetResults();
                if (res.TotalSearchResults == 0)
                {
                    return result;
                }

                result = res.Hits
                    .Select(h => h.Document.GetItem())
                    .Where(i => i != null)
                    .ToList();

                return result;
            }

        } 
    }
}