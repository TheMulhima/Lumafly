window.addEventListener("load",(e) => {
    let mods = getParam('mods');

    let modNames = []


    document.getElementById("open-link").onclick = function () {
        if ($("#mySelect").val().length !== 0) {
            window.location.search = "?mods=" + encodeURI($("#mySelect").val().join("/"));
        } else {
            alert("Invalid link. Please select at least one mod.")
        }
    }
    document.getElementById("copy-link").onclick = function () {

        if ($("#mySelect").val().length !== 0) {
            navigator.clipboard.writeText(location.protocol + '//' + location.host + location.pathname + "?mods=" + encodeURI($("#mySelect").val().join("/")))

            let copied = document.getElementById("copied");
            copied.classList.toggle("notDisplayed");
            setTimeout(function() {copied.classList.toggle("notDisplayed")}, 1000);
        } else {
            alert("Invalid link. Please select at least one mod.")
        }
    }
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

            let xmlCommentRegex = /<!--[\S\s]+-->/g;
            data = data.replace(xmlCommentRegex, ""); // remove comments

            const json = JSON.parse(xml2json(data));

            // get list of manifests
            let manifestArr = json.elements[0].elements;

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

            // add mods to list from url
            let add = getParam('list');
            if (add !== null) {
                let modToAddToList = add.split("/");

                // sanitise input
                let invalidMods = [];
                modToAddToList.forEach(mod => {if (!modNames.includes(mod)) invalidMods.push(mod)});
                invalidMods.forEach(mod => {modToAddToList.filter(item => item !== mod)});
                
                $('#mySelect').val(modToAddToList).trigger('change');
            }
        });
        });
    }
});