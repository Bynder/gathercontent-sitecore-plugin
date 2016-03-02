using System.Diagnostics;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using GatherContent.Connector.WebControllers.Controllers;

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
            Debugger.Break();

            container.Register(Classes.FromThisAssembly()
                .BasedOn<BaseController>()
                .LifestyleTransient()
                );

            var controllerFactory = new WindsorControllerFactory(container.Kernel);
            System.Web.Mvc.ControllerBuilder.Current.SetControllerFactory(controllerFactory);
        }
    }
}
