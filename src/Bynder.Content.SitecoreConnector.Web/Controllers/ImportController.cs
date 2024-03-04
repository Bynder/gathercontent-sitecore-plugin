using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Mvc;
using Bynder.Content.SitecoreConnector.Managers.Interfaces;
using Bynder.Content.SitecoreConnector.Managers.Models.ImportItems;
using Bynder.Content.SitecoreConnector.Web.Models.Import;
using Newtonsoft.Json;
using Sitecore.Diagnostics;

namespace Bynder.Content.SitecoreConnector.Web.Controllers
{
    public class ImportController : BaseController
    {
        protected IImportManager ImportManager;
        protected IDropTreeManager DropTreeManager;
        protected ILinkManager LinkManager;

        public ImportController(IImportManager importManager, IDropTreeManager dropTreeManager, ILinkManager linkManager)
        {
            ImportManager = importManager;
            LinkManager = linkManager;
            DropTreeManager = dropTreeManager;
        }

        public FiltersViewModel GetFilters(string projectId) 
        {
            var filtersViewModel = new FiltersViewModel();
            var filters = ImportManager.GetFilters(projectId);

            if (filters.CurrentProject != null)
            {
                filtersViewModel.Project = new ProjectViewModel
                {
                    Id = filters.CurrentProject.Id,
                    Name = filters.CurrentProject.Name
                };
            }

            filtersViewModel.Projects.Add(new ProjectViewModel
            {
                Id = "0",
                Name = "Select project"
            });
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
        
        public string Get(string id, string projectId, string db)
        {
            try
            {
                var items = ImportManager.GetImportDialogModel(id, projectId);
                var importViewModel = new ImportViewModel();
                importViewModel.Languages = GetLanguages(db);
                
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var importItem = new ImportListItemViewModel
                        {
                            Id = item.CwbItem.Id,
                            Title = item.CwbItem.Title,
                            Template = new TemplateViewModel
                            {
                                Name = item.CwbTemplate.Name,
                                Id = item.CwbTemplate.Id
                            },
                            Status = new StatusViewModel
                            {
                                Color = item.Status.Color,
                                Name = item.Status.Name,
                                Id = item.Status.Id
                            },
                            Breadcrumb = item.Breadcrumb,
                            LastUpdatedInCwb = item.CwbItem.LastUpdatedInCwb

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

                importViewModel.Filters = GetFilters(projectId);
                
                var model = JsonConvert.SerializeObject(importViewModel);
                return model;
            }
            catch (WebException exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message;
            }    
        }
        
        public string GetMultiLocation(string id, string projectId,string db)
        {
            try
            {
                var items = ImportManager.GetImportDialogModel(id, projectId);
                var importViewModel = new ImportViewModel();

                importViewModel.Languages = GetLanguages(db);

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        var importItem = new ImportListItemViewModel
                        {
                            Id = item.CwbItem.Id,
                            Title = item.CwbItem.Title,
                            Template = new TemplateViewModel
                            {
                                Name = item.CwbTemplate.Name,
                                Id = item.CwbTemplate.Id
                            },
                            Status = new StatusViewModel
                            {
                                Color = item.Status.Color,
                                Name = item.Status.Name,
                                Id = item.Status.Id
                            },
                            Breadcrumb = item.Breadcrumb,
                            LastUpdatedInCwb = item.CwbItem.LastUpdatedInCwb

                        };

                        foreach (var availableMapping in item.AvailableMappings.Mappings)
                        {
                            importItem.AvailableMappings.Mappings.Add(new AvailableMappingViewModel
                            {
                                Id = availableMapping.Id,
                                Title = availableMapping.Title,
                                OpenerId = "drop-tree" + Guid.NewGuid(),
                                DefaultLocation = availableMapping.DefaultLocationId,
                                DefaultLocationTitle = availableMapping.DefaultLocationTitle,
                                ScTemplate = availableMapping.CmsTemplateName,
                            });
                        }

                        importViewModel.Items.Add(importItem);
                    }
                }
                
                importViewModel.Filters = GetFilters(projectId);

                var model = JsonConvert.SerializeObject(importViewModel);
                return model;
            }
            catch (WebException exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message + " Please check your credentials";
            }
            catch (Exception exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return exception.Message;
            }

        }

        [HttpPost]
        public ActionResult ImportItems(string id, string projectId, string statusId, string language, bool expandLinks)
        {
            try
            {
                List<ImportItemModel> items;
                if (System.Web.HttpContext.Current.Request.InputStream.CanSeek)
                {
                    System.Web.HttpContext.Current.Request.InputStream.Seek(0, System.IO.SeekOrigin.Begin);
                }
                using (var reader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    var body = reader.ReadToEnd();
                    items = JsonConvert.DeserializeObject<List<ImportItemModel>>(body);
                }
                var model = new List<ImportResultViewModel>();
                var result = ImportManager.ImportItems(id, items, projectId, statusId, language);
                foreach (var item in result)
                {
                    if (expandLinks && item.IsImportSuccessful)
                    {
                        LinkManager.ExpandLinksInText(item.CmsId, false);
                    }

                    model.Add(new ImportResultViewModel
                    {
                        Title = item.CwbItem.Title,
                        IsImportSuccessful = item.IsImportSuccessful,
                        Message = item.ImportMessage,
                        CmsLink = item.CmsLink,
                        CwbLink = item.CwbLink,
                        Status = new StatusViewModel
                        {
                            Color = item.Status.Color,
                            Name = item.Status.Name
                        },
                        CwbTemplateName = item.CwbTemplate.Name
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            catch (WebException exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult ImportItemsWithLocation(string projectId, string statusId, string language, bool expandLinks)
        {
            try
            {
                List<LocationImportItemModel> items;
                if (System.Web.HttpContext.Current.Request.InputStream.CanSeek)
                {
                    System.Web.HttpContext.Current.Request.InputStream.Seek(0, System.IO.SeekOrigin.Begin);
                }
                using (var reader = new StreamReader(System.Web.HttpContext.Current.Request.InputStream))
                {
                    var body = reader.ReadToEnd();
                    items = JsonConvert.DeserializeObject<List<LocationImportItemModel>>(body);
                }
                var model = new List<ImportResultViewModel>();
                var result = ImportManager.ImportItemsWithLocation(items, projectId, statusId, language);
                foreach (var item in result)
                {
                    if (expandLinks && item.IsImportSuccessful)
                    {
                        LinkManager.ExpandLinksInText(item.CmsId, false);
                    }

                    model.Add(new ImportResultViewModel
                    {
                        Title = item.CwbItem.Title,
                        IsImportSuccessful = item.IsImportSuccessful,
                        Message = item.ImportMessage,
                        CmsLink = item.CmsLink,
                        CwbLink = item.CwbLink,
                        Status = new StatusViewModel
                        {
                            Color = item.Status.Color,
                            Name = item.Status.Name
                        },
                        CwbTemplateName = item.CwbTemplate.Name
                    });
                }

                return Json(model, JsonRequestBehavior.AllowGet);
            }
            catch (WebException exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
