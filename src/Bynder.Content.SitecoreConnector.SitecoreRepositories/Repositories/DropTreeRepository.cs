namespace Bynder.Content.SitecoreConnector.SitecoreRepositories.Repositories
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using Core.DependencyInjection;
    using Core.Interfaces;
    using Core.Models.Import;
    
    using Sitecore.Data.Items;

    [Service(typeof(IDropTreeRepository))]
    public class DropTreeRepository : BaseSitecoreRepository, IDropTreeRepository
    {
        private readonly IAccountsRepository accountsRepository;

        public DropTreeRepository(IAccountsRepository accountsRepository)
        {
            this.accountsRepository = accountsRepository;
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
                            Path = item.Paths.Path,
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
                            Path = item.Paths.Path,
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
                        Path = item.Paths.Path,
                        Icon = template != null ? template.Icon : "",
                        Children = CreateChildrenTree(id, item.Children),
                    };
                    list.Add(node);
                }
            }

            return list;
        }

        public CmsItem GetHomeNode(string id)
        {
            var accountSettings = accountsRepository.GetAccountSettings();
            var dropTreeHomeNode = accountSettings.DropTreeHomeNode;
            if (string.IsNullOrEmpty(dropTreeHomeNode))
            {
                dropTreeHomeNode = Constants.DropTreeHomeNode;
            }

            return GetNode(dropTreeHomeNode);
        }

        public CmsItem GetOptionsContentFoldersNode()
        {
            var accountSettings = accountsRepository.GetAccountSettings();
            var optionsContentFolderIdNode = accountSettings.OptionsContentFolderId;

            return GetNode(optionsContentFolderIdNode);
        }

        public CmsItem GetOptionsTemplatesNode()
        {
            var accountSettings = accountsRepository.GetAccountSettings();
            var optionsTemplateIdNode = accountSettings.OptionsTemplateId;

            return GetNode(optionsTemplateIdNode);
        }

        public CmsItem GetNode(string itemId)
        {
            CmsItem model = null;
            var home = GetItem(itemId);
            var template = GetItemTemplate(home.TemplateID);

            if (string.IsNullOrEmpty(itemId) || itemId == "null")
            {
                model = new CmsItem
                {
                    Title = home.Name,
                    Path = home.Paths.Path,
                    Id = home.ID.ToString(),
                    Icon = template != null ? template.Icon : "",
                };
            }
            else
            {
                model = new CmsItem
                {
                    Title = home.Name,
                    Id = home.ID.ToString(),
                    Icon = template != null ? template.Icon : "",
                    Children = CreateChildrenTree(itemId, home.Children),
                    Path = home.Paths.Path
                };
            }

            return model;
        }

        public List<CmsItem> GetChildren(string id, bool isTemplateRestriction)
        {
            var model = new List<CmsItem>();
            var parent = GetItem(id);
            if (parent == null) return model;

            var children = parent.Children;

            foreach (var child in children)
            {
                var item = (Item)child;
                var template = GetItemTemplate(item.TemplateID);

                if (!isTemplateRestriction)
                {
                    model.Add(new CmsItem
                    {
                        Title = item.Name,
                        Path = item.Paths.Path,
                        Id = item.ID.ToString(),
                        Icon = template != null ? template.Icon : ""
                    });
                }
                else
                {
                    if (item.TemplateID == Sitecore.TemplateIDs.Template)
                    {
                        model.Add(new CmsItem
                        {
                            Title = item.Name,
                            Path = item.Paths.Path,
                            Id = item.ID.ToString(),
                            Icon = template != null ? template.Icon : ""
                        });
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            return model;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetHomeNodeId()
        {
            var accountSettings = accountsRepository.GetAccountSettings();

            var dropTreeHomeNode = accountSettings.DropTreeHomeNode;
            if (string.IsNullOrEmpty(dropTreeHomeNode))
            {
                dropTreeHomeNode = Constants.DropTreeHomeNode;
            }

            return dropTreeHomeNode;
        }

        public List<string> GetIdPath(string parentId, string decendantId)
        {
            List<string> results = new List<string>();


            if (!String.IsNullOrEmpty(parentId) && !String.IsNullOrEmpty(decendantId))
            {

                var parentItem = GetItem(parentId);
                var decendantItem = GetItem(decendantId);

                if (parentItem != null && decendantItem != null && decendantItem.Paths.FullPath.Contains(parentItem.Paths.FullPath))
                {
                    Item i = decendantItem;
                    while(i.ID != parentItem.ID){
                        results.Add(i.ID.ToString());
                        i = i.Parent;
                    }
                    results.Reverse();
                }
            }

            return results;
        
        }
    }
}
