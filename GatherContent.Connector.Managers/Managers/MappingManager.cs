using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.SitecoreRepositories;

namespace GatherContent.Connector.Managers.Managers
{
    public class MappingManager
    {
        private const string TEXT_TYPE = "Single-Line Text";
        private MappingRepository _mappingRepository { get; set; }

        public MappingManager()
        {
            _mappingRepository = new MappingRepository();
        }

        public List<ImportCMSItem> MappingItems(List<GCItem> items, string projectId)
        {
            List<MappingTemplateModel> templates = _mappingRepository.GetTemplateMappings(projectId);

            List<ImportCMSItem> result = MapItems(items, templates);

            return result;
        }

        private List<ImportCMSItem> MapItems(List<GCItem> items, List<MappingTemplateModel> templates)
        {
            var result = new List<ImportCMSItem>();
            foreach (GCItem gcItem in items)
            {
                MappingTemplateModel template = templates.FirstOrDefault(i => gcItem.TemplateId.ToString() == i.GCTemplate);
                ImportCMSItem cmsItem = MapItem(gcItem, template);
                result.Add(cmsItem);
            }

            return result;
        }

        private ImportCMSItem MapItem(GCItem item, MappingTemplateModel template)
        {
            List<Element> gcFields = item.Config.SelectMany(i => i.Elements).ToList();

            List<ImportCMSFiled> fields = MapFields(gcFields, template);

            var result = new ImportCMSItem(item, item.Name, template.CMSTemplate, fields);

            return result;
        }

        private List<ImportCMSFiled> MapFields(List<Element> gcFields, MappingTemplateModel template)
        {
            List<ImportCMSFiled> result = gcFields.Select(i => MapField(i, template)).ToList();
            return result;
        }

        private ImportCMSFiled MapField(Element gcField, MappingTemplateModel template)
        {
            string cmsFieldName = GetCMSFieldNameForGCField(template.Fields, gcField);
            var result = new ImportCMSFiled(TEXT_TYPE, cmsFieldName, gcField.Value);
            return result;
        }

        private string GetCMSFieldNameForGCField(List<MappingFieldModel> fields, Element gcField)
        {
            MappingFieldModel field = fields.FirstOrDefault(i => i.GCField == gcField.Name);
            return field != null ? field.CMSField : string.Empty;
        }

    }
}
