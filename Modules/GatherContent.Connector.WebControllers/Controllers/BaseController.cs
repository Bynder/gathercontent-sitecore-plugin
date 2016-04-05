using System.Collections.Generic;
using GatherContent.Connector.WebControllers.Models.Import;
using Sitecore.Mvc.Controllers;

namespace GatherContent.Connector.WebControllers.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class BaseController : SitecoreController
    {
        /// <summary>
        /// 
        /// </summary>
        public BaseController()
        {

        }

        public List<LanguageViewModel> GetLanguages() 
        {
            var model = new List<LanguageViewModel>();
            var languages = Sitecore.Context.Database.GetLanguages();

            foreach (var language in languages)
            {
                model.Add(new LanguageViewModel
                {
                    Name = language.CultureInfo.DisplayName,
                    IsoCode = language.CultureInfo.TwoLetterISOLanguageName
                });
            }
            return model;
        }
    }
}