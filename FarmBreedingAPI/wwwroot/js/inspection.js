// ==========================================
// GOAT INSPECTION - BULK ENTRY
// ==========================================

let inspectionRowCounter = 0;

async function loadInspectionAnimalNos() {
    const list = document.getElementById("inspectionAnimalList");
    const tbody = document.getElementById("inspectionRows");

    try {
        const response = await fetch("/api/animal/animalnos", {
            headers: { "app-version": "2" }
        });

        if (!response.ok) {
            throw new Error("Unable to load Goat Numbers. HTTP " + response.status);
        }

        const result = await response.json();
        const animalNos = Array.isArray(result) ? result : [];

        list.innerHTML = "";
        tbody.innerHTML = "";
        inspectionRowCounter = 0;

        animalNos.forEach(function (item) {
            // API currently returns integer AnimalNo values. Also
            // accept object-shaped values without breaking the list.
            const animalNo = (item && typeof item === "object")
                ? (item.animalNo ?? item.AnimalNo)
                : item;

            if (animalNo === null || animalNo === undefined || animalNo === "") return;

            const option = document.createElement("option");
            option.value = animalNo;
            list.appendChild(option);

            addInspectionRow(animalNo);
        });

        updateInspectionGrandTotal();

        if (animalNos.length === 0) {
            showInspectionMessage("No registered goats found in ArticleInfo01.", true);
        } else {
            document.getElementById("inspectionMessage").style.display = "none";
        }

    } catch (error) {
        console.error("Inspection Goat Number loading error:", error);
        showInspectionMessage(error.message, true);
    }
}


function addInspectionRow(animalNo = "") {
    const tbody = document.getElementById("inspectionRows");
    const rowId = ++inspectionRowCounter;

    const row = document.createElement("tr");
    row.dataset.rowId = rowId;

    row.innerHTML = `
        <td class="inspection-sr"></td>
        <td><input class="inspection-goat-no" type="number" min="1" step="1" list="inspectionAnimalList" placeholder="Goat No." value="${animalNo}" /></td>
        <td><input class="inspection-weight" type="number" min="0" step="0.01" placeholder="Weight" /></td>
        <td>
            <select class="inspection-teeth">
                <option value="">-</option>
                <option value="0">0</option>
                <option value="2">2</option>
                <option value="4">4</option>
                <option value="6">6</option>
                <option value="8">8</option>
            </select>
        </td>
        <td>
            <select class="inspection-nails">
                <option value="">-</option>
                <option value="true">Yes</option>
            </select>
        </td>
        <td>
            <select class="inspection-hair">
                <option value="">-</option>
                <option value="true">Yes</option>
            </select>
        </td>
        <td>
            <button type="button" class="remove-row" onclick="removeInspectionRow(this)">Remove</button>
        </td>
    `;

    tbody.appendChild(row);

    updateInspectionRowNumbers();

    return row;
}


// Add rows in batches (20 per click).
function addInspectionRows(count) {
    for (let i = 0; i < count; i++) {
        addInspectionRow();
    }
}


function removeInspectionRow(button) {
    const tbody = document.getElementById("inspectionRows");

    if (tbody.rows.length <= 1) {
        showInspectionMessage("At least one row must remain.", true);
        return;
    }

    button.closest("tr").remove();

    updateInspectionRowNumbers();
    updateInspectionGrandTotal();
}


function updateInspectionRowNumbers() {
    document.querySelectorAll("#inspectionRows tr").forEach(function (row, index) {
        row.querySelector(".inspection-sr").textContent = index + 1;
    });
}


// Excel-style cell navigation: arrows move one cell; Enter moves down.
// The grid wraps at row edges. At the bottom, 20 more rows are added.

document.getElementById("inspectionRows").addEventListener("keydown", function (event) {

    const selectors = [
        ".inspection-goat-no",
        ".inspection-weight",
        ".inspection-teeth",
        ".inspection-nails",
        ".inspection-hair"
    ];

    const current = event.target;

    if (!current.matches(selectors.join(","))) return;

    let rowIndex = Array.from(this.rows).indexOf(current.closest("tr"));
    let colIndex = selectors.findIndex(selector => current.matches(selector));

    let nextRow = rowIndex;
    let nextCol = colIndex;

    if (event.key === "ArrowUp") nextRow--;
    else if (event.key === "ArrowDown" || event.key === "Enter") nextRow++;
    else if (event.key === "ArrowLeft") nextCol--;
    else if (event.key === "ArrowRight") nextCol++;
    else return;

    event.preventDefault();

    if (nextCol < 0) {
        nextCol = selectors.length - 1;
        nextRow--;
    }

    if (nextCol >= selectors.length) {
        nextCol = 0;
        nextRow++;
    }

    if (nextRow < 0) nextRow = 0;

    if (nextRow >= this.rows.length) {
        addInspectionRows(20);
    }

    const targetRow = this.rows[nextRow];
    const target = targetRow.querySelector(selectors[nextCol]);

    target.focus();

    if (target.tagName === "INPUT" && target.type !== "number") {
        target.select();
    }
});


document.getElementById("inspectionRows").addEventListener("input", function (event) {
    if (event.target.classList.contains("inspection-weight")) {
        updateInspectionGrandTotal();
    }
});


function updateInspectionGrandTotal() {
    const total = Array.from(
        document.querySelectorAll("#inspectionRows .inspection-weight")
    ).reduce(function (sum, input) {

        const value = input.value.trim() === "" ? 0 : Number(input.value);

        return sum + (Number.isFinite(value) ? value : 0);

    }, 0);

    document.getElementById("inspectionGrandTotal").textContent =
        total.toFixed(2);
}


function showInspectionMessage(text, isError) {
    const message = document.getElementById("inspectionMessage");

    message.textContent = text;
    message.className = isError ? "message error" : "message success";
    message.style.display = "block";
}


async function saveInspection() {
    const date = document.getElementById("inspectionDate").value;

    const rows = Array.from(
        document.querySelectorAll("#inspectionRows tr")
    );

    const records = [];

    if (!date) {
        showInspectionMessage(
            "Please select the Recording Date. It will apply to all goats.",
            true
        );
        return;
    }

    // Validate every populated row before sending anything.

    for (let i = 0; i < rows.length; i++) {

        const row = rows[i];

        const animalNoValue =
            row.querySelector(".inspection-goat-no").value.trim();

        const weightValue =
            row.querySelector(".inspection-weight").value.trim();

        const teethValue =
            row.querySelector(".inspection-teeth").value;

        const nailsValue =
            row.querySelector(".inspection-nails").value;

        const hairValue =
            row.querySelector(".inspection-hair").value;


        // Blank Goat Number means the row is unused, so skip it.

        if (!animalNoValue) continue;


        if (
            !Number.isInteger(Number(animalNoValue)) ||
            Number(animalNoValue) <= 0
        ) {
            showInspectionMessage(
                "Invalid Goat Number in row " + (i + 1) + ".",
                true
            );

            row.querySelector(".inspection-goat-no").focus();
            return;
        }


        if (
            weightValue !== "" &&
            (!Number.isFinite(Number(weightValue)) || Number(weightValue) < 0)
        ) {
            showInspectionMessage(
                "Invalid Weight in row " + (i + 1) + ".",
                true
            );

            row.querySelector(".inspection-weight").focus();
            return;
        }


        if (
            teethValue !== "" &&
            !["0", "2", "4", "6", "8"].includes(teethValue)
        ) {
            showInspectionMessage(
                "Teeth must be 0, 2, 4, 6 or 8 in row " + (i + 1) + ".",
                true
            );

            row.querySelector(".inspection-teeth").focus();
            return;
        }


        records.push({
            row: row,
            rowNumber: i + 1,
            animalNo: Number(animalNoValue),

            model: {
                AnimalNo: Number(animalNoValue),
                RecordDate: date,
                Weight: weightValue === "" ? null : Number(weightValue),
                Teeth: teethValue === "" ? null : Number(teethValue),
                Nails: nailsValue === "" ? null : true,
                Hair: hairValue === "" ? null : true
            }
        });
    }


    if (records.length === 0) {
        showInspectionMessage(
            "Please enter at least one Goat Number before saving.",
            true
        );
        return;
    }


    // Avoid accidentally submitting the same Goat Number twice in one session.

    const seen = new Set();

    for (const item of records) {

        if (seen.has(item.animalNo)) {

            showInspectionMessage(
                "Goat Number " + item.animalNo +
                " is entered more than once. Please check row " +
                item.rowNumber + ".",
                true
            );

            item.row.querySelector(".inspection-goat-no").focus();
            return;
        }

        seen.add(item.animalNo);
    }


    const saveButton =
        document.getElementById("saveInspectionButton");

    saveButton.disabled = true;

    let savedRows = [];
    const failures = [];


    try {

        // Keep sending after an individual row fails, so one bad row
        // cannot prevent later goats from being saved.

        for (const item of records) {

            try {

                const response = await fetch("/api/animal/inspection/save", {
                    method: "POST",

                    headers: {
                        "Content-Type": "application/json",
                        "app-version": "2"
                    },

                    body: JSON.stringify(item.model)
                });


                const responseText = await response.text();

                if (!response.ok) {
                    throw new Error(
                        "HTTP " + response.status + ": " +
                        (responseText || response.statusText)
                    );
                }

                savedRows.push(item.row);

            } catch (error) {

                failures.push(
                    "Row " + item.rowNumber +
                    " (Goat " + item.animalNo + "): " +
                    error.message
                );
            }
        }


        // Remove only rows confirmed saved. Failed rows remain for correction/retry.

        savedRows.forEach(row => row.remove());

        updateInspectionRowNumbers();
        updateInspectionGrandTotal();


        if (failures.length === 0) {

            showInspectionMessage(
                records.length +
                " goat inspection record(s) saved successfully for " +
                date + ".",
                false
            );

            if (
                document.querySelectorAll("#inspectionRows tr").length < 25
            ) {
                addInspectionRows(
                    25 - document.querySelectorAll("#inspectionRows tr").length
                );
            }

        } else {

            showInspectionMessage(
                savedRows.length + " of " + records.length +
                " saved. Failed rows remain in the sheet:\n" +
                failures.join("\n"),
                true
            );
        }

    } finally {

        saveButton.disabled = false;
    }
}


// Goat rows are populated from ArticleInfo01 after successful login.
// Add 20 Rows remains available for additional blank rows.
// ==========================================
