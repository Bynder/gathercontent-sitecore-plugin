using System;
using System.Linq;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.HtmlControls;
using Version = Sitecore.Data.Version;

namespace GatherContent.Connector.Website.Shell
{
    public class Mappings : DialogForm
    {
        protected Memo ErrorText;
        protected Memo ResultText;
        protected Checklist Projects;
        protected Button AddMore;
        protected Listview ProjectTemplateMappings;

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");

            var id = WebUtil.GetQueryString("id");
            var language = Language.Parse(WebUtil.GetQueryString("language"));
            var version = WebUtil.GetQueryString("version");
            var item = Client.ContentDatabase.Items[id, language, Version.Parse(version)];
            var mappings = item.Axes.GetDescendants().Where(i => i.TemplateName == "GC Template Mapping");

            AddMore.Header = "Add more templates";

            foreach (var mapping in mappings)
            {
                // Create and add the new ListviewItem control to the Listview. 
                // We have to assign an unique ID to each control on the page. 
                var listItem = new ListviewItem();
                Context.ClientPage.AddControl(ProjectTemplateMappings, listItem);
                listItem.ID = Control.GetUniqueID("I");

                // Populate the list item with data. 
                listItem.Header = mapping.Name;
                listItem.Icon = mapping.Appearance.Icon;
                listItem.ColumnValues["path"] = mapping.Paths.Path;
                listItem.ColumnValues["language"] = mapping.Language.Name;
                listItem.ColumnValues["version"] = mapping.Version.ToString();
                listItem.ColumnValues["lastUpdatedGcDate"] = DateTime.Now;
                listItem.ColumnValues["buttons"] = mapping.Version.ToString();
            }  



            base.OnLoad(e);
        }

    }

}