using System.Collections.Generic;

namespace GatherContent.Connector.WebControllers.Models.Import
{
    public class ImportViewModel
    {
        public ImportViewModel()
        {
            Items = new List<ImportListItemViewModel>();
            Filters = new FiltersViewModel();
        }
        public FiltersViewModel Filters { get; set; }

        public List<ImportListItemViewModel> Items { get; set; }

    }
}
