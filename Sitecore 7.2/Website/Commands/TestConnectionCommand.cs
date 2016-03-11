using System;
using System.Collections.Specialized;
using Sitecore;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using GatherContent.Connector.Managers.Managers;

namespace GatherContent.Connector.Website7.Commands
{
    public class TestConnectionCommand : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            var item = context.Items[0];

            return item.TemplateID == new ID("F8B571EE-59A4-4EA5-9B32-9C57720F87ED") ? base.QueryState(context) : CommandState.Hidden;
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
                                         { "language", item.Language.ToString() }, 
                                         { "version", item.Version.ToString() }, 
                                         { "load", StringUtil.GetString(new[] { context.Parameters["load"] }) }, 
                                     };

                Context.ClientPage.Start(this, "Run", parameters);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, ex, typeof(MappingsCommand));
                SheerResponse.Alert("Sync failed. See log for details.");
            }
        }

        protected void Run(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            if (!SheerResponse.CheckModified())
            {
                return;
            }

            //var testConnectionManager = new TestConnectionManager();
            //var uri = "/sitecore modules/shell/gathercontent/testconnection/testconnection.html";
            //var path = string.Format("{0}?success={1}", uri, testConnectionManager.TestConnection());
            //Context.ClientPage.ClientResponse.Broadcast(Context.ClientPage.ClientResponse.ShowModalDialog(path, "250", "100", "Test Connection", false), "Shell");
        }
    }
}