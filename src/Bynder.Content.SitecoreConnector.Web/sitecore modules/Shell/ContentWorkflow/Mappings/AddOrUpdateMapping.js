var cwbTemplateId = getUrlVars()["id"];
if (cwbTemplateId == undefined) {
    cwbTemplateId = "";
}
var scMappingId = getUrlVars()["scMappingId"];
if (scMappingId == undefined) {
    scMappingId = "";
}
var url = '/api/sitecore/mappings/GetMapping?cwbTemplateId=' + cwbTemplateId + "&scMappingId=" + scMappingId;


function Init() {
    var resultData;

    jQuery.ajax({
        url: url,
        dataType: 'json',
        async: false,
        success: function (data) {
            resultData = data;
            resultData.IsError = false;
            resultData.IsShowing = false;
            jQuery(".preloader").hide();
        }
    });

    return resultData;
}


function ViewModel(data) {
    var self = this;

    this.NotValid = ko.observable(false);
    this.NotValidCwbProject = ko.observable(false);
    this.NotValidCwbTemplate = ko.observable(false);
    this.NotValidScTemplate = ko.observable(false);

    this.validate = function () {
        var isValid = true;
        if (self.CwbMappingTitle() == null || self.CwbMappingTitle() == "") {
            self.NotValid(true);
            isValid = false;
        }
        if (self.SelectedCwbProject().Id == "0") {
            self.NotValidCwbProject(true);
            isValid = false;
        }
        if (self.SelectedTemplateId() == "0") {
            self.NotValidScTemplate(true);
            isValid = false;
        }

        if (self.SelectedCwbTemplate().Id == "0") {
            self.NotValidCwbTemplate(true);
            isValid = false;
        }

        return isValid;
    };

    //Methods
    this.saveMapping = function () {

        if (self.validate()) {

            var model = new function () {
                this.TemplateTabs = self.Tabs();
                this.IsEdit = self.IsEdit();
                this.SelectedTemplateId = self.SelectedTemplateId();
                this.TemplateId = self.SelectedCwbTemplate().Id;
                this.CwbMappingTitle = self.CwbMappingTitle();
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

        } else {
            //self.ValidationMessage("Mapping Title is mandatory  field");
            //self.NotValid(true);
        }
    };

    this.closeDropTree = function (model, e) {
        if (e.target.tagName === "INPUT") {
            return;
        }

        var id = this.OpenerId();
        jQuery("#" + id).hide();
        this.IsShowing(false);
    };

    this.openDropTree = function () {
        var id = this.OpenerId();
        var locationId = this.DefaultLocation();

        var t = this;

        if (!this.IsShowing()) {
            //TODO use Knockout
            jQuery("#" + id).show();
            this.IsShowing(true);
            var mapping = this;
            jQuery("#" + id).dynatree({
                autoFocus: false,
                imagePath: "~/icon/",
                initAjax: {
                    url: '/api/sitecore/DropTree/GetTopLevelNode?id=' + locationId,
                    data: { mode: "funnyMode" }
                },
                onActivate: function (node) {
                    var path = "";
                    var keys = node.getKeyPath().split("/");
                    keys.shift();

                    for (var i = 0; i < keys.length; i++) {
                        if (i != keys.length - 1) {
                            path += node.tree.getNodeByKey(keys[i]).data.title + " / ";
                        } else {
                            path += node.tree.getNodeByKey(keys[i]).data.title;
                        }
                    }

                    jQuery("#" + id).hide();
                    t.IsShowing(false);

                    jQuery('[data-openerid="' + id + '"]').val(path);
                    mapping.DefaultLocation(node.data.key);
                    mapping.DefaultLocationText(node.data.title);
                },
                onLazyRead: function (node) {
                    node.appendAjax({
                        url: "/api/sitecore/DropTree/GetChildren?id=" + node.data.key,
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

    this.cwbProjectChanged = function () {

        if (self.SelectedCwbProject != undefined && self.SelectedCwbProject() != null) {
            jQuery.ajax({
                url: '/api/sitecore/mappings/GetTemplatesByProjectId?cwbProjectId=' + self.SelectedCwbProject().Id,
                dataType: 'json',
                async: false,
                success: function (data) {
                    if (data.status != "error") {
                        self.CwbTemplates(data);
                        if (self.IsEdit()) {
                            self.SelectedCwbTemplate(self.find("Id", data, self.CwbTemplateId()));
                        } else {
                            self.SelectedCwbTemplate(data[0]);
                        }

                        self.cwbTemplateChanged();
                    } else {
                        self.ErrorText("Error:" + " " + data.message);
                        self.IsError(true);
                    }
                }
            });
        }
    }

    this.cwbTemplateChanged = function () {
        if (self.SelectedCwbTemplate != undefined && self.SelectedCwbTemplate() != null) {
            jQuery.ajax({
                url: '/api/sitecore/mappings/GetFieldsByTemplateId?cwbTemplateId=' + self.SelectedCwbTemplate().Id,
                dataType: 'json',
                async: false,
                success: function (data) {
                    if (data.status != "error") {
                        for (var t = 0; t < data.length; t++) {
                            for (var i = 0; i < data[t].Fields.length; i++) {
                                var currentElement = data[t].Fields[i];
                                var selectedItem = self.find("CwbFieldId", self.SelectedFields(), data[t].Fields[i].FieldId);
                                if (selectedItem != null) {
                                    currentElement.SelectedScField = selectedItem.SitecoreTemplateId;
                                    currentElement.OptionsContentFolderId = selectedItem.OptionsContentFolderId;
                                    currentElement.OptionsTemplateId = selectedItem.OptionsTemplateId;
                                }
                            }
                        }
                        self.Tabs(data);
                        tabSlideFirst();
                    } else {
                        self.ErrorText("Error:" + " " + data.message);
                        self.IsError(true);
                    }
                }
            });
        }
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
                    var selectedItem = self.find("CwbFieldId", self.SelectedFields(), item.FieldId);
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

    this.GetOptionContentFolders = function (item) {
        var currentCollection = self.SelectedScOptionContentFolders();
        return currentCollection;
    };

    this.GetOptionTemplates = function (item) {
        var currentCollection = self.SelectedScOptionTemplates();
        return currentCollection;
    };

    this.RenderOptionSettings = function (item) {
        var fieldType = item.FieldType;
        if (fieldType === "choice_checkbox" || fieldType === "choice_radio") {
            return true;
        }
        else {
            return false;
        }
    };

    this.returnFieldName = function (item) {
        if (item.FieldName === null) {
            return "[Guideline]";
        } else {
            return item.FieldName;
        }
    };

    this.find = function (prop, array, property) {
        return ko.utils.arrayFirst(array, function (item) {
            return item[prop] === property;
        });
    };


    //Fields
    this.Rules = ko.observable(data.Rules);
    this.IsEdit = ko.observable(data.IsEdit);
    this.ScMappingId = ko.observable(data.ScMappingId);
    this.CwbMappingTitle = ko.observable(data.AddMappingModel?.CwbMappingTitle);
    this.OpenerId = ko.observable(data.AddMappingModel?.OpenerId);
    this.DefaultLocation = ko.observable(data.AddMappingModel?.DefaultLocation);
    this.DefaultLocationTitle = ko.observable(data.AddMappingModel?.DefaultLocationTitle);
    this.IsShowing = ko.observable(data.IsShowing);
    this.CwbProjectId = ko.observable(data.AddMappingModel?.CwbProjectId);
    this.CwbTemplateId = ko.observable(data.AddMappingModel?.CwbTemplateId);
    this.SelectedTemplateId = ko.observable();
    this.SitecoreTemplates = ko.observableArray(data.SitecoreTemplates);
    this.OptionFolders = ko.observableArray(data.OptionFolders);
    this.SelectedFields = ko.observableArray(data.SelectedFields);
    this.CwbProjects = ko.observableArray(data.CwbProjects);
    this.CwbTemplates = ko.observableArray();
    this.SitecoreFields = ko.observableArray();
    this.Tabs = ko.observableArray();

    this.ErrorText = ko.observable();
    this.IsError = ko.observable(data.IsError);
    this.ValidationMessage = ko.observable();

    if (data.IsEdit) {
        this.SelectedCwbProject = ko.observable(self.find("Id", data.CwbProjects, data.CwbProjectId));
        this.SelectedTemplateId(data.AddMappingModel.SelectedTemplateId);
    } else {
        this.SelectedCwbProject = ko.observable(data.CwbProjects[0]);
        this.SelectedTemplateId(this.SitecoreTemplates()[0].SitrecoreTemplateId);
    }
    this.SelectedCwbTemplate = ko.observable();
    this.SelectedScTemplate = ko.observable(self.find("SitrecoreTemplateId", data.SitecoreTemplates, data.AddMappingModel.SelectedTemplateId));
    this.SelectedScOptionContentFolders = ko.observable(data.OptionsContentFolders);
    this.SelectedScOptionTemplates = ko.observable(data.OptionsTemplates);

    self.cwbProjectChanged();


};



function tabSlideFirst() {
    jQuery(".content_mapping").slideUp(0);
    jQuery(".title_mapping").removeClass("open");
    jQuery(jQuery(".title_mapping")[0]).addClass("open");
    jQuery(jQuery(".content_mapping")[0]).slideDown(0);
}
function tabInitSlide() {

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

jQuery(window).resize(function () {
    jQuery(".tabs_mapping").css("max-height", jQuery(".gathercontent-dialog").height() - 240);
});
