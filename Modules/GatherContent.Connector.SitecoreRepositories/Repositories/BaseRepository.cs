using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.IRepositories.Interfaces;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseSitecoreRepository
    {
        protected readonly Database ContextDatabase;
        protected readonly Language ContextLanguage;
        
        /// <summary>
        /// 
        /// </summary>
        protected BaseSitecoreRepository()
        {
            ContextDatabase = Factory.GetDatabase("master");
            ContextLanguage = Context.Language;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sitecoreId"></param>
        /// <returns></returns>
        public Item GetItem(string sitecoreId)
        {
            var resultItem = GetItem(sitecoreId, ContextLanguage);
            return resultItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sitecoreId"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public Item GetItem(string sitecoreId, Language language)
        {
            Item resultItem = null;
            if (!String.IsNullOrEmpty(sitecoreId))
                resultItem = ContextDatabase.GetItem(sitecoreId, language);

            return resultItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Item GetItemByPath(string path)
        {
            Item resultItem = null;
            if (!String.IsNullOrEmpty(path))
                resultItem = ContextDatabase.GetItem(path);

            return resultItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sitecoreId"></param>
        /// <returns></returns>
        public TemplateItem GetItemTemplate(ID sitecoreId)
        {
            return ContextDatabase.GetTemplate(sitecoreId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected IEnumerable<Item> GetAllProjects()
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var projects = accountSettingItem.Axes.SelectItems(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectTemplateName));

            return projects == null ? Enumerable.Empty<Item>() : projects.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
            if (projects == null || projects.Count() <= 0) return null;
            var project = projects.FirstOrDefault(i => i["Id"] == projectId);
            return project;
        }
    }
}
