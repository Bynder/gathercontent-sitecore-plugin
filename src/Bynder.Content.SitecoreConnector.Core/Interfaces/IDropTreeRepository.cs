namespace Bynder.Content.SitecoreConnector.Core.Interfaces
{
    using System.Collections.Generic;

    using Models.Import;

    public interface IDropTreeRepository : IRepository
    {
        CmsItem GetHomeNode(string id);
        List<CmsItem> GetChildren(string id);
        string GetHomeNodeId();
        List<string> GetIdPath(string parent, string decendant);
    }
}
