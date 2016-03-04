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
                    if (field.SelectedField != "0")
                    {
                        var fieldMapping = new FieldMappingModel
                        {
                            CmsTemplateId = field.SelectedField,
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

        public ActionResult GetMapping(string id, string scMappingId)
        {
            try
            {             
                var mappingModel = _mappingManager.GetSingleMappingModel(id, scMappingId);

              
                var model = new GatherContent.Connector.WebControllers.Models.Mapping.TemplateMapModel
                {
                    GcProjectName = mappingModel.GcProjectName,
                    GcTemplateName = mappingModel.GcTemplateName,
                    ScMappingId = mappingModel.MappingId
                };

             


                var tabs = new List<TemplateTab>();

                foreach (var tab in mappingModel.AddMappingModel.Tabs)
                {
                    var templateTab = new TemplateTab
                    {
                        TabName = tab.TabName
                    };
                    
                    foreach (var templateField in tab.Fields)
                    {
                        var field = new GatherContent.Connector.WebControllers.Models.Mapping.TemplateField();
                        field.FieldId = templateField.Id;
                        field.FieldName = templateField.Name;
                        field.FieldType = templateField.Type;
                        field.SelectedField = templateField.SelectedFieldId;
                        templateTab.Fields.Add(field);
                    }
                    tabs.Add(templateTab);
                }


                model.AddMappingModel = new AddMappingViewModel
                {
                    DefaultLocationTitle = mappingModel.AddMappingModel.DefaultLocationTitle,
                    DefaultLocation = mappingModel.AddMappingModel.DefaultLocation,
                    GcMappingTitle = mappingModel.AddMappingModel.GcMappingTitle,
                    OpenerId = "drop-tree" + Guid.NewGuid(),
                    GcTemplateId = mappingModel.AddMappingModel.GcTemplateId,
                    SelectedTemplateId = mappingModel.AddMappingModel.CmsTemplateId,
                    Tabs = tabs

                };


                if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(scMappingId))
                {
                    model.AddMappingModel.IsEdit = false;
                }
                else
                {
                    model.AddMappingModel.IsEdit = true;
                }


                var sitecoreTemplates = new List<SitecoreTemplate>();

                foreach (var template in mappingModel.AvailableCmsTemplates)
                {
                  var st = new SitecoreTemplate
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


                model.Rules = GetMapRules();
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

