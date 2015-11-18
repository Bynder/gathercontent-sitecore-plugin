function getUrlVars() {
    var vars = [], hash;
    var hashes = location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (var i = 0; i < hashes.length; i++) {
        hash = hashes[i].split('=');
        vars.push(hash[0]);
        vars[hash[0]] = hash[1];
    }
    return vars;
}

var id = getUrlVars()["id"];
jQuery.getJSON('/sitecore/api/mappings?id={' + id + '}', null, function (data) {
    var viewModel = {
        mappings: ko.observableArray(data)
    };
    ko.applyBindings(viewModel);
});



jQuery(document).on("click", "#AddMore", function (e) {
    openTemplateWindow();
    e.preventDefault();
});



function openTemplateWindow() {
    var id = getUrlVars()["id"];
    var language = getUrlVars()["l"];
    var database = getUrlVars()["d"];
    var version = getUrlVars()["v"];

    // open window
    var query = "gc:addtemplate(id=" + id + ", language = " + language + ", database = " + database
        + ", version = " + version + ")";

    scForm.postRequest("", "", "", query);
};