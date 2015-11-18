using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using GatherContent.Connector.Service.Entities;

namespace GatherContent.Connector.Website.Models.Import
{
    public class StatusModel
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Color { get; set; }

        public StatusModel(Status status)
        {
            if (status != null)
            {
                Id = status.Id;
                Title = status.Name;
                Color = status.Color;
            }
        }
    }
}