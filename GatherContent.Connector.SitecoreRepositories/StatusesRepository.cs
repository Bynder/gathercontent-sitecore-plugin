using System.Linq;
using GatherContent.Connector.Entities.Entities;
using GatherContent.Connector.IRepositories.Interfaces;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.SecurityModel;

namespace GatherContent.Connector.SitecoreRepositories
{
    public class StatusesRepository : BaseSitecoreRepository, IStatusesRepository
    {
        public StatusesRepository() : base() { }

        public void CreateStatus(string id, Status status)
        {
            var parentItem = GetItem(id);
            var statusesFolder = parentItem.Children.FirstOrDefault(item => item.Name == Constants.StatusFolderName);
            if (statusesFolder != null)
            {
                var folders = statusesFolder.Axes.GetDescendants().Select(item => item.Name).ToList();
                if (!folders.Contains(status.Name))
                {
                    using (new SecurityDisabler())
                    {
                        var template = ContextDatabase.GetTemplate(new ID(Constants.GcStatus));
                        var validFolderName = ItemUtil.ProposeValidItemName(status.Name);
                        var createdItem = statusesFolder.Add(validFolderName, template);
                        using (new SecurityDisabler())
                        {
                            createdItem.Editing.BeginEdit();
                            createdItem.Fields["Id"].Value = status.Id.ToString();
                            createdItem.Fields["Name"].Value = status.Name;
                            createdItem.Fields["Description"].Value = status.Description;
                            createdItem.Editing.EndEdit();
                        }

                        //return createdItem;
                    }
                }
            }
            //return null;
        }
    }
}
