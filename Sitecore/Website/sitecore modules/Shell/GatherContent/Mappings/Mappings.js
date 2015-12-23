
function ViewModel() {
    var self = this;

    this.mappings = ko.observableArray();
    this.errorText = ko.observable();

    jQuery.getJSON('/api/sitecore/mappings/Get', function () {
    })
        .success(function (data) {
            if (data.status != "error") {
                self.mappings(data);
            }
            self.errorText(data.message);
            jQuery(".preloader").hide();
        });


    editMapping = function () {
        var id = this.GcTemplateId;
        scForm.showModalDialog("/sitecore modules/shell/gathercontent/Mappings/AddOrUpdateMapping.html?id=" + id,
          null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:600;dialogMinWidth:700;dialogWidth:700;dialogHeight:800;header: Manage Field Mappings");
    };


    removeMapping = function () {

        var id = this.GcTemplateId;

        var confirmDelete = confirm('Are you sure you want to delete this?');
        if (confirmDelete) {
            jQuery.ajax({
                type: 'DELETE',
                url: '/api/sitecore/mappings/Delete?id=' + id,
                //dataType: 'json',
                success: function () {
                    self.mappings.remove(function (mapping) {
                        return mapping.GcTemplateId == id;
                    });
                },
                error: function (data) {
                    self.errorText(data.message);
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
jQuery(window).resize(function () {
    jQuery(".table_mappings_scroll").css("max-height", jQuery(".gathercontent-dialog").height() - 160)
})

