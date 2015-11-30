using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.UpdateItems;
using Sitecore.Services.Infrastructure.Web.Http;

namespace GatherContent.Connector.Website.Controllers
{
    public class UpdateController : ServicesApiController
    {
        public SelectImportItemsModel Get(string id, string projectId)
        {
            var importManager = new UpdateManager();
            return importManager.GetItemsForUpdate(id, projectId);
        }

        public ImportResultModel UpdateItems(string id, string projectId, List<ItemModel> items)
        {
            var importManager = new UpdateManager();
            return importManager.UpdateItems(id, items, projectId);
        }
    }
}