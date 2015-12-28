var UpdateManager = function () {
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
    self.notUpdaredItemsCount = ko.observable(),

    self.currentMode = ko.observable(MODE.ChooseItmesForImort);

    self.items = ko.observableArray([]),
    self.statuses = ko.observableArray([]),
    self.templates = ko.observableArray([]),
    self.projects = ko.observableArray([]),

    self.statusPostState = ko.observable(false),

    self.projectFilter = ko.observable(),
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
         
         jQuery.getJSON('/api/sitecore/Update/Get?id={' + id + '}').success(function (response) {
             callbackFunction(response);
             jQuery(".preloader").hide();
             initTooltip();
             jQuery("thead th.cell_resize").each(function(){
                 jQuery(this).find("div").css("width",jQuery(this).width())
             });
             jQuery("thead th div").each(function(){
                 if( jQuery(this).height()>18){
                     jQuery(this).css("padding-top",0);
                     jQuery(this).css("margin-top",9)
                 }
             })
         }).error(function(response) {
             self.errorCallbackHandle(response);
         });
         jQuery("thead th.cell_resize").each(function(){
             jQuery(this).find("div").css("width",jQuery(this).width())
         });
         jQuery("thead th div").each(function(){
             if( jQuery(this).height()>18){
                 jQuery(this).css("padding-top",0);
                 jQuery(this).css("margin-top",9)
             }
         })
         document_resize();
     }

    self.errorCallbackHandle = function (response) {
        jQuery(".preloader").hide();
        self.errorText(response.responseText);
        self.buttonClick(MODE.Error);
    }

    self.initVariables = function (response) {
        var items = self.setupWatcher(response.Data.Items);
        self.items(items);
        allItems = items.slice(0);
        
        self.statuses(response.Filters.Statuses);
        self.projects(response.Filters.Projects);
        self.templates(response.Filters.Templates);
    }
    
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
        currentCollection = self.filterByProject(currentCollection);

        self.items(currentCollection);
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

    self.filterByProject = function (currentCollection) {
        var resultCollection = currentCollection;
        var value = self.projectFilter();
        if (value) {
            resultCollection = [];
            for (var i = 0; i < currentCollection.length; i++) {
                var currentElement = currentCollection[i];
                if (currentElement.ProjectName === value) {
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

    self.afterStatusesSelectRender = function (option, status) {
        if (status.color) {
            option.style.color = status.color;
        }
    };

    self.getSelectedStatusColor = function () {
        var result = "";
        ko.utils.arrayForEach(self.statuses(), function (status) {
            if (status.id.toLowerCase() === self.statusFilter()) {
                result = status.color;
                return;
            }
        });

        return result;
    }

    self.checkRow = function () {
        this.Checked(!this.Checked());
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
        var itemids = [];
        items.forEach(function (item, i) {
            itemids.push({
                GCId: item.GCId,
                CMSId: item.CMSId
            });
        });
        var status = self.statusFilter();
        if (!self.statusPostState())
            status = "";
        jQuery.ajax
        ({
            type: "POST",
            url: '/api/sitecore/Update/UpdateItems?id={' + id + '}&statusId=' + status,
            dataType: 'json',
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(itemids),
            success: function (response) {
                if (response.status == 'error') {
                    self.errorCallbackHandle(response.message);
                }
                var notUpdatedCount = self.getNotUpdatedItemsCount(response.Items);
                self.successImportedItemsCount(response.Items.length - notUpdatedCount);
                self.notUpdaredItemsCount(notUpdatedCount);
                self.items(response.Items);
                self.buttonClick(MODE.ImportResult);
                jQuery("thead th.cell_resize").each(function(){
                    jQuery(this).find("div").css("width",jQuery(this).width());
                });
                jQuery("thead th div").each(function(){
                    if( jQuery(this).height()>18){
                        jQuery(this).css("padding-top",0);
                        jQuery(this).css("margin-top",9)
                    }
                })
            },
            error: function (response) {
                self.errorCallbackHandle(response);
            }
        });
    }

    self.getNotUpdatedItemsCount = function (items) {
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

    self.buttonClick = function (newMode) {
        if (newMode === MODE.CheckItemsBeforeImport) {
            if (self.getCheckedCount() == 0) {
                self.errorText('Please select at least one item');
            } else {
                self.currentMode(newMode);
                jQuery("thead th.cell_resize").each(function() {
                    jQuery(this).find("div").css("width", jQuery(this).width());
                });
                jQuery("thead th div").each(function() {
                    if (jQuery(this).height() > 18) {
                        jQuery(this).css("padding-top", 0);
                        jQuery(this).css("margin-top", 9);
                    }
                });
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
        jQuery("thead th.cell_resize").each(function(){
            jQuery(this).find("div").css("width",jQuery(this).width());
        });
        jQuery("thead th div").each(function() {
            if (jQuery(this).height() > 18) {
                jQuery(this).css("padding-top", 0);
                jQuery(this).css("margin-top", 9);
            }
        });
    }

    self.getMode = function (section) {
        if (self.currentMode() === section) {
            return true;
        }
        return false;
        jQuery("thead th.cell_resize").each(function(){
            jQuery(this).find("div").css("width",jQuery(this).width());
        });
        jQuery("thead th div").each(function() {
            if (jQuery(this).height() > 18) {
                jQuery(this).css("padding-top", 0);
                jQuery(this).css("margin-top", 9);
            }
        });
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

    self.getDateColor = function (item) {
        //if (item.IsNeedToHighlightingDate)
        return 'dateWarnings';
    }

    self.init();
}
jQuery(window).resize(function() {
    jQuery("thead th.cell_resize").each(function() {
        jQuery(this).find("div").css("width", jQuery(this).width());
    });
    jQuery("thead th div").each(function() {
        if (jQuery(this).height() > 18) {
            jQuery(this).css("padding-top", 0);
            jQuery(this).css("margin-top", 9);
        }
    });
});

jQuery(function () {
    jQuery("thead th.cell_resize").each(function(){
        jQuery(this).find("div").css("width",jQuery(this).width());
    });
    jQuery("thead th div").each(function() {
        if (jQuery(this).height() > 18) {
            jQuery(this).css("padding-top", 0);
            jQuery(this).css("margin-top", 9);
        }
    });

});