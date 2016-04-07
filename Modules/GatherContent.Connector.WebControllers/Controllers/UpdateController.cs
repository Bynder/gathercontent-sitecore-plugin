using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.UpdateItems;
using GatherContent.Connector.WebControllers.Models.Import;
using GatherContent.Connector.WebControllers.Models.Update;
using Newtonsoft.Json;
using Sitecore.Diagnostics;

namespace GatherContent.Connector.WebControllers.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class UpdateController : BaseController
    {
        protected IUpdateManager UpdateManager;
        protected IImportManager ImportManager;

       /// <summary>
       /// 
       /// </summary>
       /// <param name="updateManager"></param>
       /// <param name="importManager"></param>
        public UpdateController(IUpdateManager updateManager, IImportManager importManager)
        {
            ImportManager = importManager;
            UpdateManager = updateManager;
        }

        #region Utilities


        private FiltersViewModel GetFilters(UpdateFiltersModel filters)
        {
            var filtersViewModel = new FiltersViewModel();

            foreach (var project in filters.Projects)
            {
                filtersViewModel.Projects.Add(new ProjectViewModel
                {
                    Id = project.Id,
                    Name = project.Name
                });
            }

            foreach (var status in filters.Statuses)
            {
                filtersViewModel.Statuses.Add(new StatusViewModel
                {
                    Id = status.Id,
                    Name = status.Name
                });
            }

            foreach (var template in filters.Templates)
            {
                filtersViewModel.Templates.Add(new TemplateViewModel
                {
                    Id = template.Id,
                    Name = template.Name
                });
            }


            return filtersViewModel;
        }


 

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public string Get(string id)
        {
            try
            {
                var language = Sitecore.Context.Language;
                var updateModel = UpdateManager.GetItemsForUpdate(id, language.CultureInfo.TwoLetterISOLanguageName);
                var importViewModel = new UpdateViewModel {Languages = GetLanguages()};

                foreach (var updateItemModel in updateModel.Items)
                {
                    importViewModel.Items.Add(new UpdateListItemViewModel
                    {
                        Id = updateItemModel.CmsId,
                        ScTitle = updateItemModel.Title,
                        ScTemplateName = updateItemModel.CmsTemplate.Name,
                        CmsLink = updateItemModel.CmsLink,
                        GcLink = updateItemModel.GcLink,
                        LastUpdatedInGc = updateItemModel.GcItem.LastUpdatedInGc,
                        LastUpdatedInSitecore = updateItemModel.LastUpdatedInCms,
                        GcProject = new ProjectViewModel
                        {
                            Id = updateItemModel.Project.Id,
                            Name = updateItemModel.Project.Name
                        },
                        GcTemplate = new TemplateViewModel
                        {
                            Id = updateItemModel.GcTemplate.Id,
                            Name = updateItemModel.GcTemplate.Name
                        },
                        Status = new StatusViewModel
                        {
                            Id = updateItemModel.Status.Id,
                            Name = updateItemModel.Status.Name,
                            Color = updateItemModel.Status.Color
                        },
                        GcItem = new ItemViewModel
                        {
                            Id = updateItemModel.GcItem.Id,
                            Name = updateItemModel.GcItem.Title
                        },

                    });
                }

                importViewModel.Filters = GetFilters(updateModel.Filters);

                var model = JsonConvert.SerializeObject(importViewModel);
                return model;
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message;
            }
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="statusId"></param>
        /// <param name="items"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public ActionResult UpdateItems(string id, string statusId, string language, List<UpdateListIds> items)
        {
            try
            {
                var model = new List<ImportResultViewModel>();
                var result = UpdateManager.UpdateItems(id, items, language);
                foreach (var item in result)
                {
                    model.Add(new ImportResultViewModel
                    {
                        Title = item.GcItem.Title,
                        IsImportSuccessful = item.IsImportSuccessful,
                        Message = item.ImportMessage,
                        CmsLink = item.CmsLink,
                        GcLink = item.GcLink,
                        Status = new StatusViewModel
                        {
                            Color = item.Status.Color,
                            Name = item.Status.Name
                        },
                        GcTemplateName = item.GcTemplate.Name
                    });
                }
                return Json(model, JsonRequestBehavior.AllowGet);
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}