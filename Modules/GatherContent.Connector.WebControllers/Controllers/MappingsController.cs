using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Web.Mvc;
using GatherContent.Connector.Managers.Managers;
using GatherContent.Connector.Managers.Models.Mapping;
using GatherContent.Connector.WebControllers.Models.Mapping;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;
using TemplateTab = GatherContent.Connector.WebControllers.Models.Mapping.TemplateTab;

namespace GatherContent.Connector.WebControllers.Controllers
{
    public class MappingsController : SitecoreController
    {
        private readonly MappingManager _mappingManager;

        public MappingsController()
        {
            _mappingManager = new MappingManager();
        }


        #region Utilities

        private Dictionary<string, string> GetMapRules()
        {
            return new Dictionary<string, string>
            {
                {"text", "Single-Line Text, Multi-Line Text, Rich Text"},
                {"section", "Single-Line Text, Multi-Line Text, Rich Text"},
                {"choice_radio", "Droptree, Checklist, Multilist, Multilist with Search, Treelist, TreelistEx"},
                {"choice_checkbox", "Checklist, Multilist, Multilist with Search, Treelist, TreelistEx"},
                {"files", "Image, File, Droptree, Multilist, Multilist with Search, Treelist, TreelistEx"}
            };
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
                            GcFieldId = field.FieldId,
                            GcFieldName = field.FieldName
                        };
                        fieldMappings.Add(fieldMapping);
                    }
                }
            }
            return fieldMappings;
        }


        #endregion



        public ActionResult Get()
        {
            try
            {
                var model = new List<TemplateMappingViewModel>();
                var mappings = _mappingManager.GetMappingModel();
                foreach (var mapping in mappings)
                {
                    var mappingModel = new TemplateMappingViewModel
                    {
                        GcProjectName = mapping.GcProjectName,
                        GcTemplateId = mapping.GcTemplateId,
                        GcTemplateName = mapping.GcTemplateName,
                        ScTemplateName = mapping.ScTemplateName,
                        ScMappingId = mapping.ScMappingId,
                        MappingTitle = mapping.MappingTitle,
                        LastMappedDateTime = mapping.LastMappedDateTime,
                        LastUpdatedDate = mapping.LastUpdatedDate,
                        IsMapped = mapping.LastMappedDateTime != "never",
                        RemovedFromGc = mapping.LastUpdatedDate == "Removed from GatherContent"
                    };

                    if (mappingModel.IsMapped)
                    {
                        var lastMappedDate = DateTime.ParseExact(mapping.LastMappedDateTime, "dd/MM/yyyy hh:mm tt",
                            CultureInfo.InvariantCulture);
                        var lastUpdateDate = DateTime.ParseExact(mapping.LastUpdatedDate, "dd/MM/yyyy hh:mm tt",
                           CultureInfo.InvariantCulture);
                        mappingModel.IsHighlightingDate = lastMappedDate < lastUpdateDate;
                    }

                    model.Add(mappingModel);
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
                return Json(new {status = "error", message = exception.Message}, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetMapping(string gcTemplateId, string scMappingId)
        {
            try
            {             
                var mappingModel = _mappingManager.GetSingleMappingModel(gcTemplateId, scMappingId);
              
                var model = new TemplateMapViewModel
                {
                    GcProjectName = mappingModel.GcProjectName,
                    GcTemplateName = mappingModel.GcTemplateName,
                    ScMappingId = mappingModel.MappingId,
                    GcProjectId = mappingModel.GcProjectId,
                    AddMappingModel = new AddMappingViewModel
                    {
                        DefaultLocationTitle = mappingModel.DefaultLocationTitle,
                        DefaultLocation = mappingModel.DefaultLocation,
                        GcMappingTitle = mappingModel.MappingTitle,
                        OpenerId = "drop-tree" + Guid.NewGuid(),
                        GcTemplateId = mappingModel.GcTemplateId,
                        SelectedTemplateId = mappingModel.CmsTemplateId,
                    },
                };


                foreach (var fieldMapping in mappingModel.SelectedFields)
                {
                    model.SelectedFields.Add(new FieldMappingViewModel
                    {
                        SitecoreTemplateId = fieldMapping.CmsTemplateId,
                        GcFieldId = fieldMapping.GcFieldId                
                    });
                }


                #region Available templates

                var sitecoreTemplates = new List<SitecoreTemplateViewModel>();
                var availableCmsTemplates = _mappingManager.GetAvailableTemplates();
                foreach (var template in availableCmsTemplates)
                {
                  var st = new SitecoreTemplateViewModel
                    {
                        SitrecoreTemplateId = template.Id,
                        SitrecoreTemplateName = template.Name
                    };
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


                model.Rules = GetMapRules();

                var projects =  _mappingManager.GetAllGcProjects();

                foreach (var project in projects)
                {
                    model.GcProjects.Add(new ProjectViewModel
                    {
                        Id = project.Id,
                        Name = project.Name
                    });
                }

                if (string.IsNullOrEmpty(gcTemplateId) && string.IsNullOrEmpty(scMappingId))
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
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                
                return Json(new { status = "error", message = exception.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetTemplatesByProjectId(string gcProjectId)
        {
            try
            {
                var model = new List<TemplateViewModel>();
                var templates = _mappingManager.GetTemplatesByProjectId(gcProjectId);
                foreach (var template in templates)
                {
                    var templateModel = new TemplateViewModel
                    {
                        Id = template.Id,
                        Name = template.Name,
                    };

                    model.Add(templateModel);
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

        public ActionResult GetFieldsByTemplateId(string gcTemplateId)
        {
            try
            {
                var model = new List<TemplateTab>();
                var tabs = _mappingManager.GetFieldsByTemplateId(gcTemplateId);         

                foreach (var tab in tabs)
                {
                    var templateTab = new TemplateTab
                    {
                        TabName = tab.TabName
                    };

                    foreach (var templateField in tab.Fields)
                    {
                        var field = new Models.Mapping.TemplateField
                        {
                            FieldId = templateField.Id,
                            FieldName = templateField.Name,
                            FieldType = templateField.Type,
                        };
                        templateTab.Fields.Add(field);
                    }
                    model.Add(templateTab);
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

        [HttpPost]
        public ActionResult Post(PostMappingViewModel model)
        {
            if (model.TemplateId == null)
                return Json(new { status = "error", message = "GatherContent template isn't selected" }, JsonRequestBehavior.AllowGet);
            try
            {
                var postMappingModel = new PostMappingModel
                {
                    DefaultLocation   = model.DefaultLocation,
                    MappingTitle = model.GcMappingTitle,
                    MappingId = model.ScMappingId,
                    GcTemplateId = model.TemplateId,
                    CmsTemplateId = model.SelectedTemplateId,
                    FieldMappings = GetFieldMappings(model.TemplateTabs)
                };
                if (model.IsEdit)
                {
                    _mappingManager.UpdateMapping(postMappingModel);
                }
                else
                {
                    _mappingManager.CreateMapping(postMappingModel);
                }
                
                return new EmptyResult();
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Log.Error("GatherContent message: " + e.Message + e.StackTrace, e);
                
                return Json(new { status = "error", message = e.Message }, JsonRequestBehavior.AllowGet);
            }

        }

        [HttpDelete]
        public ActionResult Delete(string scMappingId)
        {
            try
            {
                _mappingManager.DeleteMapping(scMappingId);
                return new EmptyResult();
            }
            catch (WebException exception)
            {
                Log.Error("GatherContent message: " + exception.Message + exception.StackTrace, exception);
                return Json(new { status = "error", message = exception.Message + " Please check your credentials" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                Log.Error("GatherContent message: " + e.Message + e.StackTrace, e);
                
                return Json(new { status = "error", message = e.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}

