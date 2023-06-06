// src badge is added automatically
window.addEventListener("load",(e) => {
  let srcBadge = 
  `
  <a id="src" href="https://github.com/TheMulhima/Scarab/tree/website">
    <img alt="source code link" class="center sourceCodeBadge" src="https://img.shields.io/static/v1?style=for-the-badge&message=Source%20Code&color=181717&logo=GitHub&logoColor=FFFFFF&label="/>
  </a>
  `
  console.log("Adding navbar")
  document.body.innerHTML = srcBadge + document.body.innerHTML;
});

function getPathPrefix(levelDeep) {
  let pathPrefix = "./";
  if (levelDeep > 0) {
    pathPrefix = ""
    for (let i = 0; i < levelDeep; i++) {
      pathPrefix += "../";
    }
  }
  return pathPrefix;
}
  
function addNavBar(levelDeep = 0) {
  let pathPrefix = getPathPrefix(levelDeep);
  let html = `
      <div class="navbar" style="flex-wrap: wrap;">
        <a href="${pathPrefix}" class="imageButton"><img src="${pathPrefix}assets/ConstructionKnight.ico" alt="Scarab+ icon" id="navbarIcon"></a>
        <a href="${pathPrefix}commands">Commands</a>
        <a href="https://www.github.com/TheMulhima/Scarab#readme">Repository</a>
        <a href="https://discord.gg/VDsg3HmWuB">Discord</a>
        <div class="dropdown">
          <button onclick="window.location.href = '${pathPrefix}?download'" class="dropbtn">Download</button>
          <div class="dropdown-content">
            <a href="${pathPrefix}?download">Stable</a>
            <a href="${pathPrefix}?download=latest">Latest</a>
          </div>
        </div>
        <div></div>
      </div>
  `
  document.body.innerHTML = html + document.body.innerHTML;
}

function addHKMBanner(levelDeep = 0) {
  let pathPrefix = getPathPrefix(levelDeep);
  let html = `
    <img src="${pathPrefix}assets/HKMBanner.png" alt="HKM Banner" class="center hkmBanner">
  `
  document.body.innerHTML = html + document.body.innerHTML;
}