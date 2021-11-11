let TagsIW = [];
let TagsSW = [];

//Wiederhole Tag-Abfrage
var intervalIW = setInterval(readIW, 1117);
var intervalSW = setInterval(readSW, 5331);

function readIW() {
    if (TagsIW.length == 0)
        TagsIW = parseTagNames("IW");

    readTagValues(TagsIW);
}


function readSW() {
    if (TagsSW.length == 0)
        TagsSW = parseTagNames("SW");

    readTagValues(TagsSW);
    //document.getElementById("message").innerHTML = new Date().toISOString();
}


function parseTagNames(className) { //ItemNames aus DOM lesen
    var x = document.getElementsByClassName(className);
    const dataArr = [];

    for (let i = 0; i < x.length; i++) {
        if (dataArr.indexOf(x[i].id) == -1) //nur einmal
            dataArr.push(x[i].id);
    }

    return dataArr;
}


async function readTagValues(TagNameArr) {
    try {
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
            else if (tag.Value === true || tag.Value === false) {
                if (tag.Value == true) {
                    obj.classList.add('true');
                    obj.classList.remove('false');
                } else {
                    obj.classList.add('false');
                    obj.classList.remove('true');
                }
            }
            else if (tag.Value != null) {
                obj.innerHTML = tag.Value;
            }
        }
    }
    catch (err) {
        clearInterval(intervalIW);
        clearInterval(intervalSW);
        document.getElementById("message").innerHTML = 'Fehler beim Lesen von SPS: ' + err.message;
    }
}


async function getRandOben(pageTitle) {
    fetch('/statusbar/'+ pageTitle)
        .then(x => x.text())
        .then(y => document.getElementById('RandOben').innerHTML = y);

    readIW();
    readSW();
    AddEventWriteVal("SW");
}


function w3_toggle(id) {
    var x = document.getElementById(id).style.display;

    if (x == "block")
        document.getElementById(id).style.display = "none";
    else
        document.getElementById(id).style.display = "block";
}


function AddEventWriteVal(className) { //funktioniert!
    var x = document.getElementsByClassName(className);
    //alert(x.length + ' ' + className + '-Elemente');
    for (let i = 0; i < x.length; i++) {
        var y = x[i];
        y.addEventListener("change", function () { writeSW(this) });
    }
}


async function writeSW(obj) { //funktioniert!
    const writeTag = { Name:obj.id, Value:obj.value };

    const response = await fetch('/api/write', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json;charset=utf-8'
        },
        body: JSON.stringify(writeTag)
    });

    if (!response.ok)
        alert('Fehler beim Schreiben von ' + obj.id);
}


