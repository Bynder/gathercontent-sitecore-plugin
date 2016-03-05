using System.Collections.Generic;


namespace GatherContent.Connector.Managers.Models.Mapping
{
    public class TemplateTabModel
    {
        public TemplateTabModel()
        {
            Fields = new List<TemplateField>();
        }

        public string TabName { get; set; }
        public List<TemplateField> Fields { get; set; }
    }

    public class TemplateField
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
