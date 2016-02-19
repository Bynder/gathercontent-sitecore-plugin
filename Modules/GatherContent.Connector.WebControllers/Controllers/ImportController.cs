using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.ImportItems;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;

namespace GatherContent.Connector.WebControllers.Controllers
{
    public class ImportController : SitecoreController
    {
        private readonly ImportManager _importManager;
        private readonly DropTreeManager _dropTreeManager;

        public ImportController()
        {
            _importManager = new ImportManager();
            _dropTreeManager = new DropTreeManager();
        }


        public string Get(string id, string projectId)
        {
            try
            {
                SelectItemsForImportModel result = _importManager.GetModelForSelectImportItemsDialog(id, projectId);
                var model = JsonConvert.SerializeObject(result);
                return model;
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message;
            }
            
        }

        public string GetMultiLocation(string id, string projectId)
        {
            try
            {
                SelectItemsForImportWithLocation result = _importManager.GetDialogModelWithLocation(id, projectId);
                var model = JsonConvert.SerializeObject(result);
                return model;
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message;
            }

        }


        [HttpPost]
        public ActionResult ImportItems(string id, string projectId, string statusId, string language, List<ImportItemModel> items)
        {
            try
            {
                ImportResultModel result = _importManager.ImportItems(id, items, projectId, statusId, language);
                return Json(result, JsonRequestBehavior.AllowGet); 
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult ImportItemsWithLocation(string id, string projectId, string statusId, string language, List<LocationImportItemModel> items)
        {
            try
            {
                ImportResultModel result = _importManager.ImportItemsWithLocation(id, items, projectId, statusId, language);
                return Json(new { status = "error", message =" Hello world" }, JsonRequestBehavior.AllowGet);
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public string GetTopLevelNode()
        {
            try
            {
                List<DropTreeModel> result = _dropTreeManager.GetTopLevelNode();
                var model = JsonConvert.SerializeObject(result);
                return model;
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message;
            }

        }

        public string GetChildrenAsJson(string id)
        {
            try
            {
                List<DropTreeModel> result = _dropTreeManager.GetChildrenNodes(id);
                var model = JsonConvert.SerializeObject(result);
                return model;
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message;
            }
        }

    }
}