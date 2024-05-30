using System.Collections.Generic;

using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;
using Bynder.Content.SitecoreConnector.Web.Models.Import;

namespace Bynder.Content.SitecoreConnector.Web.Models.Mapping
{
    public class TemplateMapViewModel
    {
        public TemplateMapViewModel()
        {
            SitecoreTemplates = new List<SitecoreTemplateViewModel>();
            Rules = new Dictionary<string, string>();
            CwbProjects = new List<ProjectViewModel>();
            SelectedFields = new List<FieldMappingViewModel>();
        }

        public string CwbProjectName { get; set; }
        public string CwbProjectId { get; set; }
        public string CwbTemplateName { get; set; }
        public List<SitecoreTemplateViewModel> SitecoreTemplates { get; set; }
        public List<SitecoreTemplateField> OptionsContentFolders { get; set; }
        public List<SitecoreTemplateField> OptionsTemplates { get; set; }

        public List<ProjectViewModel> CwbProjects { get; set; }
        public AddMappingViewModel AddMappingModel { get; set; }
        public Dictionary<string, string> Rules { get; set; }
        public string ScMappingId { get; set; }
        public List<FieldMappingViewModel> SelectedFields { get; set; }
        public bool IsEdit { get; set; }
    }
}
