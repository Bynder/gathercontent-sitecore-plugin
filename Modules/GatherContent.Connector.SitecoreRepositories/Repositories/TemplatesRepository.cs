using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Mapping;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Template = GatherContent.Connector.Entities.Entities.GCTemplate;
using TemplateField = Sitecore.Data.Templates.TemplateField;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
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
                        CmsFieldId = f.ID.ToString(),
                        CmsFieldType = f.Type
                    });

                model.Add(sitecoreTemplate);
            }
            return model;
        }

    }
}
