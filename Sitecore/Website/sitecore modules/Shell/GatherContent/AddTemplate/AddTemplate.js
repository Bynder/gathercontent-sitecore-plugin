
function ViewModel() {
    var self = this;

    this.Projects = ko.observableArray();
    this.Selected = ko.observableArray();

    jQuery.getJSON('/sitecore/api/templates', null, function (data) {
        self.Projects(data.Projects);
        self.Selected(data.Selected);
    });


    addTemplateMapping = function () {
        var dataObject = ko.toJSON(this);
        jQuery.ajax({
            url: '/sitecore/api/posttemplates',
            type: 'post',
            data: dataObject,
            contentType: 'application/json',
            success: function () {
                parent.location.reload();
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