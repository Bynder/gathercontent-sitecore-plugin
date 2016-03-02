using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Models.Mapping;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITemplatesRepository : IRepository
    {
	    List<CmsTemplate> GetTemplatesModel(string id);
    }
}
