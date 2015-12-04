using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.UpdateItems;
using Sitecore.Diagnostics;
using Sitecore.Services.Infrastructure.Web.Http;

namespace GatherContent.Connector.Website.Controllers
{
    public class UpdateController : ServicesApiController
    {
        private readonly UpdateManager _updateManager;

        public UpdateController()
        {
            _updateManager = new UpdateManager();
        }

        public HttpResponseMessage Get(string id)
        {
            HttpResponseMessage response;
            try
            {
                SelectItemsForUpdateModel result = _updateManager.GetItemsForUpdate(id);
                response = Request.CreateResponse(HttpStatusCode.OK, result);
                return response;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message, exception);
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, exception.Message);
                return response;
            }
        }

        public HttpResponseMessage UpdateItems(string id, string statusId, List<UpdateListItem> items)
        {
            HttpResponseMessage response;
            try
            {
                UpdateResultModel result = _updateManager.UpdateItems(id, items);
                response = Request.CreateResponse(HttpStatusCode.OK, result);
                return response;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message, exception);
                response = Request.CreateResponse(HttpStatusCode.InternalServerError, exception.Message);
                return response;
            }
        }
    }
}