
// DAILY FOOD ISSUE MODULE
// ==========================================

let foodIssueDates = [],
    foodIssueRecord = null,
    foodIssueEditable = false,
    foodModuleItems = [];


async function loadFoodIssueModule() {
    try {
        foodModuleItems = (await foodApi('/api/animal/items'))
            .filter(x => x.isActive && x.itemType === 'FINISHED_FOOD');

        document.getElementById('foodIssueItemID').innerHTML =
            '<option value="">Select Finished Food</option>' +
            foodModuleItems.map(x =>
                `<option value="${x.itemID}">${escapeFoodHtml(x.itemName)}</option>`
            ).join('');

        foodIssueDates = await foodApi('/api/animal/food/issues/dates');

        if (foodIssueDates.length) {
            await loadFoodIssueByDate(foodIssueDates[foodIssueDates.length - 1]);
        } else {
            await newFoodIssue();
        }

    } catch (e) {
        showFoodMessage(
            'foodIssueMessage',
            'Unable to load food issues: ' + e.message,
            true
        );
    }
}


async function loadFoodIssueByDate(date) {
    if (!date) return;

    try {
        foodIssueRecord = await foodApi(
            '/api/animal/food/issues/current?date=' + encodeURIComponent(date)
        );

        renderFoodIssue();

    } catch (e) {
        showFoodMessage(
            'foodIssueMessage',
            'Unable to load selected date: ' + e.message,
            true
        );
    }
}


async function newFoodIssue() {
    const today = new Date().toISOString().slice(0, 10);

    document.getElementById('foodIssueDate').value = today;

    try {
        foodIssueRecord = await foodApi(
            '/api/animal/food/issues/current?date=' + today
        );

        if (foodIssueRecord.isSaved) {
            showFoodMessage(
                'foodIssueMessage',
                'A food issue already exists for today. Use Previous/Next to view it or select another date.',
                true
            );
            return;
        }

        foodIssueEditable = true;
        renderFoodIssue();

    } catch (e) {
        showFoodMessage('foodIssueMessage', e.message, true);
    }
}


// ==========================================
// UPDATE GRAND TOTALS
// ==========================================

function updateFoodIssueTotals() {

    const details = foodIssueRecord?.details || [];

    const totalWeight = details.reduce(
        (sum, d) => sum + Number(d.totalActiveWeight || 0),
        0
    );

    const totalCalculated = details.reduce(
        (sum, d) => sum + Number(d.calculatedFood || 0),
        0
    );

    // Calculate Food Issued directly from the current grid inputs.
    // This ensures that edited values are included immediately.

    const totalIssued = [...document.querySelectorAll(
        '#foodIssueRows .food-issued'
    )].reduce(
        (sum, input) => sum + (Number(input.value) || 0),
        0
    );

    const weightElement =
        document.getElementById('foodIssueGrandWeight');

    const calculatedElement =
        document.getElementById('foodIssueGrandCalculated');

    const issuedElement =
        document.getElementById('foodIssueGrandIssued');

    if (weightElement) {
        weightElement.textContent = totalWeight.toFixed(2) + ' kg';
    }

    if (calculatedElement) {
        calculatedElement.textContent = totalCalculated.toFixed(2) + ' kg';
    }

    if (issuedElement) {
        issuedElement.textContent = totalIssued.toFixed(2) + ' kg';
    }
}


// ==========================================
// RENDER FOOD ISSUE GRID
// ==========================================

function renderFoodIssue() {

    const tb = document.getElementById('foodIssueRows');

    tb.innerHTML = '';

    document.getElementById('foodIssueDate').value =
        foodIssueRecord.issueDate;

    document.getElementById('foodIssueStatus').textContent =
        foodIssueRecord.isSaved ? 'Saved' : 'New / Not Saved';

    document.getElementById('foodIssueItemID').value =
        foodIssueRecord.foodItemID || '';

    document.getElementById('foodIssueItemID').disabled =
        foodIssueRecord.isSaved;

    foodIssueEditable = !foodIssueRecord.isSaved;

    foodIssueRecord.details.forEach(d => {

        const tr = document.createElement('tr');

        tr.dataset.departmentId = d.departmentID;

        tr.innerHTML = `
            <td>${escapeFoodHtml(d.departmentName)}</td>
            <td>${d.goatCount}</td>
            <td>${Number(d.totalActiveWeight).toFixed(2)}</td>
            <td>${Number(d.calculatedFood).toFixed(2)}</td>
            <td>
                <input
                    class="food-issued"
                    type="number"
                    min="0"
                    step="0.01"
                    value="${Number(d.foodIssued).toFixed(2)}"
                    ${foodIssueEditable ? '' : 'disabled'}
                />
            </td>
        `;

        tb.appendChild(tr);

        // Recalculate grand total whenever Food Issued changes.

        const issuedInput = tr.querySelector('.food-issued');

        issuedInput.addEventListener('input', function () {
            updateFoodIssueTotals();
        });

        issuedInput.addEventListener('change', function () {
            updateFoodIssueTotals();
        });

    });

    // Calculate all three totals immediately after loading the grid.

    updateFoodIssueTotals();

    document.getElementById('saveFoodIssueButton').disabled =
        !foodIssueEditable;

    document.getElementById('editFoodIssueButton').disabled =
        !foodIssueRecord.isSaved;
}


// ==========================================
// ENABLE EDIT MODE
// ==========================================

function enableFoodIssueEdit() {

    if (!foodIssueRecord?.isSaved) return;

    foodIssueEditable = true;

    document.getElementById('foodIssueItemID').disabled = false;

    document.querySelectorAll('.food-issued').forEach(x => {
        x.disabled = false;
    });

    document.getElementById('saveFoodIssueButton').disabled = false;

    showFoodMessage(
        'foodIssueMessage',
        'Edit mode enabled. Change Food Issued and save.',
        false
    );
}


// ==========================================
// COLLECT FOOD ISSUE DATA
// ==========================================

function collectFoodIssue() {

    return [
        ...document.querySelectorAll('#foodIssueRows tr')
    ].map(tr => ({

        departmentID: Number(tr.dataset.departmentId),

        goatCount: Number(tr.children[1].textContent),

        totalActiveWeight: Number(tr.children[2].textContent),

        calculatedFood: Number(tr.children[3].textContent),

        foodIssued: Number(tr.querySelector('.food-issued').value)

    }));
}


// ==========================================
// SAVE FOOD ISSUE
// ==========================================

async function saveFoodIssue() {

    if (!foodIssueEditable) return;

    const payload = {

        issueDate: document.getElementById('foodIssueDate').value,

        foodItemID: Number(document.getElementById('foodIssueItemID').value),

        details: collectFoodIssue()

    };

    if (!payload.foodItemID) {

        showFoodMessage(
            'foodIssueMessage',
            'Please select a Finished Food item.',
            true
        );

        return;
    }

    try {

        const url = foodIssueRecord.isSaved
            ? '/api/animal/food/issues/update'
            : '/api/animal/food/issues/save';

        const r = await foodApi(url, {

            method: foodIssueRecord.isSaved ? 'PUT' : 'POST',

            body: JSON.stringify(payload)

        });

        showFoodMessage(
            'foodIssueMessage',
            r.message || 'Saved.',
            false
        );

        foodIssueDates = await foodApi('/api/animal/food/issues/dates');

        await loadFoodIssueByDate(payload.issueDate);

    } catch (e) {

        showFoodMessage(
            'foodIssueMessage',
            'Unable to save: ' + e.message,
            true
        );

    }
}


// ==========================================
// PREVIOUS / NEXT DATE
// ==========================================

async function moveFoodIssue(delta) {

    if (!foodIssueDates.length) return;

    const current = document.getElementById('foodIssueDate').value;

    let i = foodIssueDates.indexOf(current);

    if (i < 0) {
        i = foodIssueDates.length - 1;
    }

    i += delta;

    if (i < 0 || i >= foodIssueDates.length) return;

    await loadFoodIssueByDate(foodIssueDates[i]);
}


// ==========================================
// DELETE FOOD ISSUE
// ==========================================

async function deleteFoodIssue() {

    if (!foodIssueRecord?.isSaved) return;

    if (!confirm(
        'Delete the complete food issue for ' +
        foodIssueRecord.issueDate +
        '?'
    )) {
        return;
    }

    try {

        const r = await foodApi(
            '/api/animal/food/issues/delete?date=' + foodIssueRecord.issueDate,
            { method: 'DELETE' }
        );

        showFoodMessage(
            'foodIssueMessage',
            r.message || 'Deleted.',
            false
        );

        foodIssueDates = await foodApi('/api/animal/food/issues/dates');

        if (foodIssueDates.length) {

            await loadFoodIssueByDate(foodIssueDates[foodIssueDates.length - 1]);

        } else {

            await newFoodIssue();

        }

    } catch (e) {

        showFoodMessage(
            'foodIssueMessage',
            'Unable to delete: ' + e.message,
            true
        );

    }
}


// ==========================================
// PRINT FOOD ISSUE
// ==========================================

function printFoodIssue() {

    if (!foodIssueRecord) return;

    const rows = collectFoodIssue();

    const date = document.getElementById('foodIssueDate').value;

    const totalGoats = rows.reduce(
        (sum, x) => sum + x.goatCount,
        0
    );

    const totalWeight = rows.reduce(
        (sum, x) => sum + x.totalActiveWeight,
        0
    );

    const totalCalculated = rows.reduce(
        (sum, x) => sum + x.calculatedFood,
        0
    );

    const totalIssued = rows.reduce(
        (sum, x) => sum + x.foodIssued,
        0
    );

    const bodyRows = rows.map(x => {

        const name =
            foodIssueRecord.details.find(
                d => d.departmentID === x.departmentID
            )?.departmentName || '';

        return '<tr><td>' + date +
            '</td><td>' + escapeFoodHtml(name) +
            '</td><td>' + x.goatCount +
            '</td><td>' + x.totalActiveWeight.toFixed(2) +
            '</td><td>' + x.calculatedFood.toFixed(2) +
            '</td><td>' + x.foodIssued.toFixed(2) +
            '</td></tr>';

    }).join('');

    const html =
        '<!doctype html><html><head><title>Daily Food Issuance</title><style>' +
        'body{font-family:Arial;margin:25px}' +
        'h2{text-align:center}' +
        '.summary{display:flex;gap:30px;margin:18px 0;font-weight:bold;flex-wrap:wrap}' +
        'table{width:100%;border-collapse:collapse}' +
        'th,td{border:1px solid #555;padding:8px;text-align:left}' +
        '@media print{@page{size:landscape;margin:12mm}}' +
        '</style></head><body>' +
        '<h2>Farm Breeding Management</h2>' +
        '<h3>Daily Food Issuance - ' + date + '</h3>' +
        '<div class="summary">' +
        '<div>Grand Total Goat Weight: ' + totalWeight.toFixed(2) + ' kg</div>' +
        '<div>Grand Total Calculated Food: ' + totalCalculated.toFixed(2) + ' kg</div>' +
        '<div>Grand Total Food Issued: ' + totalIssued.toFixed(2) + ' kg</div>' +
        '</div>' +
        '<table><thead><tr>' +
        '<th>Date</th><th>Department</th><th>No. of Goats</th>' +
        '<th>Total Goat Weight (kg)</th><th>Calculated Food (kg)</th>' +
        '<th>Food Issued (kg)</th>' +
        '</tr></thead><tbody>' + bodyRows +
        '</tbody><tfoot><tr><th colspan="2">Grand Total</th>' +
        '<th>' + totalGoats + '</th>' +
        '<th>' + totalWeight.toFixed(2) + '</th>' +
        '<th>' + totalCalculated.toFixed(2) + '</th>' +
        '<th>' + totalIssued.toFixed(2) + '</th>' +
        '</tr></tfoot></table></body></html>';

    const w = window.open('', '_blank');

    if (!w) {

        alert('Please allow pop-ups to print.');

        return;

    }

    w.document.write(html);

    w.document.close();

    w.onload = () => w.print();

}
