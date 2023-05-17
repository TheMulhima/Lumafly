function openLink(url) {
    setTimeout(function(){
        window.location.replace(url);
      }, 500);
}

function getParam(paramName) {
  const urlParams = new URLSearchParams(window.location.search);
  return urlParams.get(paramName);
}