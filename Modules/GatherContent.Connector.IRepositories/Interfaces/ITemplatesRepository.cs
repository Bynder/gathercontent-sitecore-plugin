using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    public interface ITemplatesRepository
    {
        void CreateTemplate(string projectId, GCTemplate template);
    }
}
