using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    public interface IStatusesRepository : IRepository
    {
        void CreateStatus(string id, GCStatus status);
    }
}
