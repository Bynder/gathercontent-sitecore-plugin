namespace Bynder.Content.SitecoreConnector.GatherContentService.Services
{
    using System.Net;
    using System.Text;
    using System.IO;

    using Newtonsoft.Json;
    
    using Interfaces;
    using Abstract;
    using Core.DependencyInjection;
    using Core.Entities;
    using Core.Interfaces;

    [Service(typeof(IItemsService))]
    public class ItemsService : BaseService, IItemsService
    {
        protected override string ServiceUrl => "items";


        public ItemsService(IAccountsRepository accountsRepository) : base(accountsRepository)
        {
        }

        public ItemsEntity GetItems(string projectId)
        {
            string url = string.Format("{0}?project_id={1}", ServiceUrl, projectId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            return ReadResponse<ItemsEntity>(webrequest);
        }

        public ItemEntity GetSingleItem(string itemId)
        {
            string url = string.Format("{0}/{1}", ServiceUrl, itemId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            return ReadResponse<ItemEntity>(webrequest);
        }

        public ItemFiles GetItemFiles(string itemId)
        {
            var url = string.Format("{0}/{1}/files", ServiceUrl, itemId);
            var webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            return ReadResponse<ItemFiles>(webrequest);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="name"></param>
        /// <param name="parentId"></param>
        /// <param name="templateId"></param>
        /// <param name="config"></param>
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
                var json = JsonConvert.SerializeObject(config);
                data.Append(string.Format("&config={0}", json));
            }

            WebRequest webrequest = CreateRequest(ServiceUrl);
            webrequest.Method = WebRequestMethods.Http.Post;

            AddPostData(data.ToString(), webrequest);

            ReadResponse(webrequest);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="config"></param>
        public void SaveItem(string itemId, Config config = null)
        {
            var data = string.Empty;
            if (config != null)
            {
                var json = JsonConvert.SerializeObject(config);
                data = string.Format("&config={0}", json);
            }

            string url = string.Format("{0}/{1}/save", ServiceUrl, itemId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Post;

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="templateId"></param>
        public void ApplyTemplateToItem(string itemId, string templateId)
        {
            var data = string.Format("template_id={0}", templateId);
            string url = string.Format("{0}/{1}/apply_template", ServiceUrl, itemId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Post;

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="statusId"></param>
        public void ChooseStatusForItem(string itemId, string statusId)
        {
            var data = string.Format("status_id={0}", statusId);
            string url = string.Format("{0}/{1}/choose_status", ServiceUrl, itemId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Post;

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }


        public Stream DownloadFile(int fileId)
        {
            string url = string.Format("files/{0}/download", fileId);
            WebRequest webrequest = CreateRequest(url);
            webrequest.Method = WebRequestMethods.Http.Get;

            var response = ReadBinaryResponse(webrequest);

            return response;
        }
    }
}