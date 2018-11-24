const fs=require("fs-extra");
const xmldom=require("xmldom"); //https://www.npmjs.com/package/xmldom
const domParser=new xmldom.DOMParser();

var lang_id2abbr={}; //eg. "432543" -> "ga"
var subdomain2superdomain={}; //eg. "545473" --> "544354"

deed(1000000);
//deedAgain(1000, 2000);

//deed(10000);
//deedAgain(10000, 20000);
//deedAgain(20000, 30000);
//deedAgain(30000, 40000);
//deedAgain(40000, 50000);
//deedAgain(50000, 60000);
//deedAgain(60000, 70000);
//deedAgain(70000, 80000);
//deedAgain(80000, 90000);
//deedAgain(90000, 100000);

function deed(stop){
    doLanguages(function(){
      doAcceptLabels(function(){
        doInflectLabels(function(){
          doSources(function(){
            doPosLabels(function(){
              doDomains(function(){
                doCollections(function(){
                  doNoteTypes(function(){
                    doConcepts(0, stop, function(){
						console.log(`finito`);
					});
                  });
                });
              });
            });
          });
        });
      });
    });
}
function deedAgain(start, stop){
    doLanguages(function(){
      doAcceptLabels(function(){
        doInflectLabels(function(){
          doSources(function(){
            doPosLabels(function(){
              doDomains(function(){
                doCollections(function(){
                  doConcepts(start, stop, function(){
                    console.log(`finito`);
                  });
                });
              });
            });
          });
        });
      });
    });
}

function doLanguages(callnext){
	var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.language\\";
	var lingo={languages: [
		{abbr: "ga", role: "major",title: {ga: "Gaeilge", en: "Irish"}},
		{abbr: "en", role: "major",title: {ga: "Béarla", en: "English"}},
	]};
	var filenames=fs.readdirSync(dir);
	filenames.map(filename => {
	if(filename.match(/\.xml$/)){
	  var id=filename.replace(/\.xml$/, "");
	  var xml=fs.readFileSync(dir+filename, "utf8");
	  var doc=domParser.parseFromString(xml, 'text/xml');
	  var abbr=doc.documentElement.getAttribute("abbr");
	  lang_id2abbr[id]=abbr;
	  if(abbr!="ga" && abbr!="en") {
		var json={abbr: abbr, role: "minor", title: {
		  ga: doc.getElementsByTagName("nameGA")[0].getAttribute("default"),
		  en: doc.getElementsByTagName("nameEN")[0].getAttribute("default"),
		}};
		lingo.languages.push(json);
	  }
	}
	});
	fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\config\\lingo.js", lingo, {spaces: 2});
	var defaultAbc=[["a", "á", "à", "â", "ä", "ă", "ā", "ã", "å", "ą", "æ"],["b"],["c", "ć", "ċ", "ĉ", "č", "ç"],["d", "ď", "đ"],["e", "é", "è", "ė", "ê", "ë", "ě", "ē", "ę"],["f"],["g", "ġ", "ĝ", "ğ", "ģ"],["h", "ĥ", "ħ"],["i", "ı", "í", "ì", "i", "î", "ï", "ī", "į"],["j", "ĵ"],["k", "ĸ", "ķ"],["l", "ĺ", "ŀ", "ľ", "ļ", "ł"],["m"],["n", "ń", "ň", "ñ", "ņ"],["o", "ó", "ò", "ô", "ö", "ō", "õ", "ő", "ø", "œ"],["p"],["q"],["r", "ŕ", "ř", "ŗ"],["s", "ś", "ŝ", "š", "ş", "ș", "ß"],["t", "ť", "ţ", "ț"],["u", "ú", "ù", "û", "ü", "ŭ", "ū", "ů", "ų", "ű"],["v"],["w", "ẃ", "ẁ", "ŵ", "ẅ"],["x"],["y", "ý", "ỳ", "ŷ", "ÿ"],["z", "ź", "ż", "ž"]]
	fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\config\\abc.js", {en: defaultAbc, ga: defaultAbc}, {spaces: 2});
	console.log("languages done");
	callnext();
}
function doAcceptLabels(callnext){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.acceptLabel\\";
  var filenames=fs.readdirSync(dir);
  doOne();
  function doOne(){
    if(filenames.length>0){
      var filename=filenames.pop();
      var id=filename.replace(/\.xml$/, "");
      var xml=fs.readFileSync(dir+filename, "utf8");
      var doc=domParser.parseFromString(xml, 'text/xml');
      var json={
        title: {
          ga: doc.getElementsByTagName("nameGA")[0].getAttribute("default"),
          en: doc.getElementsByTagName("nameEN")[0].getAttribute("default"),
        },
        level: doc.getElementsByTagName("level")[0].getAttribute("default") || "0",
      };
	  fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\metadata\\acceptLabel\\"+id+".js", json, {spaces: 2});
	  doOne();
    } else {
      console.log("acceptability labels done");
      callnext();
    }
  };
}
function doInflectLabels(callnext){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.inflectLabel\\";
  var filenames=fs.readdirSync(dir);
  doOne();
  function doOne(){
    if(filenames.length>0){
      var filename=filenames.pop();
      var id=filename.replace(/\.xml$/, "");
      var xml=fs.readFileSync(dir+filename, "utf8");
      var doc=domParser.parseFromString(xml, 'text/xml');
      var json={
        abbr: doc.documentElement.getAttribute("abbr"),
        title: {
          ga: doc.getElementsByTagName("nameGA")[0].getAttribute("default"),
          en: doc.getElementsByTagName("nameEN")[0].getAttribute("default"),
        },
        isfor: [],
      };
      var isForGA=(doc.getElementsByTagName("isForGA")[0].getAttribute("default")=="1");
      var isForNonGA=(doc.getElementsByTagName("isForNonGA")[0].getAttribute("default")=="1");
      if(isForGA && isForNonGA) json.isfor=["_all"];
        else if(isForGA) json.isfor=["ga"];
        else if(isForNonGA) json.isfor=["en", "_allminor"];
	  fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\metadata\\inflectLabel\\"+id+".js", json, {spaces: 2});
      doOne();
    } else {
      console.log("inflect labels done");
      callnext();
    }
  };
}
function doSources(callnext){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.source\\";
  var filenames=fs.readdirSync(dir);
  doOne();
  function doOne(){
    if(filenames.length>0){
      var filename=filenames.pop();
      var id=filename.replace(/\.xml$/, "");
      var xml=fs.readFileSync(dir+filename, "utf8");
      var doc=domParser.parseFromString(xml, 'text/xml');
      var json={
        title: {
          ga: doc.getElementsByTagName("name")[0].getAttribute("default"),
          en: doc.getElementsByTagName("name")[0].getAttribute("default"),
        },
      };
	  fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\metadata\\source\\"+id+".js", json, {spaces: 2});
      doOne();
    } else {
      console.log("sources done");
      callnext();
    }
  };
}
function doPosLabels(callnext){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.posLabel\\";
  var filenames=fs.readdirSync(dir);
  doOne();
  function doOne(){
    if(filenames.length>0){
      var filename=filenames.pop();
      var id=filename.replace(/\.xml$/, "");
      var xml=fs.readFileSync(dir+filename, "utf8");
      var doc=domParser.parseFromString(xml, 'text/xml');
      var json={
        abbr: doc.documentElement.getAttribute("abbr"),
        title: {
          ga: doc.getElementsByTagName("nameGA")[0].getAttribute("default"),
          en: doc.getElementsByTagName("nameEN")[0].getAttribute("default"),
        },
        isfor: [],
      };
      var isForGA=(doc.getElementsByTagName("isForGA")[0].getAttribute("default")=="1");
      var isForNonGA=(doc.getElementsByTagName("isForNonGA")[0].getAttribute("default")=="1");
      if(isForGA && isForNonGA) json.isfor=["_all"];
        else if(isForGA) json.isfor=["ga"];
        else if(isForNonGA) json.isfor=["en", "_allminor"];
      fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\metadata\\posLabel\\"+id+".js", json, {spaces: 2});
      doOne();
    } else {
      console.log("pos labels done");
      callnext();
    }
  };
}
function doDomains(callnext){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.domain\\";
  var filenames=fs.readdirSync(dir);
  var domains={}; //"3543543" -> {...}
  filenames.map(filename => {
    var id=filename.replace(/\.xml$/, "");
    var xml=fs.readFileSync(dir+filename, "utf8");
    var doc=domParser.parseFromString(xml, 'text/xml');
    var json={
      _parentID: (doc.getElementsByTagName("parent").length>0 ? doc.getElementsByTagName("parent")[0].getAttribute("default") : null),
      title: {
        ga: (doc.getElementsByTagName("nameGA").length>0 ? doc.getElementsByTagName("nameGA")[0].getAttribute("default") : ""),
        en: (doc.getElementsByTagName("nameEN").length>0 ? doc.getElementsByTagName("nameEN")[0].getAttribute("default") : ""),
      },
      subdomains: [],
    };
    domains[id]=json;
  });
  var domainhier=[];
  for(domainID in domains){
    var domain=domains[domainID];
    if(domain._parentID) {
      domains[domain._parentID].subdomains.push(domain);
      domain.lid=domainID;
    } else {
      domainhier.push(domain);
      domain._id=domainID;
    }
    delete domain._parentID;
  }
  doOne();
  function doOne(){
    if(domainhier.length>0){
      var domain=domainhier.pop();
      var id=domain._id;
      delete domain._id;
      remember(id, domain.subdomains);
	  fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\metadata\\domain\\"+id+".js", domain, {spaces: 2});
      doOne();
    } else {
      console.log("domains done");
      callnext();
    }
  };
  function remember(superdomainID, subdomains){
    subdomains.map(subdomain => {
      subdomain2superdomain[subdomain.lid]=superdomainID;
      remember(superdomainID, subdomain.subdomains);
    });
  }
}
function doCollections(callnext){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.collection\\";
  var filenames=fs.readdirSync(dir);
  doOne();
  function doOne(){
    if(filenames.length>0){
      var filename=filenames.pop();
      var id=filename.replace(/\.xml$/, "");
      var xml=fs.readFileSync(dir+filename, "utf8");
      var doc=domParser.parseFromString(xml, 'text/xml');
      var json={
        title: {
          ga: doc.getElementsByTagName("nameGA")[0].getAttribute("default"),
          en: doc.getElementsByTagName("nameEN").length>0 ? doc.getElementsByTagName("nameEN")[0].getAttribute("default") : doc.getElementsByTagName("nameGA")[0].getAttribute("default"),
        },
      };
	  fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\metadata\\collection\\"+id+".js", json, {spaces: 2});
      doOne();
    } else {
      console.log("collections done");
      callnext();
    }
  };
}
function doNoteTypes(callnext){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.noteType\\";
  var filenames=fs.readdirSync(dir);
  doOne();
  function doOne(){
    if(filenames.length>0){
      var filename=filenames.pop();
      var id=filename.replace(/\.xml$/, "");
      var xml=fs.readFileSync(dir+filename, "utf8");
      var doc=domParser.parseFromString(xml, 'text/xml');
      var json={
        title: doc.getElementsByTagName("name")[0].getAttribute("default"),
      };
	  fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\metadata\\tag\\"+id+".js", json, {spaces: 2});
      doOne();
    } else {
      console.log("note types done");
      callnext();
    }
  };
}
function doConcepts(start, stop, callnext){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.concept\\";
  var filenames=fs.readdirSync(dir).slice(start, stop);
  var todo=0;
  var done=0;
  filenames.map((filename, filenameIndex) => {
    var id=filename.replace(/\.xml$/, "");
    //console.log(`starting to process entry ID ${id}`);
    var xml=fs.readFileSync(dir+filename, "utf8");
    var doc=domParser.parseFromString(xml, 'text/xml');
    var json={
      cStatus: (doc.documentElement.getAttribute("checked")=="0" ? "0" : "1"),
      pStatus: (doc.documentElement.getAttribute("hidden")=="1" ? "0" : "1"),
      dateStamp: "",
      domains: [],
      desigs: [],
      intros: {ga: "", en: ""},
      definitions: [],
      examples: [],
      collections: [],
      extranets: [],
    };
    //desigs:
    var els=doc.getElementsByTagName("term");
    for(var i=0; i<els.length; i++) { var el=els[i];
      var desig={
        term: {},
        clarif: el.getAttribute("clarification") || "",
        accept: el.getAttribute("acceptLabel") || null,
        sources: [],
      };
      //sources:
      var subels=el.getElementsByTagName("source");
      for(var ii=0; ii<subels.length; ii++) { var subel=subels[ii];
        desig.sources.push(subel.getAttribute("default"));
      }
      //the term:
      var termID=el.getAttribute("default");
      desig.term=getTerm(termID);
      if(desig.term) json.desigs.push(desig);
    }
    //domains:
    var els=doc.getElementsByTagName("domain");
    for(var i=0; i<els.length; i++) { el=els[i];
      if(el.parentNode.nodeName=="concept"){
        var domainID=el.getAttribute("default");
        if(subdomain2superdomain[domainID]) json.domains.push({superdomain: subdomain2superdomain[domainID], subdomain: domainID});
        else json.domains.push({superdomain: domainID, subdomain: null});
      }
    }
    //intros:
    var els=doc.getElementsByTagName("introGA");
    if(els.length>0 && els[0].getAttribute("default")!="") json.intros.ga=els[0].getAttribute("default");
    var els=doc.getElementsByTagName("introEN");
    if(els.length>0 && els[0].getAttribute("default")!="") json.intros.en=els[0].getAttribute("default");
    //definitions:
    var els=doc.getElementsByTagName("definition");
    for(var i=0; i<els.length; i++) { el=els[i];
      var domains=[];
      var domels=el.getElementsByTagName("domain");
      for(var ii=0; ii<domels.length; ii++) { domel=domels[ii];
        var domainID=domel.getAttribute("default");
        if(subdomain2superdomain[domainID]) domains.push({superdomain: subdomain2superdomain[domainID], subdomain: domainID});
        else domains.push({superdomain: domainID, subdomain: null});
      }
      var obj={texts: {ga: "", en: ""}, domains: domains, sources: []};
      var subels=doc.getElementsByTagName("textEN");
      if(subels.length>0) {
        var text=subels[0].getAttribute("default");
        if(text!="") obj.texts["en"]=text;
      }
      subels=doc.getElementsByTagName("textGA");
      if(subels.length>0) {
        var text=subels[0].getAttribute("default");
        if(text!="") obj.texts["ga"]=text;
      }
      json.definitions.push(obj);
    }
    //examples:
    var els=doc.getElementsByTagName("example");
    for(var i=0; i<els.length; i++) { el=els[i];
      var example=getExample(el.getAttribute("default"));
      if(example) json.examples.push(example);
    }
    //collections:
    var els=doc.getElementsByTagName("collection");
    for(var i=0; i<els.length; i++) { el=els[i];
      json.collections.push(el.getAttribute("default"));
    }
    //save it:
    todo++;
	fs.writeJsonSync("C:\\MBM\\gaois\\Tearma\\Migration\\json\\entry\\"+id+".js", json, {spaces: 2});
    done++;
    //console.log(`entry ID ${id} saved: ${done} entries done`);
    if(done>=filenames.length) callnext();
  });
}
function getTerm(termID){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.term\\";
  if(!fs.existsSync(dir+termID+".xml")) return null;
  var xml=fs.readFileSync(dir+termID+".xml", "utf8");
  var doc=domParser.parseFromString(xml, 'text/xml');
  var json={
    id: termID,
    lang: lang_id2abbr[ doc.getElementsByTagName("language")[0].getAttribute("default") ],
    wording: doc.getElementsByTagName("wording")[0].getAttribute("default"),
    annots: [],
    inflects: [],
  };
  //annotations:
  var els=doc.getElementsByTagName("annotation");
  for(var i=0; i<els.length; i++) { el=els[i];
    if(el.getAttribute("posLabel")){
      var annot={start: el.getAttribute("start"), stop: el.getAttribute("stop"), label: {type: "posLabel", value: el.getAttribute("posLabel")}};
      json.annots.push(annot);
    }
    if(el.getAttribute("inflectLabel")){
      var annot={start: el.getAttribute("start"), stop: el.getAttribute("stop"), label: {type: "inflectLabel", value: el.getAttribute("inflectLabel")}};
      json.annots.push(annot);
    }
    if(el.getAttribute("langLabel")){
      var annot={start: el.getAttribute("start"), stop: el.getAttribute("stop"), label: {type: "langLabel", value: lang_id2abbr[el.getAttribute("langLabel")]}};
      json.annots.push(annot);
    }
    if(el.getAttribute("tm")=="1"){
      var annot={start: el.getAttribute("start"), stop: el.getAttribute("stop"), label: {type: "symbol", value: "tm"}};
      json.annots.push(annot);
    }
    if(el.getAttribute("regTM")=="1"){
      var annot={start: el.getAttribute("start"), stop: el.getAttribute("stop"), label: {type: "symbol", value: "regtm"}};
      json.annots.push(annot);
    }
    if(el.getAttribute("proper")=="1"){
      var annot={start: el.getAttribute("start"), stop: el.getAttribute("stop"), label: {type: "symbol", value: "proper"}};
      json.annots.push(annot);
    }
    if(el.getAttribute("italic")=="1"){
      var annot={start: el.getAttribute("start"), stop: el.getAttribute("stop"), label: {type: "formatting", value: "italic"}};
      json.annots.push(annot);
    }
  }
  //inflects:
  var els=doc.getElementsByTagName("inflect");
  for(var i=0; i<els.length; i++) { el=els[i];
    var inflect={label: el.getAttribute("label"), text: el.getAttribute("text")};
    json.inflects.push(inflect);
  }
  return json;
}
function getExample(exampleID){
  var dir="C:\\MBM\\Fiontar\\Export2Terminologue\\data-out\\focal.example\\";
  if(!fs.existsSync(dir+exampleID+".xml")) return null;
  var xml=fs.readFileSync(dir+exampleID+".xml", "utf8");
  var doc=domParser.parseFromString(xml, 'text/xml');
  var json={
    texts: {ga: [], en: []},
    sources: []
  };
  //English phrases:
  var els=doc.getElementsByTagName("phraseEN");
  for(var i=0; i<els.length; i++) { el=els[i];
    if(el.getAttribute("default")!="") json.texts.en.push(el.getAttribute("default"));
  }
  //Irish phrases:
  var els=doc.getElementsByTagName("phraseGA");
  for(var i=0; i<els.length; i++) { el=els[i];
    if(el.getAttribute("default")!="") json.texts.ga.push(el.getAttribute("default"));
  }
  return json;
}
