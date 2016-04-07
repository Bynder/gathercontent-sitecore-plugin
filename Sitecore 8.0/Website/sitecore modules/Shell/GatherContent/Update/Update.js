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

    self.language = ko.observable(getUrlVars()["l"]),
    self.languages = ko.observableArray([]),

    self.items = ko.observableArray([]),
    self.statuses = ko.observableArray([]),
    self.templates = ko.observableArray([]),
    self.projects = ko.observableArray([]),

    self.statusPostState = ko.observable(false),

    self.projectFilter = ko.observable(),
    self.statusFilter = ko.observable(),
    self.templateFilter = ko.observable(),

    self.query = ko.observable(''),


    self.selectedItems = ko.observableArray([]);
    self.resultItems = ko.observableArray([]);

    self.filterOptions = {
        filterText: ko.observable(""),
        useExternalFilter: false
    };

    self.pagingOptions = {
        pageSizes: ko.observableArray([15, 20, 30]),
        pageSize: ko.observable(10),
        totalServerItems: ko.observable(0),
        currentPage: ko.observable(1)
    };


    self.filterConfirmOptions = {
        filterText: ko.observable(""),
        useExternalFilter: true
    };

    self.pagingConfirmOptions = {
        pageSizes: ko.observableArray([15, 20, 30]),
        pageSize: ko.observable(10),
        totalServerItems: ko.observable(0),
        currentPage: ko.observable(1)
    };

    self.filterResultOptions = {
        filterText: ko.observable(""),
        useExternalFilter: true
    };

    self.pagingResultOptions = {
        pageSizes: ko.observableArray([15, 20, 30]),
        pageSize: ko.observable(10),
        totalServerItems: ko.observable(0),
        currentPage: ko.observable(1)
    };


    self.setPagingData = function (data, page, pageSize) {
        var items = data;
        allItems = items.slice(0);
        var pagedData = data.slice((page - 1) * pageSize, page * pageSize);
        self.items(pagedData);
        self.pagingOptions.totalServerItems(data.length);
    };

    this.getPagedData = function (pageSize, page) {
        var id = getUrlVars()["id"];
        jQuery.ajax({
            url: '/api/sitecore/Update/Get?id={' + id + '}',
            dataType: 'json',
            async: true,
            success: function (response) {
                jQuery(".preloader").hide();
                self.initVariables(response);
                self.setPagingData(response.Items, page, pageSize);
                document.getElementsByTagName('input')[1].focus();              
            },
            error: function (response) {
                self.errorCallbackHandle(response);
            }
        });
    };


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
        //var items = self.setupWatcher(response.Items);
        self.items(response.Items);
        allItems = response.Items.slice(0);

        self.statuses(response.Filters.Statuses);
        self.projects(response.Filters.Projects);
        self.templates(response.Filters.Templates);

        self.languages(response.Languages);
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
                if (currentElement.GcItem.Name.toLowerCase().indexOf(value.toLowerCase()) >= 0) {
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
                if (currentElement.GcProject.Name === value) {
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
                if (currentElement.Status.Id === value) {
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
                if (currentElement.GcTemplate.Id === value) {
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


    self.openCmsLink = function (item) {
        var link = item.CmsLink;
        window.open(link);
    }

    self.openGcLink = function (item) {
        var link = item.GcLink;
        window.open(link);
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
        var lang = self.language();
        var id = getUrlVars()["id"];
        var selectedItems = self.selectedItems();
        var items = [];
        selectedItems.forEach(function (item, i) {
            items.push({
                GCId: item.GcItem.Id,
                CMSId: item.Id
            });
        });
        var status = self.statusFilter();
        if (!self.statusPostState())
            status = "";
        jQuery.ajax
        ({
            type: "POST",
            url: '/api/sitecore/Update/UpdateItems?id={' + id + '}&statusId=' + status + '&language='+ lang,
            dataType: 'json',
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(items),
            success: function (response) {
                if (response.status == 'error') {
                    self.postErrorHandle(response.message);
                }
                var notUpdatedCount = self.getNotUpdatedItemsCount(response);
                self.successImportedItemsCount(response.length - notUpdatedCount);
                self.notUpdaredItemsCount(notUpdatedCount);
                self.resultItems(response);
                self.buttonClick(MODE.ImportResult);
                
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
            if (self.selectedItems().length == 0) {
                self.errorText('Please select at least one item');
            } else {
                self.currentMode(newMode);
                resizeTableHead();
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
    }

    self.getMode = function (section) {
        if (self.currentMode() === section) {
            return true;
        }
        return false;
        
    }

    //self.setupWatcher = function (items) {
    //    for (var i = 0; i < items.length; i++) {
    //        items[i].Checked = ko.observable(false);
    //    }

    //    return items;
    //}

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

  
    self.filterOptions.filterText.subscribe(function (data) {
        self.setPagingData(allItems, self.pagingOptions.currentPage(), self.pagingOptions.pageSize());
    });
    self.pagingOptions.pageSizes.subscribe(function (data) {
        self.setPagingData(allItems, self.pagingOptions.currentPage(), self.pagingOptions.pageSize());
    });
    self.pagingOptions.pageSize.subscribe(function (data) {
        self.setPagingData(allItems, self.pagingOptions.currentPage(), self.pagingOptions.pageSize());
    });
    self.pagingOptions.totalServerItems.subscribe(function (data) {
        self.setPagingData(allItems, self.pagingOptions.currentPage(), self.pagingOptions.pageSize());
    });
    self.pagingOptions.currentPage.subscribe(function (data) {
        self.setPagingData(allItems, self.pagingOptions.currentPage(), self.pagingOptions.pageSize());
    });

    self.getPagedData(self.pagingOptions.pageSize(), self.pagingOptions.currentPage());


    var options =
        {
            afterSelectionChange: function () { return true; },
            showColumnMenu: false,
            showFilter: false,
            data: self.items,
            selectedItems: self.selectedItems,
            enablePaging: true,
            pagingOptions: self.pagingOptions,
            filterOptions: self.filterOptions,            
            columnDefs: [
                {
                    field: 'Status.Name',  width: '**',
                    displayName: 'Status', cellTemplate: '<div class="cell-padding"><div class="status-color" data-bind="style: { backgroundColor : $parent.entity.Status.Color }"></div><span data-bind="text: $parent.entity.Status.Name"></span></div>'
                },
                { field: 'ScTitle', width: '**', displayName: 'Sitecore Title' },
                { field: 'GcItem.Name', width: '**', displayName: 'GatherContent Item Name' },
                { field: 'GcProject.Name', width: '**', displayName: 'GatherContent Project' },
                {
                    field: 'LastUpdatedInGc', width: '**', displayName: 'Last updated in GatherContent',
                    sortFn: dateSort
                },
                {
                    field: 'LastUpdatedInSitecore', width: '**', displayName: 'Last updated in Sitecore',
                    sortFn: dateSort
                },
                { field: 'GcTemplate.Name', width: '**', displayName: 'GatherContent Template' },
                { field: 'ScTemplateName', width: '**', displayName: 'Sitecore Template' },
                { displayName: 'Open in Sitecore', cellClass: 'cell-padding', sortable: false, cellTemplate: '<a data-bind="if: $parent.entity.CmsLink!=null, click: function(){$parent.$userViewModel.openCmsLink($parent.entity)}">Open</a>' },
                { displayName: 'Open in GatherContent', cellClass: 'cell-padding', sortable: false, cellTemplate: '<a data-bind="click: function(){$parent.$userViewModel.openGcLink($parent.entity)}">Open</a>' }
            ]
        };


    this.gridOptions = options;

    var confirmOptions =
          {
              canSelectRows: false,
              showColumnMenu: false,
              showFilter: false,
              data: self.selectedItems,
              enablePaging: true,
              pagingOptions: self.pagingConfirmOptions,
              filterOptions: self.filterConfirmOptions,
              columnDefs: [
                {
                    field: 'Status.Name', width: '**',
                    displayName: 'Status', cellTemplate: '<div class="cell-padding"><div class="status-color" data-bind="style: { backgroundColor : $parent.entity.Status.Color }"></div><span data-bind="text: $parent.entity.Status.Name"></span></div>'
                },
                { field: 'ScTitle', width: '**', displayName: 'Sitecore Title' },
                { field: 'GcItem.Name', width: '**', displayName: 'GatherContent Item Name' },
                { field: 'GcProject.Name', width: '**', displayName: 'GatherContent Project' },
                {
                    field: 'LastUpdatedInGc', width: '**', displayName: 'Last updated in GatherContent',
                    sortFn: dateSort
                },
                {
                    field: 'LastUpdatedInSitecore', width: '**', displayName: 'Last updated in Sitecore',
                    sortFn: dateSort
                },
                { field: 'GcTemplate.Name', width: '**', displayName: 'GatherContent Template' },
                { field: 'ScTemplateName', width: '**', displayName: 'Sitecore Template' },
                { displayName: 'Open in Sitecore', cellClass: 'cell-padding', sortable: false, cellTemplate: '<a data-bind="if: $parent.entity.CmsLink!=null, click: function(){$parent.$userViewModel.openCmsLink($parent.entity)}">Open</a>' },
                { displayName: 'Open in GatherContent', cellClass: 'cell-padding', sortable: false, cellTemplate: '<a data-bind="click: function(){$parent.$userViewModel.openGcLink($parent.entity)}">Open</a>' }
              ]
          };

    this.gridConfirmOptions = confirmOptions;


    var resultOptions =
    {
        displaySelectionCheckbox: false,
        canSelectRows: false,
        showColumnMenu: false,
        showFilter: false,
        data: self.resultItems,
        enablePaging: true,
        pagingOptions: self.pagingResultOptions,
        filterOptions: self.filterResultOptions,
        columnDefs: [
            {
                field: 'Status.Name',
                displayName: 'Status', cellTemplate: '<div class="cell-padding">' +
                    '<div class="status-color" data-bind="style: { backgroundColor : $parent.entity.Status.Color }">' +
                    '</div>' +
                    '<span data-bind="text: $parent.entity.Status.Name">' +
                    '</span>' +
                    '</div>'
            },
            { field: 'Title', displayName: 'Item name' },
            {
                field: 'Message',
                displayName: 'Import status',
                cellTemplate: '<div class="kgCellText" data-bind="style: { color: $parent.$userViewModel.getImportResultMessageColor($parent.entity) }, text: $parent.entity.Message"></div>'
            },
            {
                field: 'GcTemplateName',
                displayName: 'Template name',
                cellTemplate: '<div class="kgCellText" data-bind="style: { color: $parent.$userViewModel.getImportResultTemplateColor($parent.entity) }, text: $parent.entity.GcTemplateName"></div>'
            },
            {
                displayName: 'Open in Sitecore',
                cellClass: 'cell-padding',
                sortable: false,
                cellTemplate: '<a data-bind="if: $parent.entity.CmsLink!=null, click: function(){$parent.$userViewModel.openCmsLink($parent.entity)}">Open</a>'
            },
            {
                displayName: 'Open in GatherContent',
                cellClass: 'cell-padding',
                sortable: false,
                cellTemplate: '<a data-bind="click: function(){$parent.$userViewModel.openGcLink($parent.entity)}">Open</a>'
            }
        ]
    };

    this.gridResultOptions = resultOptions;

}
