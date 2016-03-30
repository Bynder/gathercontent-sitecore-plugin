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

    self.language = ko.observable(getUrlVars()["l"]),
    self.languages = ko.observableArray([]),

    self.defaultLocationTitle = ko.observable(getUrlVars()["t"]),
    self.defaultLocation = ko.observable(getUrlVars()["id"]),
    self.showDropTree = ko.observable(false);

    self.projects = ko.observableArray([]),
    self.items = ko.observableArray(),
    self.statuses = ko.observableArray([]),
    self.templates = ko.observableArray([]),

    self.statusPostState = ko.observable(false),

    self.project = ko.observable(),
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
        var project = self.project();
        project = project ? project : 0;
        jQuery.ajax({
            url: '/api/sitecore/Import/Get?id={' + id + '}&projectId=' + project,
            dataType: 'json',
            async: true,
            success: function (response) {
                self.setPagingData(response.Items, page, pageSize);
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


    //filters
    self.filter = function () {
        self.items.removeAll();

        var currentCollection = allItems.slice(0);
        currentCollection = self.search(currentCollection);
        currentCollection = self.filterByStatus(currentCollection);
        currentCollection = self.filterByTemplate(currentCollection);

        self.items(currentCollection);
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

    self.openDropTree = function () {
        var id = "location-droptree";
        var locationId = self.defaultLocation();
        if (!locationId.startsWith("{")) {
            locationId = "{" + locationId + "}";
        }

        if (!self.showDropTree()) {
            self.showDropTree(true);
            
            jQuery("#" + id).dynatree({
                autoFocus: false,
                imagePath: "~/icon/",
                initAjax: {
                    url: '/api/sitecore/DropTree/GetTopLevelNode?id=' + locationId,
                    data: { mode: "funnyMode" }
                },
                onActivate: function (node) {
                    var path = "";
                    var keys = node.getKeyPath().split("/");
                    keys.shift();

                    for (var i = 0; i < keys.length; i++) {
                        if (i != keys.length - 1) {
                            path += node.tree.getNodeByKey(keys[i]).data.title + " / ";
                        } else {
                            path += node.tree.getNodeByKey(keys[i]).data.title;
                        }
                    }

                    self.showDropTree(false);
                    self.defaultLocation(node.data.key);
                    self.defaultLocationTitle(node.data.title);
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
            self.showDropTree(false);
        }
    }

    //button click events

    self.import = function () {
        //self.defaultLocation
        //self.language
        var id = getUrlVars()["id"];
        var selectedItems = self.selectedItems();
        var items = [];
        selectedItems.forEach(function (item, i) {
            items.push({ Id: item.Id, SelectedMappingId: item.AvailableMappings.SelectedMappingId });
        });
        var lang = getUrlVars()["l"];
        var status = self.statusFilter();
        var project = self.project();
        if (!self.statusPostState())
            status = "";
        //jQuery.ajax
        //({
        //    type: "POST",
        //    url: '/api/sitecore/Import/ImportItems?id={' + id + '}&projectId=' + project + '&statusId=' + status + '&language=' + lang,
        //    dataType: 'json',
        //    contentType: "application/json; charset=utf-8",
        //    data: JSON.stringify(items),
        //    success: function (response) {
        //        if (response.status == 'error') {
        //            self.postErrorHandle(response.message);
        //        }
        //        var notImportedItemsCount = self.getNotImportedItemsCount(response);
        //        self.notImportedItemsCount(notImportedItemsCount);
        //        self.successImportedItemsCount(response.length - notImportedItemsCount);
        //        self.resultItems(response);
        //        self.buttonClick(MODE.ImportResult);
        //    },
        //    error: function (response) {
        //        self.errorCallbackHandle(response);
        //    }
        //});
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


    self.buttonClick = function (newMode) {
        if (newMode === MODE.CheckItemsBeforeImport) {
            if (self.selectedItems().length == 0) {
                self.errorText('Please select at least one item');
            } else {
                self.currentMode(newMode);
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
        self.setPagingData(allItems,  self.pagingOptions.currentPage(),self.pagingOptions.pageSize());
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
                displayName: 'Status', cellTemplate: '<div class="cell-padding"><div class="status-color" data-bind="style: { backgroundColor : $parent.entity.Status.Color }"></div><span data-bind="text: $parent.entity.Status.Name"></span></div>'
            },
            { field: 'Title', displayName: 'Item name' },
            { field: 'LastUpdatedInGC', displayName: 'Last updated in GatherContent' },
            { field: 'Breadcrumb', displayName: 'Path' },
            { field: 'Template.Name', displayName: 'Template name' }
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
                  field: 'Status.Name',
                  displayName: 'Status', cellTemplate: '<div class="cell-padding"><div class="status-color" data-bind="style: { backgroundColor : $parent.entity.Status.Color }"></div><span data-bind="text: $parent.entity.Status.Name"></span></div>'
              },
              { field: 'Title', displayName: 'Item name' },
              { field: 'Template.Name', displayName: 'Template name' },
              {
                  displayName: 'Specify mappings', cellTemplate: '<div data-bind="if: $parent.entity.AvailableMappings.Mappings.length > 0">' +
                      '<div data-bind="if: $parent.entity.AvailableMappings.Mappings.length == 1">' +
                      '<span class="cell-padding" data-bind="text: $parent.entity.AvailableMappings.Mappings[0].Title"></span>' +
                      '<select class=\"mappings-cell\" \
                               data-bind="visible:false, options: $parent.entity.AvailableMappings.Mappings, \
                               optionsValue: \'Id\', \
                               optionsText: \'Title\',\
                               value: $parent.entity.AvailableMappings.SelectedMappingId"> \
                         </select>' +
                      '</div>' +
                      '<div data-bind="if: $parent.entity.AvailableMappings.Mappings.length > 1">' +
                          '<select class=\"mappings-cell\" \
                               data-bind="options: $parent.entity.AvailableMappings.Mappings, \
                               optionsValue: \'Id\', \
                               optionsText: \'Title\',\
                               value: $parent.entity.AvailableMappings.SelectedMappingId"> \
                         </select>' +
                      '</div>' +
                      '</div>'
              }
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
            { field: 'Message', displayName: 'Import status' },
            { field: 'GcTemplateName', displayName: 'Template name' },
            { displayName: 'Open in Sitecore', cellTemplate: '<a data-bind="if: $parent.entity.CmsLink!=null", click: function(){$parent.$userViewModel.openCmsLink($parent.entity)}">Open</a>' },
            { displayName: 'Open in GatherContent', cellTemplate: '<a data-bind="click: function(){$parent.$userViewModel.openGcLink($parent.entity)}">Open</a>' }
        ]
    };

    this.gridResultOptions = resultOptions;

}

