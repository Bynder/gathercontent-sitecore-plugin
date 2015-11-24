using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.IRepositories.Interfaces
{
    public interface IStatusesRepository
    {
        void CreateStatus(string id, Status status);
    }
}
