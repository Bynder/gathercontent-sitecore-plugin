using System.Web.Http;
using Sitecore.Pipelines;

namespace GatherContent.Connector.Website
{
    public class RegisterHttpRoutes
    {
        public void Process(PipelineArgs args)
        {
            GlobalConfiguration.Configure(Configure);
        }

        protected void Configure(HttpConfiguration configuration)
        {
            var routes = configuration.Routes;
            routes.MapHttpRoute("MappingsCommand", "sitecore/api/mappings", new
            {
                controller = "Mappings",
                action = "Get"
            });

            routes.MapHttpRoute("ImportCommand", "sitecore/api/import", new
            {
                controller = "Import",
                action = "Get"
            });
        }
    }
}