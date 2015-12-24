﻿define([], function () {
  var action = function (context, args) {
    var targetDisplayTextID = context.app[args.targetDisplayTextID],
      targetPathID = context.app[args.targetPathID],
      targetPathProperty = args.targetPathProperty,
      targetQueryID = context.app[args.targetQueryID],
      targetAltTextID = context.app[args.targetAltTextID],
      targetStyleID = context.app[args.targetStyleID],
      targetWindowID = context.app[args.targetWindowID],
      targetControlID = context.app[args.targetControlID],
	    selectedItemsPropertyName="selectedNode",
      template = "<link text='<%=displayText%>' linktype='internal' class='<%=styleClass%>' alt='<%=alternateText%>' <%=target%> querystring='<%=queryString%>' id='<%=itemId%>' />",
      targetWindowValue, itemLink, path,
      emptyOptionID = "{A3C9DB39-1D1B-4AA1-8C68-7B9674D055EE}";

    if (!targetDisplayTextID) {
      console.log("Provide at least display text for your link");
      return false;
    }

    targetWindowValue = targetWindowID.get("selectedItem");

    if (!targetWindowValue || targetWindowValue.itemId === emptyOptionID) {
      targetWindowValue = "";
    } else {
      targetWindowValue = "target='" + targetWindowValue.$displayName.trim() + "'";
    }

    if (targetPathID.get(selectedItemsPropertyName) &&
		  "rawItem" in targetPathID.get(selectedItemsPropertyName) &&
		  targetPathID.get(selectedItemsPropertyName).rawItem[targetPathProperty]) 
	  {
      path = targetPathID.get(selectedItemsPropertyName).rawItem[targetPathProperty];
    }

    itemLink = _.template(template,{
      displayText: targetDisplayTextID.get("text"),
      alternateText: encodeURIComponent(targetAltTextID.get("text")),
      itemId: targetControlID.get("selectedItemId"),
      queryString: encodeURIComponent(targetQueryID.get("text")),
      target: targetWindowValue,
      styleClass: targetStyleID.get("text"),
      path: path
    });
		
	  context.app.closeDialog(itemLink);
  };

  return action;
});