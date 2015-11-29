var id = getUrlVars()["id"];
var name = getUrlVars()["name"];
var url = '/sitecore/api/mapping/' + id + '?projectName=' + name;

function ViewModel() {
    var self = this;
 
    this.SelectedTemplate = ko.observable();
    this.IsEdit = ko.observable();
    this.GcProjectName = ko.observable();
    this.GcTemplateName = ko.observable();
    this.GcTemplateId = ko.observable();
    this.SelectedTemplateId = ko.observable();
    this.SitecoreTemplates = ko.observableArray();
    this.SitecoreFields = ko.observableArray();
    this.Tabs = ko.observableArray();

    jQuery.getJSON(url, null, function (data) {
        self.GcProjectName("Project:" + " " + data.GcProjectName);
        self.GcTemplateName("Template:" + " " + data.GcTemplateName);    
        self.SitecoreTemplates(data.SitecoreTemplates),
        self.SelectedTemplate(self.find("SitrecoreTemplateId", data.AddMappingModel.SelectedTemplateId));
        self.templateChanged();
        self.GcTemplateId(data.AddMappingModel.GcTemplateId),
        self.Tabs(data.AddMappingModel.Tabs);
        self.IsEdit(data.AddMappingModel.IsEdit);
        jQuery(".preloader").hide();
    });



    this.saveMapping = function () {
        var dataObject = ko.toJSON(this);

        jQuery.ajax({
            url: '/sitecore/api/postmappings',
            type: 'post',
            data: dataObject,
            contentType: 'application/json',
            success: function () {
                //parent.location.reload();
                window.top.dialogClose();
            }
        });
    };



    this.templateChanged = function () {
        this.SitecoreFields(self.SelectedTemplate().SitecoreFields);
        this.SelectedTemplateId(self.SelectedTemplate().SitrecoreTemplateId);
    }


    this.find = function (prop, data) {
        return ko.utils.arrayFirst(self.SitecoreTemplates(), function (item) {
            return item[prop] === data;
        });
    };
 
};

