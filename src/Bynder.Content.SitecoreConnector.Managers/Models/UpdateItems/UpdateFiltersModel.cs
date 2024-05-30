using System.Collections.Generic;
using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;

namespace Bynder.Content.SitecoreConnector.Managers.Models.UpdateItems
{
    public class UpdateFiltersModel
    {
        public UpdateFiltersModel()
        {
            Projects = new List<CwbProjectModel>();
            Templates = new List<CwbTemplateModel>();
            Statuses = new List<CwbStatusModel>();
        }

        public List<CwbProjectModel> Projects { get; set; }

        public List<CwbTemplateModel> Templates { get; set; }

        public List<CwbStatusModel> Statuses { get; set; }

    }
}
