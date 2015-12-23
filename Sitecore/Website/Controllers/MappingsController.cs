using System;
using System.Collections.Generic;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.Mapping;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;


namespace GatherContent.Connector.Website.Controllers
{
    public class MappingsController : SitecoreController
    {
        private readonly MappingManager _mappingManager;

        public MappingsController()
        {
            _mappingManager = new MappingManager();
        }


        public ActionResult Get()
        {
            try
            {
                var model = _mappingManager.GetMappingModel();
                return Json(model, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message }, JsonRequestBehavior.AllowGet);
            }
        }



        public ActionResult GetMapping(string id)
        {
            try
            {
                var model = _mappingManager.GetTemplateMappingModel(id);
                return Json(model, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                
                return Json(new { status = "error", message = exception.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult Post(List<TemplateTab> model, bool isEdit, string templateId, string selectedTemplateId)
        {
            if (templateId == null)
                return Json(new { status = "error", message = "GatherContent template isn't selected" }, JsonRequestBehavior.AllowGet);
            try
            {
                _mappingManager.PostMapping(model, isEdit, templateId, selectedTemplateId);
                return new EmptyResult();
            }
            catch (Exception e)
            {
                Log.Error("GatherContent message: " + e.Message + e.StackTrace, e);
                
                return Json(new { status = "error", message = e.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpDelete]
        public ActionResult Delete(string id)
        {
            try
            {
                _mappingManager.DeleteMapping(id);
                return new EmptyResult();
            }
            catch (Exception e)
            {
                Log.Error("GatherContent message: " + e.Message + e.StackTrace, e);
                
                return Json(new { status = "error", message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}

