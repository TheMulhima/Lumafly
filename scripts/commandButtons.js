window.addEventListener("load",(e) => {
  document.getElementById('download').onclick = function () {
    window.location.href = "/Lumafly/commands/download";
  }

  document.getElementById('forceUpdateAll').onclick = function () {
    window.location.href = '/Lumafly/commands/forceUpdateAll';
  }

  document.getElementById('reset').onclick = function () {
    window.location.href = '/Lumafly/commands/reset';
  }
  
  document.getElementById('customModLinks').onclick = function () {
    var link = prompt('Enter the url to the ModLinks.xml file');
    if (link !== null)
    {
      window.location.href = "/Lumafly/commands/customModLinks?link=" + link;
    }
  }
  document.getElementById('redirect').onclick = function () {
    var link = prompt('Enter the command', "scarab://");
    if (link !== null) {
      window.location.href = "/Lumafly/redirect?link=" + link;
    }
  }
});