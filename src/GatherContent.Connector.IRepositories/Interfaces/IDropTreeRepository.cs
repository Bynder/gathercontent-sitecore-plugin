using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.Import;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    public interface IDropTreeRepository : IRepository
    {
        CmsItem GetHomeNode(string id);
        List<CmsItem> GetChildren(string id);
        string GetHomeNodeId();
        List<string> GetIdPath(string parent, string decendant);
    }
}
