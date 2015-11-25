using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.IRepositories.Models
{
    public class ImportCMSItem
    {
        public GCItem GCItem { get; set; }

        public string GCItemId { get; set; }

        public string Name { get; set; }

        public string CMSTemplate { get; set; }

        public List<ImportCMSFiled> Fields { get; set; }

        public ImportCMSItem(GCItem gcItem,  string name, string cmsTemplate, List<ImportCMSFiled> fields)
        {
            GCItem = gcItem;
            GCItemId = gcItem.Id.ToString();
            Name = name;
            CMSTemplate = cmsTemplate;
            Fields = fields;
        }
    }
}
