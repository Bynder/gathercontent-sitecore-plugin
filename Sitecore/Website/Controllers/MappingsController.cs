using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.Website.Extensions;
using GatherContent.Connector.Website.Managers;
using GatherContent.Connector.Website.Models.Mapping;
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



        public HttpResponseMessage Post(Connector.Managers.Models.Mapping.AddMappingModel model)
        {
            //var db = Sitecore.Configuration.Factory.GetDatabase("master");
            //var accountItem = db.GetItem(AccountItemId);

            //var manager = new SitecoreDataManager(accountItem.Database, accountItem.Language);
            //var gcSettings = GcAccountExtension.GetSettings(accountItem);
            //var service = new GatherContentService.GatherContentService(gcSettings.ApiUrl, gcSettings.Username,
            //    gcSettings.ApiKey);

            try
            {
                _mappingManager.PostMapping(model);
                var response = Request.CreateResponse(HttpStatusCode.OK, model);
                return response;
            }
            catch (Exception e)
            {
                //Add logging
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError, model);
                return response;
            }
            
        }
    }
}

