using System.Collections.Generic;
using Bynder.Content.SitecoreConnector.Web.Models.Import;
using Sitecore.Mvc.Controllers;

namespace Bynder.Content.SitecoreConnector.Web.Controllers
{
    public class BaseController : SitecoreController
    {
        /// <summary>
        /// 
        /// </summary>
        public BaseController()
        {
        }

        public List<LanguageViewModel> GetLanguages(string databse) 
        {
            var model = new List<LanguageViewModel>();
            var database = Sitecore.Configuration.Factory.GetDatabase(databse);
            var languages = database.GetLanguages();

            foreach (var language in languages)
            {
                model.Add(new LanguageViewModel
                {
                    Name = language.CultureInfo.DisplayName,
                    IsoCode = language.CultureInfo.Name
                });
            }
            return model;
        }
    }
}
