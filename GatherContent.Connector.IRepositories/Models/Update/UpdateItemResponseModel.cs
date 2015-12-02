using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Models.Import;

namespace GatherContent.Connector.IRepositories.Models.Update
{
    public class UpdateItemResponseModel
    {
        public string CMSId { get; set; }

        public string GCItemId { get; set; }

        public GCStatus Status { get; set; }

        public string Title { get; set; }

        public string GCTemplate { get; set; }


        public bool IsImportSuccessful { get; set; }
        
        public string Message { get; set; }


        public List<ImportCMSField> Fields { get; set; }

        public string CMSTemplate { get; set; }

        public UpdateItemResponseModel(string itemId, GCStatus status, string title, string template, string message, bool isImportSuccessful, List<ImportCMSField> fields, string cmsTemplate)
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
