namespace Bynder.Content.SitecoreConnector.Core.Interfaces
{
    using System.Collections.Generic;

    using Models.Import;

    public interface IDropTreeRepository : IRepository
    {
        CmsItem GetHomeNode(string id);
        CmsItem GetOptionsContentFoldersNode();
        CmsItem GetOptionsTemplatesNode();
        List<CmsItem> GetChildren(string id, bool isTemplateRestriction);
        string GetHomeNodeId();
        List<string> GetIdPath(string parent, string decendant);
    }
}
