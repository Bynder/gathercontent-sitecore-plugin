namespace Bynder.Content.SitecoreConnector.GatherContentService.Services
{
    using System.Net;

    using Interfaces;
    using Abstract;
    using Core.Entities;
    using Bynder.Content.SitecoreConnector.Core.Interfaces;

    public class TemplatesService : BaseService, ITemplatesService
    {
        protected override string ServiceUrl => "templates";

        public TemplatesService(IAccountsRepository accountsRepository) : base(accountsRepository)
        {
        }

        public TemplatesEntity GetTemplates(string projectId)
        {
            string url = string.Format("{0}?project_id={1}", ServiceUrl, projectId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            return ReadResponse<TemplatesEntity>(webrequest);
        }

        public TemplateEntity GetSingleTemplate(string templateId)
        {
            string url = string.Format("{0}/{1}", ServiceUrl, templateId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            return ReadResponse<TemplateEntity>(webrequest);
        }
    }
}
