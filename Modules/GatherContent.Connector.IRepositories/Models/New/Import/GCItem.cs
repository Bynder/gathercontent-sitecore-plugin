using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.New.Mapping;

namespace GatherContent.Connector.IRepositories.Models.New.Import
{
    public class GcItem
    {
        public string Id { get; set; }
        public GcTemplate Template { get; set; }

        public IList<GcField> Fields { get; set; }
    }
}
