using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Models.Update;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class UpdateListItem
    {
        public bool IsNeedToHighlightingDate { get; set; }

        public string CMSId { get; set; }

        public string GCId { get; set; }

        public bool Checked { get; set; }

        public GCStatus Status { get; set; }

        public string ProjectName { get; set; }

        public string Title { get; set; }

        public string LastUpdatedInGC { get; set; }

        public string LastUpdatedInCMS { get; set; }

        public GCTemplate Template { get; set; }

        public string CMSTemplate { get; set; }

        public UpdateListItem() { }

        public UpdateListItem(GCItem item, GCTemplate template, CMSUpdateItem cmsItem, string dateFormat, string projectName)
        {
            Checked = false;

            CMSId = cmsItem.CMSId;
            GCId = item.Id.ToString();
            Title = cmsItem.Title;
            Status = item.Status.Data;
            ProjectName = projectName;
            Template = template;
            CMSTemplate = cmsItem.CMSTemplate;

            IsNeedToHighlightingDate = cmsItem.LastUpdatedTime < item.Updated.Date;

            LastUpdatedInCMS = cmsItem.LastUpdatedTime.ToString(dateFormat);
            LastUpdatedInGC = item.Updated.Date.ToString(dateFormat);
        }
    }
}
