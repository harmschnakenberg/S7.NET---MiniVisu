//function startWorker() {        if (ty eof (Worker) !== "undefined")              if ( ypeof (w) == "undefined                    w = new Worker("demo_workers        //         
//        w.onmessag  = function            //            document.getElementById("result").innerHTML =        ata;            };        } else {
//        document.getElementById("result").innerHTML = "Sorry, your browser does not suppor    b Workers...";
//    }
//}


//Wiederhole Tag-Abfrage
setInterval(readTimer, 1000);

loadTagNames("SW");

function readTimer() {
    loadTagNames("IW");    
}

async function loadTagNames(className) {
    //ItemNames aus DOM lesen
    var x = document.getElementsByClassName(className);
    const data = [];

    for (let i = 0; i < x.length; i++) {
        data.push(x[i].id);
    }

    //Leseanfrage senden
    const response = await fetch('/api', {
        method: 'POST',
        body: JSON.stringify(data)
    });
    const responseText = await response.text()

    //Ergebnis verarbeiten  (Array von JS-Objekten)
    const tagArr = JSON.parse(responseText);

    for (let i = 0; i < tagArr.length; i++) {
        const tag = tagArr[i];
        var obj = document.getElementById(tag.Name);

        if (obj.tagName == "INPUT") {
            obj.value = tag.Value;
        }
        else {
            obj.innerHTML = tag.Value;
        }
    }

    //nur Debug
    document.getElementById("demo").innerHTML = responseText;
}