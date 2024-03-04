using System.Collections.Generic;
using Bynder.Content.SitecoreConnector.Managers.Models.ImportItems;

namespace Bynder.Content.SitecoreConnector.Managers.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDropTreeManager : IManager
    {
        List<DropTreeModel> GetTopLevelNode(string id);

        List<DropTreeModel> GetChildrenNodes(string id);
    }
}
