﻿using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
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

        public Item GetItemByPath(string path)
        {
            Item resultItem = null;
            if (!String.IsNullOrEmpty(path))
                resultItem = ContextDatabase.GetItem(path);

            return resultItem;
        }

        protected IEnumerable<Item> GetAllProjects()
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var projects = accountSettingItem.Axes.SelectItems(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectTemplateName));

            return projects == null ? Enumerable.Empty<Item>() : projects.ToList();
        }

        protected IEnumerable<Item> GetAllMappings()
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var mappings = accountSettingItem.Axes.SelectItems(String.Format("./descendant::*[@@templatename='{0}']", Constants.TemplateMappingName));

            return mappings == null ? Enumerable.Empty<Item>() : mappings.ToList(); 
        }

        /// <summary>
        /// Get project folder by gatherContent Id
        /// </summary>
        /// <param name="projectId">GC project Id</param>
        /// <returns>Project folder Item</returns>
        protected Item GetProject(string projectId)
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var projects = accountSettingItem.Axes.SelectItems(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectTemplateName));
            var project = projects.FirstOrDefault(i => i["Id"] == projectId);
            return project;
        }
    }
}
