using System.Linq;
using GatherContent.Connector.Entities;
using GatherContent.Connector.Managers.Models.ImportItems;
using System.Collections.Generic;
using GatherContent.Connector.IRepositories.Interfaces;
using GatherContent.Connector.Managers.Interfaces;
using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework.Commands.Carousel;

namespace GatherContent.Connector.Managers.Managers
{
    /// <summary>
    /// 
    /// </summary>
    public class DropTreeManager : IDropTreeManager
    {
        protected IItemsRepository _itemsRepository;
        protected IAccountsRepository _accountsRepository;

        protected GCAccountSettings _gcAccountSettings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="itemsRepository"></param>
        /// <param name="accountsRepository"></param>
        /// <param name="gcAccountSettings"></param>
        public DropTreeManager(IItemsRepository itemsRepository, IAccountsRepository accountsRepository, GCAccountSettings gcAccountSettings)
        {
            _accountsRepository = accountsRepository;
            _gcAccountSettings = gcAccountSettings;
            _itemsRepository = itemsRepository;
        }


        private List<DropTreeModel> CreateChildrenTree(string id, IEnumerable<Item> items)
        {
            var list = new List<DropTreeModel>();

            //List<Item> items = query.Cast<Item>().ToList();

            //TODO: using sitecore item
            //if (items.Select(i => i.ID.ToString()).Contains(id))
            //{
            //    foreach (var item in items)
            //    {
            //        var template = _itemsRepository.GetItemTemplate(item.TemplateID);
            //        if (id == item.ID.ToString())
            //        {
            //            var node = new DropTreeModel
            //            {
            //                Title = item.Name,
            //                Key = item.ID.ToString(),
            //                IsLazy = true,
            //                Icon = template != null ? template.Icon : "",
            //                Selected = true
            //            };
            //            list.Add(node);
            //        }
            //        else
            //        {
            //            var node = new DropTreeModel
            //            {
            //                Title = item.Name,
            //                Key = item.ID.ToString(),
            //                IsLazy = true,
            //                Icon = template != null ? template.Icon : "",
            //                Selected = false
            //            };
            //            list.Add(node);
            //        }
            //    }
            //}
            //else
            //{
            //    foreach (var item in items)
            //    {
            //        var template = _itemsRepository.GetItemTemplate(item.TemplateID);

            //        var node = new DropTreeModel
            //        {
            //            Title = item.Name,
            //            Key = item.ID.ToString(),
            //            IsLazy = false,
            //            Icon = template != null ? template.Icon : "",
            //            Selected = false,
            //            Children = CreateChildrenTree(id, item.Children),
            //            Expanded = true
            //        };
            //        list.Add(node);
            //    }
            //}

            return list;
        }



        public List<DropTreeModel> GetTopLevelNode(string id)
        {
            var model = new List<DropTreeModel>();

            //TODO: using sitecore item
            //var dropTreeHomeNode = _gcAccountSettings.DropTreeHomeNode;
            //if (string.IsNullOrEmpty(dropTreeHomeNode))
            //{
            //    dropTreeHomeNode = Constants.DropTreeHomeNode;
            //}
            //var home = _itemsRepository.GetItem(dropTreeHomeNode);
            //var template = _itemsRepository.GetItemTemplate(home.TemplateID);

            //if (string.IsNullOrEmpty(id) || id == "null")
            //{
            //    model.Add(new DropTreeModel
            //    {
            //        Title = home.Name,
            //        Key = home.ID.ToString(),
            //        IsLazy = true,
            //        Icon = template != null ? template.Icon : "",
            //    });
            //}
            //else
            //{
            //    var homeNode = new DropTreeModel
            //     {
            //         Title = home.Name,
            //         Key = home.ID.ToString(),
            //         IsLazy = false,
            //         Icon = template != null ? template.Icon : "",
            //         Selected = id == dropTreeHomeNode,
            //         Children = CreateChildrenTree(id, home.Children),
            //         Expanded = true
            //     };

            //    model.Add(homeNode);
            //}

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

            //TODO: using sitecore item
            //var parent = _itemsRepository.GetItem(id);
            //if (parent == null) return model;

            //var children = parent.Children;

            //foreach (var child in children)
            //{
            //    var item = (Item)child;
            //    var template = _itemsRepository.GetItemTemplate(item.TemplateID);
            //    model.Add(new DropTreeModel
            //    {
            //        Title = item.Name,
            //        Key = item.ID.ToString(),
            //        IsLazy = item.HasChildren,
            //        Icon = template != null ? template.Icon : ""
            //    });
            //}

            return model;
        }
    }
}
