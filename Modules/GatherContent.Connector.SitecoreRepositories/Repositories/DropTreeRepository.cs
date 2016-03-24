using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using Sitecore.Data.Items;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    public class DropTreeRepository : BaseSitecoreRepository, IDropTreeRepository
    {
        private readonly GCAccountSettings _accountSettings;

        public DropTreeRepository()
        {
            var accountsRepository = new AccountsRepository();
            _accountSettings = accountsRepository.GetAccountSettings();
        }
        private List<CmsItem> CreateChildrenTree(string id, IEnumerable<Item> items)
        {
            var list = new List<CmsItem>();

            if (items.Select(i => i.ID.ToString()).Contains(id))
            {
                foreach (var item in items)
                {
                    var template = GetItemTemplate(item.TemplateID);
                    if (id == item.ID.ToString())
                    {
                        var node = new CmsItem
                        {
                            Title = item.Name,
                            Id = item.ID.ToString(),
                            Icon = template != null ? template.Icon : "",
                        };
                        list.Add(node);
                    }
                    else
                    {
                        var node = new CmsItem
                        {
                            Title = item.Name,
                            Id = item.ID.ToString(),                            
                            Icon = template != null ? template.Icon : "",
                        };
                        list.Add(node);
                    }
                }
            }
            else
            {
                foreach (var item in items)
                {
                    var template = GetItemTemplate(item.TemplateID);

                    var node = new CmsItem
                    {
                        Title = item.Name,
                        Id = item.ID.ToString(),
                        Icon = template != null ? template.Icon : "",
                        Children = CreateChildrenTree(id, item.Children),
                    };
                    list.Add(node);
                }
            }

            return list;
        }

        public List<CmsItem> GetHomeNode(string id)
        {
            var model = new List<CmsItem>();
            var dropTreeHomeNode = _accountSettings.DropTreeHomeNode;
            if (string.IsNullOrEmpty(dropTreeHomeNode))
            {
                dropTreeHomeNode = Constants.DropTreeHomeNode;
            }
            var home = GetItem(dropTreeHomeNode);
            var template = GetItemTemplate(home.TemplateID);

            if (string.IsNullOrEmpty(id) || id == "null")
            {
                model.Add(new CmsItem
                {
                    Title = home.Name,
                    Id = home.ID.ToString(),
                    Icon = template != null ? template.Icon : "",
                });
            }
            else
            {
                var homeNode = new CmsItem
                {
                    Title = home.Name,
                    Id = home.ID.ToString(),
                    Icon = template != null ? template.Icon : "",
                    Children = CreateChildrenTree(id, home.Children),
                };

                model.Add(homeNode);
            }

            return model;
        }

        public List<CmsItem> GetChildren(string id)
        {
            var model = new List<CmsItem>();
            var parent = GetItem(id);
            if (parent == null) return model;

            var children = parent.Children;

            foreach (var child in children)
            {
                var item = (Item)child;
                var template = GetItemTemplate(item.TemplateID);
                model.Add(new CmsItem
                {
                    Title = item.Name,
                    Id = item.ID.ToString(),
                    Icon = template != null ? template.Icon : ""
                });
            }

            return model;
        }


        public string GetHomeNodeId()
        {
            var dropTreeHomeNode = _accountSettings.DropTreeHomeNode;
            if (string.IsNullOrEmpty(dropTreeHomeNode))
            {
                dropTreeHomeNode = Constants.DropTreeHomeNode;
            }

            return dropTreeHomeNode;
        }
    }
}
