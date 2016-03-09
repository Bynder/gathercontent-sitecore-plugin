using System;
using System.Net;
using GatherContent.Connector.Managers.Managers;
using Newtonsoft.Json;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;

namespace GatherContent.Connector.WebControllers.Controllers
{
    public class DropTreeController : SitecoreController
    {
        private readonly DropTreeManager _dropTreeManager;

        public DropTreeController()
        {
            _dropTreeManager = new DropTreeManager();
        }


        public string GetTopLevelNode(string id)
        {
            try
            {
                var result = _dropTreeManager.GetTopLevelNode(id);
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

        public string GetChildren(string id)
        {
            try
            {
                var result = _dropTreeManager.GetChildrenNodes(id);
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
    }
}
