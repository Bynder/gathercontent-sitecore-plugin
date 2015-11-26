
function ViewModel() {
    var self = this;

    this.mappings = ko.observableArray();

    jQuery.getJSON('/sitecore/api/mappings', null, function(data) {
        self.mappings(data);
    });


    editMapping = function () {
        var id = this.GcTemplateId;
        openMappingWindow(id);
    };


    addMoreTemplates = function () {
        openTemplateWindow();
    }
}


function openTemplateWindow() {
    var id = getUrlVars()["id"];
    scForm.showModalDialog("/sitecore modules/shell/gathercontent/AddTemplate/AddTemplate.html?id=" + id, null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:400;dialogMinWidth:600;dialogWidth:800;dialogHeight:600;header: Setup template mapping");
};


function openMappingWindow(id) {
    scForm.showModalDialog("/sitecore modules/shell/gathercontent/Mappings/AddOrUpdateMapping.html?id=" + id,
        null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:400;dialogMinWidth:600;dialogWidth:800;dialogHeight:600;header: Manage template mappings");
};