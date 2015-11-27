using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace GatherContent.Connector.SitecoreRepositories
{
    public abstract class BaseSitecoreRepository
    {
        protected readonly Database ContextDatabase;
        protected readonly Language ContextLanguage;

        protected BaseSitecoreRepository()
        {
            ContextDatabase = Sitecore.Configuration.Factory.GetDatabase("master");
            ContextLanguage = Sitecore.Context.Language;
        }

        public Item GetItem(string sitecoreId)
        {
            Item resultItem = null;
            if (!String.IsNullOrEmpty(sitecoreId))
                resultItem = ContextDatabase.GetItem(sitecoreId, ContextLanguage);

            return resultItem;
        }

        protected IEnumerable<Item> GetAllProjects()
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var projectsFolder = accountSettingItem.Axes.SelectSingleItem(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectFolderTemplateName));

            return projectsFolder.Axes.GetDescendants().Where(i => i.TemplateName == Constants.ProjectTemplateName).ToList();
        }

        protected List<Item> GetAllMappings()
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var projectsFolder = accountSettingItem.Axes.SelectSingleItem(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectFolderTemplateName));

            return projectsFolder.Axes.GetDescendants().Where(i => i.TemplateName == Constants.TemplateMappingName).ToList();
        }

        /// <summary>
        /// Get project folder by gatherContent Id
        /// </summary>
        /// <param name="projectId">GC project Id</param>
        /// <returns>Project folder Item</returns>
        protected Item GetProjectFolder(string projectId)
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var projectsFolder = accountSettingItem.Axes.SelectSingleItem(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectFolderTemplateName));
            var project = projectsFolder.Axes.GetDescendants().FirstOrDefault(i => i["Id"] == projectId);
            return project;
        }
    }
}
