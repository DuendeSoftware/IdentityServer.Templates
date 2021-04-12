const todoUrl = "/todos";
const todos = document.getElementById("todos");

document.getElementById("createNewButton").addEventListener("click", createTodo);
document.getElementById("getUserData").addEventListener("click", getUserData);
document.getElementById("callRemoteApi").addEventListener("click", callRemoteApi);

const name = document.getElementById("name");
const date = document.getElementById("date");

async function getUserData() {
    var req = new Request("/bff/user", {
        headers: new Headers({
            'X-CSRF': '1'
        })
    })

    try {
        var resp = await fetch(req);
        if (resp.ok) {
            log("user logged in");

            let claims = await resp.json();
            showUser(claims);

            let logoutUrlClaim = claims.find(claim => claim.type === 'bff:logout');
            if (logoutUrlClaim) {
                logoutUrl = logoutUrlClaim.value;
            }
        } else if (resp.status === 401) {
            log("user not logged in");
        }
    }
    catch (e) {
        log("error checking user status");
    }
}

async function callRemoteApi() {
    var req = new Request("/remote", {
        headers: new Headers({
            'X-CSRF': '1'
        })
    })
    var resp = await fetch(req);

    log("API Result: " + resp.status);
    if (resp.ok) {
        log(await resp.json());
    }
}

async function createTodo() {
    let request = new Request(todoUrl, {
        method: "POST",
        headers: {
            "content-type": "application/json",
            'x-csrf': '1'
        },
        body: JSON.stringify({
            name: name.value,
            date: date.value,
        })
    });

    let result = await fetch(request);
    if (result.ok) {
        var item = await result.json();
        addRow(item);
    }
}

async function showTodos() {
    let result = await fetch(new Request(todoUrl, {
        headers: {
            'x-csrf': '1'
        },
    }));

    if (result.ok) {
        let data = await result.json();
        data.forEach(item => addRow(item));
    }
}

function addRow(item) {
    let row = document.createElement("tr");
    row.dataset.id = item.id;
    todos.appendChild(row);

    function addCell(row, text) {
        let cell = document.createElement("td");
        cell.innerText = text;
        row.appendChild(cell);
    }

    function addDeleteButton(row, id) {
        let cell = document.createElement("td");
        row.appendChild(cell);
        let btn = document.createElement("button");
        cell.appendChild(btn);
        btn.textContent = "delete";
        btn.addEventListener("click", async () => await deleteTodo(id));
    }

    addDeleteButton(row, item.id);
    addCell(row, item.id);
    addCell(row, item.date);
    addCell(row, item.name);
    addCell(row, item.user);
}


async function deleteRow(id) {
    let row = todos.querySelector(`tr[data-id='${id}']`);
    if (row) {
        todos.removeChild(row);
    }
}

async function deleteTodo(id) {
    let request = new Request(todoUrl + "/" + id, {
        headers: {
            'x-csrf': '1'
        },
        method: "DELETE"
    });

    let result = await fetch(request);
    if (result.ok) {
        deleteRow(id);
    }
}

function log() {
    document.getElementById('response').innerText = '';

    Array.prototype.forEach.call(arguments, function (msg) {
        if (msg instanceof Error) {
            msg = "Error: " + msg.message;
        } else if (typeof msg !== 'string') {
            msg = JSON.stringify(msg, null, 2);
        }
        document.getElementById('response').innerText += msg + '\r\n';
    });
}

function showUser() {
    document.getElementById('response').innerText = '';

    Array.prototype.forEach.call(arguments, function (msg) {
        if (msg instanceof Error) {
            msg = "Error: " + msg.message;
        } else if (typeof msg !== 'string') {
            msg = JSON.stringify(msg, null, 2);
        }
        document.getElementById('response').innerText += msg + '\r\n';
    });
}


showTodos();


