using System;
using System.Collections.Generic;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.ImportItems;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;

namespace GatherContent.Connector.Website.Controllers
{
    public class ImportController : SitecoreController
    {
        private readonly ImportManager _importManager;

        public ImportController()
        {
            _importManager = new ImportManager();
        }


        public string Get(string id, string projectId)
        {
            try
            {
                SelectItemsForImportModel result = _importManager.GetModelForSelectImportItemsDialog(id, projectId);
                var model = JsonConvert.SerializeObject(result);
                return model;
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
            }
            return null;
        }



        [HttpPost]
        public ActionResult ImportItems(string id, string projectId, string statusId, List<string> items)
        {
            try
            {
                ImportResultModel result = _importManager.ImportItems(id, items, projectId, statusId);

                return Json(result, JsonRequestBehavior.AllowGet); 
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message }, JsonRequestBehavior.AllowGet);
            }
            return null;
        }

    }
}