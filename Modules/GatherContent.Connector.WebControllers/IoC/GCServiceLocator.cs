using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;

namespace GatherContent.Connector.WebControllers.IoC
{
    public static class GCServiceLocator
    {
        private static ServiceLocatorProvider currentProvider;

        /// <summary>
        /// The current ambient container.
        /// 
        /// </summary>
        public static IServiceLocator Current
        {
            get
            {
                return GCServiceLocator.currentProvider();
            }
        }

        /// <summary>
        /// Set the delegate that is used to retrieve the current container.
        /// 
        /// </summary>
        /// <param name="newProvider">Delegate that, when called, will return
        ///             the current ambient container.</param>
        public static void SetLocatorProvider(ServiceLocatorProvider newProvider)
        {
            GCServiceLocator.currentProvider = newProvider;
        }
    }
}
