using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Helpers;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.Mapping;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;
using Sitecore.Services.Infrastructure.Web.Http;


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



        public HttpResponseMessage Post(AddMappingModel model)
        {
            var response = new HttpResponseMessage();
            try
            {
                _mappingManager.PostMapping(model);

                response.StatusCode = HttpStatusCode.OK;
                response.Content = new ObjectContent<AddMappingModel>(model, new JsonMediaTypeFormatter());
                return response;
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content = new StringContent(e.Message);
                return response;
            }
            
        }

        public HttpResponseMessage Delete(string id)
        {
            var response = new HttpResponseMessage();
            try
            {              
                _mappingManager.DeleteMapping(id);
                response.StatusCode = HttpStatusCode.OK;
                return response;
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content = new StringContent(e.Message);
                return response;
            }
        }
    }
}

