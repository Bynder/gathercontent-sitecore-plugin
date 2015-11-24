using System.Collections.Generic;

namespace GatherContent.Connector.Website.Models.Template
{
    public class TemplateMappingModel
    {

        public TemplateMappingModel()
        {
            Projects = new List<GcProjectModel>();
            Selected = new List<int>();
        }
     

        public List<GcProjectModel> Projects { get; set; }
        public List<int> Selected { get; set; }
    }

    public class GcProjectModel
    {
        public GcProjectModel()
        {
            Templates = new List<GcTemplateModel>();
        }

        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        public List<GcTemplateModel> Templates { get; set; }
    }
    public class GcTemplateModel
    {
        public string TemplateName { get; set; }
        public int TemplateId { get; set; }
    }
}