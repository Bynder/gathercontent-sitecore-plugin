var ImportManager = function () {
    var MODE = {
        ChooseItmesForImort: 1,
        CheckItemsBeforeImport: 2,
        Confirm: 3,
        Import: 4,
        ImportResult: 5,
        Close: 6,
        Error: 7
    };

    var allItems = [];
    var self = this;

    self.errorText = ko.observable(),
    self.successImportedItemsCount = ko.observable(),
    self.notImportedItemsCount = ko.observable(),
    self.currentMode = ko.observable(MODE.ChooseItmesForImort);

    self.language = ko.observable(getUrlVars()["l"]),
    self.languages = ko.observableArray([]),

    self.projects = ko.observableArray([]),
        self.items = ko.observableArray([]),
        self.confirmItems = ko.observableArray([]),
        self.groupedItems = ko.observableArray([]),
        self.statuses = ko.observableArray([]),
        self.templates = ko.observableArray([]),
        self.statusPostState = ko.observable(false),
        self.project = ko.observable(),
        self.statusFilter = ko.observable(),
        self.templateFilter = ko.observable(),


    self.query = ko.observable(''),

    self.selectedGroupItems = ko.observableArray([]),
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

    self.groupedGridPagingOptions = {
        pageSizes: ko.observableArray([15, 20, 30]),
        pageSize: ko.observable(10),
        totalServerItems: ko.observable(0),
        currentPage: ko.observable(1)
    };

    self.groupedGridFilterOptions = {
        filterText: ko.observable(""),
        useExternalFilter: true
    };

    self.confirmedFilterOptions = {
        filterText: ko.observable(""),
        useExternalFilter: false
    };

    self.confirmedPagingOptions = {
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
        var project = self.project();
        project = project ? project : 0;
        jQuery.ajax({
            url: '/api/sitecore/Import/GetMultiLocation?id={' + id + '}&projectId=' + project,
            dataType: 'json',
            async: true,
            success: function (response) {
                self.setPagingData(response.Items, page, pageSize);
                document.getElementsByTagName('input')[1].focus();
                self.initVariables(response);
                jQuery(".preloader").hide();
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
        self.projects(response.Filters.Projects);
        self.project(response.Filters.Project);

        self.statuses(response.Filters.Statuses);

        self.templates(response.Filters.Templates);

        self.languages(response.Languages);
    }

    self.projectChanged = function (obj, event) {
        if (event.originalEvent) {
            jQuery(".preloader").show();
            self.getPagedData(self.pagingOptions.pageSize(), self.pagingOptions.currentPage());
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
                if (currentElement.Template.Id === value) {
                    resultCollection.push(currentElement);
                }
            }
        }

        return resultCollection;
    }


    self.query.subscribe(self.filter);

    //button click events
    self.ChooseDefaultLocation = function () {
        var selectedItems = self.selectedItems();

        var result = [];
        ko.utils.arrayForEach(selectedItems, function (item) {
            ko.utils.arrayForEach(item.AvailableMappings.Mappings, function (mapping) {

                var found = false;
                for (var i = 0; i < result.length; i++) {
                    if (result[i].MappingName == mapping.Title) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    result.push({
                        Id: mapping.Id,
                        TemplateName: item.Template.Name,
                        MappingName: mapping.Title,
                        ScTemplate: mapping.ScTemplate,
                        DefaultLocation: mapping.DefaultLocation,
                        DefaultLocationTitle: mapping.DefaultLocationTitle,
                        OpenerId: mapping.OpenerId
                    });
                }

            });
        });
        self.groupedItems(result);

    }

    self.switchToCheckItemsBeforeImport = function () {
        var result = [];
        var items = self.selectedItems();
        self.confirmItems.removeAll();
        ko.utils.arrayForEach(items, function (item) {
            var templateItems = self.findByTemplateName(item.Template.Name);
            ko.utils.arrayForEach(templateItems, function (templateItem) {
                ko.utils.arrayForEach(item.AvailableMappings.Mappings, function (mapping) {
                    if (mapping.Id == templateItem.Id) {
                        result.push({
                            ItemId: item.Id,
                            ItemTitle: item.Title,
                            TemplateName: item.Template.Name,
                            MappingId: mapping.Id,
                            MappingName: mapping.Title,
                            ScTemplate: mapping.ScTemplate,
                            DefaultLocation: templateItem.DefaultLocation,
                            DefaultLocationTitle: templateItem.DefaultLocationTitle,
                        });
                    }
                });
            });
        });

        self.confirmItems(result);
    }

    self.findByTemplateName = function (data) {
        var result = [];
        ko.utils.arrayForEach(self.selectedGroupItems(), function (item) {
            if (item.TemplateName === data) {
                result.push(item);
            }
        });
        return result;
    };


    self.import = function () {
        var importItems = [];
        ko.utils.arrayForEach(self.confirmItems(), function (item) {
            importItems.push({
                Id: item.ItemId,
                SelectedLocation: item.DefaultLocation,
                IsImport: true, // TODO redundant?
                SelectedMappingId: item.MappingId
            });
        });

        var lang = self.language();
        var status = self.statusFilter();
        var project = self.project();
        if (!self.statusPostState())
            status = "";
        jQuery.ajax
        ({
            type: "POST",
            url: '/api/sitecore/Import/ImportItemsWithLocation?projectId=' + project + '&statusId=' + status + '&language=' + lang,
            dataType: 'json',
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(importItems),
            success: function (response) {
                if (response.status == 'error') {
                    self.postErrorHandle(response.message);
                }
                var notImportedItemsCount = self.getNotImportedItemsCount(response);
                self.notImportedItemsCount(notImportedItemsCount);
                self.successImportedItemsCount(response.length - notImportedItemsCount);
                self.resultItems(response);
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


    self.openDropTree = function (item) {
        var id = item.OpenerId;
        var locationId = item.DefaultLocation;
        var items = self.groupedItems();

        for (var i = 0; i < items.length; i++) {
            if (items[i].OpenerId != id) {
                items[i].IsShowing = false;
                //TODO use Knockout
                jQuery("#" + items[i].OpenerId).hide();
            }
        }


        if (!item.IsShowing) {
            //TODO use Knockout
            jQuery("#" + id).show();
            item.IsShowing = true;
            jQuery("#" + id).dynatree({
                autoFocus: false,
                imagePath: "~/icon/",
                initAjax: {
                    url: '/api/sitecore/DropTree/GetTopLevelNode?id=' + locationId,
                    data: { mode: "funnyMode" }
                },
                onActivate: function (node) {
                    jQuery('[data-openerid="' + id + '"]').val(node.data.title);
                    jQuery("#" + id).hide();
                    item.IsShowing = false;
                    item.DefaultLocation = node.data.key;
                    item.DefaultLocationTitle = node.data.title;
                },
                onLazyRead: function (node) {
                    node.appendAjax({
                        url: "/api/sitecore/DropTree/GetChildren?id=" + node.data.key,
                        data: {
                            key: node.data.key,
                            mode: "funnyMode"
                        }
                    });
                }
            });
        }
        else {
            //TODO use Knockout
            jQuery("#" + id).hide();
            item.IsShowing = false;
        }
    }


    self.buttonClick = function (newMode) {
        if (newMode === MODE.CheckItemsBeforeImport) {
            if (self.selectedItems().length == 0) {
                self.errorText('Please select at least one item');
            } else {
                self.currentMode(newMode);
                self.ChooseDefaultLocation();
            }
        }
        else if (newMode === MODE.Confirm) {
            var emptyLocationError = false;
            if (self.selectedGroupItems().length == 0) {
                self.errorText('Please select at least one item');
            } else {
                ko.utils.arrayForEach(self.selectedGroupItems(), function (item) {
                    if (item.DefaultLocation == "") {
                        emptyLocationError = true;
                    }
                });
                if (!emptyLocationError) {
                    self.currentMode(newMode);
                    self.switchToCheckItemsBeforeImport();
                } else {
                    self.errorText('Please select default location');
                }
            }
        }
        else if (newMode === MODE.Import) {
            self.currentMode(newMode);
            self.import();
        } else if (newMode === MODE.Close) {
            self.currentMode(newMode);
            self.close();
        } else if (newMode === MODE.ChooseItmesForImort) {
            self.statusFilter = ko.observable();
            self.currentMode(newMode);
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


    self.getImportResultTemplateColor = function (item) {
        if (!item.IsImportSuccessful)
            return 'red';
    }

    self.getImportResultMessageColor = function (item) {
        if (!item.IsImportSuccessful)
            return 'red';
        return 'green';
    }

    self.openCmsLink = function (item) {
        var link = item.CmsLink;
        window.open(link);
    }

    self.openGcLink = function (item) {
        var link = item.GcLink;
        window.open(link);
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
                       field: 'Status.Name',
                       width: '**',
                       displayName: 'Status',
                       cellTemplate: '<div class="kgCellText"><div class="status-color" data-bind="style: { backgroundColor : $parent.entity.Status.Color }"></div><span data-bind="text: $parent.entity.Status.Name"></span></div>'
                   },
                    {
                        field: 'Title',
                        width: '**',
                        displayName: 'Item name'
                    },
                    {
                        field: 'LastUpdatedInGC',
                        width: '**',
                        displayName: 'Last updated in GatherContent',
                        sortFn: dateSort
                    },
                    { field: 'Breadcrumb', width: '**', displayName: 'Path' },
                    { field: 'Template.Name', width: 200, displayName: 'Template name' }
                ]
            };


    this.gridOptions = options;

    var groupedOptions =
        {
            afterSelectionChange: function () { return true; },
            selectWithCheckboxOnly: true,
            showColumnMenu: false,
            showFilter: false,
            canSelectRows: true,
            data: self.groupedItems,
            selectedItems: self.selectedGroupItems,
            enablePaging: true,
            pagingOptions: self.groupedGridPagingOptions,
            filterOptions: self.groupedGridFilterOptions,
            groups: ["TemplateName"],
            columnDefs: [
                { field: 'TemplateName', width: '**', displayName: 'Template Name' },
                { field: 'MappingName', width: '**', displayName: 'Mapping Name' },
                { field: 'ScTemplate', width: '**', displayName: 'Sitecore Template' },
                {
                    field: 'DefaultLocationTitle', width: 320, displayName: 'Default Location',
                    cellTemplate: '<div><input data-bind="value: $parent.entity.DefaultLocationTitle, attr: {\'data-openerid\': $parent.entity.OpenerId }, click: function(){$parent.$userViewModel.openDropTree($parent.entity)}" type="text" />' +
                           '<input data-bind="value: $parent.entity.DefaultLocation, visible: false" type="text" />' +
                           '<div data-bind="attr: { id: $parent.entity.OpenerId }" style="position: absolute; left: 0; top: 30px; width: 300px; height: 400px; z-index: 1000;">' +
                              '<div style="width:300px;height:400px;">' +
                                  '<div data-bind="css: { \'class\': $parent.entity.OpenerId }"> </div>' +
                              '</div>' +
                           '</div></div>'
                }
            ]
        };

    this.groupedGridOptions = groupedOptions;


    var confirmOptions =
    {
        displaySelectionCheckbox: false,
        canSelectRows: false,
        showColumnMenu: false,
        showFilter: false,
        data: self.confirmItems,
        enablePaging: true,
        pagingOptions: self.confirmedPagingOptions,
        filterOptions: self.confirmedFilterOptions,
        columnDefs: [
            { field: 'ItemTitle', displayName: 'Title' },
            { field: 'MappingName', displayName: 'Mapping Name' },
            { field: 'ScTemplate', displayName: 'Sitecore Template' },
            { field: 'DefaultLocationTitle', displayName: 'Default Location' }
        ]
    };

    this.confirmGridOptions = confirmOptions;


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
                displayName: 'Status', cellTemplate: '<div class="kgCellText">' +
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

