using System;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Models;
using GatherContent.Connector.IRepositories.Models.Update;

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

        public ItemModel(GCItem item, Template template, CMSUpdateItem cmsItem, string dateFormat)
        {
            Checked = false;

            Id = item.Id.ToString();
            Title = item.Name;
            Status = item.Status.Data;
            LastUpdatedInCMS = GetLastUpdatedInGC(cmsItem.LastUpdatedTime, dateFormat);
            LastUpdatedInGC = GetLastUpdatedInGC(item.Updated.Date, dateFormat);

            Template = template;
        }

        private string GetLastUpdatedInGC(DateTime dtEvent, string format)
        {
            return dtEvent.ToString(format);
        }
    }
}
