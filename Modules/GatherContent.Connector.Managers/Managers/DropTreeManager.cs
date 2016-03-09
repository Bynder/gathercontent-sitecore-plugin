using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Managers.Models.ImportItems;
using System.Collections.Generic;
using GatherContent.Connector.SitecoreRepositories.Repositories;

namespace GatherContent.Connector.Managers.Managers
{
    public class DropTreeManager
    {
        private readonly DropTreeRepository _dropTreeRepository;
        private readonly GCAccountSettings _gcAccountSettings;
        public DropTreeManager()
        {
            var accountsRepository = new AccountsRepository();
            _gcAccountSettings = accountsRepository.GetAccountSettings();
            _dropTreeRepository = new DropTreeRepository();

        }

        private List<DropTreeModel> CreateChildrenTree(string id, List<IRepositories.Models.New.Import.CmsItem> items)
        {
            var list = new List<DropTreeModel>();

            if (items.Select(i => i.Id.ToString()).Contains(id))
            {
                foreach (var item in items)
                {
                    if (id == item.Id)
                    {
                        var node = new DropTreeModel
                        {
                            Title = item.Title,
                            Key = item.Id,
                            IsLazy = true,
                            Icon = item.Icon,
                            Selected = true
                        };
                        list.Add(node);
                    }
                    else
                    {
                        var node = new DropTreeModel
                        {
                            Title = item.Title,
                            Key = item.Id,
                            IsLazy = true,
                            Icon = item.Icon,
                            Selected = false
                        };
                        list.Add(node);
                    }
                }
            }
            else
            {
                foreach (var item in items)
                {
                    var node = new DropTreeModel
                    {
                        Title = item.Title,
                        Key = item.Id,
                        IsLazy = false,
                        Icon = item.Icon,
                        Selected = false,
                        Children = CreateChildrenTree(id, item.Children),
                        Expanded = true
                    };
                    list.Add(node);
                }
            }
            return list;
        }


        public List<DropTreeModel> GetTopLevelNode(string id)
        {
            var model = new List<DropTreeModel>();
            var items = _dropTreeRepository.GetHomeNode(id);

            if (string.IsNullOrEmpty(id) || id == "null")
            {
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
            }
            else
            {
                var dropTreeHomeNode = _gcAccountSettings.DropTreeHomeNode;
                if (string.IsNullOrEmpty(dropTreeHomeNode))
                {
                    dropTreeHomeNode = Constants.DropTreeHomeNode;
                }
                foreach (var cmsItem in items)
                {
                    model.Add(new DropTreeModel
                    {
                        Title = cmsItem.Title,
                        Key = cmsItem.Id,
                        Icon = cmsItem.Icon,
                        IsLazy = false,
                        Selected = id == dropTreeHomeNode,
                        Expanded = true,
                        Children = CreateChildrenTree(id, cmsItem.Children),
                    });
                }
            }

            return model;
        }




        public List<DropTreeModel> GetChildrenNodes(string id)
        {
            var model = new List<DropTreeModel>();
            var items = _dropTreeRepository.GetChildren(id);
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
