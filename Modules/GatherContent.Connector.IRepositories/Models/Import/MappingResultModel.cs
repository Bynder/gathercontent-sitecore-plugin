using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.IRepositories.Models.Import
{
    public class MappingResultModel
    {
        public bool IsImportSuccessful { get; set; }

        public string Message { get; set; }

        public string GCItemId { get; set; }

        public string GCTemplate { get; set; }

        public string Title { get; set; }
        
        public GCStatus Status { get; set; }
        
        public string CMSId { get; set; }

        public string CMSTemplateId { get; set; }
        public string CmsLink { get; set; }
        public string GcLink { get; set; }

        public List<ImportCMSField> Fields { get; set; }

        public MappingResultModel(GCItem item, List<ImportCMSField> fields, string template, string cmsTemplate, string cmsId = "", string message = "", bool isImportSuccessful = true)
        {      
            GCItemId = item.Id.ToString();
            Status = item.Status.Data;
            Title = item.Name;
            GCTemplate = template;
            CMSTemplateId = cmsTemplate;
            Fields = fields;
            CMSId = cmsId;
            Message = message;
            IsImportSuccessful = isImportSuccessful;
        }
    }
}
