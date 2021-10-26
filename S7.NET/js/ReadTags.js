let TagsIW = [];
let TagsSW = [];
readIW();
readSW();

//Wiederhole Tag-Abfrage
setInterval(readIW, 1117);
setInterval(readSW, 3331);

function readIW() {
    if (TagsIW.length == 0)
        TagsIW = parseTagNames("IW");

    readTagValues(TagsIW);
}


function readSW() {
    if (TagsSW.length == 0)
        TagsSW = parseTagNames("SW");

    readTagValues(TagsSW);
}


function parseTagNames(className) { //ItemNames aus DOM lesen
    var x = document.getElementsByClassName(className);
    const dataArr = [];

    for (let i = 0; i < x.length; i++) {
        if (dataArr.indexOf(x[i].id) == -1) //nur einmal
            dataArr.push(x[i].id);
    }

   // alert(className + ": " + dataArr.length + "/" + x.length)
    return dataArr;
}


async function readTagValues(TagNameArr) {
    //Leseanfrage senden
    const response = await fetch('/api', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json;charset=utf-8'
        },
        body: JSON.stringify(TagNameArr)
    });

    if (!response.ok) return;
    let tagArr = await response.json();
  
    //verarbeite Array von JS-Objekten
    for (let i = 0; i < tagArr.length; i++) {
        const tag = tagArr[i];
        var obj = document.getElementById(tag.Name);

        if (obj.tagName == 'INPUT') {
            obj.value = tag.Value;
        }
        else {
            obj.innerHTML = tag.Value;
        }
    }

    //nur Debug
   // document.getElementById("demo").innerHTML = tagArr[0].Value;
}

async function getRandOben(pageTitle) {
    fetch('/status/'+ pageTitle)
        .then(x => x.text())
        .then(y => document.getElementById('RandOben').innerHTML = y);
}