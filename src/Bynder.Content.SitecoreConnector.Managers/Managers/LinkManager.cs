namespace Bynder.Content.SitecoreConnector.Managers.Managers
{
    using Core.DependencyInjection;
    using Core.Interfaces;
    using Interfaces;

    [Service(typeof(ILinkManager))]
    public class LinkManager : ILinkManager, IManager
    {
        private readonly IItemsRepository itemsRepository;

        public LinkManager(IItemsRepository itemsRepository)
        {
            this.itemsRepository = itemsRepository;
        }

        public string GetLinkedItemUrl(int cwbId)
        {
            return itemsRepository.GetLinkedItemUrl(cwbId);
        }

        public void ExpandLinksInText(string cmsRootId, bool includeDescendants)
        {
            itemsRepository.ExpandLinksInText(cmsRootId, includeDescendants);
        }
    }
}
