namespace Bynder.Content.SitecoreConnector.Web
{
    using Bynder.Content.SitecoreConnector.Core.Interfaces;
    using Bynder.Content.SitecoreConnector.GatherContentService.Interfaces;
    using Bynder.Content.SitecoreConnector.GatherContentService.Services;
    using Bynder.Content.SitecoreConnector.Managers.Interfaces;
    using Bynder.Content.SitecoreConnector.Managers.Managers;
    using Bynder.Content.SitecoreConnector.SitecoreRepositories.Repositories;
    using Microsoft.Extensions.DependencyInjection;
    using Sitecore.Data.Items;
    using Sitecore.DependencyInjection;

    public class ServicesConfigurator : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(typeof(IAccountsRepository), typeof(AccountsRepository));
            serviceCollection.AddTransient(typeof(IMediaRepository<Item>), typeof(SimpleMediaRepository));
            serviceCollection.AddTransient(typeof(IItemsRepository), typeof(ItemsRepository));
            serviceCollection.AddTransient(typeof(IMappingRepository), typeof(MappingRepository));
            serviceCollection.AddTransient(typeof(IDropTreeRepository), typeof(DropTreeRepository));

            serviceCollection.AddTransient(typeof(IAccountsService), typeof(AccountsService));
            serviceCollection.AddTransient(typeof(IProjectsService), typeof(ProjectsService));
            serviceCollection.AddTransient(typeof(ITemplatesService), typeof(TemplatesService));
            serviceCollection.AddTransient(typeof(IItemsService), typeof(ItemsService));
            serviceCollection.AddTransient(typeof(ICacheManager), typeof(CacheManager));
            serviceCollection.AddTransient(typeof(ILogger), typeof(NullLogger));

            serviceCollection.AddTransient(typeof(IMappingManager), typeof(MappingManager));
            serviceCollection.AddTransient(typeof(ILinkManager), typeof(LinkManager));
            serviceCollection.AddTransient(typeof(IImportManager), typeof(ImportManager));
            serviceCollection.AddTransient(typeof(IDropTreeManager), typeof(DropTreeManager));
            serviceCollection.AddTransient(typeof(IUpdateManager), typeof(UpdateManager));

            serviceCollection.AddTransient(typeof(Controllers.DropTreeController));
            serviceCollection.AddTransient(typeof(Controllers.ImportController));
            serviceCollection.AddTransient(typeof(Controllers.MappingsController));
            serviceCollection.AddTransient(typeof(Controllers.UpdateController));
        }
    }
}