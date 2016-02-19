using System;
using System.Collections.Generic;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.IRepositories.Models.Import
{
    public class LocationMappingResultModel 
    {
        //public LocationMappingResultModel(GCItem item, List<ImportCMSField> fields, string template, string cmsTemplate, 
        //    string cmsId = "", string message = "", bool isImportSuccessful = true) 
        //{      
        //    GCItemId = item.Id.ToString();
        //    Status = item.Status.Data;
        //    Title = item.Name;
        //    GCTemplate = template;
        //    CMSTemplateId = cmsTemplate;
        //    Fields = fields;
        //    CMSId = cmsId;
        //    Message = message;
        //    IsImportSuccessful = isImportSuccessful;
        //}
        public string DefaultLocation { get; set; }
    }

    public static class MyExtensions
    {
        public static string WordCount(this MappingResultModel model)
        {
            return "";
        }
    } 
}
