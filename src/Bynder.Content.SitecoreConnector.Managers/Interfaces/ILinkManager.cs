namespace Bynder.Content.SitecoreConnector.Managers.Interfaces
{
    public interface ILinkManager
    {
        string GetLinkedItemUrl(int cwbId);

        void ExpandLinksInText(string cmsRootId, bool includeDescendants);
    }
}
