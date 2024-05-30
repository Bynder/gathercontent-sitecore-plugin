namespace Bynder.Content.SitecoreConnector.Managers.Managers
{
    using System.Collections.Generic;

    using Bynder.Content.SitecoreConnector.Core.Entities;

    using Core;
    using Core.DependencyInjection;
    using Core.Interfaces;
    using Core.Models.Import;

    using Interfaces;

    using Models.ImportItems;

    using Sitecore.Data.Items;

    [Service(typeof(IDropTreeManager))]
    public class DropTreeManager : IDropTreeManager
    {
        protected IDropTreeRepository DropTreeRepository;
        protected CWBAccountSettings CwbAccountSettings;

        public DropTreeManager(IDropTreeRepository dropTreeRepository, IAccountsRepository accountsRepository)
        {
            CwbAccountSettings = accountsRepository.GetAccountSettings();
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
                    Path = homeItem.Path,
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

        public List<DropTreeModel> GetChildrenNodes(string id, bool isTemplateRestriction)
        {
            var model = new List<DropTreeModel>();
            var items = DropTreeRepository.GetChildren(id, isTemplateRestriction);
            foreach (var cmsItem in items)
            {
                model.Add(new DropTreeModel
                {
                    Title = cmsItem.Title,
                    Path = cmsItem.Path,
                    Key = cmsItem.Id,
                    Icon = cmsItem.Icon,
                    IsLazy = true
                });
            }

            return model;
        }

        public List<DropTreeModel> GetOptionsContentFoldersTopLevelNode()
        {
            var model = new List<DropTreeModel>();
            var optionsContentFoldersItem = DropTreeRepository.GetOptionsContentFoldersNode();

            model.Add(new DropTreeModel
            {
                Title = optionsContentFoldersItem.Title,
                Key = optionsContentFoldersItem.Id,
                Path = optionsContentFoldersItem.Title,
                Icon = optionsContentFoldersItem.Icon,
                IsLazy = true
            });

            return model;
        }

        public List<DropTreeModel> GetOptionsTemplatesTopLevelNode()
        {
            var model = new List<DropTreeModel>();
            var optionsTemplatesItem = DropTreeRepository.GetOptionsTemplatesNode();

            model.Add(new DropTreeModel
            {
                Title = optionsTemplatesItem.Title,
                Key = optionsTemplatesItem.Id,
                Path = optionsTemplatesItem.Title,
                Icon = optionsTemplatesItem.Icon,
                IsLazy = true
            });

            return model;
        }
    }
}
