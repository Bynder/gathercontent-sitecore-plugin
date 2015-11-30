using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class ImportResultItemModel
    {
        public string Id { get; set; }

        public bool IsImportSuccessful { get; set; }

        public string ResultMessage { get; set; }

        public Status Status { get; set; }

        public string Title { get; set; }

        public Template Template { get; set; }

        public ImportResultItemModel(GCItem item, Template template, bool isImportSuccessful, string resultMessage)
        {
            Id = item.Id.ToString();
            IsImportSuccessful = isImportSuccessful;
            ResultMessage = resultMessage;
            Status = item.Status.Data;
            Template = template;
            Title = item.Name;
        }
    }
}
