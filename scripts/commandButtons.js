window.addEventListener("load",(e) => {
  document.getElementById('download').onclick = function () {
    window.location.href = "/Scarab/commands/download";
  }

  document.getElementById('forceUpdateAll').onclick = function () {
    window.location.href = '/Scarab/commands/forceUpdateAll';
  }

  document.getElementById('reset').onclick = function () {
    window.location.href = '/Scarab/commands/reset';
  }
  
  document.getElementById('customModLinks').onclick = function () {
    var link = prompt('Enter the url to the ModLinks.xml file');
    if (link !== null)
    {
      window.location.href = "/Scarab/commands/customModLinks?link=" + link;
    }
  }
  document.getElementById('redirect').onclick = function () {
    var link = prompt('Enter the command', "scarab://");
    if (link !== null) {
      window.location.href = "/Scarab/redirect?link=" + link;
    }
  }
});