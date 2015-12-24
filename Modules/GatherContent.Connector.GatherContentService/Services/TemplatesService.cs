using System.Net;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services.Abstract;

namespace GatherContent.Connector.GatherContentService.Services
{
    public class TemplatesService : BaseService
    {
        protected override string ServiceUrl
        {
            get { return "templates"; }
        }

        public TemplatesService(GCAccountSettings accountSettings)
            : base(accountSettings)
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
