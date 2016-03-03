using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.New.Mapping;

namespace GatherContent.Connector.IRepositories.Models.New.Import
{
   public class CmsItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Language { get; set; }
        public CmsTemplate Template { get; set; }
        public IList<CmsField> Fields { get; set; }

    }
}
