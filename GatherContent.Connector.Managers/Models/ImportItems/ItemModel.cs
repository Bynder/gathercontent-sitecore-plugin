using System;
using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class ItemModel
    {
        public string Id { get; set; }

        public bool Checked { get; set; }

        public Status Status { get; set; }

        public string Title { get; set; }

        public string LastUpdatedInGC { get; set; }

        public string Breadcrumb { get; set; }

        public Template Template { get; set; }

        public ItemModel()
        {
        }

        public ItemModel(GCItem item, Template template, List<GCItem> items, string dateFormat)
        {
            Checked = false;

            Id = item.Id.ToString();
            Title = item.Name;
            Status = item.Status.Data;
            Breadcrumb = GetBreadcrumb(item, items);
            //Breadcrumb += Breadcrumb;
            //Breadcrumb += Breadcrumb;
            LastUpdatedInGC = GetLastUpdatedInGC(item.Updated.Date, dateFormat);

            Template = template;
        }

        private string GetLastUpdatedInGC(DateTime dtEvent, string format)
        {
            return dtEvent.ToString(format);
        }

        private string GetBreadcrumb(GCItem item, List<GCItem> items)
        {
            var names = new List<string>();
            string result = BuildBreadcrumb(item, items, names);
            return result;
        }

        private string BuildBreadcrumb(GCItem item, List<GCItem> items, List<string> names)
        {
            names.Add(item.Name);

            if (item.ParentId != 0)
            {
                GCItem next = items.FirstOrDefault(i => i.Id == item.ParentId);
                return BuildBreadcrumb(next, items, names);
            }

            names.Reverse();
            
            string url = string.Join("/", names);

            return string.Format("/{0}", url);
        }
    }
}
