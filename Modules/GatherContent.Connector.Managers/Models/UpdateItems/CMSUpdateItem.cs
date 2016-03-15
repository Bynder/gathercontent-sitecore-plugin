using System;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class CMSUpdateItem
    {
        public string CMSId { get; set; }

        public string Title { get; set; }

        public string CMSTemplate { get; set; }

        public string GCItemId { get; set; }

        public DateTime LastUpdatedTime { get; set; }

        public CMSUpdateItem(string cmsId, string title, string cmsTemplate, string gcItemId, DateTime lastUpdatedTime)
        {
            CMSId = cmsId;
            Title = title;
            CMSTemplate = cmsTemplate;
            GCItemId = gcItemId;
            LastUpdatedTime = lastUpdatedTime;
        }
    }
}
