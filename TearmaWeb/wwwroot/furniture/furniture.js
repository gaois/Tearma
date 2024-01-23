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

function hon(label, i) {
    $(label).addClass("on").closest(".prettyWording").find(".h" + i).addClass("on");
}
function hoff(label, i) {
    $(label).removeClass("on").closest(".prettyWording").find(".h" + i).removeClass("on");
}

$(document).ready(function () { //hide term menu on click-off
    $(document).on("click", function (e) {
        var $clicked = $(e.target);
        if ($clicked.closest("#termMenu").length == 0 && $clicked.closest(".clickme").length == 0 && $clicked.closest(".playme").length == 0)  $("#termMenu").fadeOut("fast");
    });
});
function playerMenuClick(clicker) {
    var $clicker = $(clicker);

    $("#termMenu").remove();
    var $menu = $("<div id='termMenu'></div>");

    if ($clicker.attr("data-U")!="") {
        var $item = $("<div class='soundItem'></div>");
        $item.append("<button data-filename='" + $clicker.attr("data-U") + "'><i class='fas fa-volume-up'></i></button>");
        $item.append("<span class='title ga' lang='ga'>Cúige Uladh</span>");
        $item.append("<span class='title en' lang='en'>Ulster dialect</span>");
        $item.appendTo($menu);
    }
    if ($clicker.attr("data-C")!="") {
        var $item = $("<div class='soundItem'></div>");
        $item.append("<button data-filename='" + $clicker.attr("data-C") + "'><i class='fas fa-volume-up'></i></button>");
        $item.append("<span class='title ga' lang='ga'>Cúige Chonnacht</span>");
        $item.append("<span class='title en' lang='en'>Connacht dialect</span>");
        $item.appendTo($menu);
    }
    if ($clicker.attr("data-M")!="") {
        var $item = $("<div class='soundItem'></div>");
        $item.append("<button data-filename='" + $clicker.attr("data-M") + "'><i class='fas fa-volume-up'></i></button>");
        $item.append("<span class='title ga' lang='ga'>Cúige Mumhan</span>");
        $item.append("<span class='title en' lang='en'>Munster dialect</span>");
        $item.appendTo($menu);
    }

    var $item = $("<div class='instructions'></div>");
    $item.append("<div class='ga' lang='ga'>Gineadh na comhaid fuaime seo le córas fóinéimithe agus sintéisithe <em>Abair</em> (TCD). Má tá aiseolas agat ina dtaobh, ní mór é a chur go díreach chuig foireann <em>Abair</em> ag <a href='mailto:abair.tcd@gmail.com'>abair.tcd@gmail.com</a>. Déanfar na comhaid ar <em>Téarma</em> a athghiniúint go tráthrialta.</div>");
    //$item.append("<div class='en' lang='en'>Bla bla</div>");
    $item.appendTo($menu);

    $menu.find("button").on("click", play);
    $menu.hide().insertAfter($clicker).slideDown("fast");
}
function play(e) {
    var $player = $(e.delegateTarget);
    var url = $player.attr("data-filename");
    $player.find("audio").remove();
    $player.append("<audio><source src=\""+url+"\" type=\"audio/wav\"></audio>");
    //$player.append("<audio><source src=\"" + url + "\" type=\"audio/mp3\"></audio>");
    $player.find("audio").toArray()[0].play();
}
function termMenuClick(clicker) {
    var $clicker = $(clicker);
    var $desig = $clicker.closest(".prettyDesig");
    var $wording = $desig.find(".prettyWording");
    var wording = $desig.attr("data-wording");
    var lang = $desig.attr("data-lang");

    $("#termMenu").remove();
    var $menu = $("<div id='termMenu'></div>");
    $menu.css("min-width", ($wording.width() + 30) + "px");

    // var $item = $("<div class='copypaste'><input/></div>");
    // $item.append("<div class='instruction ga' lang='ga'>Brúigh Ctrl + C le cóipeáil</div>");
    // $item.append("<div class='instruction en' lang='en'>Press Ctrl + C to copy</div>");
    // $item.find("input").val(wording);
    // $item.appendTo($menu);

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
        var $item = $("<a class='icon nci' target='_blank' href='https://corpas.focloir.ie/crystal/#concordance?corpname=gaeilge2&tab=basic&keyword=" + encodeURIComponent(wording) + "&attrs=word&viewmode=kwic&attr_allpos=all&refs_up=0&shorten_refs=1&glue=1&gdexcnt=300&show_gdex_scores=0&itemsPerPage=20&structs=s%2Cg&refs=&showresults=1&showTBL=0&tbl_template=&gdexconf=&f_tab=basic&f_showrelfrq=1&f_showperc=0&f_showreldens=0&f_showreltt=0&c_customrange=0'></a>");
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

function advDomTitles(select) {
    var $select = $(select);
    if ($select.attr("size") == "1") {
        $select.find("option").each(function () {
            var $option = $(this);
            if ($option.data("longTitle")) $option.html($option.data("longTitle"));
        });
    } else {
        $select.find("option").each(function () {
            var $option = $(this);
            if ($option.data("shortTitle")) $option.html($option.data("shortTitle"));
        });
    }
};
function getDomain (id) {
    var ret = null;
    allDomains.map(function(datum){ if (!ret && datum.id == id) ret = datum; });
    return ret;
};
function domLongTitle(domain) {
    var ret = domain.title;
    var dom = domain; while (dom.parentID) {
        var dom = getDomain(dom.parentID);
        if (dom) ret = dom.title + " &nbsp;►&nbsp; " + ret;
    }
    return ret;
};
function advDomRefill(selectedDomainID) {
    domains = [];
    var selectedDomain = getDomain(selectedDomainID);
    if (!selectedDomain) { //if no domain is selected:
        allDomains.map(function(domain){ if (!domain.parentID) { domain.expanded = false; domains.push(domain); } });
    } else {
        selectedDomain.expanded = true;
        domains.push(selectedDomain);
        allDomains.map(function(domain){ if (domain.parentID == selectedDomainID) { domain.expanded = false; domains.push(domain); } });
        //add all parents to the front of the list:
        var parentID = selectedDomain.parentID;
        while (parentID) {
            var domain = getDomain(parentID);
            parentID = null;
            if (domain) {
                domain.expanded = true;
                domains.unshift(domain);
                parentID = domain.parentID;
            }
        }
    }
    var $select = $("select#dom").html("");
    var level = 0;
    $select.append('<option value="0">réimse ar bith &middot; any domain</option>');
    if (selectedDomain) {
        var level = 1;
    }
    var prevDomainID = 0;
    domains.map(function(domain){
        if (domain.parentID == prevDomainID) level++;
        var padding = ""; for (var i = 0; i < level; i++) padding += "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
        var driller = "&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;";
        if (domain.hasChildren) {
            if (domain.expanded) driller = "▼&nbsp;";
            else driller = "►&nbsp;";
        }
        var shortTitle = padding+driller+domain.title;
        var longTitle = domLongTitle(domain);
        var $option = $('<option value="'+domain.id+'">'+longTitle+'</option>');
        $option.data("shortTitle", shortTitle);
        $option.data("longTitle", longTitle);
        $select.append($option);
        prevDomainID = domain.id;
    });
    $select.find("option").on("click", function (e) {
        advDomRefill($(e.delegateTarget).attr("value"));
    });
    advDomTitles($select);
    if (typeof (selectedDomainID) == "number" || typeof (selectedDomainID) == "string") $select.val(selectedDomainID.toString());
    else $select.val(0);
};

function copyClick(clicker){
    var $clicker = $(clicker);
    var $desig = $clicker.closest(".prettyDesig");
    var $wording = $desig.find(".prettyWording");
    var wording = $desig.attr("data-wording");
    copyTextToClipboard(wording);
    window.setTimeout(function(){
        $clicker.addClass("justClicked");
        window.setTimeout(function(){
            $clicker.removeClass("justClicked");
        }, 500);
    }, 100);
}
function copyTextToClipboard(text) {
    if (!navigator.clipboard) {
        fallbackCopyTextToClipboard(text);
        return;
    }
    navigator.clipboard.writeText(text).catch(err => { console.error("Async copy: Could not copy to clipboard: ", err); });
  }
  function fallbackCopyTextToClipboard(text) {
    var textArea = document.createElement("textarea");
    textArea.value = text;
    textArea.style.top = "0";
    textArea.style.left = "0";
    textArea.style.position = "fixed";
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();
    try {
      document.execCommand("copy");
    } catch (err) {
      console.error("Fallback: Cound not copy to clipboard", err);
    }
    document.body.removeChild(textArea);
  }