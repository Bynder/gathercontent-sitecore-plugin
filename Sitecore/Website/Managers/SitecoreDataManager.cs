using System;
using System.Linq;
using GatherContent.Connector.Service.Entities;
using GatherContent.Connector.Website.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Item = Sitecore.Data.Items.Item;

namespace GatherContent.Connector.Website.Managers
{
    public class SitecoreDataManager
    {

        #region const

        private const string GcProject = "{AC94D4A6-A684-44EA-A97A-5A49128DB470}";
        private const string GcStatus = "{B2132F07-5488-4BEF-8C0D-300072C7D64C}";
        private const string GcTemplate = "{A7E151EC-BA54-4804-B282-AEAF6AF039A1}";
        private const string Folder = "{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}";
        private const string ProjectFolderTemplateName = "GC Projects Folder";
        private const string StatusFolderName = "GC Statuses";
        private const string TemplatesFolderName = "GC Templates";
        private const string MappingFolderName = "Mappings";

        #endregion



        private readonly Database _contextDatabase;
        private readonly Language _contextLanguage;



        public SitecoreDataManager(Database contextDatabase, Language contextLanguage)
        {
            _contextDatabase = contextDatabase;
            _contextLanguage = contextLanguage;
        }


        #region Utilities

        private void CreateProjectFolders(string id)
        {
            var names = new[] { StatusFolderName, TemplatesFolderName, MappingFolderName };
            foreach (var name in names)
            {
                using (new SecurityDisabler())
                {
                    var folder = GetItem(id);
                    var template = _contextDatabase.GetTemplate(new ID(Folder));
                    var validFolderName = ItemUtil.ProposeValidItemName(name);
                    folder.Add(validFolderName, template);
                }
            }
        }

        #endregion




        public Item GetItem(string sitecoreId)
        {
            Item resultItem = null;
            if (!String.IsNullOrEmpty(sitecoreId))
                resultItem = _contextDatabase.GetItem(sitecoreId, _contextLanguage);
            return resultItem;
        }


        public Item AddProjectFolder(string parentSitecoreId, Project project)
        {
            var parentItem = GetItem(parentSitecoreId);
            var projectsFolder = parentItem.Axes.SelectSingleItem(String.Format("./descendant::*[@@templatename='{0}']", ProjectFolderTemplateName));
            if (projectsFolder != null)
            {
                var folders = projectsFolder.Axes.GetDescendants();
                if (!folders.Select(item => item.Name).ToList().Contains(project.Name))
                {
                    using (new SecurityDisabler())
                    {
                        var template = _contextDatabase.GetTemplate(new ID(GcProject));
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

        public void CreateTemplate(string id, Template template)
        {
            var parentItem = GetItem(id);
            var templatesFolder = parentItem.Children.FirstOrDefault(item => item.Name == TemplatesFolderName);
            if (templatesFolder != null)
            {
                var folders = templatesFolder.Axes.GetDescendants().Select(item => item.Name).ToList();
                if (!folders.Contains(template.Name))
                {
                    using (new SecurityDisabler())
                    {
                        var scTemplate = _contextDatabase.GetTemplate(new ID(GcTemplate));
                        var validFolderName = ItemUtil.ProposeValidItemName(template.Name);
                        var createdItem = templatesFolder.Add(validFolderName, scTemplate);
                        using (new SecurityDisabler())
                        {
                            createdItem.Editing.BeginEdit();
                            createdItem.Fields["Temaplate Id"].Value = template.Id.ToString();
                            createdItem.Fields["Template Name"].Value = template.Name;
                            createdItem.Editing.EndEdit();
                        }
                    }
                }
            }
        }

        public void CreateTemplate(string id, GcTemplateModel gcTemplate)
        {
            var parentItem = GetItem(id);
            var statusesFolder = parentItem.Children.FirstOrDefault(item => item.Name == TemplatesFolderName);
            if (statusesFolder != null)
            {
                var folders = statusesFolder.Axes.GetDescendants().Select(item => item.Name).ToList();
                if (!folders.Contains(gcTemplate.TemplateName))
                {
                    using (new SecurityDisabler())
                    {
                        var template = _contextDatabase.GetTemplate(new ID(GcTemplate));
                        var validFolderName = ItemUtil.ProposeValidItemName(gcTemplate.TemplateName);
                        var createdItem = statusesFolder.Add(validFolderName, template);
                        using (new SecurityDisabler())
                        {
                            createdItem.Editing.BeginEdit();
                            createdItem.Fields["Temaplate Id"].Value = gcTemplate.TemplateId.ToString();
                            createdItem.Fields["Template Name"].Value = gcTemplate.TemplateName;
                            createdItem.Editing.EndEdit();
                        }

                        //return createdItem;
                    }
                }
            }
            //return null;
        }

        public void CreateStatus(string id, Status status)
        {
            var parentItem = GetItem(id);
            var statusesFolder = parentItem.Children.FirstOrDefault(item => item.Name == StatusFolderName);
            if (statusesFolder != null)
            {
                var folders = statusesFolder.Axes.GetDescendants().Select(item => item.Name).ToList();
                if (!folders.Contains(status.Name))
                {
                    using (new SecurityDisabler())
                    {
                        var template = _contextDatabase.GetTemplate(new ID(GcStatus));
                        var validFolderName = ItemUtil.ProposeValidItemName(status.Name);
                        var createdItem = statusesFolder.Add(validFolderName, template);
                        using (new SecurityDisabler())
                        {
                            createdItem.Editing.BeginEdit();
                            createdItem.Fields["Id"].Value = status.Id.ToString();
                            createdItem.Fields["Name"].Value = status.Name;
                            createdItem.Fields["Description"].Value = status.Description;
                            createdItem.Editing.EndEdit();
                        }

                        //return createdItem;
                    }
                }
            }
            //return null;
        }
    }
}