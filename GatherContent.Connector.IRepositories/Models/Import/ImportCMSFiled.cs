namespace GatherContent.Connector.IRepositories.Models.Import
{
    public class ImportCMSFiled
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public ImportCMSFiled(string type, string name, string value)
        {
            Type = type;
            Name = name;
            Value = value;
        }
    }
}
