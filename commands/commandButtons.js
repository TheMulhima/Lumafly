document.getElementById('download').onclick = function () {
    var mods = prompt('Enter mods to download here. If there is more than one mod, seperate them by "/" (eg. Satchel/HKMirror/Osmi)');
    if (mods !== null) {
      window.location = "./download/index.html?mods=" + encodeURIComponent(mods);
    }
  }

  document.getElementById('forceUpdateAll').onclick = function () {
    window.location = './forceUpdateAll/index.html';
  }

  document.getElementById('reset').onclick = function () {
    window.location = './reset/index.html';
  }
  
  document.getElementById('customModLinks').onclick = function () {
    var link = prompt('Enter the url to the ModLinks.xml file');
    if (link !== null)
    {
      window.location = "./customModLinks/index.html?link=" + link;
    }
  }
  document.getElementById('redirect').onclick = function () {
    var link = prompt('Enter the command', "scarab://");
    if (link !== null)
    {
      window.location = "../redirect/index.html?link=" + link;
    }
  }