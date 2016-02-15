
function ViewModel() {
    var self = this;

    this.mappings = ko.observableArray();
    this.errorText = ko.observable();
    this.isError = ko.observable();

    jQuery.getJSON('/api/sitecore/mappings/Get', function (data) {
        if (data.status != "error") {
            self.mappings(data);
            self.isError(false);
        } else {
            self.errorText("Error:" + " " + data.message);
            self.isError(true);
        }
        jQuery(".preloader").hide();
        resizeTableHead();
    });


    editMapping = function () {
        var id = this.GcTemplateId;
        var gcTemplateProxyId = this.GcTemplateProxy;
        scForm.showModalDialog("/sitecore modules/shell/gathercontent/Mappings/AddOrUpdateMapping.html?id=" + id + "&gcTemplateProxyId=" + gcTemplateProxyId,
            null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:600;dialogMinWidth:700;dialogWidth:700;dialogHeight:800;header: Manage Field Mappings");
    };


    removeMapping = function () {

        var id = this.GcTemplateId;
        var gcTemplateProxyId = this.GcTemplateProxy;

        var confirmDelete = confirm('Are you sure you want to delete this?');
        if (confirmDelete) {
            jQuery.ajax({
                type: 'DELETE',
                url: '/api/sitecore/mappings/Delete?id=' + id + "&gcTemplateProxyId=" + gcTemplateProxyId,
                success: function () {
                    self.mappings.remove(function (mapping) {
                        return mapping.GcTemplateProxy == gcTemplateProxyId;
                    });
                    self.isError(false);
                },
                error: function (data) {
                    self.errorText("Error:" + " " + data.message);
                    self.isError(true);
                }
            });
        }
    };


    addMoreTemplates = function () {
        var id = getUrlVars()["id"];
        scForm.showModalDialog("/sitecore modules/shell/gathercontent/AddTemplate/AddTemplate.html?id=" + id, null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:550;dialogMinWidth:600;dialogWidth:800;dialogHeight:550;header: Setup template mapping");
    }


    openImportPopup = function () {
        scForm.showModalDialog("/sitecore modules/shell/gathercontent/import/import.html", null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:400;dialogMinWidth:881;dialogWidth:1200;dialogHeight:700;header: Import Content from GatherContent");
    }

}

jQuery(window).resize(function() {
    jQuery(".table_mappings_scroll").css("max-height", jQuery(".gathercontent-dialog").height() - 155);
    jQuery(".tabs_mapping").css("max-height", jQuery(".gathercontent-dialog").height() - 255);
    jQuery("thead th.cell_resize").each(function() {
        jQuery(this).find("div").css("width", jQuery(this).width());
    });
});

jQuery(function () {
    resizeTableHead();
});