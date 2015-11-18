using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
using Sitecore;
using Sitecore.Data;
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

        private readonly JavaScriptSerializer _javaScriptSerializer = new JavaScriptSerializer();
        private Database _database;
        protected Memo ErrorText;
        protected Memo ResultText;
        protected Checklist Projects;
        protected Button AddMore;
        protected Listview ProjectTemplateMappings;
        protected Literal ConfigLiteral;

        public static class ParamNames
        {
            public const string ItemId = "itemId";
            public const string Database = "database";
            public const string Language = "language";
            public const string Version = "version";
        }


        #region Utilities

        protected Database GetContextDatabase(string database)
        {
            if (_database == null)
                _database = Context.Database;

            return _database;
        }


        protected void FillConfig()
        {
            var url = Sitecore.Context.Request.QueryString;
            var itemid = new ID(url["id"]);
            var version = Sitecore.Data.Version.Parse(url["v"]);
            var language = Language.Parse(url["l"]);
            string database = url["d"];

            var config = new Dictionary<string, string>
            {
                {ParamNames.ItemId, itemid.ToString()},
                {ParamNames.Database, GetContextDatabase(database).ToString()},
                {ParamNames.Language, language.ToString()},
                {ParamNames.Version, version.ToString()}
            };

            var configStr = _javaScriptSerializer.Serialize(config);
            ConfigLiteral.Text = configStr;
        }

        #endregion


        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");


            if (!Sitecore.Context.ClientPage.IsEvent)
            {
                FillConfig();
            }

            var id = WebUtil.GetQueryString("id");
            var language = Language.Parse(WebUtil.GetQueryString("language"));
            var version = WebUtil.GetQueryString("version");
            var item = Client.ContentDatabase.Items[id, language, Version.Parse(version)];
            //var mappings = item.Axes.GetDescendants().Where(i => i.TemplateName == "GC Template Mapping");
            var mappings = item.Axes.GetDescendants().Where(i => i.TemplateName == "GC Project");

            AddMore.Header = "Add more templates";

            foreach (var mapping in mappings)
            {
                // Create and add the new ListviewItem control to the Listview. 
                // We have to assign an unique ID to each control on the page. 
                var listItem = new ListviewItem();
                Context.ClientPage.AddControl(ProjectTemplateMappings, listItem);
                listItem.ID = Control.GetUniqueID("I");

                // Populate the list item with data. 
                //listItem.Header = mapping.Name;
                //listItem.Icon = mapping.Appearance.Icon;
                listItem.ColumnValues["gcProject"] = mapping.Paths.Path;
                listItem.ColumnValues["gcTemplate"] = mapping.Language.Name;
                listItem.ColumnValues["scTemplate"] = mapping.Version.ToString();
                listItem.ColumnValues["lastMapped"] = DateTime.Now;
                listItem.ColumnValues["lastUpdatedGcDate"] = DateTime.Now;
                listItem.ColumnValues["buttons"] = mapping.Version.ToString();
            }  

            base.OnLoad(e);
        }
    }
}