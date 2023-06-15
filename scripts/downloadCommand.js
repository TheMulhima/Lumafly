let modNames = []

function isValidUrl(url) {
    if (url === undefined || url === null || url === "") return false
    try { 
        new URL(url); 
        return true;
    }
    catch { 
        return false;
    }
}

function isValidName(name) {
    if (name === undefined || name === null || name === "") return false;

    let invalidChars =/^[\\\/\:\*\?\'\<\>\|]+$/; // forbidden characters \ / : * ? " < > |
    return !invalidChars.test(name);
}

function parseCommand(data) {
    data = decodeURI(data)
    let index = 0;
    let modNamesAndUrls = new Map();
    console.log("parsing " + data)
    while (index < data.length)
    {
        let modName = "";
        let url = null
        while (index < data.length && data.charAt(index) !== '/')
        {
            console.log(index)
            if (data.charAt(index) == ':') // starter of url
            {
                index++; // consume :
                
                const LinkSep = "'";
                
                if (index >= data.length || data.charAt(index) != LinkSep) return null // invalid format refuse to parse
                
                index++; // consume "
                while (index < data.length && data.charAt(index) != LinkSep)
                {
                    if (url === null) url = "";
                    url += data.charAt(index);
                    index++;
                }

                if (index < data.length && data.charAt(index) == LinkSep)
                    index++; // consume "
                break;
            }

            modName += data.charAt(index);
            index++;
        }

        if (url !== null && !isValidUrl(url)) {
            console.log("invalid url " + url)
            return null; // if link is provided and is invalid link refuse to parse
        }

        if (!isValidName(modName)) {
            console.log("invalid name " + modName)
            return null; // invalid name refuse to parse
        }
        
        modNamesAndUrls.set(modName, url);
        console.log("added " + modName + " " + url + " to map")
        index++; // consume /
    }

    return modNamesAndUrls;
}

function addCustomMod() {
    let input =
    `<div class="centerItemContainer" style="margin-bottom: 10px;" >
        <input class="fit-width" type="text" id="name" placeholder="Enter Mod Name" style="margin-right: 20px;" >
        <input class="fit-width" type="text" id="link" placeholder="Enter Link">
    </div>`
    document.getElementById("customModsContainer").innerHTML += input
}

function handleModsParam(mods) {
    console.log("mods provided")
    document.getElementById("no-mods-provided").remove();

    let modList = parseCommand(mods);

    let displayText = "";
    let shouldOpen = true;
    if (modList === null) {
        displayText = "Invalid formatted link"
    } else if(modList.size > 0) {
        modList.forEach((link, name) => {
            if (link !== null) {
                displayText += name + " (Custom Mod),</br>";
            }
            else {
                displayText += name + ",</br>";
            }
        });

        displayText = displayText.slice(0, -6); //remove last comma
    } else {
        displayText = "No mods provided"
        shouldOpen = false;
    }

    document.getElementById("mod-list").innerHTML = displayText;
    if (shouldOpen) {
        openLink("scarab://download/" + mods);
    }
}

function makeInputPage() {
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

function encodeMods() {
    let toEncode = [];
    if ($("#mySelect").val() !== undefined && $("#mySelect").val() !== null && $("#mySelect").val().length > 0) {
        $("#mySelect").val().forEach(mod => toEncode.push(mod));
    }

    let customModConatiners = document.querySelectorAll("#customModsContainer > div")

    customModConatiners.forEach(c =>
    {
        let name = c.children[0].value
        let link = c.children[1].value

        console.log(name);
        console.log(link)

        if (isValidName(name) && isValidUrl(link)) {
            toEncode.push(`${name}:'${link}'`)
        }
        else {
            console.log(isValidName(name) + " " + isValidUrl(link))
        }
    });
    
    let encodedList = []
    toEncode.forEach(e => encodedList.push(encodeURI(e)))
    return encodedList.join("/");
}

function openModsLink() {
    window.location.search = "?mods=" + encodeMods();
}

function copyLink() {
    navigator.clipboard.writeText(location.protocol + '//' + location.host + location.pathname + "?mods=" + encodeMods())

    // show copied badge
    let copied = document.getElementById("copied");
    copied.classList.toggle("notDisplayed");
    setTimeout(function() {copied.classList.toggle("notDisplayed")}, 1000);
}

window.addEventListener("load",(e) => {
    let mods = getParam('mods');

    document.getElementById("create-download-link").onclick = function () {window.location.search = "";}

    if (mods !== null) {
        console.log("mods not null")
        handleModsParam(mods)
    }
    else {
        console.log("make input page")
        makeInputPage();
    }
});