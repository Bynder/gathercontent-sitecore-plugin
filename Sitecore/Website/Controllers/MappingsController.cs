using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Website.Models;
using System;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Services.Infrastructure.Web.Http;
using Sitecore.Web;

namespace GatherContent.Connector.Website.Controllers
{
    public class MappingsController : ServicesApiController
    {
        public List<MappingModel> Get(string id)
        {
            var model = new List<MappingModel>();

            var db = Sitecore.Configuration.Factory.GetDatabase("master");
            var item = db.GetItem(id);
            //var mappings = item.Axes.GetDescendants().Where(i => i.TemplateName == "GC Template Mapping");
            var mappings = item.Axes.GetDescendants().Where(i => i.TemplateName == "GC Project");


            foreach (var mapping in mappings)
            {
                model.Add(new MappingModel
                {
                    GcProjectName = mapping.Name,
                    GcTemplateName = "",
                    ScTemplateName = "",
                    LastMappedDateTime = DateTime.Now,
                    LastUpdatedDate = DateTime.Now
                });
            }

            return model;

        }
    }

}

