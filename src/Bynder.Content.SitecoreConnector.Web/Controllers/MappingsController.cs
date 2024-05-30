using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Bynder.Content.SitecoreConnector.Managers.Interfaces;
using Bynder.Content.SitecoreConnector.Managers.Models.Mapping;
using Bynder.Content.SitecoreConnector.Web.Models.Import;
using Bynder.Content.SitecoreConnector.Web.Models.Mapping;
using Sitecore.Diagnostics;

namespace Bynder.Content.SitecoreConnector.Web.Controllers
{
    public class MappingsController : BaseController
    {
        protected IMappingManager MappingManager;
        protected ILinkManager LinkManager;


        public MappingsController(IMappingManager mappingManager, ILinkManager linkManager)
        {
            MappingManager = mappingManager;
            LinkManager = linkManager;
        }


        private static List<FieldMappingModel> GetFieldMappings(IEnumerable<TemplateTab> tabs)
        {
            var fieldMappings = new List<FieldMappingModel>();
            foreach (var tab in tabs)
            {
                foreach (var field in tab.Fields)
                {
                    if (field.SelectedScField != "0")
                    {
                        var fieldMapping = new FieldMappingModel
                        {
                            CmsTemplateId = field.SelectedScField,
                            CwbFieldId = field.FieldId,
                            CwbFieldName = field.FieldName,
                            OptionsContentFolderId = field.OptionsContentFolderId,
                            OptionsTemplateId = field.OptionsTemplateId
                        };
                        fieldMappings.Add(fieldMapping);
                    }
                }
            }
            return fieldMappings;
        }

        public ActionResult Get()
        {
            try
            {
                var model = new List<TemplateMappingViewModel>();
                var mappings = MappingManager.GetMappingModel();
                foreach (var mapping in mappings)
                {
                    model.Add(new TemplateMappingViewModel(mapping));
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

        public ActionResult GetMainMappingsByProject(string cwbProjectId)
        {
            try
            {
                var model = new List<TemplateMappingViewModel>();
                model.Add(new TemplateMappingViewModel() { MappingTitle = "Select main mapping *", ScMappingId = "Do not map" });
                if (cwbProjectId != "0")
                {
                    var mappings = MappingManager.GetMappingModel(cwbProjectId);
                    foreach (var mapping in mappings)
                    {
                        if (!mapping.IsRelated)
                        {
                            model.Add(new TemplateMappingViewModel(mapping));
                        }
                    }
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

        public ActionResult GetMapping(string cwbTemplateId, string scMappingId)
        {
            try
            {
                var mappingModel = MappingManager.GetSingleMappingModel(cwbTemplateId, scMappingId);

                var model = new TemplateMapViewModel
                {
                    ScMappingId = mappingModel.MappingId,
                };

                if (mappingModel.CwbProject != null)
                {
                    model.CwbProjectId = mappingModel.CwbProject.Id;
                    model.CwbProjectName = mappingModel.CwbProject.Name;
                }
                var addMappingModel = new AddMappingViewModel
                {
                    DefaultLocationTitle = mappingModel.DefaultLocationTitle,
                    DefaultLocation = mappingModel.DefaultLocationId,
                    CwbMappingTitle = mappingModel.MappingTitle,
                    OpenerId = "drop-tree" + Guid.NewGuid(),
                   
                };

                if (mappingModel.CwbTemplate != null)
                {
                    model.CwbTemplateName = mappingModel.CwbTemplate.Name;
                    addMappingModel.CwbTemplateId = mappingModel.CwbTemplate.Id;
                }

                if (mappingModel.CmsTemplate != null)
                {
                    addMappingModel.SelectedTemplateId = mappingModel.CmsTemplate.Id;
                }

                model.AddMappingModel = addMappingModel;

                foreach (var fieldMapping in mappingModel.FieldMappings)
                {
                    model.SelectedFields.Add(new FieldMappingViewModel
                    {
                        SitecoreTemplateId = fieldMapping.CmsTemplateId,
                        CwbFieldId = fieldMapping.CwbFieldId,
                        OptionsContentFolderId = fieldMapping.OptionsContentFolderId,
                        OptionsTemplateId = fieldMapping.OptionsTemplateId
                    });
                }


                #region Available templates

                var sitecoreTemplates = new List<SitecoreTemplateViewModel>();
                var defaultTemplate = new SitecoreTemplateViewModel
                {
                    SitrecoreTemplateName = "Select sitecore mapping template *",
                    SitrecoreTemplateId = "0",

                };
                defaultTemplate.SitecoreFields.Add(new SitecoreTemplateField
                {
                    SitecoreFieldId = "0",
                    SitrecoreFieldName = "Do not map"
                });

                sitecoreTemplates.Add(defaultTemplate);
                var availableCmsTemplates = MappingManager.GetAvailableTemplates();
                foreach (var template in availableCmsTemplates)
                {
                    var st = new SitecoreTemplateViewModel
                      {
                          SitrecoreTemplateId = template.Id,
                          SitrecoreTemplateName = template.Name
                      };
                    st.SitecoreFields.Add(new SitecoreTemplateField
                    {
                        SitecoreFieldId = "0",
                        SitrecoreFieldName = "Do not map",
                    });
                    foreach (var field in template.Fields)
                    {
                        st.SitecoreFields.Add(new SitecoreTemplateField
                        {
                            SitecoreFieldId = field.Id,
                            SitrecoreFieldName = field.Name,
                            SitecoreFieldType = field.Type
                        });
                    }
                    sitecoreTemplates.Add(st);
                }

                model.SitecoreTemplates = sitecoreTemplates;

                var sitecoreOptionsContentFolders = new List<SitecoreTemplateField>
                {
                    new SitecoreTemplateField
                    {
                        SitecoreFieldId = "0",
                        SitrecoreFieldName = "Do not map",
                    }
                };

                var availableCmsOptionsContentFolder = MappingManager.GetAvailableOptionsContentFolder();
                foreach (var template in availableCmsOptionsContentFolder)
                {
                    sitecoreOptionsContentFolders.Add(new SitecoreTemplateField
                    {
                        SitecoreFieldId = template.Id,
                        SitrecoreFieldName = template.Name,
                        SitecoreFieldType = "some_type",
                    });
                }
                model.OptionsContentFolders = sitecoreOptionsContentFolders;

                var sitecoreOptionTemplates = new List<SitecoreTemplateField>
                {
                    new SitecoreTemplateField
                    {
                        SitecoreFieldId = "0",
                        SitrecoreFieldName = "Do not map",
                    }
                };
                var availableCmsOptionTemplates = MappingManager.GetAvailableOptionTemplates();
                foreach (var template in availableCmsOptionTemplates)
                {
                    sitecoreOptionTemplates.Add(new SitecoreTemplateField
                    {
                        SitecoreFieldId = template.Id,
                        SitrecoreFieldName = template.Name,
                        SitecoreFieldType = "some_type",
                    });
                }
                model.OptionsTemplates = sitecoreOptionTemplates;

                #endregion

                model.Rules = MappingManager.GetAllowedMappings();
                model.CwbProjects.Add(new ProjectViewModel
                {
                    Id = "0",
                    Name = "Select project *"
                });

                var projects = MappingManager.GetAllCwbProjects();
                foreach (var project in projects)
                {
                    model.CwbProjects.Add(new ProjectViewModel
                    {
                        Id = project.Id,
                        Name = project.Name
                    });
                }

                if (string.IsNullOrEmpty(cwbTemplateId) && string.IsNullOrEmpty(scMappingId))
                {
                    model.IsEdit = false;
                }
                else
                {
                    model.IsEdit = true;
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

        public ActionResult GetRelatedMapping(string cwbTemplateId, string scMappingId)
        {
            try
            {
                var mappingModel = MappingManager.GetSingleRelatedMappingModel(cwbTemplateId, scMappingId);

                var model = new RelatedTemplateMapViewModel
                {
                    ScMappingId = mappingModel.MappingId,
                };

                if (mappingModel.CwbProject != null)
                {
                    model.CwbProjectId = mappingModel.CwbProject.Id;
                    model.CwbProjectName = mappingModel.CwbProject.Name;
                }
                var addMappingModel = new AddRelatedMappingViewModel()
                {
                    DefaultLocationTitle = mappingModel.DefaultLocationTitle,
                    DefaultLocation = mappingModel.DefaultLocationId,
                    CwbMappingTitle = mappingModel.MappingTitle,
                    OpenerId = "drop-tree" + Guid.NewGuid(),
                    CwbRelatedMappingId = mappingModel.CmsRelatedMappingId,
                    SelectedContainerTemplateId = mappingModel.CmsContainerTemplateId

                };

                if (mappingModel.CwbTemplate != null)
                {
                    model.CwbTemplateName = mappingModel.CwbTemplate.Name;
                    addMappingModel.CwbTemplateId = mappingModel.CwbTemplate.Id;
                }

                if (mappingModel.CmsTemplate != null)
                {
                    addMappingModel.SelectedTemplateId = mappingModel.CmsTemplate.Id;
                }

                model.AddMappingModel = addMappingModel;

                foreach (var fieldMapping in mappingModel.FieldMappings)
                {
                    model.SelectedFields.Add(new FieldMappingViewModel
                    {
                        SitecoreTemplateId = fieldMapping.CmsTemplateId,
                        CwbFieldId = fieldMapping.CwbFieldId,
                        OptionsContentFolderId = fieldMapping.OptionsContentFolderId,
                        OptionsTemplateId = fieldMapping.OptionsTemplateId
                    });
                }


                #region Available templates

                var sitecoreTemplates = new List<SitecoreTemplateViewModel>();


                var defaultTemplate = new SitecoreTemplateViewModel
                {
                    SitrecoreTemplateName = "Select sitecore mapping template *",
                    SitrecoreTemplateId = "0",

                };
                defaultTemplate.SitecoreFields.Add(new SitecoreTemplateField
                {
                    SitecoreFieldId = "0",
                    SitrecoreFieldName = "Do not map"
                });

                sitecoreTemplates.Add(defaultTemplate);
                var availableCmsTemplates = MappingManager.GetAvailableTemplates();
                foreach (var template in availableCmsTemplates)
                {
                    var st = new SitecoreTemplateViewModel
                    {
                        SitrecoreTemplateId = template.Id,
                        SitrecoreTemplateName = template.Name
                    };
                    st.SitecoreFields.Add(new SitecoreTemplateField
                    {
                        SitecoreFieldId = "0",
                        SitrecoreFieldName = "Do not map",
                    });
                    foreach (var field in template.Fields)
                    {
                        st.SitecoreFields.Add(new SitecoreTemplateField
                        {
                            SitecoreFieldId = field.Id,
                            SitrecoreFieldName = field.Name,
                            SitecoreFieldType = field.Type
                        });
                    }
                    sitecoreTemplates.Add(st);
                }
                model.SitecoreTemplates = sitecoreTemplates;
                
                #endregion

                #region Available Mappings

                var mappingsList = new List<TemplateMappingViewModel>();
                mappingsList.Add(new TemplateMappingViewModel() { MappingTitle = "Select mapping *", ScMappingId = "Do not map" });

                if (mappingModel.CwbProject != null)
                {
                    var availableMappings = MappingManager.GetMappingModel(mappingModel.CwbProject.Id);
                    foreach (var mapping in availableMappings)
                    {
                        mappingsList.Add(new TemplateMappingViewModel(mapping));
                    }
                }

                model.Mappings = mappingsList;
                #endregion

                model.Rules = MappingManager.GetAllowedMappings();
                model.CwbProjects.Add(new ProjectViewModel
                {
                    Id = "0",
                    Name = "Select project *"
                });

                var projects = MappingManager.GetAllCwbProjects();
                foreach (var project in projects)
                {
                    model.CwbProjects.Add(new ProjectViewModel
                    {
                        Id = project.Id,
                        Name = project.Name
                    });
                }

                if (string.IsNullOrEmpty(cwbTemplateId) && string.IsNullOrEmpty(scMappingId))
                {
                    model.IsEdit = false;
                }
                else
                {
                    model.IsEdit = true;
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

        public ActionResult GetTemplatesByProjectId(string cwbProjectId)
        {
            try
            {
                var model = new List<TemplateViewModel>();

                model.Add(new TemplateViewModel
                {            
                    Id = "0",
                    Name = "Select template *"
                });

                if (cwbProjectId != "0")
                {
                    var templates = MappingManager.GetTemplatesByProjectId(cwbProjectId);
                    foreach (var template in templates)
                    {
                        var templateModel = new TemplateViewModel
                        {
                            Id = template.Id,
                            Name = template.Name,
                        };

                        model.Add(templateModel);
                    }
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

        public ActionResult GetFieldsByTemplateId(string cwbTemplateId)
        {
            try
            {
                var model = new List<TemplateTab>();

                if (cwbTemplateId != "0")
                {
                    var tabs = MappingManager.GetFieldsByTemplateId(cwbTemplateId);

                    foreach (var tab in tabs)
                    {
                        var templateTab = new TemplateTab
                        {
                            TabName = tab.TabName
                        };

                        foreach (var templateField in tab.Fields)
                        {
                            var field = new TemplateField
                            {
                                FieldId = templateField.Id,
                                FieldName = templateField.Name,
                                FieldType = templateField.Type,
                            };
                            templateTab.Fields.Add(field);
                        }
                        model.Add(templateTab);
                    }
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
        public ActionResult Post(PostMappingViewModel model)
        {
            if (model.TemplateId == null)
                return Json(new { status = "error", message = "ContentWorkflow template isn't selected" }, JsonRequestBehavior.AllowGet);
            try
            {
                var postMappingModel = new MappingModel
                {
                    DefaultLocationId = model.DefaultLocation,
                    MappingTitle = model.CwbMappingTitle,
                    MappingId = model.ScMappingId,
                    CwbTemplate = new CwbTemplateModel { Id = model.TemplateId },
                    CmsTemplate = new CmsTemplateModel { Id = model.SelectedTemplateId },
                    FieldMappings = GetFieldMappings(model.TemplateTabs)
                };
                if (model.IsEdit)
                {
                    MappingManager.UpdateMapping(postMappingModel);
                }
                else
                {
                    MappingManager.CreateMapping(postMappingModel);
                }

                return new EmptyResult();
            }
            catch (WebException exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Log.Error("ContentWorkflow message: " + e.Message + e.StackTrace, e);

                return Json(new { status = "error", message = e.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        public ActionResult CheckIfMappingExists(string mappingName)
        {
            try
            {
                var model = new List<TemplateMappingViewModel>();
                var mappings = MappingManager.GetMappingModel();

                var ifMappingExists = mappings.Exists(x => x.MappingTitle.Equals(mappingName));

                return Json(ifMappingExists, JsonRequestBehavior.AllowGet);
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
        public ActionResult PostRelatedMapping(PostRelatedMappingViewModel model)
        {
            if (model.TemplateId == null || model.SelectedCwbRelatedMapping == null)
                return Json(new { status = "error", message = "ContentWorkflow template or related mapping isn't selected" }, JsonRequestBehavior.AllowGet);
            try
            {
                var postMappingModel = new RelatedMappingModel
                {
                    DefaultLocationId = model.DefaultLocation,
                    MappingTitle = model.CwbMappingTitle,
                    MappingId = model.ScMappingId,
                    CwbTemplate = new CwbTemplateModel { Id = model.TemplateId },
                    CmsTemplate = new CmsTemplateModel { Id = model.SelectedTemplateId },
                    FieldMappings = GetFieldMappings(model.TemplateTabs),
                    CmsRelatedMappingId = model.SelectedCwbRelatedMapping,
                    CmsContainerTemplateId = model.SelectedContainerTemplateId
                };
                if (model.IsEdit)
                {
                    MappingManager.UpdateRelatedMapping(postMappingModel);
                }
                else
                {
                    MappingManager.CreateRelatedMapping(postMappingModel);
                }

                return new EmptyResult();
            }
            catch (WebException exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Log.Error("ContentWorkflow message: " + e.Message + e.StackTrace, e);

                return Json(new { status = "error", message = e.Message }, JsonRequestBehavior.AllowGet);
            }

        }


        [HttpDelete]
        public ActionResult Delete(string scMappingId)
        {
            try
            {
                MappingManager.DeleteMapping(scMappingId);
                return new EmptyResult();
            }
            catch (WebException exception)
            {
                Log.Error("ContentWorkflow message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Log.Error("ContentWorkflow message: " + e.Message + e.StackTrace, e);

                return Json(new { status = "error", message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// used for 'test connection' button
        /// </summary>
        /// <returns></returns>
        public ActionResult Try()
        {
            try
            {
                var data = MappingManager.GetAllCwbProjects();
                return Content("OK");
            }
            catch (Exception ex)
            {
                Log.Error("ContentWorkflow message: " + ex.Message + ex.StackTrace, ex);
                return new HttpStatusCodeResult(204, ex.Message);
            }
        }
    }
}

