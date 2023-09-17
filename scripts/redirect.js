var link = getParam('link');
if (link === null || link === undefined || link === "") {
  var error = document.createElement("h4");
  error.innerHTML = "No link was provided. Please provide a link by adding ?link= to the end of the url!";
  error.className = "center, centertext";
  var example = document.createElement("p");
  example.innerHTML = "For example: https://themulhima.github.io/Lumafly/redirect?link=scarab://download/Satchel";
  example.className = "center, centertext";
  document.body.removeChild(document.getElementById("header"));
  document.body.appendChild(error);
  document.body.appendChild(example);
} else {
  if (link.startsWith("scarab://") || link.startsWith("steam://")) {
    document.getElementById("message").innerHTML = link;
    openLink(link);
  } else {
    document.body.removeChild(document.getElementById("header"));
    document.getElementById("message").innerHTML = "Invalid link! Only scarab:// links are allowed!"
  }
}
