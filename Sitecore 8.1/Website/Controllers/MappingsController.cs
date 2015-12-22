using System;
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
            var model = _mappingManager.GetMappingModel();
            return Json(model, JsonRequestBehavior.AllowGet);       
        }



        public ActionResult GetMapping(string id)
        {
            var model = _mappingManager.GetTemplateMappingModel(id);
            return Json(model, JsonRequestBehavior.AllowGet);   
        }



        public ActionResult Post(AddMappingModel model)
        {
            try
            {
                _mappingManager.PostMapping(model);
                return new EmptyResult();
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                return Json(new { status = "error", message = e.Message});
            }
            
        }

        public ActionResult Delete(string id)
        {
            try
            {              
                _mappingManager.DeleteMapping(id);
                return new EmptyResult();
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                return Json(new { status = "error", message = e.Message });
            }
        }
    }
}

