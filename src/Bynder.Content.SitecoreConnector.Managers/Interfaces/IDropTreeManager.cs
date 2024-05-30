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
        List<DropTreeModel> GetOptionsContentFoldersTopLevelNode();
        List<DropTreeModel> GetOptionsTemplatesTopLevelNode();

        List<DropTreeModel> GetChildrenNodes(string id, bool isTemplateRestriction);
    }
}
