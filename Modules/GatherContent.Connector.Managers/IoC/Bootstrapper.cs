using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using GatherContent.Connector.Managers.IoC;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Bootstrapper), "Initialize")]

[assembly: WebActivatorEx.ApplicationShutdownMethod(typeof(Bootstrapper), "Dispose")]

namespace GatherContent.Connector.Managers.IoC
{
    /// <summary>
    /// 
    /// </summary>
    public static class Bootstrapper
    {
        private static IWindsorContainer _container;

        /// <summary>
        /// 
        /// </summary>
        public static void Initialize()
        {
            _container = new WindsorContainer()
                .Install(
                    FromAssembly.InDirectory(new AssemblyFilter("bin").FilterByName(a => a.Name.StartsWith("GatherContent.Connector."))));
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Dispose()
        {
            _container.Dispose();
        }
    }
}