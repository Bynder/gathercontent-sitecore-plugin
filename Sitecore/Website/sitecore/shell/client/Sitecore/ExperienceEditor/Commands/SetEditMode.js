﻿define(["sitecore", "/-/speak/v1/ExperienceEditor/ExperienceEditor.js"], function (Sitecore, ExperienceEditor) {
  var canEditMode;
  Sitecore.Commands.SetEditMode =
  {
    canExecute: function (context) {
      if (canEditMode == undefined) {
        context.button.set({ isPressed: ExperienceEditor.isInMode("edit") });
        canEditMode = context.app.canExecute("ExperienceEditor.EditMode.CanEdit", context.currentContext);
      }

      return canEditMode;
    },
    execute: function (context) {
      context.currentContext.value = encodeURIComponent(window.parent.location);
      ExperienceEditor.PipelinesUtil.generateRequestProcessor("ExperienceEditor.EditMode.SelectEdit", function (response) {
        window.parent.location = response.responseValue.value;
      }).execute(context);
    }
  };
});