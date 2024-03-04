using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems
{
    using Core.Entities;

    public class MappingResultModel
    {
        public bool IsImportSuccessful { get; set; }

        public string Message { get; set; }

        public string CWBItemId { get; set; }

        public string CWBTemplate { get; set; }

        public string Title { get; set; }
        
        public CWBStatus Status { get; set; }
        
        public string CMSId { get; set; }

        public string CMSTemplateId { get; set; }
        public string CmsLink { get; set; }
        public string CwbLink { get; set; }

        public string DefaultLocation { get; set; }

        public List<ImportCMSField> Fields { get; set; }

        public MappingResultModel(CWBItem item, List<ImportCMSField> fields, string template, string cmsTemplate, string cmsId = "", string message = "", bool isImportSuccessful = true, string defaultLocation = null)
        {
            CWBItemId = item.Id.ToString();
            Status = item.Status.Data;
            Title = item.Name;
            CWBTemplate = template;
            CMSTemplateId = cmsTemplate;
            Fields = fields;
            CMSId = cmsId;
            Message = message;
            DefaultLocation = defaultLocation;
            IsImportSuccessful = isImportSuccessful;
        }
    }
}
