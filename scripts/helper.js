function openLink(url, onclicked = false) {
  // whether its on clicked or not matters because browsers block opening links in new
  // tab if its not onclick
  if (onclicked) {
    window.open(url);
  }
  else {
    setTimeout(function(){
      window.location.href = url;
    }, 500);
  }
}

function getParam(paramName) {
  const urlParams = new URLSearchParams(window.location.search);
  return urlParams.get(paramName);
}