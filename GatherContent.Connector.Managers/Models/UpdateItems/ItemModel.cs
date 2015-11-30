using System;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Models;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class ItemModel
    {
        public string Id { get; set; }

        public bool Checked { get; set; }

        public Status Status { get; set; }

        public string Title { get; set; }

        public string LastUpdatedInGC { get; set; }

        public string LastUpdatedInCMS { get; set; }

        public Template Template { get; set; }

        public ItemModel()
        {
        }

        public ItemModel(GCItem item, Template template, CMSUpdateItem cmsItem)
        {
            Checked = false;

            Id = item.Id.ToString();
            Title = item.Name;
            Status = item.Status.Data;
            LastUpdatedInCMS = GetLastUpdatedInGC(cmsItem.LastUpdatedTime);
            LastUpdatedInGC = GetLastUpdatedInGC(item.Updated.Date);

            Template = template;
        }

        private string GetLastUpdatedInGC(DateTime dtEvent)
        {
            int intDays = DateTime.UtcNow.Day - dtEvent.Day;
            if (intDays > 0 && intDays < 2)
            {
                return String.Format("{0} - {1}", (intDays == 1) ? "Yesterday" : "Today", dtEvent.ToLongDateString());
            }

            return dtEvent.ToLongDateString();
        }
    }
}
