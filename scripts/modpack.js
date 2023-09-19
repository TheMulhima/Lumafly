function copyLink() {
    let pack = document.getElementById("pack-code").value;

    if (pack.length != 8) {
        alert("Invalid pack code. Must be 8 characters long")
        return;
    }

    navigator.clipboard.writeText(location.protocol + '//' + location.host + location.pathname + "?modpack=" + pack)

    // show copied badge
    let copied = document.getElementById("copied");
    copied.classList.toggle("notDisplayed");
    setTimeout(function() {copied.classList.toggle("notDisplayed")}, 1000);
}

window.addEventListener("load",(e) => {
    let pack = getParam('modpack');

    if (pack !== null) {
        document.getElementById("no-pack-provided").remove();
        openLink("scarab://modpack/" + pack);
    }
    else {
        document.getElementById("pack-provided").remove();
    }
});