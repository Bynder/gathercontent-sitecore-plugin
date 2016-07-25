namespace GatherContent.Connector.Managers.Interfaces
{
    using System.Collections.Generic;

    public interface ILinkManager
    {
        List<string> GetLinkedItemsIds(int gcId);

        string GetLinkedItemUrl(int gcId);

        void ExpandLinksInText(string cmsRootId);
    }
}