namespace Bynder.Content.SitecoreConnector.Core.Models.Import
{
    using System.Collections.Generic;

    /// <summary>
    /// 
    /// </summary>
    public class CmsField
    {
        public CmsTemplateField TemplateField { get; set; }

        public object Value { get; set; }

        //public List<string> Options => _Options.Values.ToList();

        public Dictionary<string, string> Options { get; set; }

        public List<File> Files { get; set; }
    }
}
