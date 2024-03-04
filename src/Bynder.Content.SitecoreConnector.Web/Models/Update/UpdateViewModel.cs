using System.Collections.Generic;
using Bynder.Content.SitecoreConnector.Web.Models.Import;

namespace Bynder.Content.SitecoreConnector.Web.Models.Update
{
    public class UpdateViewModel
    {
        public UpdateViewModel()
        {
            Items = new List<UpdateListItemViewModel>();
            Filters = new FiltersViewModel();
            Languages = new List<LanguageViewModel>();
        }
        public FiltersViewModel Filters { get; set; }
        public List<UpdateListItemViewModel> Items { get; set; }
        public List<LanguageViewModel> Languages { get; set; }
    }
}
