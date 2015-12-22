using System;
using System.Collections.Generic;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.UpdateItems;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;

namespace GatherContent.Connector.Website.Controllers
{
    public class UpdateController : SitecoreController
    {
        private readonly UpdateManager _updateManager;

        public UpdateController()
        {
            _updateManager = new UpdateManager();
        }

        public string Get(string id)
        {
            try
            {
                SelectItemsForUpdateModel result = _updateManager.GetItemsForUpdate(id);
                var model = JsonConvert.SerializeObject(result);
                return model;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message, exception);
            }
            return null;
        }

        public string UpdateItems(string id, string statusId, List<UpdateListItem> items)
        {
            try
            {
                UpdateResultModel result = _updateManager.UpdateItems(id, items);
                var model = JsonConvert.SerializeObject(result);
                return model;
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message, exception);
            }
            return null;
        }
    }
}