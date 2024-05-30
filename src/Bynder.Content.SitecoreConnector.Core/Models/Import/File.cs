namespace Bynder.Content.SitecoreConnector.Core.Models.Import
{
    using System;

    public class File
    {
        public string FieldId { get; set; }
        public string Url { get; set; }
        public string FileName { get; set; }
        public DateTime UpdatedDate { get; set; }
        public int FileId { get; set; }
    }
}
