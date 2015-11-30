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

            routes.MapHttpRoute("GetItemsForUpdate", "sitecore/api/getItemsForUpdate", new
            {
                controller = "Update",
                action = "Get",
                //id = RouteParameter.Optional,
                //projectId = RouteParameter.Optional
            });

            routes.MapHttpRoute("UpdateItems", "sitecore/api/update", new
            {
                controller = "Update",
                action = "UpdateItems"
            });

            routes.MapHttpRoute("MappingsCommand", "sitecore/api/mappings", new
            {
                controller = "Mappings",
                action = "Get"
            });
            routes.MapHttpRoute("SitecoreGatherContentMap", "sitecore/api/mapping/{id}", new
            {
                controller = "Mappings",
                action = "GetMapping",
                id = RouteParameter.Optional
            });
            routes.MapHttpRoute("PostMapping", "sitecore/api/postmappings", new
            {
                controller = "Mappings",
                action = "Post"
            });
            routes.MapHttpRoute("TemplatesCommand", "sitecore/api/templates", new
            {
                controller = "TemplatesMapping",
                action = "Get"
            });
            routes.MapHttpRoute("PostTemplateMapping", "sitecore/api/posttemplates", new
            {
                controller = "TemplatesMapping",
                action = "Post"
            });
            routes.MapHttpRoute("GetItemsForImport", "sitecore/api/getItemsForImport", new
            {
                controller = "Import",
                action = "Get"
            });

            routes.MapHttpRoute("ImportItems", "sitecore/api/import", new
            {
                controller = "Import",
                action = "ImportItems"
            });

        }
    }
}