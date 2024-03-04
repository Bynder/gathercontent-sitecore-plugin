namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems
{
    using Core.Entities;

    public class ImportResultItemModel
    {
        public string Id { get; set; }

        public bool IsImportSuccessful { get; set; }

        public string ResultMessage { get; set; }

        public CWBStatus Status { get; set; }

        public string Title { get; set; }

        public CWBTemplate Template { get; set; }

        public ImportResultItemModel(CWBItem item, CWBTemplate template, bool isImportSuccessful, string resultMessage)
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
