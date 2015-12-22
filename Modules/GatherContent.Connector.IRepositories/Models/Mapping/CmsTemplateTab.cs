using System.Collections.Generic;

namespace GatherContent.Connector.IRepositories.Models.Mapping
{

    public class CmsTemplateTab
    {
        public CmsTemplateTab()
        {
            Fields = new List<CmsTemplateField>();
        }

        public string TabName { get; set; }
        public List<CmsTemplateField> Fields { get; set; }
    }


}