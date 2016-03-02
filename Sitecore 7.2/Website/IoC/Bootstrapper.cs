using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;

namespace GatherContent.Connector.Website7.IoC
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