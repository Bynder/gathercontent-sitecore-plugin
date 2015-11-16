using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GatherContent.Connector.Service.Services;
using GatherContent.Connector.Website.Extensions;
using GatherContent.Connector.Website.Managers;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Jobs;
using Sitecore.Web;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.HtmlControls;
using Version = Sitecore.Data.Version;

namespace GatherContent.Connector.Website.Shell
{
    public class Builder
    {
        private readonly List<Item> _items;
        private string _id;
        private string _version;
        private Language _language;

        public Builder(List<Item> items)
        {
            Assert.ArgumentNotNull(items, "items");
            _items = items;
        }

        public Builder(List<Item> list, string id, string version, Language language)
        {
            _items = list;
            _id = id;
            _version = version;
            _language = language;
        }

        protected void Build()
        {
            Job job = Context.Job;
            if (job == null) return;

            try
            {
                Log.Info("Sync - Update started", typeof(Builder));
                var sw = Stopwatch.StartNew();
                var item = Client.ContentDatabase.Items[_id, _language, Version.Parse(_version)];
                var manager = new SitecoreDataManager(item.Database, item.Language);
                var gcSettings = GcAccountExtension.GetSettings(item);
                var service = new GatherContentService(gcSettings.ApiUrl, gcSettings.Username, gcSettings.ApiKey);
                foreach (var scProject in _items)
                {
                    var project = Client.ContentDatabase.Items[scProject.ID.ToString(), _language, Version.Parse(_version)];
                    var projectId = project["Id"];
                    var statuses = service.GetAllStatuses(projectId);
                    foreach (var status in statuses.Data)
                    {
                        manager.CreateStatus(scProject.ID.ToString(), status);
                    }

                    var templates = service.GetTemplates(projectId);
                    foreach (var template in templates.Data)
                    {
                        manager.CreateTemplate(scProject.ID.ToString(), template);
                    }
                }
              
          
              //  var success = updateManager.Update();
                sw.Stop();

                Log.Info("Sync - Update finished: " + sw.Elapsed, typeof(Builder));

                //if (!success)
                //{
                //    job.Status.Failed = true;
                //    job.Status.Messages.Add("IntelligenceBank Sync process has failed.");
                //    job.Status.Messages.Add(updateManager.GetLastErrorMessage());
                //    job.Status.Messages.Add("Please check logs for details.");
                //}
            }
            catch (Exception ex)
            {
                Log.Error("Sync - Update Error", ex);
                job.Status.Failed = true;
                job.Status.Messages.Add(ex.ToString());
            }

            job.Status.State = JobState.Finished;
            job.Wait(2500);
        }
    }


    public class SyncBar : WizardForm
    {
        protected Memo ErrorText;
        protected Memo ResultText;
        protected Checklist Projects;

        protected override void ActivePageChanged(string page, string oldPage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(oldPage, "oldPage");
            base.ActivePageChanged(page, oldPage);

            NextButton.Header = "Next >";
            if (page == "Synchronization")
            {
                NextButton.Disabled = true;
                BackButton.Disabled = true;
                CancelButton.Disabled = true;
                Context.ClientPage.ClientResponse.Timer("StartSync", 100);
            }
        }

        protected override bool ActivePageChanging(string page, ref string newpage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(newpage, "newpage");
            if (page == "AddMoreTemplates" && newpage == "Synchronization")
            {
                newpage = "Synchronization";
                NextButton.Disabled = false;
                BackButton.Disabled = false;
                CancelButton.Disabled = false;
            }
            return base.ActivePageChanging(page, ref newpage);
        }




        protected virtual void CheckStatus()
        {
            var str = Context.ClientPage.ServerProperties["handle"] as string;
            Assert.IsNotNullOrEmpty(str, "handle");
            var job = JobManager.GetJob(Handle.Parse(str));
            if (job == null)
            {
                Active = "Retry";
                NextButton.Disabled = true;
                BackButton.Disabled = false;
                CancelButton.Disabled = false;
                ErrorText.Value = "Job has finished unexpectedly";
            }
            else if (job.Status.Failed)
            {
                Active = "Retry";
                NextButton.Disabled = true;
                BackButton.Disabled = false;
                CancelButton.Disabled = false;
                ErrorText.Value = StringUtil.StringCollectionToString(job.Status.Messages, Environment.NewLine);
            }
            else
            {
                string status = job.Status.State == JobState.Running
                    ? Translate.Text("Sync in progress, please wait.")
                    : Translate.Text("Queued.");
                if (job.IsDone)
                {
                    Active = "LastPage";
                    BackButton.Disabled = true;
                    ResultText.Value = job.Status.State.ToString();
                    var id = (string)job.Options.CustomData;

                    var reload = string.Format("item:load(id={0})", id);
                    Context.ClientPage.ClientResponse.Timer(reload, 1);
                    SheerResponse.Alert("The Sync process has completed. Please refresh the destination folder for the item to appear");
                }
                else
                {
                    Context.ClientPage.ClientResponse.SetInnerHtml("Status", status);
                    Context.ClientPage.ClientResponse.Timer("CheckStatus", 2000);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");


            var id = WebUtil.GetQueryString("id");
            var language = Language.Parse(WebUtil.GetQueryString("language"));
            var version = WebUtil.GetQueryString("version");
            var item = Client.ContentDatabase.Items[id, language, Sitecore.Data.Version.Parse(version)];

            
            var projects = item.Axes.GetDescendants().Where(i => i.TemplateName == "GC Project");
           
             
                foreach (var project in projects)
                {
                    var cli = new ChecklistItem
                    {
                        ID = Guid.NewGuid().ToString(),
                        Header = project.Name,
                        Value = project.ID.ToString(),
                    };
                    Context.ClientPage.AddControl(Projects, cli);
                }
            
         

            base.OnLoad(e);
        }


	



        protected virtual void StartSync()
        {
            var id = Context.Request.QueryString["id"];
            var language = Language.Parse(Context.Request.QueryString["la"]);
            var version = Context.Request.QueryString["v"];
            var item = Client.ContentDatabase.Items[id, language, Version.Parse(version)];


            var list = new List<Item>();

            var checkedItems = Projects.Items.Where(ci => ci.Checked);
            foreach (var checklistItem in checkedItems)
            {
                var i = Client.ContentDatabase.Items[checklistItem.Value, language, Version.Parse(version)];
                list.Add(i);
            }
          

            var options = new JobOptions("Sync", "gc", Client.Site.Name, new Builder(list, id, version, language), "Build")
            {
                AfterLife = TimeSpan.FromSeconds(1),
                ContextUser = Context.User,
                CustomData = id
            };
            var job = JobManager.Start(options);
            Context.ClientPage.ServerProperties["handle"] = job.Handle.ToString();
            Context.ClientPage.ClientResponse.Timer("CheckStatus", 2500);
        }
    }
}