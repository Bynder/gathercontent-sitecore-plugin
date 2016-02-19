using GatherContent.Connector.Managers.Models.ImportItems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GatherContent.Connector.SitecoreRepositories.Repositories;
using Sitecore.Data.Items;

namespace GatherContent.Connector.Managers.Managers
{
    public class DropTreeManager
    {
        private readonly ItemsRepository _itemsRepository;
        public DropTreeManager()
        {
            _itemsRepository = new ItemsRepository();

        }
        public List<DropTreeModel> GetTopLevelNode()
        {
            var model = new List<DropTreeModel>();
            var home = _itemsRepository.GetItem("{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}");
            if (home == null) return model;
            model.Add(new DropTreeModel
            {
                title = home.Name,
                key = home.ID.ToString(),
                isLazy = true,
                icon = home["Icon"]
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

                model.Add(new DropTreeModel
                {
                    title = item.Name,
                    key = item.ID.ToString(),
                    isLazy = item.HasChildren,
                    icon = item["Icon"]
                });
            }

            return model;
        }
    }
}
