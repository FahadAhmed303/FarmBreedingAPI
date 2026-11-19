// ITEM MASTER
// ==========================================

let itemMasterData = [];
let editingItemID = null;

async function loadItems() {
    const tbody = document.getElementById("itemRows");
    tbody.innerHTML = '<tr><td colspan="6">Loading items...</td></tr>';

    try {
        const response = await fetch("/api/animal/items", {
            headers: { "app-version": "2" }
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || ("HTTP " + response.status));
        }

        itemMasterData = await response.json();
        renderItems();

    } catch (error) {
        tbody.innerHTML = "";
        showItemMessage("Unable to load items: " + error.message, true);
    }
}


function renderItems() {
    const tbody = document.getElementById("itemRows");
    tbody.innerHTML = "";

    if (!Array.isArray(itemMasterData) || itemMasterData.length === 0) {
        const tr = document.createElement("tr");
        const td = document.createElement("td");

        td.colSpan = 6;
        td.textContent = "No items registered.";

        tr.appendChild(td);
        tbody.appendChild(tr);
        return;
    }

    itemMasterData.forEach(function (item) {
        const tr = document.createElement("tr");

        [
            item.itemName,
            item.itemType === "FINISHED_FOOD" ? "Finished Food" : "Raw Material",
            item.baseUnit,
            item.description || "-",
            item.isActive ? "Active" : "Inactive"
        ].forEach(function (value) {
            const td = document.createElement("td");
            td.textContent = value == null ? "" : value;
            tr.appendChild(td);
        });

        const actionCell = document.createElement("td");
        const editButton = document.createElement("button");

        editButton.type = "button";
        editButton.textContent = "Edit";
        editButton.style.padding = "7px 12px";
        editButton.style.fontSize = "14px";

        editButton.addEventListener("click", function () {
            editItem(item.itemID);
        });

        actionCell.appendChild(editButton);
        tr.appendChild(actionCell);
        tbody.appendChild(tr);
    });
}


function editItem(itemID) {
    const item = itemMasterData.find(function (x) {
        return x.itemID === itemID;
    });

    if (!item) return;

    editingItemID = item.itemID;

    document.getElementById("itemName").value = item.itemName || "";
    document.getElementById("itemType").value = item.itemType || "FINISHED_FOOD";
    document.getElementById("itemBaseUnit").value = item.baseUnit || "kg";
    document.getElementById("itemDescription").value = item.description || "";
    document.getElementById("itemIsActive").checked = !!item.isActive;

    document.getElementById("saveItemButton").textContent = "Update Item";

    showItemMessage("Editing: " + item.itemName, false);

    document.getElementById("itemName").focus();
}


function resetItemForm() {
    editingItemID = null;

    document.getElementById("itemName").value = "";
    document.getElementById("itemType").value = "FINISHED_FOOD";
    document.getElementById("itemBaseUnit").value = "kg";
    document.getElementById("itemDescription").value = "";
    document.getElementById("itemIsActive").checked = true;

    document.getElementById("saveItemButton").textContent = "Save Item";
    document.getElementById("itemMessage").style.display = "none";
}


async function saveItem() {
    const saveButton = document.getElementById("saveItemButton");

    const payload = {
        itemID: editingItemID,
        itemName: document.getElementById("itemName").value.trim(),
        itemType: document.getElementById("itemType").value,
        baseUnit: document.getElementById("itemBaseUnit").value,
        description: document.getElementById("itemDescription").value.trim(),
        isActive: document.getElementById("itemIsActive").checked
    };

    if (!payload.itemName) {
        showItemMessage("Please enter Item Name.", true);
        return;
    }

    saveButton.disabled = true;

    try {
        const response = await fetch("/api/animal/items/save", {
            method: "POST",

            headers: {
                "Content-Type": "application/json",
                "app-version": "2"
            },

            body: JSON.stringify(payload)
        });

        const resultText = await response.text();

        if (!response.ok) {
            throw new Error(resultText || ("HTTP " + response.status));
        }

        let result = {};

        try {
            result = JSON.parse(resultText);
        } catch (_) { }

        showItemMessage(result.message || "Item saved successfully.", false);

        resetItemForm();
        await loadItems();

        showItemMessage(result.message || "Item saved successfully.", false);

    } catch (error) {
        showItemMessage("Unable to save item: " + error.message, true);

    } finally {
        saveButton.disabled = false;
    }
}


function showItemMessage(message, isError) {
    const element = document.getElementById("itemMessage");

    element.textContent = message;
    element.className = isError ? "message error" : "message success";
    element.style.display = "block";
}
