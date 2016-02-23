using GatherContent.Connector.Entities;
using GatherContent.Connector.Managers.Models.ImportItems;
using System.Collections.Generic;
using GatherContent.Connector.SitecoreRepositories.Repositories;
using Sitecore.Data.Items;

namespace GatherContent.Connector.Managers.Managers
{
    public class DropTreeManager
    {
        private readonly ItemsRepository _itemsRepository;
        private readonly GCAccountSettings _gcAccountSettings;
        public DropTreeManager()
        {
            var accountsRepository = new AccountsRepository();
            _gcAccountSettings = accountsRepository.GetAccountSettings();
            _itemsRepository = new ItemsRepository();

        }
        public List<DropTreeModel> GetTopLevelNode()
        {
            var model = new List<DropTreeModel>();
            var dropTreeHomeNode = _gcAccountSettings.DropTreeHomeNode;
            if (string.IsNullOrEmpty(dropTreeHomeNode))
            {
                dropTreeHomeNode = Constants.DropTreeHomeNode;
            }
            var home = _itemsRepository.GetItem(dropTreeHomeNode);
            var template = _itemsRepository.GetItemTemplate(home.TemplateID);

            if (home == null) return model;
            model.Add(new DropTreeModel
            {
                Title = home.Name,
                Key = home.ID.ToString(),
                IsLazy = true,
                Icon = template != null ? template.Icon : ""
            });
            return model;
        }

        public List<DropTreeModel> GetChildrenNodes(string id)
        {
            var model = new List<DropTreeModel>();
            var parent = _itemsRepository.GetItem(id);
            if (parent == null) return model;

            var children = parent.Children;

            foreach (var child in children)
            {
                var item = (Item)child;
                var template = _itemsRepository.GetItemTemplate(item.TemplateID);
                model.Add(new DropTreeModel
                {
                    Title = item.Name,
                    Key = item.ID.ToString(),
                    IsLazy = item.HasChildren,
                    Icon = template != null ? template.Icon : ""
                });
            }

            return model;
        }
    }
}
