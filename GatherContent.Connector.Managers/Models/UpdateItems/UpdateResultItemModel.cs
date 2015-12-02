using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.Managers.Models.UpdateItems
{
    public class UpdateResultItemModel
    {
        public string Id { get; set; }

        public bool IsImportSuccessful { get; set; }

        public string ResultMessage { get; set; }

        public GCStatus Status { get; set; }

        public string Title { get; set; }

        public GCTemplate Template { get; set; }

        public UpdateResultItemModel(GCItem item, GCTemplate template, bool isImportSuccessful, string resultMessage)
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
