﻿define([
  "sitecore",
  "/-/speak/v1/contenttesting/BindingUtil.js",
  "/-/speak/v1/contenttesting/SelectPagesToTest.js",
  "/-/speak/v1/contenttesting/VersionInfo.js",
  "/-/speak/v1/contenttesting/DataUtil.js",
  "/-/speak/v1/contenttesting/PageTestUtil.js",
], function (_sc, bindingUtil, selectPagesToTestMod, versionInfoMod, dataUtilMod, PageTestUtil) {
  var PageTest = _sc.Definitions.App.extend({
    _testItemUriProperty: "testItemUri",
    _testItemTemplateIdProperty: "testItemTemplateId",
    invalidated: false,
    
    showThumbnails: true,

    pageTestUtil: null,

    initialized: function () {

      var self = this;
      
      this.AppProgressIndicator.set("isBusy", true);

      // PageTestUtil - initialization
      this.pageTestUtil = new PageTestUtil(this);
      this.pageTestUtil.initialize();

      this.pageTestUtil.initPagesTab();
      this.pageTestUtil.initReviewTab();

      //this.SelectPageWindow.show();

      
      var uri = _sc.Helpers.url.getQueryParameters(window.location.href);

      // Make sure the ID is not double encoded
      if (uri.id.indexOf("%7b") >= 0) {
        uri.id = decodeURI(uri.id);
      }

      var databaseUri = new _sc.Definitions.Data.DatabaseUri("master");
      var db = new _sc.Definitions.Data.Database(databaseUri);

      db.getItem(uri.id, function(item){
        self.set("selectedTemplateId", item.$templateId);
        self.selectTestPage(uri);
      });
      
      // check if we have a previously started test saved in the parent
      var key = "saved" + uri.id;
      if (window.top[key]) {
        this.loadTest(window.top[key]);
      }

      // Set default values for the controls
      this.isExperienceOptimizationPageTest = true;
      dataUtilMod.setDefaultsParameters(this);
    },

    selectTestPage: function (itemParams) {
      var itemTempateId = this.get("selectedTemplateId");
      if (!itemParams)
      {
        itemParams = this.get("selectedItemId");
        if (!itemParams || itemParams.length === 0) {
          alert(this.Texts.get("You must select a page to test"));
          return;
        }
      }

      var self = this;
      versionInfoMod.getLatestVersionNumber({
        id: itemParams.id,
        lang: itemParams.la
      }, function(id, version, revision, language) {
        var uri = new dataUtilMod.DataUri();
        uri.id = id;
        uri.ver = version;
        uri.rev = revision;
        uri.lang = language;

        self.set(self._testItemUriProperty, uri.toString());
        self.set(self._testItemTemplateIdProperty, itemTempateId);
        self.AppProgressIndicator.set("isBusy", false);
        self.SelectPageWindow.hide();
      });
    },

    // Called from the "Add page to test" dialog
    addPageVersionTestItem: function () {
      this.selectPagesToTest.addPageVersionTestItem();
    },

    // Called from the "Add page to test" dialog
    selectExistingItemTestItem: function () {
      this.selectPagesToTest.selectExistingItemTestItem();
    },

    // Called from the "Select page to add version test to" dialog
    addExistingItemTest: function () {
      var self = this;
      if (this.pageTestUtil) {
        this.pageTestUtil.validateSelectTest(function () {
          self.selectPagesToTest.addExistingItemTest();
        });
      }
    },

    saveTest: function () {
      if (this.pageTestUtil) {
        this.pageTestUtil.saveTest();
      }
    },

    startTest: function () {
      if (this.pageTestUtil) {
        this.pageTestUtil.startTest();
      }
    },

    loadTest: function (options) {
      this.selectTestPage(options.ItemUri);
      this.selectPagesToTest.loadTest(options);
      this.reviewTest.loadTest(options);
    },
    
    close: function() {
      var frame = window.frameElement;
      $(frame).hide();
    }
  });

  return PageTest;
});