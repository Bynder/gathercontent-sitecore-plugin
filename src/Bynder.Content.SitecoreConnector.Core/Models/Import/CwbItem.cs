namespace Bynder.Content.SitecoreConnector.Core.Models.Import
{
    using System.Collections.Generic;

    using Mapping;

    public class CwbItem
    {
        public string Id { get; set; }
        public CwbTemplate Template { get; set; }

        public IList<CwbField> Fields { get; set; }
    }
}
