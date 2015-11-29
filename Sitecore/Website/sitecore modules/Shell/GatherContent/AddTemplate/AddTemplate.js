
function ViewModel() {
    var self = this;

    this.Projects = ko.observableArray();
    this.Selected = ko.observableArray();

    jQuery.ajax({
        type: 'GET',
        url: '/sitecore/api/templates',
        dataType: 'json',
        success: function (data) {
            self.Projects(data.Projects);
            self.Selected(data.Selected);
            jQuery(".preloader").hide();
        },
        async: true
    });

    closeDialog = function() {
        setTimeout(window.top.dialogClose(), 1000);
    }
    
    addTemplateMapping = function () {
        var dataObject = ko.toJSON(this);
        jQuery.ajax({
            url: '/sitecore/api/posttemplates',
            type: 'post',
            data: dataObject,
            contentType: 'application/json',
            success: function () {
                //window.parent.location.reload();
                //setTimeout(window.top.dialogClose(), 1000);
                window.top.dialogClose();
            }
        });
    };
}




//TODO use KO
jQuery(document).ready(function () {
    jQuery(document).on("click", ".project-item", function () {
        jQuery(".project-item").children('ul').hide();
        jQuery(this).children('ul').show();
        jQuery(".project-item").removeClass("active");
        jQuery(this).addClass("active");

    });
});