async function loadGrowthRecords() {
    const animalNo = document.getElementById("growthAnimalNo").value.trim();
    const message = document.getElementById("growthMessage");
    const tbody = document.getElementById("growthRows");

    message.style.display = "none";
    tbody.innerHTML = "";

    if (!animalNo) {
        showGrowthMessage("Please enter Animal No.", true);
        return;
    }

    try {
        const animalResponse = await fetch(
            `/api/animal/byanimalno/${encodeURIComponent(animalNo)}`,
            { headers: { "app-version": "2" } }
        );

        if (!animalResponse.ok) {
            showGrowthMessage(animalResponse.status === 404
                ? "Animal not found."
                : "Unable to find goat. HTTP " + animalResponse.status, true);
            return;
        }

        const animal = await animalResponse.json();
        if (!animal || !animal.atCode) {
            showGrowthMessage("Animal not found.", true);
            return;
        }

        const response = await fetch(
            `/api/growthrecord/${encodeURIComponent(animal.atCode)}`,
            { headers: { "app-version": "2" } }
        );

        if (!response.ok) {
            showGrowthMessage("Unable to load growth records. HTTP " + response.status, true);
            return;
        }

        const records = await response.json();
        if (!Array.isArray(records) || records.length === 0) {
            showGrowthMessage("No growth records found for Goat No. " + animalNo + ".", false);
            return;
        }

        records.forEach(function (record) {
            const tr = document.createElement("tr");
            const values = [
                formatDate(record.recordDate),
                record.weight ?? "-",
                record.height ?? "-",
                record.width ?? "-"
            ];
            values.forEach(function (value) {
                const td = document.createElement("td");
                td.textContent = value;
                tr.appendChild(td);
            });
            tbody.appendChild(tr);
        });
        showGrowthMessage("Growth records for Goat No. " + animalNo + " (" + records.length + " record(s)).", false);
    } catch (error) {
        showGrowthMessage("Error loading growth records: " + error.message, true);
    }
}

function showGrowthMessage(text, isError) {
    const message = document.getElementById("growthMessage");
    message.textContent = text;
    message.className = isError ? "message error" : "message success";
    message.style.display = "block";
}
