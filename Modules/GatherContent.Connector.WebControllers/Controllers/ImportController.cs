using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.ImportItems;
using Newtonsoft.Json;
using Sitecore.Diagnostics;

namespace GatherContent.Connector.WebControllers.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class ImportController : BaseController
    {
        protected IImportManager _importManager;
        protected IDropTreeManager _dropTreeManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dropTreeManager"></param>
        /// <param name="importManager"></param>
        public ImportController(IDropTreeManager dropTreeManager, IImportManager importManager)
        {
            Debugger.Break();

            _importManager = importManager;
            _dropTreeManager = dropTreeManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public string Get(string id, string projectId)
        {
            Debugger.Break();

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectId"></param>
        /// <param name="statusId"></param>
        /// <param name="language"></param>
        /// <param name="items"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="statusId"></param>
        /// <param name="language"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ImportItemsWithLocation(string projectId, string statusId, string language, List<LocationImportItemModel> items)
        {
            try
            {
                ImportResultModel result = _importManager.ImportItemsWithLocation(items, projectId, statusId, language);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetTopLevelNode(string id)
        {
            try
            {
                List<DropTreeModel> result = _dropTreeManager.GetTopLevelNode(id);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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