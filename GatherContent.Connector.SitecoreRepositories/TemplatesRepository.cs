using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Mapping;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.SecurityModel;
using Template = GatherContent.Connector.Entities.Entities.Template;
using TemplateField = Sitecore.Data.Templates.TemplateField;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class TemplatesRepository : BaseSitecoreRepository, ITemplatesRepository
    {
        public TemplatesRepository() : base() { }


        #region Utilities

      

        private IEnumerable<Item> GetTemplates(string id)
        {
            var item = GetItem(id);
            return item != null ? item.Axes.GetDescendants().Where(t => t.TemplateName == "Template").ToList() : null;
        }

        private IEnumerable<TemplateField> GetFields(Item template)
        {
            return TemplateManager.GetTemplate(template.ID, ContextDatabase).GetFields();
        }


        #endregion


      

        public void CreateTemplate(string projectId, Template template)
        {
            var project = GetProject(projectId);
            var templatesFolder = project.Children.FirstOrDefault(item => item.Name == Constants.TemplatesFolderName);
            if (templatesFolder != null)
            {
                var folders = templatesFolder.Axes.GetDescendants().Select(item => item.Name).ToList();
                if (!folders.Contains(template.Name))
                {
                    using (new SecurityDisabler())
                    {
                        var scTemplate = ContextDatabase.GetTemplate(new ID(Constants.GcTemplate));
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


        public List<CmsTemplate> GetTemplatesModel(string id)
        {
            var model = new List<CmsTemplate>();
            var scTemplates = GetTemplates(id);
            foreach (var scTemplate in scTemplates)
            {
                var sitecoreTemplate = new CmsTemplate
                {
                    CmsTemplateName = scTemplate.Name,
                    CmsTemplateId = scTemplate.ID.ToString()
                };

                var fields = GetFields(scTemplate);
                sitecoreTemplate.CmsFields.AddRange(
                    from f in fields
                    where !f.Name.StartsWith("__")
                    select new CmsField
                    {
                        CmsFieldName = f.Name,
                        CmsFieldId = f.ID.ToString()
                    });

                model.Add(sitecoreTemplate);
            }
            return model;
        }
    }
}
