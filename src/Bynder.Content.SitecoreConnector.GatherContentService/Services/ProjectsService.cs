namespace Bynder.Content.SitecoreConnector.GatherContentService.Services
{
    using System.Net;

    using Interfaces;
    using Abstract;
    using Core.DependencyInjection;
    using Core.Entities;
    using Bynder.Content.SitecoreConnector.Core.Interfaces;

    [Service(typeof(IProjectsService))]
    public class ProjectsService : BaseService, IProjectsService
    {
        protected override string ServiceUrl => "projects";

        public ProjectsService(IAccountsRepository accountsRepository) : base(accountsRepository)
        {
        }

        public ProjectsEntity GetProjects(int accountId)
        {
            string url = string.Format("{0}?account_id={1}", ServiceUrl, accountId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            return ReadResponse<ProjectsEntity>(webrequest);
        }

        public StatusesEntity GetAllStatuses(string projectId)
        {
            string url = string.Format("{0}/{1}/statuses", ServiceUrl, projectId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            return ReadResponse<StatusesEntity>(webrequest);
        }

        public StatusEntity GetSingleStatus(string statusId, string projectId)
        {
            string url = string.Format("{0}/{1}/statuses/{2}", ServiceUrl, projectId, statusId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            return ReadResponse<StatusEntity>(webrequest);
        }

        public ProjectEntity GetSingleProject(string projectId)
        {
            string url = string.Format("{0}/{1}", ServiceUrl, projectId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            return ReadResponse<ProjectEntity>(webrequest);
        }

        public void PostProject(string accountId, string textDirection, string name)
        {
            var data = string.Format("account_id={0}&text_direction={1}&name={2}", accountId, textDirection, name);
            WebRequest webrequest = CreateRequest(ServiceUrl);
            webrequest.Method = WebRequestMethods.Http.Post;

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }
    }
}
