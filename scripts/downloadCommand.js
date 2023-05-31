window.addEventListener("load",(e) => {
    var mods = getParam('mods');

    var modNames = []


    document.getElementById("create-link").onclick = function () {window.location.search = "?mods=" + encodeURI($("#mySelect").val().join("/"));}
    document.getElementById("create-download-link").onclick = function () {window.location.search = "";}

    if (mods !== null) {
        console.log("mods provided")
        document.getElementById("no-mods-provided").remove();
        document.getElementById("mod-list").innerHTML = mods.replaceAll('/', ', ') || document.getElementById("mod-list").innerHTML;
        openLink("scarab://download/" + mods);
    }
    else {
        document.getElementById("mods-provided").remove();
        $(document).ready(
        function() {
        fetch("https://raw.githubusercontent.com/hk-modding/modlinks/main/ModLinks.xml")
        .then(response => response.text())
        .then(data => {
            data = data.substr(21); // remove xml version header

            var xmlCommentRegex = /<!--[\S\s]+-->/g;
            data = data.replace(xmlCommentRegex, ""); // remove comments

            const json = JSON.parse(xml2json(data));

            // get list of manifests
            var manifestArr = json.elements[0].elements;

            manifestArr.forEach(manifest => {
            // manifest -> name element -> name tag contents -> xml tag value
            modNames.push(manifest.elements[0].elements[0].text)
            });

            modNames.sort();
            
            modNames.forEach(mod => {
            let option = document.createElement("option");
            option.setAttribute("value", mod);
            option.innerHTML = mod;
            document.getElementById("mySelect").appendChild(option);
            });
            
            $('#mySelect').select2({
            placeholder: "Select mods",
            allowClear: true,
            scrollAfterSelect:true,
            closeOnSelect: false
            });
        });
        });
    }
});