using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.IRepositories.Models.Import;
using GatherContent.Connector.Managers.Models.ImportItems;
using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.Managers.Interfaces;

namespace GatherContent.Connector.Managers.Managers
{
    public class DropTreeManager : IDropTreeManager
    {
        protected IDropTreeRepository DropTreeRepository;
        protected IAccountsRepository AccountsRepository;
        protected GCAccountSettings GcAccountSettings;

        public DropTreeManager(IDropTreeRepository dropTreeRepository, IAccountsRepository accountsRepository, GCAccountSettings gcAccountSettings)
        {
            AccountsRepository = accountsRepository;
            GcAccountSettings = gcAccountSettings;
            DropTreeRepository = dropTreeRepository;
        }

        private List<DropTreeModel> CreateChildrenTree(List<string> idPath, int level, List<CmsItem> items)
        {
            var list = new List<DropTreeModel>();

            foreach (var item in items)
            {

                if (item.Id != idPath[level] || idPath.Count - 1 == level)
                {
                    var node = new DropTreeModel
                    {
                        Title = item.Title,
                        Key = item.Id,
                        IsLazy = true,
                        Icon = item.Icon,
                        Selected = idPath[level] == item.Id
                    };
                    list.Add(node);
                }
                else
                {
                    var node = new DropTreeModel
                    {
                        Title = item.Title,
                        Key = item.Id,
                        IsLazy = false,
                        Icon = item.Icon,
                        Selected = false,
                        Children = CreateChildrenTree(idPath, ++level, item.Children),
                        Expanded = true
                    };
                    list.Add(node);
                }
            }
            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<DropTreeModel> GetTopLevelNode(string id)
        {
            var model = new List<DropTreeModel>();
            var homeItem = DropTreeRepository.GetHomeNode(id);

            if (string.IsNullOrEmpty(id) || id == "null")
            {
                model.Add(new DropTreeModel
                {
                    Title = homeItem.Title,
                    Key = homeItem.Id,
                    Icon = homeItem.Icon,
                    IsLazy = true
                });
            }
            else
            {

                var idPath = DropTreeRepository.GetIdPath(homeItem.Id, id);

                model.Add(new DropTreeModel
                {
                    Title = homeItem.Title,
                    Key = homeItem.Id,
                    Icon = homeItem.Icon,
                    IsLazy = false,
                    Selected = id == homeItem.Id,
                    Expanded = true,
                    Children = CreateChildrenTree(idPath, 0, homeItem.Children),
                });
            }

            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<DropTreeModel> GetChildrenNodes(string id)
        {
            var model = new List<DropTreeModel>();
            var items = DropTreeRepository.GetChildren(id);
            foreach (var cmsItem in items)
            {
                model.Add(new DropTreeModel
                {
                    Title = cmsItem.Title,
                    Key = cmsItem.Id,
                    Icon = cmsItem.Icon,
                    IsLazy = true
                });
            }

            return model;
        }
    }
}
