using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.SecurityModel;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class TemplatesRepository : BaseSitecoreRepository, ITemplatesRepository
    {
        public TemplatesRepository() : base() { }

        public void CreateTemplate(string id, Template template)
        {
            var parentItem = GetItem(id);
            var templatesFolder = parentItem.Children.FirstOrDefault(item => item.Name == Constants.TemplatesFolderName);
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
    }
}
