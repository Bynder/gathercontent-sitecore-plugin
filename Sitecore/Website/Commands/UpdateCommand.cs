using System;
using System.Collections.Specialized;
using System.Linq;
using GatherContent.Connector.Service.Services;
using GatherContent.Connector.Website.Extensions;
using GatherContent.Connector.Website.Managers;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace GatherContent.Connector.Website.Commands
{
    public class UpdateCommand : Command
    {
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
                Log.Error(ex.Message, ex, typeof(UpdateCommand));
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

            string id = args.Parameters["id"];
            var language = Language.Parse(args.Parameters["language"]);
            string version = args.Parameters["version"];
            var item = Client.ContentDatabase.Items[id, language, Sitecore.Data.Version.Parse(version)];

            if (item == null)
            {
                SheerResponse.Alert("Item not found.", new string[0]);
            }
            else
            {


                #region Check projects

                var manager = new SitecoreDataManager(item.Database, item.Language);
                var gcSettings = GcAccountExtension.GetSettings(item);

                var service = new GatherContentService(gcSettings.ApiUrl, gcSettings.Username, gcSettings.ApiKey);
                var accounts = service.GetAccounts();

                var account = accounts.Data.FirstOrDefault();
                if (account != null)
                {
                    var projects = service.GetProjects(account.Id);
                    foreach (var project in projects.Data)
                    {
                        manager.AddProjectFolder(item.ID.ToString(), project);
                    }
                }


                #endregion


                if (args.IsPostBack)
                {
                    if (args.HasResult)
                    {
                        if (args.Parameters["load"] == "1")
                        {
                            Context.ClientPage.SendMessage(this, "item:load(id=" + args.Result + ")");
                        }
                        else
                        {
                            Context.ClientPage.SendMessage(this, "media:refresh");
                        }
                    }
                }
                else
                {
                    var uri = "/sitecore/shell/default.aspx?xmlcontrol=SyncBar";
                    var url = string.Format("{0}&id={1}&la={2}&v={3}", uri, id, language, version);

                    Context.ClientPage.ClientResponse.Broadcast(Context.ClientPage.ClientResponse.ShowModalDialog(url), "Shell");
                }

            }
        }
    }
}
