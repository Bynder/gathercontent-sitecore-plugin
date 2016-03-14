define(["sitecore", "/-/speak/v1/ExperienceEditor/ExperienceEditor.js"], function (Sitecore, ExperienceEditor) {
  Sitecore.Commands.GCImportCommand =
  {
    canExecute: function (context) {
        return true;
    },
    execute: function (context) {

      var itemId = context.currentContext.itemId.replace("%7b", "").replace("%7d", "");

      var dialogPath = "/sitecore modules/shell/gathercontent/import/import.html?id=" + itemId + "&l=" + context.currentContext.language + "&v=" + context.currentContext.version;
      var dialogFeatures = "dialogHeight: 700px;dialogWidth: 1200px;";
      ExperienceEditor.Dialogs.showModalDialog(dialogPath, '', dialogFeatures, null, function (result) {
        if (!result) {
          return;
        }
      });
    },
  };
});