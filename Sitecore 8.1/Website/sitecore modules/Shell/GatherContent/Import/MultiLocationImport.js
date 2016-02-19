//var Mapping = function (data) {
//    var self = this;

//    self.Id = ko.observable(data.Id);
//    self.Title = ko.observable(data.Title);
//    self.ScTemplate = ko.observable(data.ScTemplate);
//    self.OpenerId = ko.observable(data.OpenerId);
//    self.IsShowing = ko.observable(data.IsShowing);
//    self.IsImport = ko.observable(data.IsImport);
//    self.DefaultLocation = ko.observable(data.DefaultLocation);
//}

//var AvailableMapping = function (data) {
//    var self = this;

//    self.SelectedMappingId = ko.observable(data.SelectedMappingId);
//    self.Mappings = ko.observableArray();

//    var items = ko.utils.arrayMap(data.Mappings, function (item) {
//        return new Mapping(item);
//    });

//    self.Mappings(items);
//}


//var Item = function (data) {
//    var self = this;

//    self.Id = ko.observable(data.Id);
//    self.Status = ko.observable(data.Status);
//    self.Title = ko.observable(data.Title);
//    self.LastUpdatedInGC  = ko.observable(data.LastUpdatedInGC);
//    self.Breadcrumb  = ko.observable(data.Breadcrumb);
//    self.Template = ko.observable(data.Template);

//    self.Mappings = ko.observableArray();

//    var items = ko.utils.arrayMap(data.AvailableMappings.Mappings, function (item) {
//        return new Mapping(item);
//    });

//    self.Mappings(items);
//}


var ImportManager = function () {
    var MODE = {
        ChooseItmesForImort: 1,
        CheckItemsBeforeImport: 2,
        Import: 3,
        ImportResult: 4,
        Close: 5,
        Error: 6
    };

    var allItems = [];
    var self = this;

    self.errorText = ko.observable(),
    self.successImportedItemsCount = ko.observable(),
    self.notImportedItemsCount = ko.observable(),
    self.currentMode = ko.observable(MODE.ChooseItmesForImort);

    self.projects = ko.observableArray([]),
        self.items = ko.observableArray([]),
        self.statuses = ko.observableArray([]),
        self.templates = ko.observableArray([]),
        self.statusPostState = ko.observable(false),
        self.project = ko.observable(),
        self.statusFilter = ko.observable(),
        self.templateFilter = ko.observable(),


    self.query = ko.observable(''),

    self.init = function () {
        var callbackFunction = function (response) {
            self.initVariables(response);
        }
        self.initRequestHandler(callbackFunction);
    },

    self.initRequestHandler = function (callbackFunction) {
        var id = getUrlVars()["id"];
        var project = self.project();
        project = project ? project : 0;

        jQuery.getJSON('/api/sitecore/Import/GetMultiLocation?id={' + id + '}&projectId=' + project).success(function (response) {
            callbackFunction(response);
            jQuery(".preloader").hide();
            jQuery(".tooltip").remove();
            initTooltip();
            resizeTableHead();
        }).error(function (response) {
            self.errorCallbackHandle(response);
        });
        document_resize();
        resizeTableHead();
    }

    self.errorCallbackHandle = function (response) {
        jQuery(".preloader").hide();
        self.errorText(response.responseText);
        self.buttonClick(MODE.Error);
    }

    self.postErrorHandle = function (response) {
        jQuery(".preloader").hide();
        self.errorText(response);
        self.buttonClick(MODE.Error);
    }

    self.initVariables = function (response) {

        //var items = ko.utils.arrayMap(response.Data.Items, function (item) {
        //    return new Item(item);
        //});
        var items = self.setupWatcher(response.Data.Items);
        self.items(items);
        allItems = items.slice(0);

        self.projects(response.Filters.Projects);
        self.project(response.Filters.Project);

        self.statuses(response.Filters.Statuses);

        self.templates(response.Filters.Templates);
    }

    self.projectChanged = function (obj, event) {
        if (event.originalEvent) {
            jQuery(".preloader").show();
            var callbackFunction = function (response) {
                self.initVariables(response);
                self.setupDefaultValuesToFilters();
            }
            self.initRequestHandler(callbackFunction);
        }
    },

    self.setupDefaultValuesToFilters = function () {
        self.query('');
        self.statusFilter();
        self.templateFilter();
    }

    //filters
    self.filter = function () {
        self.items.removeAll();

        var currentCollection = allItems.slice(0);
        currentCollection = self.search(currentCollection);
        currentCollection = self.filterByStatus(currentCollection);
        currentCollection = self.filterByTemplate(currentCollection);

        self.items(currentCollection);
        resizeTableHead();
        jQuery(".tooltip").remove();
        initTooltip();
    }

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
                if (currentElement.Status.id === value) {
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
                if (currentElement.Template.id === value) {
                    resultCollection.push(currentElement);
                }
            }
        }

        return resultCollection;
    }

    //table rendering
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


    self.checkRow = function () {
        this.Checked(!this.Checked());
    }

    self.query.subscribe(self.filter);

    //button click events
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
        var importItems = [];
        items.forEach(function (item, i) {
        item.Mappings.forEach(function (mapping, m) {
            importItems.push({
                Id: item.Id,
                SelectedLocation: mapping.DefaultLocation,
                IsImport: mapping.IsImport,
                SelectedMappingId: mapping.Id
            });
            });
        });
        var lang = getUrlVars()["l"];
        var status = self.statusFilter();
        var project = self.project();
        if (!self.statusPostState())
            status = "";
        jQuery.ajax
        ({
            type: "POST",
            url: '/api/sitecore/Import/ImportItemsWithLocation?id={' + id + '}&projectId=' + project + '&statusId=' + status + '&language=' + lang,
            dataType: 'json',
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(importItems),
            success: function (response) {
                if (response.status == 'error') {
                    self.postErrorHandle(response.message);
                }
                var notImportedItemsCount = self.getNotImportedItemsCount(response.Items);
                self.notImportedItemsCount(notImportedItemsCount);
                self.successImportedItemsCount(response.Items.length - notImportedItemsCount);
                self.items(response.Items);
                self.buttonClick(MODE.ImportResult);
            },
            error: function (response) {
                self.errorCallbackHandle(response);
            }
        });
    }

    self.getNotImportedItemsCount = function (items) {
        var count = 0;
        items.forEach(function (item) {
            if (!item.IsImportSuccessful)
                count++;
        });

        return count;
    }

    self.close = function () {
        window.top.dialogClose();
    }

    self.backButtonClick = function () {
        self.items(allItems.slice(0));
    }

    self.openDropTree = function () {
        var id = this.OpenerId;
        if (!this.IsShowing) {
            jQuery("#" + id).show();
            this.IsShowing = true;
            var mapping = this;
            jQuery("#" + id).dynatree({
                autoFocus: false,
                initAjax: {
                    url: '/api/sitecore/Import/GetTopLevelNode',
                    data: { mode: "funnyMode" }
                },
                onActivate: function (node) {
                    jQuery('[data-openerid="' + id + '"]').val(node.data.title);
                    mapping.DefaultLocation = node.data.key;
                },
                onLazyRead: function (node) {
                    node.appendAjax({
                        url: "/api/sitecore/Import/GetChildrenAsJson?id=" + node.data.key,
                        data: {
                            key: node.data.key,
                            mode: "funnyMode"
                        }
                    });
                }    
            });
        } else {
            jQuery("#" + id).hide();
            this.IsShowing = false;
        }
    }

    self.buttonClick = function (newMode) {
        if (newMode === MODE.CheckItemsBeforeImport) {
            if (self.getCheckedCount() == 0) {
                self.errorText('Please select at least one item');
            } else {
                self.currentMode(newMode);
                self.switchToCheckItemsBeforeImport();
            }
        } else if (newMode === MODE.Import) {
            self.currentMode(newMode);
            self.import();
        } else if (newMode === MODE.Close) {
            self.currentMode(newMode);
            self.close();
        } else if (newMode === MODE.ChooseItmesForImort) {
            self.statusFilter = ko.observable();
            self.currentMode(newMode);
            self.backButtonClick();
        } else {
            self.currentMode(newMode);
        }
        resizeTableHead();
    }

    self.getMode = function (section) {
        if (self.currentMode() === section) {
            return true;
        }
        return false;
    }

    self.setupWatcher = function (items) {
        for (var i = 0; i < items.length; i++) {
            items[i].Checked = ko.observable(false);
        }

        return items;
    }

    self.getImportResultTemplateColor = function (item) {
        if (!item.IsImportSuccessful)
            return 'red';
    }

    self.getImportResultMessageColor = function (item) {
        if (!item.IsImportSuccessful)
            return 'red';
        return 'green';
    }

    self.openCmsLink = function (data, e) {
        var link = data.CmsLink;
        window.open(link);
        e.stopImmediatePropagation();
    }

    self.openGcLink = function (data, e) {
        var link = data.GcLink;
        window.open(link);
        e.stopImmediatePropagation();
    }


    self.init();
}
jQuery(window).resize(function () {
    resizeTableHead();
});

jQuery(function () {
    jQuery("thead th.cell_resize").each(function () {
        jQuery(this).find("div").css("width", jQuery(this).width());
    });
    jQuery("thead th div").each(function () {
        if (jQuery(this).height() > 18) {
            jQuery(this).css("padding-top", 0);
            jQuery(this).css("margin-top", 7);
        }
    });

});
