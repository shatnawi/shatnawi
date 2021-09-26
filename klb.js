function MM_preloadImages() {
  var d=document; if(d.images){ if(!d.MM_p) d.MM_p=new Array();
    var i,j=d.MM_p.length,a=MM_preloadImages.arguments; for(i=0; i<a.length; i++)
    if (a[i].indexOf("#")!=0){ d.MM_p[j]=new Image; d.MM_p[j++].src=a[i];}}
}

function MM_findObj(n, d) {
  var p,i,x;  if(!d) d=document; if((p=n.indexOf("?"))>0&&parent.frames.length) {
    d=parent.frames[n.substring(p+1)].document; n=n.substring(0,p);}
  if(!(x=d[n])&&d.all) x=d.all[n]; for (i=0;!x&&i<d.forms.length;i++) x=d.forms[i][n];
  for(i=0;!x&&d.layers&&i<d.layers.length;i++) x=MM_findObj(n,d.layers[i].document);
  if(!x && d.getElementById) x=d.getElementById(n); return x;
}

function MM_nbGroup(event, grpName) {
  var i,img,nbArr,args=MM_nbGroup.arguments;
  if (event == "init" && args.length > 2) {
    if ((img = MM_findObj(args[2])) != null && !img.MM_init) {
      img.MM_init = true; img.MM_up = args[3]; img.MM_dn = img.src;
      if ((nbArr = document[grpName]) == null) nbArr = document[grpName] = new Array();
      nbArr[nbArr.length] = img;
      for (i=4; i < args.length-1; i+=2) if ((img = MM_findObj(args[i])) != null) {
        if (!img.MM_up) img.MM_up = img.src;
        img.src = img.MM_dn = args[i+1];
        nbArr[nbArr.length] = img;
    } }
  } else if (event == "over") {
    document.MM_nbOver = nbArr = new Array();
    for (i=1; i < args.length-1; i+=3) if ((img = MM_findObj(args[i])) != null) {
      if (!img.MM_up) img.MM_up = img.src;
      img.src = (img.MM_dn && args[i+2]) ? args[i+2] : ((args[i+1])? args[i+1] : img.MM_up);
      nbArr[nbArr.length] = img;
    }
  } else if (event == "out" ) {
    for (i=0; i < document.MM_nbOver.length; i++) {
      img = document.MM_nbOver[i]; img.src = (img.MM_dn) ? img.MM_dn : img.MM_up; }
  } else if (event == "down") {
    nbArr = document[grpName];
    if (nbArr)
      for (i=0; i < nbArr.length; i++) { img=nbArr[i]; img.src = img.MM_up; img.MM_dn = 0; }
    document[grpName] = nbArr = new Array();
    for (i=2; i < args.length-1; i+=2) if ((img = MM_findObj(args[i])) != null) {
      if (!img.MM_up) img.MM_up = img.src;
      img.src = img.MM_dn = (args[i+1])? args[i+1] : img.MM_up;
      nbArr[nbArr.length] = img;
  } }
}
var holder = new Image();

var lastToggle = new String("");
var originalTitle = "Selected Publications, Working Papers, and Presentations";
var filterText = new String("");

// hide all elements of type TAG except for those with class matching substring name, and their descendents
function toggle(name) {
	// we should show everything
	if (lastToggle.toLowerCase() == name.toLowerCase())
	{
		// reset the buttons
		var items = document.getElementsByTagName("td");
		for ( var i = 0; i < items.length; i++ ) {
			if ( items[i].id.toLowerCase().match("shadedblock")) {
				items[i].id = "miniblock";
			}
		}
		
		// now do the show
		showall('li');
		showall('p');
		showall('div');
		lastToggle = "";
		filterText = originalTitle;
	}
	
	// we should filter
	else 
	{
		// set up the buttons
		var items = document.getElementsByTagName("td");
		for ( var i = 0; i < items.length; i++ ) {
			if ( items[i].id.toLowerCase().match("shadedblock")) {
				items[i].id = "miniblock";
			}
			if ( items[i].className.toLowerCase().match("button-"+name))
				items[i].id = "shadedblock";
		}

		// now do the filter
		hideAllExcept('li',name);
		hideAllExcept('p',name);
		hideAllExcept('div',name);
		lastToggle = name;
		
		filterText = originalTitle;
		switch(name.toLowerCase()) {
			case "md":
				filterText = filterText + "<br>about Auctions and Mechanism Design";
				break;
			case "gt":
				filterText = filterText + "<br>about Game Theory and Computational Game Theory";
				break;
			case "ea":
				filterText = filterText + "<br>about Design and Empirical Analysis of Heuristic Algorithms";
				break;
			case "ml":
				filterText = filterText + "<br>about Machine Learning";
				break;
			default:
				filterText = filterText + ": " + name;
				break;
		}
	}
	
	// publish the filterText
	var title = document.getElementById("title");

	// changes to the title based on filters are disabled - delete the following line to restore
	title = originalTitle;
	
	title.innerHTML = filterText;
}

// Hide all instances of tag except those with class containing name as a substring
function hideAllExcept(tag,name) {
	var items = document.getElementsByTagName(tag);
	for ( var i = 0; i < items.length; i++ ) {
		if ( items[i].className.toLowerCase().match("filter")) {
			if ( items[i].className.toLowerCase().match(name.toLowerCase()))
				items[i].style.display = "block";
			else 
				items[i].style.display = "none";
		}
	}
}
    
// Show all instances of tag 
function showall(tag) {
	var items = document.getElementsByTagName(tag);
	for ( var i = 0; i < items.length; i++ ) {
		items[i].style.display = "block";
	}
}
    
function init() {
	MM_preloadImages('http://www.cs.ubc.ca/~kevinlb/images/Miscellaneous-Up-On.gif','http://www.cs.ubc.ca/~kevinlb/images/Home-Up-On.gif','http://www.cs.ubc.ca/~kevinlb/images/Research-Up-On.gif','http://www.cs.ubc.ca/~kevinlb/images/Teaching-Up-On.gif','http://www.cs.ubc.ca/~kevinlb/images/Publications-Up-On.gif','http://www.cs.ubc.ca/~kevinlb/images/Downloads-Up-On.gif','http://www.cs.ubc.ca/~kevinlb/images/Miscellaneous-Down.gif','http://www.cs.ubc.ca/~kevinlb/images/Home-Down.gif','http://www.cs.ubc.ca/~kevinlb/images/Research-Down.gif','http://www.cs.ubc.ca/~kevinlb/images/Teaching-Down.gif','http://www.cs.ubc.ca/~kevinlb/images/Publications-Down.gif','http://www.cs.ubc.ca/~kevinlb/images/Downloads-Up.gif','http://www.cs.ubc.ca/~kevinlb/images/Downloads-Down.gif');
	//initShowHide();	
}