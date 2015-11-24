using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using GatherContent.Connector.Entities.Entities;
using Newtonsoft.Json;

namespace GatherContent.Connector.GatherContentService
{
    public class GatherContentService 
    {
        private static string _apiUrl; 
        private static string _userName;
        private static string _apiKey;

        public GatherContentService(string apiUrl,string userName, string apiKey)
        {
            _apiUrl = apiUrl;
            _apiKey = apiKey;
            _userName = userName;
        }

        #region Helper methods


        private static WebRequest CreateRequest(string url)
        {
            var webrequest = WebRequest.Create(_apiUrl + url) as HttpWebRequest;

            if (webrequest != null)
            {
                webrequest.Accept = "application/vnd.gathercontent.v0.5+json";
                webrequest.Headers.Add("Authorization", "Basic " +
                                                        GetBasicAuthToken(_userName, _apiKey));

                return webrequest;
            }
            return null;
        }

        private static string GetBasicAuthToken(string userName, string apiKey)
        {
            return Base64Encode(userName + ":" + apiKey);
        }


        private static string ReadResponse(WebRequest webrequest)
        {
            using (var responseStream = webrequest.GetResponse().GetResponseStream())
            {
                if (responseStream != null)
                {
                    using (var responseReader = new StreamReader(responseStream))
                    {
                        return responseReader.ReadToEnd();
                    }
                }
            }

            return null;
        }


        private static T ReadResponse<T>(WebRequest webrequest) where T : class
        {
            T result = null;
            using (var responseStream = webrequest.GetResponse().GetResponseStream())
            {
                if (responseStream != null)
                {
                    using (var responseReader = new StreamReader(responseStream))
                    {
                        var json = responseReader.ReadToEnd();
                        result = JsonConvert.DeserializeObject<T>(json);
                    }
                }
            }
            return result;
        }



        private static string Base64Encode(string s)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(s);
            return Convert.ToBase64String(bytes);
        }


        private static void AddPostData(string data, WebRequest webrequest)
        {
            var byteArray = Encoding.UTF8.GetBytes(data);
            webrequest.ContentType = "application/x-www-form-urlencoded";
            webrequest.ContentLength = byteArray.Length;
            var dataStream = webrequest.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();
        }


        #endregion

        #region Accounts

        public AccountEntity GetAccounts()
        {
            var webrequest = CreateRequest("accounts");
            webrequest.Method = "GET";
            var result = ReadResponse<AccountEntity>(webrequest);
            return result;
        }



        #endregion


        #region Projects

        public ProjectsEntity GetProjects(int accountId)
        {
            var webrequest = CreateRequest(string.Format("projects?account_id={0}", accountId));
            webrequest.Method = "GET";

            return ReadResponse<ProjectsEntity>(webrequest);
        }

        public ProjectEntity GetSingleProject(string projectId)
        {
            var webrequest = CreateRequest(string.Format("projects/{0}", projectId));
            webrequest.Method = "GET";

            return ReadResponse<ProjectEntity>(webrequest);
        }

        public void PostProject(string accountId, string textDirection, string name)
        {
            var data = string.Format("account_id={0}&text_direction={1}&name={2}", accountId, textDirection, name);
            var webrequest = CreateRequest("projects");
            webrequest.Method = "POST";

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }


        public StatusesEntity GetAllStatuses(string projectId)
        {
            var webrequest = CreateRequest(string.Format("projects/{0}/statuses", projectId));
            webrequest.Method = "GET";

            return ReadResponse<StatusesEntity>(webrequest);
        }


        public StatusEntity GetSingleStatus(string statusId, string projectId)
        {
            var webrequest = CreateRequest(string.Format("projects/{0}/statuses/{1}", projectId, statusId));
            webrequest.Method = "GET";

            return ReadResponse<StatusEntity>(webrequest);
        }

        #endregion


        #region Items


        public ItemsEntity GetItems(string projectId)
        {
            var webrequest = CreateRequest(string.Format("items?project_id={0}",projectId));
            webrequest.Method = "GET";

            return ReadResponse<ItemsEntity>(webrequest);
        }

        public ItemEntity GetSingleItem(string itemId)
        {
            var webrequest = CreateRequest(string.Format("items/{0}", itemId));
            webrequest.Method = "GET";

            return ReadResponse<ItemEntity>(webrequest);
        }

        public void PostItem(string projectId, string name, string parentId=null, string templateId=null, Config config = null)
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

            var webrequest = CreateRequest("items");
            webrequest.Method = "POST";

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
            var webrequest = CreateRequest(string.Format("items/{0}/save",itemId));
            webrequest.Method = "POST";

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }

        public void ApplyTemplate(string itemId, string templateId)
        {
            var data = string.Format("template_id={0}", templateId);
            var webrequest = CreateRequest(string.Format("items/{0}/apply_template", itemId));
            webrequest.Method = "POST";

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }

        public void ChooseStatus(string itemId, string statusId)
        {
            var data = string.Format("status_id={0}", statusId);
            var webrequest = CreateRequest(string.Format("items/{0}/choose_status", itemId));
            webrequest.Method = "POST";

            AddPostData(data, webrequest);

            ReadResponse(webrequest);
        }


        #endregion


        #region Template

        public TemplatesEntity GetTemplates(string projectId)
        {          
            var webrequest = CreateRequest(string.Format("templates?project_id={0}", projectId));
            webrequest.Method = "GET";

            return ReadResponse<TemplatesEntity>(webrequest);
        }


        public TemplateEntity GetSingleTemplate(string templateId)
        {            
            var webrequest = CreateRequest(string.Format("templates/{0}", templateId));
            webrequest.Method = "GET";

            return ReadResponse<TemplateEntity>(webrequest);
        }


        #endregion
    }
}