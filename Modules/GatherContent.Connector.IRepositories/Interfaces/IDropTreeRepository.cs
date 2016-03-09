using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.New.Import;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    public interface IDropTreeRepository
    {
        List<CmsItem> GetHomeNode(string id);
        List<CmsItem> GetChildren(string id);
    }
}
