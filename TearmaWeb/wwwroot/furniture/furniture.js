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