/* normal version, works in sc8.1 only
define(["sitecore", "/-/speak/v1/ExperienceEditor/ExperienceEditor.js"], function (Sitecore, ExperienceEditor) {
    Sitecore.Commands.GCMLImportCommand =
  {
    canExecute: function (context) {
        return true;
    },
    execute: function (context) {

      var itemId = context.currentContext.itemId.replace("%7b", "").replace("%7d", "");

      var dialogPath = "/sitecore modules/shell/gathercontent/import/MultiLocationImport.html?id=" + itemId + "&l=" + context.currentContext.language + "&v=" + context.currentContext.version + "&db=" + context.currentContext.database;
      var dialogFeatures = "dialogHeight: 700px;dialogWidth: 1200px;";
      ExperienceEditor.Dialogs.showModalDialog(dialogPath, '', dialogFeatures, null, function (result) {
        if (!result) {
          return;
        }
      });
    },
  };
});

*/

/* ugly version, may be works in 8.0-8.1 */
define(["sitecore"], function (Sitecore) {
    setTimeout(
	function () {
	    var d = Sitecore.ExperienceEditor.Dialogs;
	    if (typeof d != 'undefined') {
	        Sitecore.Commands.GCMLImportCommand =
              {
                  canExecute: function (context) {
                      return true;
                  },
                  execute: function (context) {

                      var itemId = context.currentContext.itemId.replace("%7b", "").replace("%7d", "");

                      var dialogPath = "/sitecore modules/shell/gathercontent/import/MultiLocationImport.html?id=" + itemId + "&l=" + context.currentContext.language + "&v=" + context.currentContext.version + "&db=" + context.currentContext.database;
                      var dialogFeatures = "dialogHeight: 700px;dialogWidth: 1200px;";
                      Sitecore.ExperienceEditor.Dialogs.showModalDialog(dialogPath, '', dialogFeatures, null, function (result) {
                          if (!result) {
                              return;
                          }
                      });
                  },
              };
	    }
	    else {
	        define(["sitecore", "/-/speak/v1/ExperienceEditor/ExperienceEditor.js"], function (Sitecore, ExperienceEditor) {
	            Sitecore.Commands.GCMLImportCommand =
              {
                  canExecute: function (context) {
                      return true;
                  },
                  execute: function (context) {

                      var itemId = context.currentContext.itemId.replace("%7b", "").replace("%7d", "");

                      var dialogPath = "/sitecore modules/shell/gathercontent/import/MultiLocationImport.html?id=" + itemId + "&l=" + context.currentContext.language + "&v=" + context.currentContext.version + "&db=" + context.currentContext.database;
                      var dialogFeatures = "dialogHeight: 700px;dialogWidth: 1200px;";
                      ExperienceEditor.Dialogs.showModalDialog(dialogPath, '', dialogFeatures, null, function (result) {
                          if (!result) {
                              return;
                          }
                      });
                  },
              };
	        });
	    }
	}, 200);
});