namespace GatherContent.Connector.IRepositories.Models.Import
{
    public class MappingFieldModel
    {
        public string CMSField { get; set; }

        public string GCField { get; set; }

        public MappingFieldModel(string cmsField, string gcField)
        {
            CMSField = cmsField;
            GCField = gcField;
        }
    }
}
