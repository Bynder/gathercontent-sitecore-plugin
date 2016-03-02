using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.UpdateItems;
using Newtonsoft.Json;
using Sitecore.Diagnostics;

namespace GatherContent.Connector.WebControllers.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class UpdateController : BaseController
    {
        protected IUpdateManager _updateManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="updateManager"></param>
        public UpdateController(IUpdateManager updateManager)
        {
            _updateManager = updateManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string Get(string id)
        {
            try
            {
                SelectItemsForUpdateModel result = _updateManager.GetItemsForUpdate(id);
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
        /// <param name="statusId"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public ActionResult UpdateItems(string id, string statusId, List<UpdateListIds> items)
        {
            try
            {
                UpdateResultModel result = _updateManager.UpdateItems(id, items);
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
    }
}