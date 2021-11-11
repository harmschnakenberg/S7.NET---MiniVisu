///*** SEITE LADEN ***///

function parseTagNames(className) { //ItemNames aus DOM lesen
    var x = document.getElementsByClassName(className);
    const dataArr = [];

    for (let i = 0; i < x.length; i++) {
        if (dataArr.indexOf(x[i].id) == -1) //nur einmal
            dataArr.push(x[i].id);
    }

    return dataArr;
}

async function getRandOben(pageTitle) {
    fetch('/statusbar/' + pageTitle)
        .then(x => x.text())
        .then(y => document.getElementById('RandOben').innerHTML = y);

    readIW();
    readSW();
    AddEventWriteVal("SW");
}

///*** ENDE SEITE LADEN ***///

///*** STYLING ***///

function styleIW(className) {
    var x = document.getElementsByClassName(className);

    for (let i = 0; i < x.length; i++) {
        var y = x[i];

        const input = document.createElement("input");
        input.id = y.getAttribute("data-tag");

        input.classList.add('IW');
        input.classList.add('w3-input');
        input.classList.add('w3-black');
        input.classList.add('w3-right-align');
        input.classList.add('w3-border-0');
        input.classList.add('w3-threequarter');
        input.disabled = "true";

        const unit = document.createTextNode(y.getAttribute("data-unit"))

        y.classList.add('center');
        y.classList.add('w3-black');
        y.appendChild(input);
        y.appendChild(unit);
    }

}


function styleSW(className) {
    var x = document.getElementsByClassName(className);

    for (let i = 0; i < x.length; i++) {
        var y = x[i];

        const input = document.createElement("input");
        input.id = y.getAttribute("data-tag");

        input.classList.add('SW');
        input.classList.add('w3-input');
        input.classList.add('w3-white');
        input.classList.add('w3-right-align');
        input.classList.add('w3-border-0');
        input.classList.add('w3-threequarter');
        input.type = "number";

        const unit = document.createTextNode(y.getAttribute("data-unit"))

        y.classList.add('center');
        y.classList.add('w3-white');
        y.appendChild(input);
        y.appendChild(unit);
    }

}


function w3_toggle(id) {
    var x = document.getElementById(id).style.display;

    if (x == "block")
        document.getElementById(id).style.display = "none";
    else
        document.getElementById(id).style.display = "block";
}


///*** ENDE STYLING ***///

///*** WERTE LESEN ***///

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

///*** ENDE WERTE LESEN ***///

///*** WERTE SCHREIBEN ***///

function AddEventWriteVal(className) { //funktioniert!
    var x = document.getElementsByClassName(className);
    //alert(x.length + ' ' + className + '-Elemente');
    for (let i = 0; i < x.length; i++) {
        var y = x[i];
        y.addEventListener("change", function () { writeSW(this) });
    }
}


async function writeSW(obj) { //funktioniert!
    const writeTag = { Name: obj.id, Value: obj.value };

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

///*** ENDE WERTE SCHREIBEN ***///

//MAGAZIN


//function styleIW(className) {
//    var x = document.getElementsByClassName(className);

//    for (let i = 0; i < x.length; i++) {
//        x[i].classList.add('w3-input');
//        x[i].classList.add('w3-black');
//        x[i].classList.add('w3-right-align');
//        x[i].classList.add('w3-border-0');
//        x[i].classList.add('w3-threequarter');

//        //w3-black w3-right-align w3-border-0 w3-threequarter
//    }
//}

//function styleSW(className) {
//    var x = document.getElementsByClassName(className);

//    for (let i = 0; i < x.length; i++) {
//        x[i].classList.add('w3-input');
//        x[i].classList.add('w3-white');
//        x[i].classList.add('w3-right-align');
//        x[i].classList.add('w3-border-0');
//        x[i].classList.add('w3-threequarter');

//        //w3-white w3-right-align w3-border-0 w3-twothird
//    }
//}


