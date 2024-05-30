using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Managers.Models.Mapping
{
    public class CwbTabModel
    {
        public CwbTabModel()
        {
            Fields = new List<CwbFieldModel>();
        }

        public string TabName { get; set; }
        public List<CwbFieldModel> Fields { get; set; }
    }
}
