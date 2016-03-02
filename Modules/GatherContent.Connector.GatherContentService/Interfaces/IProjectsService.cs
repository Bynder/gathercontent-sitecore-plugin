using GatherContent.Connector.Entities.Entities;

namespace GatherContent.Connector.GatherContentService.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IProjectsService : IService
    {
	    ProjectsEntity GetProjects(int accountId);

	    StatusesEntity GetAllStatuses(string projectId);

	    StatusEntity GetSingleStatus(string statusId, string projectId);

	    ProjectEntity GetSingleProject(string projectId);


    }
}
