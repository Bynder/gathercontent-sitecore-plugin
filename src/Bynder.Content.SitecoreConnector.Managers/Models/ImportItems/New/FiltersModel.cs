using System.Collections.Generic;
using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems.New
{
    public class FiltersModel
    {
        public FiltersModel()
        {
            Projects = new List<CwbProjectModel>();
            Templates = new List<CwbTemplateModel>();
            Statuses = new List<CwbStatusModel>();
        }
        public CwbProjectModel CurrentProject { get; set; }

        public List<CwbProjectModel> Projects { get; set; }

        public List<CwbTemplateModel> Templates { get; set; }

        public List<CwbStatusModel> Statuses { get; set; }
    }
}
