using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.GatherContentService.Services.Abstract;

namespace GatherContent.Connector.GatherContentService.Services
{
    public class ItemsService : BaseService
    {
        protected override string ServiceUrl
        {
            get { return "items"; }
        }

        public ItemsService(GCAccountSettings accountSettings)
            : base(accountSettings)
        {
        }

        public ItemsEntity GetItems(string projectId)
        {
            string url = string.Format("{0}?project_id={1}", ServiceUrl, projectId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get; ;

            return ReadResponse<ItemsEntity>(webrequest);
        }

        public ItemEntity GetSingleItem(string itemId)
        {
            string url = string.Format("{0}/{1}", ServiceUrl, itemId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get; ;

            return ReadResponse<ItemEntity>(webrequest);
        }

        public void PostItem(string projectId, string name, string parentId = null, string templateId = null, Config config = null)
        {
            var data = new StringBuilder();
            data.Append(string.Format("project_id={0}&name={1}", projectId, name));

            if (!string.IsNullOrEmpty(parentId))
            {
                data.Append(string.Format("&parent_id={0}", parentId));
            }

            if (!string.IsNullOrEmpty(templateId))
            {
                data.Append(string.Format("&template_id={0}", templateId));
            }

            if (config != null)
            {
                var json = new JavaScriptSerializer().Serialize(config);
                data.Append(string.Format("&config={0}", json));
            }

            WebRequest webrequest = CreateRequest(ServiceUrl);
            webrequest.Method = WebRequestMethods.Http.Post;

            AddPostData(data.ToString(), webrequest);

            ReadResponse(webrequest);

        }

        public void SaveItem(string itemId, Config config = null)
        {
            var data = string.Empty;
            if (config != null)
            {
                var json = new JavaScriptSerializer().Serialize(config);
                data = string.Format("&config={0}", json);
            }

            string url = string.Format("{0}/{1}/save", ServiceUrl, itemId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Post;

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }

        public void ApplyTemplateToItem(string itemId, string templateId)
        {
            var data = string.Format("template_id={0}", templateId);
            string url = string.Format("{0}/{1}/apply_template", ServiceUrl, itemId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Post;

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }

        public void ChooseStatusForItem(string itemId, string statusId)
        {
            var data = string.Format("status_id={0}", statusId);
            string url = string.Format("{0}/{1}/choose_status", ServiceUrl, itemId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Post;

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }
    }
}
