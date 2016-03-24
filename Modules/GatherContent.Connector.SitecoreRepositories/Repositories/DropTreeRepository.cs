using System.Collections.Generic;
using System.Linq;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.IRepositories.Models.Import;
using Sitecore.Data.Items;

namespace GatherContent.Connector.SitecoreRepositories.Repositories
{
    /// <summary>
    /// 
    /// </summary>
    public class DropTreeRepository : BaseSitecoreRepository, IDropTreeRepository
    {
        protected IAccountsRepository AccountsRepository;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accountsRepository"></param>
        public DropTreeRepository(IAccountsRepository accountsRepository) : base()
        {
            AccountsRepository = accountsRepository;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="items"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<CmsItem> GetHomeNode(string id)
        {
            var model = new List<CmsItem>();
            var accountSettings = AccountsRepository.GetAccountSettings();
            var dropTreeHomeNode = accountSettings.DropTreeHomeNode;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetHomeNodeId()
        {
            var accountSettings = AccountsRepository.GetAccountSettings();

            var dropTreeHomeNode = accountSettings.DropTreeHomeNode;
            if (string.IsNullOrEmpty(dropTreeHomeNode))
            {
                dropTreeHomeNode = Constants.DropTreeHomeNode;
            }

            return dropTreeHomeNode;
        }
    }
}
