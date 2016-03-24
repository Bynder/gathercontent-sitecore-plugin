
function ViewModel() {
    var self = this;

    this.mappings = ko.observableArray();
    this.errorText = ko.observable();
    this.isError = ko.observable();

    this.filterOptions = {
        filterText: ko.observable(""),
        useExternalFilter: true
    };

    this.pagingOptions = {
        pageSizes: ko.observableArray([10, 15, 20]),
        pageSize: ko.observable(10),
        totalServerItems: ko.observable(0),
        currentPage: ko.observable(1)
    };

    this.setPagingData = function (data, page, pageSize) {
        var pagedData = data.slice((page - 1) * pageSize, page * pageSize);
        self.mappings(pagedData);
        self.pagingOptions.totalServerItems(data.length);
    };

    this.getPagedData = function (pageSize, page) {
        jQuery.ajax({
            url: '/api/sitecore/mappings/Get',
            dataType: 'json',
            async: false,
            success: function (loadData) {
                if (loadData.status != "error") {
                    self.setPagingData(loadData, page, pageSize);
                    jQuery(".preloader").hide();
                } else {
                    self.errorText("Error:" + " " + loadData.message);
                    self.isError(true);
                }
                jQuery(".preloader").hide();

            }
        });

    };

    this.removeMapping = function (item) {
        var scMappingId = item.ScMappingId;

        var confirmDelete = confirm('Are you sure you want to delete this1?');
        if (confirmDelete) {
            jQuery.ajax({
                type: 'DELETE',
                url: '/api/sitecore/mappings/Delete?scMappingId=' + scMappingId,
                success: function () {
                    self.mappings.remove(function (mapping) {
                        return mapping.ScMappingId == scMappingId;
                    });
                    self.isError(false);
                },
                error: function (data) {
                    self.errorText("Error:" + " " + data.message);
                    self.isError(true);
                }
            });
        }
    };


    this.editMapping = function (item) {
        var id = item.GcTemplateId;
        var scMappingId = item.ScMappingId;
        scForm.showModalDialog("/sitecore modules/shell/gathercontent/Mappings/AddOrUpdateMapping.html?id=" + id + "&scMappingId=" + scMappingId,
            null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:600;dialogMinWidth:700;dialogWidth:700;dialogHeight:1000;header: Manage Field Mappings");
    }


    this.addMoreTemplates = function () {
        scForm.showModalDialog("/sitecore modules/shell/gathercontent/mappings/AddOrUpdateMapping.html",
            null, "center:yes;help:no;resizable:yes;scroll:yes;status:no;dialogMinHeight:600;dialogMinWidth:700;dialogWidth:700;dialogHeight:1000;header: Manage Field Mappings");
    }

    self.filterOptions.filterText.subscribe(function (data) {
        self.getPagedData(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });

    self.pagingOptions.pageSizes.subscribe(function (data) {
        self.getPagedData(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });
    self.pagingOptions.pageSize.subscribe(function (data) {
        self.getPagedData(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });
    self.pagingOptions.totalServerItems.subscribe(function (data) {
        self.getPagedData(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });
    self.pagingOptions.currentPage.subscribe(function (data) {
        self.getPagedData(self.pagingOptions.pageSize(), self.pagingOptions.currentPage(), self.filterOptions.filterText());
    });

    self.getPagedData(self.pagingOptions.pageSize(), self.pagingOptions.currentPage());


    var options = 
        {
            displaySelectionCheckbox: false,
            canSelectRows: false,
            showColumnMenu: false,
            showFilter: false,
            data: self.mappings,
            enablePaging: true,
            pagingOptions: self.pagingOptions,
            filterOptions: self.filterOptions,
            columnDefs: [
                { field: 'GcProjectName', displayName: 'GatherContent Project' },
                { field: 'GcTemplateName', displayName: 'GatherContent template' },
                { field: 'ScTemplateName', displayName: 'Sitecore Template' },
                { field: 'MappingTitle', displayName: 'Mapping Title' },
                { field: 'LastMappedDateTime', displayName: 'Last mapped' },
                { field: 'LastUpdatedDate', displayName: 'Last updated in GatherContent' },
                { field: 'Manage', displayName: 'Manage', cellTemplate: '<a href="#" data-bind="click: function(){$parent.$userViewModel.editMapping($parent.entity)}">Edit</a>' },
                {
                    field: 'Delete',
                    displayName: 'Delete',
                    cellTemplate: '<a href="#" data-bind="click: function(){$parent.$userViewModel.removeMapping($parent.entity)}">' +
                        '<img src="~/icon/Office/32x32/delete.png" width="20" height="20"></a>'
                }
            ]
        };

    this.gridOptions = options;
}