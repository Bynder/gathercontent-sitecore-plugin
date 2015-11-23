var ImportManager = function () {
    var MODE = {
        ChooseItmesForImort: 1,
        CheckItemsBeforeImport: 2,
        Import: 3,
        ImportResult: 4,
        Close: 5
    };

    var allItems = [];
    var self = this;

    self.currentMode = ko.observable(MODE.ChooseItmesForImort);
    self.projects = ko.observableArray([]),
    self.items = ko.observableArray([]),
    self.statuses = ko.observableArray([]),
    self.templates = ko.observableArray([]),

    self.project = ko.observable(),
    self.statusFilter = ko.observable(),
    self.templateFilter = ko.observable(),

    self.query = ko.observable(''),

    self.init = function () {
        var id = getUrlVars()["id"];
        jQuery.getJSON('/sitecore/api/getItemsForImort?id={' + id + '}&projectId=0', null, function (results) {
            var items = MapItems(results.Items);
            self.items(items);
            allItems = items.slice(0);

            self.projects(results.Projects);
            self.project(results.Project);

            self.statuses(results.Statuses);

            self.templates(results.Templates);
        });
    },

    self.projectChanged = function (obj, event) {
        if (event.originalEvent) {
            var id = getUrlVars()["id"];
            jQuery.getJSON('/sitecore/api/getItemsForImort?id={' + id + '}&projectId=' + self.project(), null, function (results) {
                var items = MapItems(results.Items);
                self.items(items);
                allItems = items.slice(0);

                self.projects(results.Projects);
                self.project(results.Project);

                self.statuses(results.Statuses);

                self.templates(results.Templates);

                self.query('');
                self.statusFilter();
                self.templateFilter();
            });
        }
    },

    self.filter = function () {
        self.items.removeAll();

        var currentCollection = allItems.slice(0);
        currentCollection = self.search(currentCollection);
        currentCollection = self.filterByStatus(currentCollection);
        currentCollection = self.filterByTemplate(currentCollection);

        self.items(currentCollection);
    },

    self.search = function (currentCollection) {
        var resultCollection = currentCollection;
        var value = self.query();
        if (value !== '') {
            resultCollection = [];
            for (var i = 0; i < currentCollection.length; i++) {
                var currentElement = currentCollection[i];
                if (currentElement.Title.toLowerCase().indexOf(value.toLowerCase()) >= 0) {
                    resultCollection.push(currentElement);
                }
            }
        }

        return resultCollection;
    },

    self.filterByStatus = function (currentCollection) {
        var resultCollection = currentCollection;
        var value = self.statusFilter();
        if (value) {
            resultCollection = [];
            for (var i = 0; i < currentCollection.length; i++) {
                var currentElement = currentCollection[i];
                if (currentElement.Status.Id.toLowerCase() === value.toLowerCase()) {
                    resultCollection.push(currentElement);
                }
            }
        }

        return resultCollection;
    },

    self.filterByTemplate = function (currentCollection) {
        var resultCollection = currentCollection;
        var value = self.templateFilter();
        if (value) {
            resultCollection = [];
            for (var i = 0; i < currentCollection.length; i++) {
                var currentElement = currentCollection[i];
                if (currentElement.Template.Id.toLowerCase() === value.toLowerCase()) {
                    resultCollection.push(currentElement);
                }
            }
        }

        return resultCollection;
    }

    self.AllChecked = ko.computed({
        read: function () {
            var items = self.items();
            if (items.length === 0)
                return false;

            var firstUnchecked = ko.utils.arrayFirst(items, function (item) {
                if (item.Checked)
                    return item.Checked() === false;
            });

            return firstUnchecked == null;
        },
        write: function (value) {
            ko.utils.arrayForEach(self.items(), function (item) {
                item.Checked(value);
            });
        }
    });

    self.getCheckedCount = ko.computed(function () {
        var counter = 0;
        ko.utils.arrayForEach(self.items(), function (item) {
            if (item.Checked && item.Checked() === true)
                counter++;
        });

        return counter;
    });

    self.afterStatusesSelectRender = function (option, status) {
        if (status.Color) {
            option.style.color = status.Color;
        }
    };

    self.getSelectedStatusColor = function () {
        var result = "";
        ko.utils.arrayForEach(self.statuses(), function (status) {
            if (status.Id.toLowerCase() === self.statusFilter()) {
                result = status.Color;
                return;
            }
        });

        return result;
    }

    self.checkRow = function() {
        this.Checked(!this.Checked());
    }
    
    self.query.subscribe(self.filter);

    self.switchToCheckItemsBeforeImport = function () {
        var result = [];
        ko.utils.arrayForEach(self.items(), function (item) {
            if (item.Checked && item.Checked() === true)
                result.push(item);
        });

        self.items(result);
    }

    self.import = function () {
        var id = getUrlVars()["id"];
        var items = self.items();
        var status = self.statusFilter();
        jQuery.ajax
        ({
            type: "POST",
            url: '/sitecore/api/import?id={' + id + '}&statusId=' + status,
            dataType: 'json',
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(items),
            success: function (response) {
                self.buttonClick(MODE.ImportResult);
                self.items(response.Items);
            }
        });
    }

    self.close = function () {
        window.top.dialogClose();
    }

    self.buttonClick = function (newMode) {
        self.currentMode(newMode);
        if (newMode === MODE.CheckItemsBeforeImport)
            self.switchToCheckItemsBeforeImport();
        else if (newMode === MODE.Import)
            self.import();
        else if (newMode === MODE.Close)
            self.close();
        else if (newMode === MODE.ChooseItmesForImort)
            self.backButtonClick();
    }

    self.getMode = function (section) {
        if (self.currentMode() === section) {
            return true;
        }
        return false;
    }

    self.backButtonClick = function() {
        self.items(allItems);
    }

    self.init();
}

function MapItems(items) {

    for (var i = 0; i < items.length; i++) {
        items[i].Checked = ko.observable(false);
    }

    return items;
}

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