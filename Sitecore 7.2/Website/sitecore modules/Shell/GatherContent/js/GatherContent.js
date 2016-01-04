﻿function getUrlVars() {
    var vars = [], hash;
    var hashes = location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (var i = 0; i < hashes.length; i++) {
        hash = hashes[i].split('=');
        vars.push(hash[0]);
        vars[hash[0]] = hash[1];
    }
    return vars;
}

function initTooltip() {
    jQuery("tr td").each(function (i) {
        if (jQuery(this).outerWidth() < this.scrollWidth) {
            simple_tooltip(this, "tooltip", i);
        }
    });
}
function resizeTableHead(){
    jQuery("thead th.cell_resize").each(function(){
        jQuery(this).find("div").css("width",jQuery(this).width())
    })
    jQuery("thead th div").each(function(){
        if( jQuery(this).height()>18){
            jQuery(this).css("padding-top",0);
            jQuery(this).css("margin-top",7)
        }
    })
}

function simple_tooltip(target_items, name, i) {
    jQuery("body").append("<div class='" + name + "' id='" + name + i + "'><p>" + jQuery(target_items).text() + "</p></div>");
    var my_tooltip = jQuery("#" + name + i);

    jQuery(target_items).mouseover(function () {
        my_tooltip.css({ opacity: 0.8, display: "none" }).fadeIn(0);
    }).mousemove(function (kmouse) {
        my_tooltip.css({ left: kmouse.pageX - 120, top: kmouse.pageY + 20 });
    }).mouseout(function () {
        my_tooltip.fadeOut(0);
    });
}
function document_resize() {
    jQuery(window).resize(function () {
        jQuery(".tooltip").remove();
        jQuery("tr td").each(function (i) {
            if (jQuery(this).outerWidth() < this.scrollWidth) {
                simple_tooltip(this, "tooltip", i);
            }
        });
        jQuery("thead th div").each(function(){
            if( jQuery(this).height()>18){
                jQuery(this).css("padding-top",0);
                jQuery(this).css("margin-top",7)
            }
            else{
                jQuery(this).css("padding-top",3);
                jQuery(this).css("margin-top",10)
            }
        })
    });
}

jQuery(function () {
    document_resize();
    jQuery(window).resize(function(){
        jQuery(".table_import_scroll").css("max-height",jQuery("body").height()-250)
})
        jQuery(".table_import_scroll").css("max-height",jQuery("body").height()-250)
});


