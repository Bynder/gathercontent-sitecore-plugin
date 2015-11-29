using System;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class ProjectsRepository : BaseSitecoreRepository, IProjectsRepository
    {
        private void CreateProjectFolders(string id)
        {
            var names = new[] { Constants.TemplatesFolderName, Constants.MappingFolderName };
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

        public Item AddProjectFolder(string parentSitecoreId, Project project)
        {
            var parentItem = GetItem(parentSitecoreId);
            var projectsFolder = parentItem.Axes.SelectSingleItem(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectFolderTemplateName));
            if (projectsFolder != null)
            {
                var folders = projectsFolder.Axes.GetDescendants();
                if (!folders.Select(item => item.Name).ToList().Contains(project.Name))
                {
                    using (new SecurityDisabler())
                    {
                        var template = ContextDatabase.GetTemplate(new ID(Constants.GcProject));
                        var validFolderName = ItemUtil.ProposeValidItemName(project.Name);
                        var createdItem = projectsFolder.Add(validFolderName, template);
                        using (new SecurityDisabler())
                        {
                            createdItem.Editing.BeginEdit();
                            createdItem.Fields["Id"].Value = project.Id.ToString();
                            createdItem.Fields["Name"].Value = project.Name;
                            createdItem.Editing.EndEdit();
                        }

                        CreateProjectFolders(createdItem.ID.ToString());

                        return createdItem;
                    }
                }
                return folders.FirstOrDefault(item => item.Name == project.Name);
            }
            return null;
        }


        public Item GetProject(int projectId)
        {
            var accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            var project = accountSettingItem.Axes.SelectSingleItem(String.Format("./descendant::*[@@templatename='{0}' and @Id='{1}']", Constants.ProjectTemplateName, projectId));
            return project;
        }


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

        //public Item GetProjectFolderByName(string name)
        //{
        //    var parentItem = GetItem(Constants.AccountItemId);
        //    var projectsFolder = parentItem.Axes.SelectSingleItem(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectFolderTemplateName));
        //    if (projectsFolder != null)
        //    {
        //        return projectsFolder.Axes.GetDescendant(name);
        //    }
        //    return null;
        //}


    }
}
