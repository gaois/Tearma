function submitQuickSearch(form) {
    var word = form["word"].value;
    if (word) {
        var url = "/q/" + encodeURIComponent(word) + "/";
        window.location = url;
    }
    return false;
}

function submitAdvSearch(form) {
    var word = form["word"].value;
    if (word) {
        var url = "/plus/" + encodeURIComponent(word) + "/";
        url += form["length"].value + "/";
        url += form["extent"].value + "/";
        if (form["lang"].value != "") url += form["lang"].value + "/";
        window.location = url;
    }
    return false;
}

function hon(label, i) {
    $(label).addClass("on").closest(".prettyWording").find(".h" + i).addClass("on");
}

function hoff(label, i) {
    $(label).removeClass("on").closest(".prettyWording").find(".h" + i).removeClass("on");
}

$(document).ready(function () { //hide term menu on click-off
    $(document).on("click", function (e) {
        var $clicked = $(e.target);
        if ($clicked.closest("#termMenu").length == 0 && $clicked.closest(".clickme").length == 0)  $("#termMenu").fadeOut("fast");
    });
});
function termMenuClick(clicker) {
    var $clicker = $(clicker);
    var $desig = $clicker.closest(".prettyDesig");
    var $wording = $desig.find(".prettyWording");
    var wording = $desig.attr("data-wording");
    var lang = $desig.attr("data-lang");

    $("#termMenu").remove();
    var $menu = $("<div id='termMenu'></div>");
    $menu.css("min-width", ($wording.width() + 30) + "px");

    var $item = $("<div class='copypaste'><input/></div>");
    $item.append("<div class='instruction ga' lang='ga'>Brúigh Ctrl + C le cóipeáil</div>");
    $item.append("<div class='instruction en' lang='en'>Press Ctrl + C to copy</div>");
    $item.find("input").val(wording);
    $item.appendTo($menu);

    if (lang == "ga" || lang == "en") {
        var $item = $("<a class='icon neid' target='_blank' href='https://www.focloir.ie/ga/search/ei/direct/?q=" + encodeURIComponent(wording) + "'></a>");
        $item.append("<span class='arrow'>»</span>")
        $item.append("<span class='title ga' lang='ga'>Foclóir Nua Béarla-Gaeilge</span>");
        $item.append("<span class='title en' lang='en'>New English-Irish Dictionary</span>");
        $item.append("<span class='url'>focloir.ie</span>")
        $item.appendTo($menu);
    }
    if (lang == "ga" || lang == "en") {
        var $item = $("<a class='icon teanglann' target='_blank' href='https://www.teanglann.ie/ga/?s=" + encodeURIComponent(wording) + "'></a>");
        $item.append("<span class='arrow'>»</span>")
        $item.append("<span class='title ga' lang='ga'>Leabharlann Teanga agus Foclóireachta</span>");
        $item.append("<span class='title en' lang='en'>Dictionary and Language Library</span>");
        $item.append("<span class='url'>teanglann.ie</span>")
        $item.appendTo($menu);
    }
    if (lang == "ga") {
        var $item = $("<a class='icon nci' target='_blank' href='http://corpas.focloir.ie/" + encodeURIComponent(wording) + "'></a>");
        $item.append("<span class='arrow'>»</span>")
        $item.append("<span class='title ga' lang='ga'>Nua-Chorpas na hÉireann <span class='remark'>(clárúchán de dhíth)</span></span>");
        $item.append("<span class='title en' lang='en'>New Corpus for Ireland <span class='remark'>(registration required)</span></span>");
        $item.append("<span class='url'>corpas.focloir.ie</span>")
        $item.appendTo($menu);
    }

    $menu.hide().insertAfter($clicker).slideDown("fast");
    $menu.find("input").focus().select();
}
