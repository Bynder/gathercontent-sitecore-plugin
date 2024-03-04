using System;
using System.Net;
using Bynder.Content.SitecoreConnector.Managers.Interfaces;
using Newtonsoft.Json;

namespace Bynder.Content.SitecoreConnector.Web.Controllers
{
    using Sitecore.Abstractions;

    public class DropTreeController : BaseController
    {
        public BaseLog Log { get; }
        protected IDropTreeManager DropTreeManager;

        public DropTreeController(IDropTreeManager dropTreeManager, BaseLog log)
        {
            Log = log;
            this.DropTreeManager = dropTreeManager;
        }

        public string GetTopLevelNode(string id)
        {
            try
            {
                var result = DropTreeManager.GetTopLevelNode(id);
                var model = JsonConvert.SerializeObject(result);
                return model;
            }
            catch (WebException exception)
            {
                Log.Error("Bynder.Content message: " + exception.Message, exception, this);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("Bynder.Content message: " + exception.Message, exception, this);
                return exception.Message;
            }

        }

        public string GetChildren(string id)
        {
            try
            {
                var result = DropTreeManager.GetChildrenNodes(id);
                var model = JsonConvert.SerializeObject(result);
                return model;
            }
            catch (WebException exception)
            {
                Log.Error("Bynder.Content message: " + exception.Message, exception, this);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("Bynder.Content message: " + exception.Message, exception, this);
                return exception.Message;
            }
        }
    }
}
