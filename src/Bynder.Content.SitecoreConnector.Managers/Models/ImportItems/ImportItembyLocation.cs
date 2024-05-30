using System.Collections.Generic;
using System.Linq;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems
{
    using Core.Entities;

    public class ImportItembyLocation
    {
        public string Id { get; set; }

        public bool Checked { get; set; }

        public CWBStatus Status { get; set; }

        public string Title { get; set; }

        public string LastUpdatedInCwb { get; set; }

        public string Breadcrumb { get; set; }

        public CWBTemplate Template { get; set; }

        public List<AvailableMappingByLocation> Mappings { get; set; }

        public ImportItembyLocation()
        {
            Mappings = new List<AvailableMappingByLocation>();
        }

        public ImportItembyLocation(CWBItem item, CWBTemplate template, List<CWBItem> items, string dateFormat, IEnumerable<AvailableMappingByLocation> mappings)
        {
            Checked = false;

            Mappings = new List<AvailableMappingByLocation>();

            Id = item.Id.ToString();
            Title = item.Name;
            Status = item.Status.Data;
            Breadcrumb = GetBreadcrumb(item, items);
            LastUpdatedInCwb = item.Updated.Date.ToString(dateFormat);
            Mappings.AddRange(mappings);
            Template = template;
        }

        private string GetBreadcrumb(CWBItem item, List<CWBItem> items)
        {
            var names = new List<string>();
            string result = BuildBreadcrumb(item, items, names);
            return result;
        }

        private string BuildBreadcrumb(CWBItem item, List<CWBItem> items, List<string> names)
        {
            names.Add(item.Name);

            if (item.ParentId != 0)
            {
                CWBItem next = items.FirstOrDefault(i => i.Id == item.ParentId);
                return BuildBreadcrumb(next, items, names);
            }

            names.Reverse();

            string url = string.Join("/", names);

            return string.Format("/{0}", url);
        }

    }
}
