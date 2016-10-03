using System;
using System.Net;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.WebControllers.IoC;
using Newtonsoft.Json;
using Sitecore.Diagnostics;

namespace GatherContent.Connector.WebControllers.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class DropTreeController : BaseController
    {
        protected IDropTreeManager DropTreeManager;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dropTreeManager"></param>
        public DropTreeController()
        {
            //DropTreeManager = GCServiceLocator.Current.GetInstance<IDropTreeManager>();
            DropTreeManager = ServiceFactory.DropTreeManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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
