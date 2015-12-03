
function ViewModel() {
    var self = this;

    this.mappings = ko.observableArray();


    jQuery.ajax({
        type: 'GET',
        url: '/sitecore/api/mappings',
        dataType: 'json',
        success: function (data) {
            self.mappings(data);
            jQuery(".preloader").hide();
        },
        async: false
    });


    editMapping = function () {
        var id = this.GcTemplateId;
        var name = this.GcProjectName;
        openMappingWindow(id, name);
    };


    removeMapping = function () {

        var id = this.GcTemplateId;

        var confirmDelete = confirm('Are you sure you want to delete this?');
        if (confirmDelete) {
            jQuery.ajax({
                type: 'DELETE',
                url: '/sitecore/api/removemapping?id='+id,
                //dataType: 'json',
                success: function () {
                    self.mappings.remove(function (mapping) {
                        return mapping.GcTemplateId == id;
                    });
                },
            });
        } 
    };


    addMoreTemplates = function () {
        openTemplateWindow();
    }
}


function openTemplateWindow() {
    var id = getUrlVars()["id"];
    scForm.showModalDialog("/sitecore modules/shell/gathercontent/AddTemplate/AddTemplate.html?id=" + id, null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:495;dialogMinWidth:600;dialogWidth:800;dialogHeight:495;header: Setup template mapping");
};


function openMappingWindow(id, name) {
    scForm.showModalDialog("/sitecore modules/shell/gathercontent/Mappings/AddOrUpdateMapping.html?id=" + id + "&name=" + name,
        null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:600;dialogMinWidth:700;dialogWidth:700;dialogHeight:800;header: Manage template mappings");
};

