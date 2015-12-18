using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.TemplateModel;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;

namespace GatherContent.Connector.Website.Controllers
{
    public class TemplatesMappingController : SitecoreController
    { 
        private readonly TemplatesManager _templateManager;

        public TemplatesMappingController()
        {
            _templateManager = new TemplatesManager();
        }

        public ActionResult Get()
        {
            var model = _templateManager.GetTemplateMappingModel();
            return Json(model, JsonRequestBehavior.AllowGet);       
        }

        public HttpResponseMessage Post(TemplateMappingModel model)
        {
            var response = new HttpResponseMessage();
            try
            {
                _templateManager.PostTemplate(model);
                response.StatusCode = HttpStatusCode.OK;
                response.Content = new ObjectContent<TemplateMappingModel>(model, new JsonMediaTypeFormatter());
                return response;
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Content = new ObjectContent<TemplateMappingModel>(model, new JsonMediaTypeFormatter());
                return response;
            }
        }
    }
}

