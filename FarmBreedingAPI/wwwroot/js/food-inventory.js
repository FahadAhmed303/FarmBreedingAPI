// ==========================================
// FOOD INVENTORY TRANSACTION MODULES
// ==========================================

let foodInventoryItems = [];


async function loadInventoryItems() {
    foodInventoryItems = (await foodApi('/api/animal/items'))
        .filter(x => x.isActive);

    return foodInventoryItems;
}


function inventoryOptions(typeFilter = 'ALL') {
    return foodInventoryItems
        .filter(x => typeFilter === 'ALL' || x.itemType === typeFilter)
        .map(x =>
            `<option value="${x.itemID}">${escapeFoodHtml(x.itemName)} (${x.baseUnit})</option>`
        )
        .join('');
}


// ==========================================
// FOOD PURCHASE
// ==========================================

async function loadPurchaseModule() {
    try {
        await loadInventoryItems();

        if (!document.getElementById('purchaseDate').value) {
            document.getElementById('purchaseDate').value =
                new Date().toISOString().slice(0, 10);
        }

        if (!document.querySelector('#purchaseLines tr')) {
            addPurchaseLine();
        }

    } catch (e) {
        showFoodMessage('purchaseMessage', e.message, true);
    }
}


function addPurchaseLine() {
    const tr = document.createElement('tr');

    tr.innerHTML = `
        <td>
            <select class="p-item">
                <option value="">Select item</option>
                ${inventoryOptions()}
            </select>
        </td>
        <td>
            <input class="p-qty" type="number" min="0.001" step="0.001" value="1">
        </td>
        <td>
            <input class="p-rate" type="number" min="0" step="0.01" value="0">
        </td>
        <td>
            <button type="button" onclick="this.closest('tr').remove()">Remove</button>
        </td>
    `;

    document.getElementById('purchaseLines').appendChild(tr);
}


async function savePurchase() {
    const details = [
        ...document.querySelectorAll('#purchaseLines tr')
    ].map(tr => ({
        itemID: Number(tr.querySelector('.p-item').value),
        quantity: Number(tr.querySelector('.p-qty').value),
        unitRate: Number(tr.querySelector('.p-rate').value)
    }));

    try {
        const r = await foodApi('/api/animal/food/purchases/save', {
            method: 'POST',
            body: JSON.stringify({
                purchaseDate: document.getElementById('purchaseDate').value,
                supplierName: document.getElementById('purchaseSupplier').value,
                invoiceNo: document.getElementById('purchaseInvoice').value,
                details
            })
        });

        showFoodMessage(
            'purchaseMessage',
            r.message || 'Purchase saved.',
            false
        );

        document.getElementById('purchaseLines').innerHTML = '';
        addPurchaseLine();

    } catch (e) {
        showFoodMessage('purchaseMessage', e.message, true);
    }
}


// ==========================================
// FOOD PRODUCTION
// ==========================================

async function loadProductionModule() {
    try {
        await loadInventoryItems();

        const recipes = await foodApi('/api/animal/food/recipes');

        document.getElementById('productionRecipe').innerHTML =
            recipes
                .filter(x => x.isActive)
                .map(x =>
                    `<option value="${x.recipeID}">${escapeFoodHtml(x.finishedFoodName)} - ${escapeFoodHtml(x.recipeName || 'Recipe')} (batch ${x.batchOutputQty})</option>`
                )
                .join('');

        if (!document.getElementById('productionDate').value) {
            document.getElementById('productionDate').value =
                new Date().toISOString().slice(0, 10);
        }

    } catch (e) {
        showFoodMessage('productionMessage', e.message, true);
    }
}


async function saveProduction() {
    try {
        const r = await foodApi('/api/animal/food/production/save', {
            method: 'POST',
            body: JSON.stringify({
                productionDate: document.getElementById('productionDate').value,
                recipeID: Number(document.getElementById('productionRecipe').value),
                producedQty: Number(document.getElementById('productionQty').value),
                remarks: document.getElementById('productionRemarks').value
            })
        });

        showFoodMessage(
            'productionMessage',
            r.message || 'Production saved.',
            false
        );

    } catch (e) {
        showFoodMessage('productionMessage', e.message, true);
    }
}


// ==========================================
// FOOD WASTE
// ==========================================

async function loadWasteModule() {
    try {
        await loadInventoryItems();

        if (!document.getElementById('wasteDate').value) {
            document.getElementById('wasteDate').value =
                new Date().toISOString().slice(0, 10);
        }

        if (!document.querySelector('#wasteLines tr')) {
            addWasteLine();
        }

    } catch (e) {
        showFoodMessage('wasteMessage', e.message, true);
    }
}


function addWasteLine() {
    const tr = document.createElement('tr');

    tr.innerHTML = `
        <td>
            <select class="w-item">
                <option value="">Select item</option>
                ${inventoryOptions()}
            </select>
        </td>
        <td>
            <input class="w-qty" type="number" min="0.001" step="0.001" value="1">
        </td>
        <td>
            <button type="button" onclick="this.closest('tr').remove()">Remove</button>
        </td>
    `;

    document.getElementById('wasteLines').appendChild(tr);
}


async function saveWaste() {
    const details = [
        ...document.querySelectorAll('#wasteLines tr')
    ].map(tr => ({
        itemID: Number(tr.querySelector('.w-item').value),
        quantity: Number(tr.querySelector('.w-qty').value)
    }));

    try {
        const r = await foodApi('/api/animal/food/waste/save', {
            method: 'POST',
            body: JSON.stringify({
                wasteDate: document.getElementById('wasteDate').value,
                reason: document.getElementById('wasteReason').value,
                remarks: document.getElementById('wasteRemarks').value,
                details
            })
        });

        showFoodMessage(
            'wasteMessage',
            r.message || 'Waste saved.',
            false
        );

        document.getElementById('wasteLines').innerHTML = '';
        addWasteLine();

    } catch (e) {
        showFoodMessage('wasteMessage', e.message, true);
    }
}


// ==========================================
// STOCK REPORT
// ==========================================

function initStockReport() {
    const today = new Date().toISOString().slice(0, 10);

    if (!document.getElementById('stockFrom').value) {
        document.getElementById('stockFrom').value = today;
    }

    if (!document.getElementById('stockTo').value) {
        document.getElementById('stockTo').value = today;
    }
}


async function loadStockReport() {
    try {
        const from = document.getElementById('stockFrom').value;
        const to = document.getElementById('stockTo').value;

        const r = await foodApi(
            `/api/animal/food/stock/report?fromDate=${encodeURIComponent(from)}&toDate=${encodeURIComponent(to)}`
        );

        const tb = document.getElementById('stockRows');
        tb.innerHTML = '';

        (r.items || []).forEach(x => {
            const tr = document.createElement('tr');

            tr.innerHTML = `
                <td>${escapeFoodHtml(x.itemName)}</td>
                <td>${escapeFoodHtml(x.itemType)}</td>
                <td>${escapeFoodHtml(x.baseUnit)}</td>
                <td>${Number(x.opening).toFixed(3)}</td>
                <td>${Number(x.purchased).toFixed(3)}</td>
                <td>${Number(x.produced).toFixed(3)}</td>
                <td>${Number(x.consumed).toFixed(3)}</td>
                <td>${Number(x.issued).toFixed(3)}</td>
                <td>${Number(x.waste).toFixed(3)}</td>
                <td><b>${Number(x.closing).toFixed(3)}</b></td>
            `;

            tb.appendChild(tr);
        });

        if (!r.items?.length) {
            tb.innerHTML = '<tr><td colspan="10">No items found.</td></tr>';
        }

        showFoodMessage(
            'stockMessage',
            `Report generated for ${from} to ${to}.`,
            false
        );

    } catch (e) {
        showFoodMessage('stockMessage', e.message, true);
    }
}


// ==========================================