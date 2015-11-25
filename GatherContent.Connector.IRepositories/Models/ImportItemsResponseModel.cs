using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.IRepositories.Models
{
    public class ImportItemsResponseModel
    {
        public bool IsImportSuccessful { get; set; }

        public GCItem Item { get; set; }

        public string Message { get; set; }

        public ImportItemsResponseModel(GCItem item, string message, bool isImportSuccessful)
        {
            IsImportSuccessful = isImportSuccessful;
            Message = message;
            Item = item;
        }
    }
}
