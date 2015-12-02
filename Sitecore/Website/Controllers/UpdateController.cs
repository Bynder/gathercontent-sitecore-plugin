using System.Collections.Generic;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.Managers.Models.UpdateItems;
using Sitecore.Services.Infrastructure.Web.Http;

namespace GatherContent.Connector.Website.Controllers
{
    public class UpdateController : ServicesApiController
    {
        public SelectItemsForUpdateModel Get(string id)
        {
            var updateManager = new UpdateManager();
            return updateManager.GetItemsForUpdate(id);
        }

        public ImportResultModel UpdateItems(string id, string statusId, List<UpdateListItem> items)
        {
            var updateManager = new UpdateManager();
            return updateManager.UpdateItems(id, items);
        }
    }
}