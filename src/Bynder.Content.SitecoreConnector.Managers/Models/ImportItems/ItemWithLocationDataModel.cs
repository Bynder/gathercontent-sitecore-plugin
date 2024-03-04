using System.Collections.Generic;

namespace Bynder.Content.SitecoreConnector.Managers.Models.ImportItems
{
    public class ItemWithLocationDataModel
    {
        public List<ImportItembyLocation> Items { get; set; }

        public ItemWithLocationDataModel() { }

        public ItemWithLocationDataModel(List<ImportItembyLocation> items)
        {
            Items = items;
        }
    }
}
