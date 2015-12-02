using System.Collections.Generic;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.ImportItems;
using Sitecore.Services.Infrastructure.Web.Http;

namespace GatherContent.Connector.Website.Controllers
{
    public class ImportController : ServicesApiController
    {
        public SelectItemsForImportModel Get(string id, string projectId)
        {
            var importManager = new ImportManager();
            return importManager.GetModelForSelectImportItemsDialog(id, projectId);
        }

        public ImportResultModel ImportItems(string id, string projectId, string statusId, List<ImportListItem> items)
        {
            var importManager = new ImportManager();
            return importManager.ImportItems(id, items, projectId, statusId);
        }
    }
}