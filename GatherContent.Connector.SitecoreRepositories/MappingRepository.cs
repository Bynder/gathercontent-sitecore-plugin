using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models;
using Sitecore.Data.Items;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class MappingRepository : BaseSitecoreRepository, IMappingRepository
    {
        public MappingRepository() : base() { }

        public List<MappingTemplateModel> GetTemplateMappings(string projectId)
        {
            Item projectFolder = GetProjectFolder(projectId);
            Item mappingFolder = GetMappingFolder(projectFolder);
            IEnumerable<Item> mappings = GetTemplateMappings(mappingFolder);

            IEnumerable<MappingTemplateModel> result = ConvertSitecoreTemplatesToModel(mappings);

            return result.ToList();
        }

        private Item GetProjectFolder(string projectId)
        {
            Item accountSettingItem = ContextDatabase.GetItem(Constants.AccountItemId, ContextLanguage);
            Item[] projectsFolders = accountSettingItem.Axes.SelectItems(String.Format("./descendant::*[@@templatename='{0}']", Constants.ProjectsFolderName));
            Item projectFolder = projectsFolders.FirstOrDefault(i => i["Id"] == projectId);

            return projectFolder;
        }

        private Item GetMappingFolder(Item projectFolder)
        {
            Item mappingFolder = projectFolder.Children.FirstOrDefault(i => i.Template.Name == Constants.MappingFolderTemplateName);
            return mappingFolder;
        }

        private IEnumerable<Item> GetTemplateMappings(Item mappingFolder)
        {
            return mappingFolder.Children;
        }

        private IEnumerable<MappingTemplateModel> ConvertSitecoreTemplatesToModel(IEnumerable<Item> templates)
        {
            IEnumerable<MappingTemplateModel> result = templates.Select(ConvertSitecoreTemplateToModel);
            return result;
        }

        private MappingTemplateModel ConvertSitecoreTemplateToModel(Item template)
        {
            IEnumerable<MappingFieldModel> fields = ConvertSitecoreFieldsToModel(template.Children);
            var result = new MappingTemplateModel(template["Sitecore Template"], template["GC Template"], fields.ToList());

            return result;
        }

        private IEnumerable<MappingFieldModel> ConvertSitecoreFieldsToModel(IEnumerable<Item> fields)
        {
            IEnumerable<MappingFieldModel> result = fields.Select(ConvertSitecoreFieldToModel);
            return result;
        }

        private MappingFieldModel ConvertSitecoreFieldToModel(Item field)
        {
            var result = new MappingFieldModel(field["Sitecore Field"], field["GC Field"]);
            return result;
        }
    }
}
