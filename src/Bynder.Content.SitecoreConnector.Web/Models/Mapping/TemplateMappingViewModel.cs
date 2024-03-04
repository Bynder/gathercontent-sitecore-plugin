
using System;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;

namespace Bynder.Content.SitecoreConnector.Web.Models.Mapping
{
    public class TemplateMappingViewModel
    {
        public TemplateMappingViewModel()
        {

        }
        public TemplateMappingViewModel(MappingModel mapping)
        {
            CwbProjectName = mapping.CwbProject.Name;
            CwbTemplateId = mapping.CwbTemplate.Id;
            CwbTemplateName = mapping.CwbTemplate.Name;
            IsRelated = mapping.IsRelated;
            ScTemplateName = mapping.CmsTemplate.Name;
            MappingGroupId = mapping.MappingGroupId;
            ScMappingId = mapping.MappingId;
            MappingTitle = mapping.MappingTitle;
            LastMappedDateTime = mapping.LastMappedDateTime;
            LastUpdatedDate = mapping.LastUpdatedDate;
            IsMapped = mapping.LastMappedDateTime != "never";
            RemovedFromCwb = mapping.LastUpdatedDate == "Removed from ContentWorkflow";

            if (IsMapped)
            {
                DateTime lastMappedDate;
                DateTime.TryParseExact(mapping.LastMappedDateTime, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out lastMappedDate);

                DateTime lastUpdateDate;
                DateTime.TryParseExact(mapping.LastUpdatedDate, "dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out lastUpdateDate);

                IsHighlightingDate = lastMappedDate < lastUpdateDate;
            }
        }

        public bool IsRelated { get; set; }
        public string MappingGroupId { get; set; }
        public string CwbProjectName { get; set; }
        public string CwbTemplateId { get; set; }
        public string CwbTemplateName { get; set; }
        public string ScTemplateName { get; set; }
        public string MappingTitle { get; set; }
        public string LastMappedDateTime { get; set; }
        public string LastUpdatedDate { get; set; }
        public string ScMappingId { get; set; }
        public bool IsMapped { get; set; }
        public bool RemovedFromCwb { get; set; }
        public bool IsHighlightingDate { get; set; }
    }
}
