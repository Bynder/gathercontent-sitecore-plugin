//using System;
//using System.Collections.Generic;
//using System.Linq;
//using GatherContent.Connector.Entities.Entities;
//using GatherContent.Connector.GatherContentService.Services;

//namespace GatherContent.Connector.Managers
//{
//    public class ItemsManager
//    {
//        public List<Item> GetItemsForImport()
//        {
//            var db = Sitecore.Configuration.Factory.GetDatabase("master");
//            var item = db.GetItem(id);

//            var gcSettings = GcAccountExtension.GetSettings(item);
//            var service = new GatherContentService.GatherContentService(gcSettings.ApiUrl, gcSettings.Username, gcSettings.ApiKey);

//            var accounts = service.GetAccounts();
//            var account = accounts.Data.FirstOrDefault();
//            var projects = service.GetProjects(account.Id);

//            int intProjectId = Convert.ToInt32(projectId);
//            Project project;
//            if (intProjectId != 0)
//            {
//                project = projects.Data.FirstOrDefault(i => i.Id == intProjectId);
//            }
//            else
//            {
//                project = projects.Data.FirstOrDefault();
//            }

//            var items = service.GetItems(project.Id.ToString());

//            var templates = service.GetTemplates(project.Id.ToString());

//            ImportResponseModel result = new ImportResponseModel(project, items, projects, templates);

//            return result;
//        }
//    }
//}
