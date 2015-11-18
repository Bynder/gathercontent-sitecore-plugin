using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GatherContent.Connector.Service.Entities;

namespace GatherContent.Connector.Website.Models.Import
{
    public class ItemModel
    {
        public string Id { get; set; }

        public bool Checked { get; set; }

        public StatusModel Status { get; set; }

        public string Title { get; set; }

        public string LastUpdatedInGC { get; set; }

        public string LastImported { get; set; }

        public TemplateModel Template { get; set; }


        public ItemModel(Item item)
        {
            Checked = false;
            if (item != null)
            {
                Status = new StatusModel(item.Status.Data);
                Title = item.Name;
                Id = item.Id.ToString();
                LastUpdatedInGC = GetLastUpdatedInGC(item.Updated);
                LastImported = GetLastImportedDate(item.Id.ToString());
                if (item.TemplateId != null)
                    Template = new TemplateModel(item.TemplateId.Value.ToString());
                else
                {
                    Template = new TemplateModel("1111");
                }
            }
        }

        private string GetLastUpdatedInGC(ItemDate date)
        {
            string result = ElapsedTime(date.Date);

            return result;
        }

        public string GetLastImportedDate(string id)
        {
            string result = "Never";

            //Search

            return result;
        }

        public string ElapsedTime(DateTime dtEvent)
        {
            int intDays = DateTime.Now.Day - dtEvent.Day;
            if (intDays > 0 && intDays <= 1)
            {
                return String.Format("{0} - {1}", (intDays == 1) ? "Yesterday" : "Today", dtEvent.ToLongDateString());
            }
            else
            {
                return dtEvent.ToLongDateString();
            }
        }

    }
}