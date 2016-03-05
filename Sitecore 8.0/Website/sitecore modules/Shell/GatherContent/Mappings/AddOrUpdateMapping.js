var gcTemplateId = getUrlVars()["id"];
if (gcTemplateId == undefined) {
    gcTemplateId = "";
}
var scMappingId = getUrlVars()["scMappingId"];
if (scMappingId == undefined) {
    scMappingId = "";
}
var url = '/api/sitecore/mappings/GetMapping?gcTemplateId=' + gcTemplateId + "&scMappingId=" + scMappingId;

function ViewModel() {
    var self = this;

    this.Rules = ko.observableArray();
    this.SelectedGcProject = ko.observable();
    this.SelectedGcTemplate = ko.observable();
    this.SelectedScTemplate = ko.observable();
    this.IsEdit = ko.observable();
    this.ScMappingId = ko.observable();
    this.GcProjectName = ko.observable();
    this.GcTemplateName = ko.observable();
    this.GcMappingTitle = ko.observable();
    this.OpenerId = ko.observable();
    this.DefaultLocation = ko.observable();
    this.DefaultLocationTitle = ko.observable();
    this.IsShowing = ko.observable();
    this.GcProjectId = ko.observable();
    this.GcTemplateId = ko.observable();
    this.SelectedTemplateId = ko.observable();
    this.SitecoreTemplates = ko.observableArray();
    this.SelectedFields = ko.observableArray();
    this.GcProjects = ko.observableArray();
    this.GcTemplates = ko.observableArray();
    this.SitecoreFields = ko.observableArray();
    this.Tabs = ko.observableArray();
    this.ErrorText = ko.observable();
    this.IsError = ko.observable();

    self.GcProjects.push("Select GC Project");
    self.GcTemplates.push("Select GC Template");

    jQuery.getJSON(url, null, function (data) {
        if (data.status != "error") {
            self.GcProjectName("Project:" + " " + data.GcProjectName);
            self.GcTemplateName("Template:" + " " + data.GcTemplateName);
            self.GcProjects(data.GcProjects);
            self.SitecoreTemplates(data.SitecoreTemplates),
            self.SelectedFields(data.SelectedFields),
            self.Rules(data.Rules),
            self.ScMappingId(data.ScMappingId),
            self.SelectedGcProject(self.find("Id", self.GcProjects(), data.GcProjectId));
            self.SelectedScTemplate(self.find("SitrecoreTemplateId", self.SitecoreTemplates(), data.AddMappingModel.SelectedTemplateId));
            self.GcMappingTitle(data.AddMappingModel.GcMappingTitle);
            self.OpenerId(data.AddMappingModel.OpenerId);
            self.DefaultLocation(data.AddMappingModel.DefaultLocation);
            self.DefaultLocationTitle(data.AddMappingModel.DefaultLocationTitle);
            self.IsShowing(false);
            self.scTemplateChanged();
            self.gcProjectChanged();
            self.GcTemplateId(data.AddMappingModel.GcTemplateId),
            self.GcProjectId(data.AddMappingModel.GcProjectId),
            self.IsEdit(data.AddMappingModel.IsEdit);
            self.IsError(false);
        } else {
            self.ErrorText("Error:" + " " + data.message);
            self.IsError(true);
        }



        jQuery(".preloader").hide();
        tabInitSlide();
    });




    function tabInitSlide() {
        jQuery(".content_mapping").slideUp(0);
        jQuery(".title_mapping").removeClass("open");
        jQuery(jQuery(".title_mapping")[0]).addClass("open");
        jQuery(jQuery(".content_mapping")[0]).slideDown(0);
        jQuery("body").on("click", ".title_mapping", function () {
            if (jQuery(this).hasClass("open")) {
                jQuery(this).next(".content_mapping").slideUp(200);
                jQuery(this).removeClass("open");
            } else {
                jQuery(".title_mapping.open").next(".content_mapping").slideUp(200);
                jQuery(".title_mapping.open").removeClass("open");
                jQuery(this).addClass("open");
                jQuery(this).next(".content_mapping").slideDown(200);

            }
        });
    }



    this.saveMapping = function () {
        var model = new function () {
            this.TemplateTabs = self.Tabs();
            this.IsEdit = self.IsEdit();
            this.SelectedTemplateId = self.SelectedTemplateId();
            this.TemplateId = self.GcTemplateId();
            this.GcMappingTitle = self.GcMappingTitle();
            this.ScMappingId = self.ScMappingId();
            this.DefaultLocation = self.DefaultLocation();
        }

        jQuery.ajax({
            url: '/api/sitecore/mappings/Post',
            type: 'post',
            data: JSON.stringify(model),
            contentType: 'application/json',
            success: function (data) {
                if (data.status != "error") {
                    window.opener.location.reload(true);
                    window.top.dialogClose();
                } else {
                    self.ErrorText("Error:" + " " + data.message);
                    self.IsError(true);
                }
            },

        });
    };


    this.openDropTree = function () {
        var id = this.OpenerId();
        var locationId = this.DefaultLocation();

        if (!this.IsShowing()) {
            //TODO use Knockout
            jQuery("#" + id).show();
            this.IsShowing(true);
            var mapping = this;
            jQuery("#" + id).dynatree({
                autoFocus: false,
                imagePath: "~/icon/",
                initAjax: {
                    url: '/api/sitecore/Import/GetTopLevelNode?id=' + locationId,
                    data: { mode: "funnyMode" }
                },
                onActivate: function (node) {
                    jQuery('[data-openerid="' + id + '"]').val(node.data.title);
                    mapping.DefaultLocation(node.data.key);
                    mapping.DefaultLocationText(node.data.title);
                },
                onLazyRead: function (node) {
                    node.appendAjax({
                        url: "/api/sitecore/Import/GetChildrenAsJson?id=" + node.data.key,
                        data: {
                            key: node.data.key,
                            mode: "funnyMode"
                        }
                    });
                }
            });
        }
        else {
            //TODO use Knockout
            jQuery("#" + id).hide();
            this.IsShowing(false);
        }
    }

    this.scTemplateChanged = function () {
        this.SitecoreFields(self.SelectedScTemplate().SitecoreFields);
        this.SelectedTemplateId(self.SelectedScTemplate().SitrecoreTemplateId);
    }



    this.gcProjectChanged = function () {
        jQuery.getJSON('/api/sitecore/mappings/GetTemplatesByProjectId?gcProjectId=' + self.SelectedGcProject().Id, null, function (data) {
            if (data.status != "error") {
                self.GcTemplates(data);
                self.SelectedGcTemplate(self.find("Id", data, self.GcTemplateId()));
            } else {
                self.ErrorText("Error:" + " " + data.message);
                self.IsError(true);
            }
        });
    }


    this.gcTemplateChanged = function () {
        jQuery.getJSON('/api/sitecore/mappings/GetFieldsByTemplateId?gcTemplateId=' + self.SelectedGcTemplate().Id, null, function (data) {
                if (data.status != "error") {                
                    for (var t = 0; t < data.length; t++) {
                        for (var i = 0; i < data[t].Fields.length; i++) {
                            var currentElement = data[t].Fields[i];
                            var selectedItem = self.find("GcFieldId", self.SelectedFields(), data[t].Fields[i].FieldId);
                            if (selectedItem != null) {
                                currentElement.SelectedScField = selectedItem.SitecoreTemplateId;
                            }
                        }
                    }
                    self.Tabs(data);
                } else {
                    self.ErrorText("Error:" + " " + data.message);
                    self.IsError(true);
                }
            });
    }


    this.GetCurrentFields = function (item) {
        var fieldType = item.FieldType;
        var allowedFields = self.Rules()[fieldType];
        if (typeof allowedFields === 'undefined') {
            return self.SelectedScTemplate().SitecoreFields[0];
        } else {
            if (allowedFields != null) {
                var allowedFieldsArr = allowedFields.split(",");
                var currentCollection = self.SelectedScTemplate().SitecoreFields;
                var resultCollection = [];
                resultCollection.push(self.SelectedScTemplate().SitecoreFields[0]);
                for (var i = 0; i < currentCollection.length; i++) {
                    var currentElement = currentCollection[i];
                    var selectedItem = self.find("GcFieldId", self.SelectedFields(), item.FieldId);
                    if (selectedItem != null) {
                        if (currentElement.SitecoreFieldId == selectedItem.SitecoreTemplateId) {
                            currentElement.Selected = true;
                        }
                    }
                    for (var f = 0; f < allowedFieldsArr.length; f++) {
                        var field = allowedFieldsArr[f];
                        if (currentElement.SitecoreFieldType == field.trim()) {
                            resultCollection.push(currentElement);
                        }
                    }
                }
                return resultCollection;
            }
        }
        return self.SelectedScTemplate().SitecoreFields[0];
    };



    this.returnFieldName = function (item) {
        if (item.FieldName === null) {
            return "[Empty]" + " (" + item.FieldId + ")";
        } else {
            return item.FieldName;
        }
    };



    this.find = function (prop, array, data) {
        return ko.utils.arrayFirst(array, function (item) {
            return item[prop] === data;
        });
    };
};

jQuery(window).resize(function () {
    jQuery(".tabs_mapping").css("max-height", jQuery(".gathercontent-dialog").height() - 240);
});
