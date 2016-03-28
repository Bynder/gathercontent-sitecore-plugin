using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Interfaces;
using GatherContent.Connector.Managers.Models.ImportItems;
using GatherContent.Connector.WebControllers.Models.Import;
using Newtonsoft.Json;
using Sitecore.Diagnostics;

namespace GatherContent.Connector.WebControllers.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class ImportController : BaseController
    {
        protected IImportManager ImportManager;
        protected IDropTreeManager DropTreeManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dropTreeManager"></param>
        /// <param name="importManager"></param>
        public ImportController(IDropTreeManager dropTreeManager, IImportManager importManager)
        {
            ImportManager = importManager;
            DropTreeManager = dropTreeManager;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="projectId"></param>
        /// <returns></returns>
        public string Get(string id, string projectId)
        {
            try
            {
                var items = ImportManager.GetImportDialogModel(id, projectId);
                var importViewModel = new ImportViewModel();
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var importItem = new ImportListItemViewModel
                        {
                            Id = item.GcItem.Id,
                            Title = item.GcItem.Title,
                            Template = new TemplateViewModel
                            {
                                Name = item.GcTemplate.Name,
                                Id = item.GcTemplate.Id
                            },
                            Status = new StatusViewModel
                            {
                                Color = item.Status.Color,
                                Name = item.Status.Name,
                                Id = item.Status.Id
                            },
                            Breadcrumb = item.Breadcrumb,
                            LastUpdatedInGC = item.GcItem.LastUpdatedInGc

                        };


                        foreach (var availableMapping in item.AvailableMappings.Mappings)
                        {
                            importItem.AvailableMappings.Mappings.Add(new AvailableMappingViewModel
                            {
                                Id = availableMapping.Id,
                                Title = availableMapping.Title
                            });
                        }

                        importViewModel.Items.Add(importItem);

                    }
                }


                #region Filters

                var filters = ImportManager.GetFilters(projectId);

                if (filters.CurrentProject != null)
                {
                    importViewModel.Filters.Project = new ProjectViewModel
                    {
                        Id = filters.CurrentProject.Id,
                        Name = filters.CurrentProject.Name
                    };
                }


                importViewModel.Filters.Projects.Add(new ProjectViewModel
                      {
                          Id = "0",
                          Name = "Select project"
                      });
                foreach (var project in filters.Projects)
                {
                    importViewModel.Filters.Projects.Add(new ProjectViewModel
                    {
                        Id = project.Id,
                        Name = project.Name
                    });
                }

                foreach (var status in filters.Statuses)
                {
                    importViewModel.Filters.Statuses.Add(new StatusViewModel
                    {
                        Id = status.Id,
                        Name = status.Name
                    });
                }

                foreach (var template in filters.Templates)
                {
                    importViewModel.Filters.Templates.Add(new TemplateViewModel
                    {
                        Id = template.Id,
                        Name = template.Name
                    });
                }


                #endregion


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
        /// <param name="projectId"></param>
        /// <returns></returns>
        public string GetMultiLocation(string id, string projectId)
        {
            try
            {
                SelectItemsForImportWithLocation result = ImportManager.GetDialogModelWithLocation(id, projectId);
                var model = JsonConvert.SerializeObject(result);
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
        /// <param name="projectId"></param>
        /// <param name="statusId"></param>
        /// <param name="language"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ImportItems(string id, string projectId, string statusId, string language, List<ImportItemModel> items)
        {
            try
            {
                var model = new List<ImportResultViewModel>();
                var result = ImportManager.ImportItems(id, items, projectId, statusId, language);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="statusId"></param>
        /// <param name="language"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ImportItemsWithLocation(string projectId, string statusId, string language, List<LocationImportItemModel> items)
        {
            try
            {
                ImportResultModel result = ImportManager.ImportItemsWithLocation(items, projectId, statusId, language);
                return Json(result, JsonRequestBehavior.AllowGet);
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