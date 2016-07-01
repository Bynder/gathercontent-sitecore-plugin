using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using GatherContent.Connector.WebControllers.Controllers;
using Microsoft.Practices.ServiceLocation;

namespace GatherContent.Connector.WebControllers.IoC
{
    /// <summary>
    /// 
    /// </summary>
    public class Installer : IWindsorInstaller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="container"></param>
        /// <param name="store"></param>
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {

            var wServiceLocator = new WindsorServiceLocator(container);
            GCServiceLocator.SetLocatorProvider(() => wServiceLocator);

            container.Register(Classes.FromThisAssembly()
                .BasedOn<BaseController>()
                .LifestyleTransient()
                );

            //var controllerFactory = new WindsorControllerFactory(container.Kernel);
            //System.Web.Mvc.ControllerBuilder.Current.SetControllerFactory(controllerFactory);
        }
    }
}