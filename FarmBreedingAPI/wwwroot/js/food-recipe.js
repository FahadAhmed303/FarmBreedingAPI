// ==========================================
// FOOD RECIPE MODULE
// ==========================================

let recipeItems = [], recipeData = [], editingRecipeID = null;
async function loadRecipeModule() {
    try {
        recipeItems = await foodApi('/api/animal/items');

        recipeItems = recipeItems.filter(x => x.isActive);

        const ff = recipeItems.filter(x => x.itemType === 'FINISHED_FOOD');

        document.getElementById('recipeFinishedFood').innerHTML =
            ff.map(x =>
                `<option value="${x.itemID}">${escapeFoodHtml(x.itemName)}</option>`
            ).join('');

        if (!document.getElementById('recipeDate').value) {
            document.getElementById('recipeDate').value =
                new Date().toISOString().slice(0, 10);
        }

        await loadRecipes();

        if (!document.querySelector('#recipeIngredientRows tr')) {
            addRecipeIngredient();
        }

    } catch (e) {
        showFoodMessage(
            'recipeMessage',
            'Unable to load recipe items: ' + e.message,
            true
        );
    }
}
function addRecipeIngredient(itemID = '', qty = '') {
    const tr = document.createElement('tr');

    const options = recipeItems.map(x =>
        `<option value="${x.itemID}" ${String(x.itemID) === String(itemID) ? 'selected' : ''}>${escapeFoodHtml(x.itemName)}</option>`
    ).join('');

    tr.innerHTML = `
        <td>
            <select class="recipe-item" onchange="updateRecipeUnit(this)">
                <option value="">Select item</option>
                ${options}
            </select>
        </td>
        <td class="recipe-type">-</td>
        <td>
            <input class="recipe-qty" type="number" min="0.001" step="0.001" value="${qty}" oninput="updateRecipeTotals()" />
        </td>
        <td class="recipe-unit">-</td>
        <td>
            <button type="button" onclick="this.closest('tr').remove();updateRecipeTotals()">Remove</button>
        </td>
    `;

    document.getElementById('recipeIngredientRows').appendChild(tr); tr.querySelector('.recipe-qty').addEventListener('input', updateRecipeTotals); updateRecipeTotals();

    if (itemID) {
        updateRecipeUnit(tr.querySelector('.recipe-item'));
    }
}


function updateRecipeUnit(sel) {
    const x = recipeItems.find(i => String(i.itemID) === sel.value);
    const tr = sel.closest('tr');

    tr.querySelector('.recipe-type').textContent =
        x ? (x.itemType === 'FINISHED_FOOD' ? 'Finished Food' : 'Raw Material') : '-';

    tr.querySelector('.recipe-unit').textContent =
        x?.baseUnit || '-';
}


async function loadRecipes() {
    recipeData = await foodApi('/api/animal/food/recipes');

    const tb = document.getElementById('recipeRows');
    tb.innerHTML = '';

    recipeData.forEach(r => {
        const tr = document.createElement('tr');

        const ing = (r.details || []).map(d =>
            `${escapeFoodHtml(d.itemName)}: ${d.quantity} ${escapeFoodHtml(d.baseUnit)}`
        ).join('<br>');

        tr.innerHTML = `
            <td>${escapeFoodHtml(r.finishedFoodName)}</td>
            <td>${escapeFoodHtml(r.recipeName || '')}</td>
            <td>${r.versionNo}</td>
            <td>${r.batchOutputQty}</td>
            <td>${r.effectiveDate}</td>
            <td>${ing}</td>
            <td><button type="button">Edit</button></td>
        `;

        tr.querySelector('button').onclick = () => editFoodRecipe(r.recipeID);

        tb.appendChild(tr);
    });

    if (!recipeData.length) {
        tb.innerHTML = '<tr><td colspan="7">No recipes saved.</td></tr>';
    }
}


function editFoodRecipe(id) {
    const r = recipeData.find(x => x.recipeID === id);

    if (!r) return;

    editingRecipeID = id;

    document.getElementById('recipeFinishedFood').value = r.finishedFoodID;
    document.getElementById('recipeName').value = r.recipeName || '';
    document.getElementById('recipeVersion').value = r.versionNo;
    document.getElementById('recipeOutputQty').value = r.batchOutputQty;
    document.getElementById('recipeDate').value = r.effectiveDate;
    document.getElementById('recipeActive').checked = r.isActive;

    document.getElementById('recipeIngredientRows').innerHTML = '';

    (r.details || []).forEach(d => {
        addRecipeIngredient(d.rawMaterialID, d.quantity);
    });

    document.getElementById('saveRecipeButton').textContent = 'Update Recipe';
    updateRecipeTotals();
}


function resetFoodRecipe() {
    editingRecipeID = null;

    document.getElementById('recipeName').value = '';
    document.getElementById('recipeVersion').value = 1;
    document.getElementById('recipeOutputQty').value = 1;

    document.getElementById('recipeDate').value =
        new Date().toISOString().slice(0, 10);

    document.getElementById('recipeActive').checked = true;

    document.getElementById('recipeIngredientRows').innerHTML = '';

    addRecipeIngredient();

    document.getElementById('saveRecipeButton').textContent = 'Save Recipe';
    updateRecipeTotals();
}


async function saveFoodRecipe() {
    updateRecipeTotals();
    const details = [
        ...document.querySelectorAll('#recipeIngredientRows tr')
    ].map(tr => ({
        rawMaterialID: Number(tr.querySelector('.recipe-item').value),
        quantity: Number(tr.querySelector('.recipe-qty').value)
    }));

    const payload = {
        recipeID: editingRecipeID,
        finishedFoodID: Number(document.getElementById('recipeFinishedFood').value),
        recipeName: document.getElementById('recipeName').value.trim(),
        versionNo: Number(document.getElementById('recipeVersion').value),
        batchOutputQty: Number(document.getElementById('recipeOutputQty').value),
        effectiveDate: document.getElementById('recipeDate').value,
        isActive: document.getElementById('recipeActive').checked,
        details
    };

    try {
        const r = await foodApi('/api/animal/food/recipes/save', {
            method: 'POST',
            body: JSON.stringify(payload)
        });

        showFoodMessage('recipeMessage', r.message || 'Recipe saved.', false);

        resetFoodRecipe();
        await loadRecipes();

    } catch (e) {
        showFoodMessage(
            'recipeMessage',
            'Unable to save recipe: ' + e.message,
            true
        );
    }
}


function updateRecipeTotals() {
    const total = [...document.querySelectorAll('#recipeIngredientRows .recipe-qty')]
        .reduce((sum, input) => sum + (Number(input.value) || 0), 0);
    const batch = Number(document.getElementById('recipeOutputQty')?.value) || 0;
    const difference = batch - total;
    const totalElement = document.getElementById('recipeIngredientTotal');
    const differenceElement = document.getElementById('recipeIngredientDifference');

    if (totalElement) totalElement.textContent = total.toFixed(3);
    if (differenceElement) {
        differenceElement.textContent = Math.abs(difference).toFixed(3) +
            (Math.abs(difference) <= 0.0005 ? ' MATCH' : difference > 0 ? ' SHORT' : ' EXCESS');
        differenceElement.style.color = Math.abs(difference) <= 0.0005 ? 'green' : 'red';
    }
}
