function openLink(url) {
    setTimeout(function(){
        window.location.replace(url);
      }, 500);
}

function getParam(paramName) {
  const urlParams = new URLSearchParams(window.location.search);
  return urlParams.get(paramName);
}

function downloadScarab()
{
  var download = getParam("download");

  if (download !== null)
  {
    let linkBase = "https://github.com/TheMulhima/Scarab/releases/latest/download/"

    if (download === "latest")
    {
      linkBase = "https://nightly.link/TheMulhima/Scarab/workflows/dotnet/master/";
    }


    var uap = new UAParser();
    platform = uap.getResult().os.name;
    macOS = ['Mac OS'];
    windowsOS = ['Windows'];
    linuxOS = ['Linux'];
    let link = null;

    if (macOS.indexOf(platform) !== -1)          link = linkBase + "Scarab-MacOS.zip";
    else if (windowsOS.indexOf(platform) !== -1) link = linkBase + "Scarab-Windows.zip";
    else if (linuxOS.indexOf(platform) !== -1)   link = linkBase + "Scarab-Linux.zip"

    if (link !== null) {
      openLink(link);
    }
    else {
      // make it feel like its loading
      setTimeout(function() {
        if (confirm("The website could not automatically detect your platform, would you like to open the releases page? You can download Scarab+ from there.")) {
          window.location.replace("https://github.com/TheMulhima/Scarab/releases/latest")
        }
      }, 500);
    }
  }
}

function addDataToHTML(data, header)
{
  let screenShotTitleRegex = /## Screenshot:.*/g
  let imageRegex = /!\[.*\]\(.*\)/g
  let releaseNotes = data.replace(screenShotTitleRegex, "").replace(imageRegex, "");

  let releaseNotesHeader = document.createElement("h2");
  releaseNotesHeader.innerHTML = header;
  releaseNotesHeader.className = "center, centertext";
  releaseNotesHeader.setAttribute("style", "margin-top:20px");
  document.body.appendChild(releaseNotesHeader);

  let releaseNotesBody = document.createElement("zero-md");
  releaseNotesBody.className = "center";
  let md = window.markdownit();
  releaseNotesBody.innerHTML = md.render(releaseNotes);
  document.body.appendChild(releaseNotesBody);
}