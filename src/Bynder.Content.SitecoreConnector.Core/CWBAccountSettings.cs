namespace Bynder.Content.SitecoreConnector.Core
{
    public class CWBAccountSettings
    {
        public string ApiUrl { get; set; }

        public string Username { get; set; }

        public string ApiKey { get; set; }

        public string TemplatesRootFolderId { get; set; }

        public string OptionsContentFolderId { get; set; }

        public string OptionsTemplateId { get; set; }

        /// <summary>
        /// Date format used for datetime output on application's dialogs and forms
        /// </summary>
        public string DateFormat { get; set; }

        /// <summary>
        /// Format used to parse datetime fields
        /// </summary>
        public string DateTimeParseFormat { get; set; }

        public string AccountItemId { get; set; }

        public string GatherContentUrl { get; set; }

	    public string TenantName { get; set; }

		public string DropTreeHomeNode { get; set; }
    }
}
