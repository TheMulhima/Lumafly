function getPathPrefix(levelDeep) {
    var pathPrefix = "./";
    if (levelDeep > 0) {
      pathPrefix = ""
      for (let i = 0; i < levelDeep; i++) {
        pathPrefix += "../";
      }
    }
    return pathPrefix;
  }
  
  function addNavBar(levelDeep = 0) {
    var pathPrefix = getPathPrefix(levelDeep);
    var html = `
        <div class="navbar">
        <a href="${pathPrefix}index.html" class="imageButton"><img src="${pathPrefix}ConstructionKnight.ico" id="navbarIcon"></a>
        <a href="${pathPrefix}commands/index.html">Commands</a>
        <a href="https://www.github.com/TheMulhima/Scarab#readme">Repository</a>
        <a href="https://discord.gg/VDsg3HmWuB">Discord</a>
        <div class="dropdown">
          <button onclick="window.location.replace('${pathPrefix}index.html?download')" class="dropbtn">Download</button>
          <div class="dropdown-content">
            <a href="${pathPrefix}index.html?download">Stable</a>
            <a href="${pathPrefix}index.html?download=latest">Latest</a>
          </div>
        </div> 
        <div></div>
        </div>
    `
    document.body.innerHTML = html + document.body.innerHTML;
  }
  
  function addHKMBanner(levelDeep = 0) {
    var pathPrefix = getPathPrefix(levelDeep);
    var html = `
      <img src="${pathPrefix}HKMBanner.png" alt="HKM Banner" class="center hkmBanner">
    `
    document.body.innerHTML = html + document.body.innerHTML;
  }