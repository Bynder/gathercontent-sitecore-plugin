using System;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    /// <summary>
    /// 
    /// </summary>
    public class ProjectsRepository : BaseSitecoreRepository, IProjectsRepository
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        private void CreateProjectFolders(string id)
        {
            var names = new[] { Constants.MappingFolderName };
            foreach (var name in names)
            {
                using (new SecurityDisabler())
                {
                    var folder = GetItem(id);
                    var project = ContextDatabase.GetTemplate(new ID(Constants.GcFolderId));
                    var validFolderName = ItemUtil.ProposeValidItemName(name);
                    folder.Add(validFolderName, project);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public Item GetProject(int projectId)
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var project = accountSettingItem.Axes.SelectSingleItem(String.Format("./descendant::*[@@templatename='{0}' and @Id='{1}']", Constants.ProjectTemplateName, projectId));
            return project;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentSitecoreId"></param>
        /// <param name="project"></param>
        /// <returns></returns>
        public Item CreateOrGetProject(string parentSitecoreId, Project project)
        {
            var projectItem = GetProject(project.Id);

            if (projectItem == null)
            {
                var parentItem = GetItem(parentSitecoreId);

                using (new SecurityDisabler())
                {
                    var template = ContextDatabase.GetTemplate(new ID(Constants.GcProject));
                    var validFolderName = ItemUtil.ProposeValidItemName(project.Name);
                    projectItem = parentItem.Add(validFolderName, template);
                    using (new SecurityDisabler())
                    {
                        projectItem.Editing.BeginEdit();
                        projectItem.Fields["Id"].Value = project.Id.ToString();
                        projectItem.Fields["Name"].Value = project.Name;
                        projectItem.Editing.EndEdit();
                    }
                    CreateProjectFolders(projectItem.ID.ToString());
                }
            }
            return projectItem;
        }
    }
}
