using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.Mapping;
using Sitecore.Diagnostics;
using Sitecore.Services.Infrastructure.Web.Http;


namespace GatherContent.Connector.Website.Controllers
{
    public class MappingsController : ServicesApiController
    {
        private readonly MappingManager _mappingManager;

        public MappingsController()
        {
            _mappingManager = new MappingManager();
        }


        public List<MappingModel> Get()
        {
            var model = _mappingManager.GetMappingModel();
            return model;
        }



        public TemplateMapModel GetMapping(string id)
        {
            var model = _mappingManager.GetTemplateMappingModel(id);
            return model;
        }



        public HttpResponseMessage Post(AddMappingModel model)
        {
            try
            {
                _mappingManager.PostMapping(model);
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

