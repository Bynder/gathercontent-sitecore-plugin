using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.IRepositories.Models
{
    public class ImportItemsResponseModel
    {
        public string GCItemId { get; set; }

        public Status Status { get; set; }

        public string Title { get; set; }

        public string GCTemplate { get; set; }


        public bool IsImportSuccessful { get; set; }
        
        public string Message { get; set; }


        public List<ImportCMSFiled> Fields { get; set; }

        public string CMSTemplate { get; set; }

        public ImportItemsResponseModel(string itemId, Status status, string title, string template, string message, bool isImportSuccessful, List<ImportCMSFiled> fields, string cmsTemplate)
        {
            GCItemId = itemId;
            Status = status;
            Title = title;
            GCTemplate = template;
            CMSTemplate = cmsTemplate;
            Fields = fields;

            Message = message; 
            IsImportSuccessful = isImportSuccessful;
        }
    }
}
