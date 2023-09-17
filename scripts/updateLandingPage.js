function downloadScarab(latest = false, onclicked = false)
{
  let linkBase = "https://github.com/TheMulhima/Lumafly/releases/latest/download/";

  let files = {
    "Windows" : "Scarab.exe",
    "Mac"     : "Scarab-MacOS.zip",
    "Linux"   : "Scarab-Linux.zip"
  }

  if (latest) {
    linkBase = "https://nightly.link/TheMulhima/Lumafly/workflows/build/master/";
    files.Windows = "Scarab-Windows.zip"
  }

  var uap = new UAParser();
  platform = uap.getResult().os.name;
  macOS = ['Mac OS'];
  windowsOS = ['Windows'];
  linuxOS = ['Linux'];
  let link = null;

  if (macOS.indexOf(platform) !== -1)          link = linkBase + files.Mac;
  else if (windowsOS.indexOf(platform) !== -1) link = linkBase + files.Windows;
  else if (linuxOS.indexOf(platform) !== -1)   link = linkBase + files.Linux

  if (link !== null) {
    openLink(link, onclicked);
  }
  else {
    setTimeout(function() {
      if (confirm("The website could not automatically detect your platform, would you like to open the releases page? You can download Lumafly from there.")) {
        openLink("https://github.com/TheMulhima/Lumafly/releases/latest", onclicked);
      }
    }, 500);
  }
}

function addDataToHTML(data, header)
{
  let screenShotTitleRegex = /## Screenshot:.*/g
  let imageRegex = /!\[.*\]\(.*\)/g
  let releaseNotes = data.replace(screenShotTitleRegex, "").replace(imageRegex, "");

  let releaseNotesHeader = document.createElement("h2");
  releaseNotesHeader.innerHTML = header;
  releaseNotesHeader.className = "center centertext";
  releaseNotesHeader.setAttribute("style", "margin-top:40px");
  document.body.appendChild(releaseNotesHeader);

  let releaseNotesBody = document.createElement("zero-md");
  releaseNotesBody.className = "center";
  let md = window.markdownit();
  releaseNotesBody.innerHTML = md.render(releaseNotes);
  document.body.appendChild(releaseNotesBody);
}

// get param and download
window.addEventListener("load",(e) => {
  var download = getParam("download");

  if (download !== null) {
    downloadScarab(download === "latest");

    document.getElementById("download-message").innerHTML = "If nothing has be downloaded, please download it from the <a href=\"https://github.com/TheMulhima/Lumafly/releases/latest\">releases page</a>";
    document.getElementById("not-needed-on-download").remove();
    document.getElementById("header").innerHTML = "Thank you for downloading Lumafly";

    if (download === 'update') {
      document.getElementById("not-needed-on-update").remove();

      fetch("https://api.github.com/repos/TheMulhima/Lumafly/releases/latest")
      .then(response => response.json())
      .then(data => {
        addDataToHTML(data.body, "Release Notes: ");
        resolve();
      })
    }
  }
});

// expander code
window.addEventListener("load",(e) => {
  var coll = document.getElementsByClassName("expander");
  var i;

  for (i = 0; i < coll.length; i++) {
    coll[i].addEventListener("click", function() {
      let icon = this.getElementsByClassName("expanderIcon")[0];
      icon.classList.toggle("expanderIconFlipper");
      var content = this.nextElementSibling;
      if (content.style.display === "block") {
        content.style.display = "none";
      } else {
        content.style.display = "block";
      }
    });
}

});
