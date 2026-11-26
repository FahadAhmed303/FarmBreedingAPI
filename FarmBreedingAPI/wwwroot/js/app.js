// ==========================================
// MODULE NAVIGATION
// ==========================================

function showModule(moduleName) {
    document.getElementById("goatInformationCard").style.display =
        moduleName === "goatInfo" ? "block" : "none";
    document.getElementById("animalCard").style.display = "none";
    document.getElementById("editCard").style.display = "none";
    document.getElementById("inspectionCard").style.display =
        moduleName === "inspection" ? "block" : "none";
    document.getElementById("growthCard").style.display =
        moduleName === "growth" ? "block" : "none";
    document.getElementById("itemMasterCard").style.display =
        moduleName === "items" ? "block" : "none";
    document.getElementById("foodRecipeCard").style.display = moduleName === "recipes" ? "block" : "none";
    document.getElementById("foodOpeningCard").style.display = moduleName === "foodOpening" ? "block" : "none";
    document.getElementById("foodIssueCard").style.display = moduleName === "foodIssue" ? "block" : "none";
    document.getElementById("foodPurchaseCard").style.display = moduleName === "foodPurchase" ? "block" : "none";
    document.getElementById("foodProductionCard").style.display = moduleName === "foodProduction" ? "block" : "none";
    document.getElementById("foodWasteCard").style.display = moduleName === "foodWaste" ? "block" : "none";
    document.getElementById("foodStockCard").style.display = moduleName === "foodStock" ? "block" : "none";

    document.getElementById("navGoatInfo").classList.toggle("active", moduleName === "goatInfo");
    document.getElementById("navInspection").classList.toggle("active", moduleName === "inspection");
    document.getElementById("navGrowth").classList.toggle("active", moduleName === "growth");
    document.getElementById("navItems").classList.toggle("active", moduleName === "items");
    document.getElementById("navRecipes").classList.toggle("active", moduleName === "recipes");
    document.getElementById("navFoodOpening").classList.toggle("active", moduleName === "foodOpening");
    document.getElementById("navFoodIssue").classList.toggle("active", moduleName === "foodIssue");
    ["FoodPurchase", "FoodProduction", "FoodWaste", "FoodStock"].forEach((n) => { const id = "nav" + n; document.getElementById(id).classList.toggle("active", moduleName === { FoodPurchase: "foodPurchase", FoodProduction: "foodProduction", FoodWaste: "foodWaste", FoodStock: "foodStock" }[n]); });

    if (moduleName === "items") loadItems();
    if (moduleName === "recipes") loadRecipeModule();
    if (moduleName === "foodOpening") loadOpeningBalanceModule();
    if (moduleName === "foodIssue") loadFoodIssueModule();
    if (moduleName === "foodPurchase") loadPurchaseModule();
    if (moduleName === "foodProduction") loadProductionModule();
    if (moduleName === "foodWaste") loadWasteModule();
    if (moduleName === "foodStock") initStockReport();

    if (moduleName === "growth") {
        document.getElementById("growthAnimalNo").focus();
    }

    // Retry loading the registered Goat Numbers whenever Inspection
    // is opened and the sheet is still empty (for example, if the
    // first API request failed during login).
    if (moduleName === "inspection" &&
        document.getElementById("inspectionRows").rows.length === 0) {
        loadInspectionAnimalNos();
    }
}
// ==========================================
// LOGIN
// ==========================================

async function login() {

    const username =
        document.getElementById("loginUsername").value.trim();

    const password =
        document.getElementById("loginPassword").value;

    if (!username || !password) {
        alert("Please enter User ID and Password.");
        return;
    }

    try {

        const response = await fetch(
            "/api/auth/login",
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    username: username,
                    password: password
                })
            }
        );

        if (!response.ok) {
            alert("Invalid User ID or Password.");
            return;
        }

        const result = await response.json();

        if (!result || result.success !== true) {
            alert("Invalid User ID or Password.");
            return;
        }

        document.getElementById("loginPage").style.display = "none";
        document.getElementById("mainPage").style.display = "block";
        showModule("goatInfo");
        await loadInspectionAnimalNos();
    }
    catch (error) {
        alert("Error connecting to API: " + error.message);
    }
}

function logout() {
    document.getElementById("mainPage").style.display = "none";
    document.getElementById("loginPage").style.display = "block";
    document.getElementById("loginUsername").value = "";
    document.getElementById("loginPassword").value = "";
}
// ==========================================
// DATE FOR INPUT
// ==========================================

function toInputDate(value) {

    if (!value) {
        return "";
    }

    return value.substring(0, 10);
}


// ==========================================
// DISPLAY DATE
// ==========================================

function formatDate(value) {

    if (!value) {
        return "-";
    }


    const date =
        new Date(value);


    if (isNaN(date.getTime())) {
        return "-";
    }


    const day =
        String(date.getDate()).padStart(2, "0");


    const months = [
        "Jan", "Feb", "Mar", "Apr",
        "May", "Jun", "Jul", "Aug",
        "Sep", "Oct", "Nov", "Dec"
    ];


    const month =
        months[date.getMonth()];


    const year =
        date.getFullYear();


    return `${day}/${month}/${year}`;
}


// ==========================================
// MESSAGE
// ==========================================

function showMessage(text, error) {

    // REAL POPUP ALERT
    if (error) {
        alert(text);
        return;
    }

    // Keep successful messages on the page
    const message =
        document.getElementById("message");

    message.textContent =
        text;

    message.className =
        "message success";

    message.style.display =
        "block";
}
function foodApiHeaders(json) {
    const h = { "app-version": "2" };

    if (json) {
        h["Content-Type"] = "application/json";
    }

    return h;
}


async function foodApi(url, options = {}) {
    const r = await fetch(url, {
        ...options,
        headers: {
            ...foodApiHeaders(!!options.body),
            ...(options.headers || {})
        }
    });

    const t = await r.text();

    if (!r.ok) {
        throw new Error(t || `HTTP ${r.status}`);
    }

    try {
        return t ? JSON.parse(t) : {};
    } catch {
        return {};
    }
}
function escapeFoodHtml(v) {
    return String(v ?? '').replace(/[&<>"']/g, c => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    }[c]));
}
// FOOD MODULE MESSAGE
// ==========================================

function showFoodMessage(id, msg, error) {
    const e = document.getElementById(id);

    e.textContent = msg;
    e.className = error ? 'message error' : 'message success';
    e.style.display = 'block';
}
// ==========================================
