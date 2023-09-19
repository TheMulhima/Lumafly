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
  
  document.getElementById('modpack').onclick = function () {
    window.location.href = "/Lumafly/commands/modpack";
    
  }
  document.getElementById('redirect').onclick = function () {
    var link = prompt('Enter the command', "scarab://");
    if (link !== null) {
      window.location.href = "/Lumafly/redirect?link=" + link;
    }
  }
});