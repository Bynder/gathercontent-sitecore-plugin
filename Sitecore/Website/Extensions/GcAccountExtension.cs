using System;
using GatherContent.Connector.Website.Models;
using Sitecore.Data.Items;

namespace GatherContent.Connector.Website.Extensions
{
    public static class GcAccountExtension
    {
        private const string AccountTemplateName = "GC Account";
        public static GcSettings GetSettings(Item item)
        {
            if (item != null)
            {
                //var accountSettingItem = item.Axes.SelectSingleItem(String.Format("./ancestor-or-self::*[@@templatename='{0}']", AccountTemplateName));
                var db = Sitecore.Configuration.Factory.GetDatabase("master");
                var accountSettingItem = db.GetItem("{B99D89BD-56AB-4F41-BB02-121D116E5145}");
                if (accountSettingItem != null)
                {
                    return new GcSettings
                    {
                        ApiUrl = accountSettingItem["API Url"],
                        Username = accountSettingItem["Username"],
                        ApiKey = accountSettingItem["API Key"],
                    };

                }
            }
            return null;
        }
    }
}