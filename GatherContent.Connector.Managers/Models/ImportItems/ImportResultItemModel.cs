using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.Managers.Models.ImportItems
{
    public class ImportResultItemModel
    {
        public bool IsImportSuccessful { get; set; }

        public Status Status { get; set; }
        
        public string Title { get; set; }

        public string ResultMessage { get; set; }

        public Template Template { get; set; }

    }
}
