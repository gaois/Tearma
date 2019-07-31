function myEncodeURIComponent(word) {
    var ret = word;
    ret = ret.replace(/\\/g, "$backslash;");
    ret = ret.replace(/\//g, "$forwardslash;");
    ret = encodeURIComponent(ret);
    return ret;
}

function submitQuickSearch(form) {
    var word = form["word"].value;
    if (word) {
        var url = "/q/" + myEncodeURIComponent(word) + "/";
        window.location = url;
    }
    return false;
}

function submitAdvSearch(form) {
    var word = form["word"].value;
    if (word) {
        var url = "/plus/" + myEncodeURIComponent(word) + "/";
        url += form["length"].value + "/";
        url += form["extent"].value + "/";
        url += "lang" + form["lang"].value + "/";
        url += "pos" + form["pos"].value + "/";
        url += "dom" + form["dom"].value + "/";
        url += "sub" + form["sub"].value + "/";
        window.location = url;
    }
    return false;
}
function advChangeLang(obj) {
    var lang = obj.value;
    var form = obj.form;
    var select = form["pos"];
    var $option = $(select).find("option[value='" + $(select).val() + "']");
    if (!(lang == "0" || $option.attr("data-isfor").indexOf(";" + lang + ";") > -1)) select.selectedIndex = 0;
    $(select).find("option").each(function () {
        var $option = $(this);
        if ($option.attr("data-isfor") == "" || $option.attr("data-isfor").indexOf(";" + lang + ";") > -1) {
            $option.show();
        } else {
            $option.hide();
        }
    });
}
function advChangeDomain(obj, subdomID) {
    var $option = $(obj).find("option[value='" + $(obj).val() + "']");
    $(".line.subdomain").hide();
    if ($option.attr("data-subs") != "") {
        step2(obj, subdomID);
    } else {
        var domID = $(obj).val();
        $.get("/subdoms/"+domID+".json", function (subdoms) {
            $option.attr("data-subs", subdoms);
            step2(obj, subdomID);
        });
    }
    function step2(obj, subdomID) {
        var $option = $(obj).find("option[value='" + $(obj).val() + "']");
        var subdoms = JSON.parse($option.attr("data-subs"));
        var $select = $(".line.subdomain select")
        var val = $select.val() || subdomID || 0;
        $select.html("");
        $select.append("<option value='0'>foréimse ar bith &middot; any subdomain</option>");
        for (var i = 0; i < subdoms.length; i++) {
            var subdom = subdoms[i];
            var $option = $("<option/>").attr("value", subdom.id).html(subdom.name);
            $option.appendTo($select);
        }
        $select.val(val);
        if (!$select.val()) $select.val("0");
        if (subdoms.length > 0) $(".line.subdomain").show();
    }
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
    if (lang == "ga" || lang == "en") {
        var $item = $("<a class='icon gaois' target='_blank' href='https://www.gaois.ie/ga/?txt=" + encodeURIComponent(wording) + "'></a>");
        $item.append("<span class='arrow'>»</span>")
        $item.append("<span class='title ga' lang='ga'>Gaois</span>");
        //$item.append("<span class='title en' lang='en'>Dictionary and Language Library</span>");
        $item.append("<span class='url'>gaois.ie</span>")
        $item.appendTo($menu);
    }
    if (lang == "ga") {
        var $item = $("<a class='icon nci' target='_blank' href='http://focloir.sketchengine.co.uk/auth/run.cgi/simple_search?queryselector=iqueryrow&iquery=" + encodeURIComponent(wording) + "'></a>");
        $item.append("<span class='arrow'>»</span>")
        $item.append("<span class='title ga' lang='ga'>Nua-Chorpas na hÉireann <span class='remark'>(clárúchán de dhíth)</span></span>");
        $item.append("<span class='title en' lang='en'>New Corpus for Ireland <span class='remark'>(registration required)</span></span>");
        $item.append("<span class='url'>corpas.focloir.ie</span>")
        $item.appendTo($menu);
    }

    $menu.hide().insertAfter($clicker).slideDown("fast");
    $menu.find("input").focus().select();
}

$(document).ready(function () {
    if ($(".nonessential").length > 0) {
        $("#showAllDetails").show().css("display", "block");
        $(".nonessential").each(function () {
            var $entry = $(this).closest(".prettyEntry");
            $entry.find(".hideDetails").hide();
            $entry.find(".showDetails").show();
        });
        if (window.SUPER || Cookies.get("showAllDetails") == "true") showAllDetails();
    }
});
function showAllDetails() {
    $(".nonessential").slideDown("fast").each(function () {
        var $entry = $(this).closest(".prettyEntry");
        $entry.find(".showDetails").hide();
        $entry.find(".hideDetails").show();
    });
    $("#showAllDetails").hide();
    $("#hideAllDetails").show().css("display", "block");
    Cookies.set("showAllDetails", "true", {expires: 1});
}
function hideAllDetails() {
    $(".nonessential").slideUp("fast").each(function () {
        var $entry = $(this).closest(".prettyEntry");
        $entry.find(".hideDetails").hide();
        $entry.find(".showDetails").show();
    });
    $("#hideAllDetails").hide();
    $("#showAllDetails").show().css("display", "block");
    Cookies.set("showAllDetails", "false", { expires: 1 });
}
function showDetails(a) {
    var $entry = $(a).closest(".prettyEntry");
    $entry.find(".nonessential").slideDown("fast");
    $entry.find(".showDetails").hide();
    $entry.find(".hideDetails").show();
}
function hideDetails(a) {
    var $entry = $(a).closest(".prettyEntry");
    $entry.find(".nonessential").slideUp("fast");
    $entry.find(".hideDetails").hide();
    $entry.find(".showDetails").show();
}

function printableOn() {
    $("body").addClass("printable");
}
function printableOff() {
    $("body").removeClass("printable");
}

function searchboxBlur() {
    var text = $("input.searchbox").val() || "";
    Cookies.set("searchText", text, { expires: 1 });
}
$(document).ready(function () {
    var text = Cookies.get("searchText") || "";
    if ($("input.searchbox").val() == "") $("input.searchbox").val(text);

    var text = $("input.searchbox").val() || "";
    Cookies.set("searchText", text, { expires: 1 });
});
