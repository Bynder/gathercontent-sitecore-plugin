using System.Diagnostics;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using GatherContent.Connector.Entities;
using GatherContent.Connector.GatherContentService.Interfaces;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.Managers.Interfaces;

namespace GatherContent.Connector.Managers.IoC
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

            //repositories (from config file)
            container.Install(Configuration.FromXmlFile("App_Config\\repositories.config"));

            //managers
            container.Register(Classes.FromThisAssembly()
                .BasedOn<IManager>()
                .WithServiceAllInterfaces()
                .LifestyleTransient()
			    );

            //services
            container.Register(Classes.FromAssemblyContaining<IService>()
                .BasedOn<IService>()
                .WithServiceAllInterfaces()
                .LifestyleTransient()
                );

            //GC account settings
            container.Register(Component.For<GCAccountSettings>()
                            .LifestylePerWebRequest()
                            .UsingFactoryMethod(kernel =>
                            {
                                var accountsRepository = kernel.Resolve<IAccountsRepository>();
                                try
                                {
	                                return accountsRepository.GetAccountSettings();
                                }
                                finally
                                {
                                    kernel.ReleaseComponent(accountsRepository);
                                }
                            }));
            
            //cache manager
            container.Register(Classes.FromThisAssembly()
                .BasedOn<ICacheManager>()
                .WithServiceAllInterfaces()
                .LifestyleTransient()
                );
	    }
    }
}
