
namespace GatherContent.Connector.Website.Models.Import
{
    public class TemplateModel
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public TemplateModel(GatherContent.Connector.Entities.Entities.Template template)
        {
            if (template != null)
            {
                Id = template.Id.ToString();
                Title = template.Name;
            }
        }
    }
}