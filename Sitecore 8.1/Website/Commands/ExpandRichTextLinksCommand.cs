using System;
using System.Collections.Specialized;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace GatherContent.Connector.Website.Commands
{
    using Managers.Interfaces;
    using WebControllers.IoC;

    public class ExpandRichTextLinksCommand : Command
    {

        public override CommandState QueryState(CommandContext context)
        {
            var item = context.Items[0];

            return item.Paths.FullPath.ToLower().StartsWith("/sitecore/content") ? base.QueryState(context) : CommandState.Hidden;
        }
        public override void Execute(CommandContext context)
        {
            try
            {
                Assert.ArgumentNotNull(context, "context");

                if (context.Items.Length != 1)
                {
                    return;
                }
                var item = context.Items[0];
                
                var parameters = new NameValueCollection
                                     {
                                         { "id", item.ID.ToString() }, 
                                         { "title", item.Paths.Path }, 
                                         { "language", item.Language.ToString() }, 
                                         { "version", item.Version.ToString() }, 
                                         { "database", item.Database.Name }, 
                                         { "load", StringUtil.GetString(new[] { context.Parameters["load"] }) }, 
                                     };

                Context.ClientPage.Start(this, "Run", parameters);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex, typeof(ImportCommand));
                SheerResponse.Alert("Expand links failed. See log for details.");
            }
        }

        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            /*
            var id = args.Parameters["id"].Replace("{", "").Replace("}", "");
            var language = Language.Parse(args.Parameters["language"]);
            var version = args.Parameters["version"];
            var db = args.Parameters["database"];
            var title = args.Parameters["title"];
            var uri = "/sitecore modules/shell/gathercontent/import/import.html";
            var path = string.Format("{0}?id={1}&l={2}&v={3}&t={4}&db={5}", uri, id, language, version, title, db);
            */
            var linkManager = GCServiceLocator.Current.GetInstance<ILinkManager>();

            linkManager.ExpandLinksInText(args.Parameters["id"], true);
            
            Context.ClientPage.ClientResponse.Broadcast(Context.ClientPage.ClientResponse.Alert("Links expanded!"), "Shell");
        }
    }
}