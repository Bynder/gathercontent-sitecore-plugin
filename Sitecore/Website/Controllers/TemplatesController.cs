using System;
using System.Net;
using System.Net.Http;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.TemplateModel;
using Sitecore.Diagnostics;
using Sitecore.Services.Infrastructure.Web.Http;

namespace GatherContent.Connector.Website.Controllers
{
    public class TemplatesMappingController : ServicesApiController
    {
     
        private readonly TemplatesManager _templateManager;

        public TemplatesMappingController()
        {
            _templateManager = new TemplatesManager();
        }

        public TemplateMappingModel Get()
        {
            var model = _templateManager.GetTemplateMappingModel();
            return model;
        }

        public HttpResponseMessage Post(TemplateMappingModel model)
        {
            try
            {
                _templateManager.PostTemplate(model);
                var response = Request.CreateResponse(HttpStatusCode.OK, model);
                return response;
            }
            catch (Exception e)
            {
                Log.Error(e.Message, e);
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError, model);
                return response;
            }
        }
    }
}

