let currentAnimal = null;

// ==========================================
// MOTHER VALIDATION STATE
// ==========================================

let motherLookupValid = true;


// ==========================================
// SAVE BUTTON CONTROL
// ==========================================

function updateSaveButton() {

    const saveButton =
        document.getElementById("saveButton");

    const sourceType =
        document.getElementById("eSourceType").value;

    const motherNumber =
        document.getElementById("eMotherNumber").value.trim();

    const motherCode =
        document.getElementById("eMotherCode").value.trim();


    // PURCHASE does not require mother

    if (sourceType === "PURCHASE") {

        saveButton.disabled = false;

        return;
    }


    // BORN requires valid mother

    if (sourceType === "BORN") {

        if (
            motherNumber &&
            motherCode &&
            motherLookupValid
        ) {

            saveButton.disabled = false;

        }
        else {

            saveButton.disabled = true;
        }

        return;
    }


    // No source selected

    saveButton.disabled = true;
}


// ==========================================
// PDF - ALL GOATS
// ==========================================

function printAllGoats() {
    // Open the filter/report screen in the current tab (avoid stale popup/PDF flow).
    window.location.href = "/animal-report.html?view=filters";
}


// ==========================================
// SEARCH ANIMAL
// ==========================================

async function loadAnimal() {

    const animalNo =
        document.getElementById("animalNo").value;

    const message =
        document.getElementById("message");


    document.getElementById("animalCard").style.display =
        "none";

    document.getElementById("editCard").style.display =
        "none";

    message.style.display =
        "none";


    if (!animalNo) {

        showMessage(
            "Please enter Animal No.",
            true
        );

        return;
    }


    try {

        const response = await fetch(
            `/api/animal/byanimalno/${animalNo}`,
            {
                headers: {
                    "app-version": "2"
                }
            }
        );


        if (!response.ok) {

            showMessage(
                response.status === 404
                    ? "Animal not found."
                    : "Unable to load animal. HTTP " +
                    response.status,
                true
            );

            return;
        }


        const result =
            await response.json();


        if (!result || !result.atCode) {

            showMessage(
                "Animal not found.",
                true
            );

            return;
        }


        await loadAnimalDetails(
            result.atCode
        );

    }
    catch (error) {

        showMessage(
            "Error connecting to API: " +
            error.message,
            true
        );
    }
}


// ==========================================
// LOAD ANIMAL DETAILS
// ==========================================

async function loadAnimalDetails(atCode) {

    const response = await fetch(
        `/api/animal/${encodeURIComponent(atCode)}`,
        {
            headers: {
                "app-version": "2"
            }
        }
    );


    if (!response.ok) {

        showMessage(
            "Unable to load animal details.",
            true
        );

        return;
    }


    const animal =
        await response.json();


    currentAnimal =
        animal;


    document.getElementById("vAnimalNo").textContent =
        animal.animalNo ?? "-";


    document.getElementById("vATCode").textContent =
        animal.atCode ?? "-";


    document.getElementById("vGender").textContent =
        animal.gender ?? "-";


    document.getElementById("vDOB").textContent =
        formatDate(animal.dob);


    document.getElementById("vBreed").textContent =
        animal.breed ?? "-";


    document.getElementById("vMotherNumber").textContent =
        animal.motherNumber ?? "-";


    document.getElementById("vMotherCode").textContent =
        animal.motherCode ?? "-";


    document.getElementById("vSourceType").textContent =
        animal.sourceType ?? "-";


    document.getElementById("vPurchaseDate").textContent =
        formatDate(animal.purchaseDate);


    document.getElementById("vPrice").textContent =
        animal.price ?? "-";


    document.getElementById("vAgentName").textContent =
        animal.agentName ?? "-";


    document.getElementById("animalCard").style.display =
        "block";


    showMessage(
        "Goat loaded successfully.",
        false
    );
}


// ==========================================
// OPEN EDIT MODE
// ==========================================

async function editAnimal() {

    if (!currentAnimal) {
        return;
    }


    document.getElementById("eAnimalNo").value =
        currentAnimal.animalNo ?? "";


    document.getElementById("eATCode").value =
        currentAnimal.atCode ?? "";


    document.getElementById("eGender").value =
        currentAnimal.gender ?? "";


    document.getElementById("eDOB").value =
        toInputDate(currentAnimal.dob);


    document.getElementById("eSourceType").value =
        currentAnimal.sourceType ?? "";


    document.getElementById("ePurchaseDate").value =
        toInputDate(currentAnimal.purchaseDate);


    document.getElementById("ePrice").value =
        currentAnimal.price ?? "";


    document.getElementById("eAgentName").value =
        currentAnimal.agentName ?? "";


    document.getElementById("eMotherNumber").value =
        currentAnimal.motherNumber ?? "";


    document.getElementById("eMotherCode").value =
        currentAnimal.motherCode ?? "";

    document.getElementById("eGoatStatus").value = currentAnimal.goatStatus || "Active";
    document.getElementById("eSoldExpiredDate").value = toInputDate(currentAnimal.soldExpiredDate);
    document.getElementById("eSellingAmount").value = currentAnimal.sellingAmount ?? "";
    document.getElementById("eExpiredRemark").value = currentAnimal.expiredRemark ?? "";
    document.getElementById("eNewWeight").value = "";
    const today = new Date(); today.setMinutes(today.getMinutes() - today.getTimezoneOffset());
    document.getElementById("eWeightRecordDate").value = today.toISOString().slice(0, 10);
    goatStatusChanged();


    // Existing mother is considered valid
    // until user changes Mother Number.

    motherLookupValid =
        !!currentAnimal.motherCode;


    await loadCategories();

    await loadBreeds();


    document.getElementById("eCategory").value =
        currentAnimal.atCategoryCode ?? "";


    document.getElementById("eBreed").value =
        currentAnimal.breed ?? "";


    sourceTypeChanged();


    document.getElementById("editCard").style.display =
        "block";


    document.getElementById("editCard").scrollIntoView({
        behavior: "smooth"
    });


    updateSaveButton();
}
// ==========================================
// LOAD CATEGORIES
// ==========================================

async function loadCategories() {

    const response = await fetch(
        "/api/category",
        {
            headers: {
                "app-version": "2"
            }
        }
    );


    if (!response.ok) {

        throw new Error(
            "Unable to load categories."
        );
    }


    const categories =
        await response.json();


    const select =
        document.getElementById("eCategory");


    select.innerHTML =
        '<option value="">Select Category</option>';


    categories.forEach(category => {

        const option =
            document.createElement("option");


        option.value =
            category.atCategoryCode;


        option.textContent =
            category.atCategoryName;


        select.appendChild(option);

    });
}


// ==========================================
// LOAD BREEDS
// ==========================================

async function loadBreeds() {

    const response = await fetch(
        "/api/breed",
        {
            headers: {
                "app-version": "2"
            }
        }
    );


    if (!response.ok) {

        throw new Error(
            "Unable to load breeds."
        );
    }


    const breeds =
        await response.json();


    const select =
        document.getElementById("eBreed");


    select.innerHTML =
        '<option value="">Select Breed</option>';


    breeds.forEach(breed => {

        const option =
            document.createElement("option");


        option.value =
            breed.breedName;


        option.textContent =
            breed.breedName;


        select.appendChild(option);

    });
}


// ==========================================
// SOURCE TYPE
// ==========================================

function sourceTypeChanged() {

    const sourceType =
        document.getElementById("eSourceType").value;


    const purchaseDate =
        document.getElementById("ePurchaseDate");

    const price =
        document.getElementById("ePrice");

    const agentName =
        document.getElementById("eAgentName");

    const motherNumber =
        document.getElementById("eMotherNumber");

    const motherCode =
        document.getElementById("eMotherCode");


    if (sourceType === "PURCHASE") {

        purchaseDate.disabled =
            false;

        price.disabled =
            false;

        agentName.disabled =
            false;


        motherNumber.value =
            "";

        motherCode.value =
            "";


        motherNumber.disabled =
            true;

        motherCode.disabled =
            true;


        motherLookupValid =
            true;
    }

    else if (sourceType === "BORN") {

        purchaseDate.value =
            "";

        price.value =
            "";

        agentName.value =
            "";


        purchaseDate.disabled =
            true;

        price.disabled =
            true;

        agentName.disabled =
            true;


        motherNumber.disabled =
            false;

        motherCode.disabled =
            true;


        motherLookupValid =
            !!motherNumber.value &&
            !!motherCode.value;
    }

    else {

        purchaseDate.disabled =
            false;

        price.disabled =
            false;

        agentName.disabled =
            false;


        motherNumber.disabled =
            false;

        motherCode.disabled =
            false;


        motherLookupValid =
            true;
    }


    document.getElementById("eGender").disabled =
        false;

    document.getElementById("eCategory").disabled =
        false;

    document.getElementById("eDOB").disabled =
        false;

    document.getElementById("eBreed").disabled =
        false;


    updateSaveButton();
}


// ==========================================
// MOTHER NUMBER → MOTHER CODE
// ==========================================

async function motherNumberChanged() {

    const input =
        document.getElementById("eMotherNumber");

    const motherCode =
        document.getElementById("eMotherCode");


    const motherNumber =
        input.value.trim();


    // ==========================================
    // EMPTY
    // ==========================================

    if (!motherNumber) {

        motherCode.value = "";

        motherLookupValid =
            false;

        updateSaveButton();

        document.getElementById("message").style.display =
            "none";

        return;
    }


    // ==========================================
    // OWN ANIMAL
    // ==========================================

    if (
        currentAnimal &&
        Number(motherNumber) ===
        Number(currentAnimal.animalNo)
    ) {

        motherCode.value = "";

        motherLookupValid =
            false;

        updateSaveButton();

        showMessage(
            "An animal cannot be its own mother.",
            true
        );

        return;
    }


    // ==========================================
    // CURRENT REQUEST
    // ==========================================

    const requestedMotherNumber =
        motherNumber;


    // Disable Save while checking

    motherLookupValid =
        false;

    updateSaveButton();


    try {

        // ======================================
        // FIND MOTHER AT CODE
        // ======================================

        const response = await fetch(
            `/api/animal/byanimalno/${encodeURIComponent(requestedMotherNumber)}`,
            {
                headers: {
                    "app-version": "2"
                }
            }
        );


        // User changed number while waiting

        if (
            document.getElementById("eMotherNumber").value.trim()
            !== requestedMotherNumber
        ) {
            return;
        }


        // ======================================
        // NOT FOUND
        // ======================================

        if (!response.ok) {

            motherCode.value = "";

            motherLookupValid =
                false;

            updateSaveButton();

            showMessage(
                "Mother Animal No not found.",
                true
            );

            return;
        }


        const result =
            await response.json();


        if (
            !result ||
            !result.atCode
        ) {

            motherCode.value = "";

            motherLookupValid =
                false;

            updateSaveButton();

            showMessage(
                "Mother Animal No not found.",
                true
            );

            return;
        }


        // ======================================
        // GET MOTHER DETAILS
        // ======================================

        const motherResponse =
            await fetch(
                `/api/animal/${encodeURIComponent(result.atCode)}`,
                {
                    headers: {
                        "app-version": "2"
                    }
                }
            );


        // User changed number while waiting

        if (
            document.getElementById("eMotherNumber").value.trim()
            !== requestedMotherNumber
        ) {
            return;
        }


        if (!motherResponse.ok) {

            motherCode.value = "";

            motherLookupValid =
                false;

            updateSaveButton();

            showMessage(
                "Unable to verify mother.",
                true
            );

            return;
        }


        const mother =
            await motherResponse.json();


        // ======================================
        // MUST BE FEMALE
        // ======================================

        if (
            !mother.gender ||
            mother.gender.toLowerCase() !== "female"
        ) {

            motherCode.value = "";

            motherLookupValid =
                false;

            updateSaveButton();

            showMessage(
                "Selected Mother Animal No is not female.",
                true
            );

            return;
        }


        // ======================================
        // FINAL INPUT CHECK
        // ======================================

        if (
            document.getElementById("eMotherNumber").value.trim()
            !== requestedMotherNumber
        ) {
            return;
        }


        // ======================================
        // VALID MOTHER
        // ======================================

        motherCode.value =
            result.atCode;


        motherLookupValid =
            true;


        updateSaveButton();


        document.getElementById("message").style.display =
            "none";

    }
    catch (error) {

        if (
            document.getElementById("eMotherNumber").value.trim()
            !== requestedMotherNumber
        ) {
            return;
        }


        motherCode.value = "";

        motherLookupValid =
            false;

        updateSaveButton();


        showMessage(
            "Error verifying mother: " +
            error.message,
            true
        );
    }
}
// ==========================================
// GOAT STATUS
// ==========================================

function goatStatusChanged() {
    const status = document.getElementById("eGoatStatus").value;
    const inactive = status !== "Active";

    document.getElementById("soldExpiredDateField").style.display =
        inactive ? "block" : "none";

    document.getElementById("sellingAmountField").style.display =
        status === "Sold" ? "block" : "none";

    document.getElementById("expiredRemarkField").style.display =
        status === "Expired" ? "block" : "none";

    document.getElementById("soldExpiredDateLabel").textContent =
        status === "Sold" ? "Sold Date" : "Expired Date";

    if (!inactive) {
        document.getElementById("eSoldExpiredDate").value = "";
        document.getElementById("eSellingAmount").value = "";
    } else if (status !== "Sold") {
        document.getElementById("eSellingAmount").value = "";
    }
}


// ==========================================
// SAVE ANIMAL
// ==========================================

async function saveAnimal() {

    if (!currentAnimal) {
        return;
    }


    const saveButton =
        document.getElementById("saveButton");


    // Extra protection

    if (saveButton.disabled) {

        showMessage(
            "Please correct the validation errors before saving.",
            true
        );

        return;
    }


    const sourceType =
        document.getElementById("eSourceType").value;


    const motherNumber =
        document.getElementById("eMotherNumber").value.trim();


    const motherCode =
        document.getElementById("eMotherCode").value.trim();


    // ==========================================
    // BORN VALIDATION
    // ==========================================

    if (sourceType === "BORN") {

        if (!motherNumber) {

            motherLookupValid =
                false;

            updateSaveButton();

            showMessage(
                "Please enter Mother Animal No.",
                true
            );

            return;
        }


        if (
            Number(motherNumber) ===
            Number(currentAnimal.animalNo)
        ) {

            motherLookupValid =
                false;

            updateSaveButton();

            showMessage(
                "An animal cannot be its own mother.",
                true
            );

            return;
        }


        if (
            !motherLookupValid ||
            !motherCode
        ) {

            motherLookupValid =
                false;

            updateSaveButton();

            showMessage(
                "Please enter a valid female Mother Animal No.",
                true
            );

            return;
        }
    }


    // ==========================================
    // MODEL
    // ==========================================

    const model = {

        ATCode:
            document.getElementById("eATCode").value,

        gender:
            document.getElementById("eGender").value,

        ATCategoryCode:
            document.getElementById("eCategory").value,

        DOB:
            document.getElementById("eDOB").value ||
            null,

        SourceType:
            sourceType ||
            null,

        PurchaseDate:
            document.getElementById("ePurchaseDate").value ||
            null,

        Price:
            document.getElementById("ePrice").value
                ? Number(
                    document.getElementById("ePrice").value
                )
                : null,

        AgentName:
            document.getElementById("eAgentName").value ||
            null,

        MotherCode:
            document.getElementById("eMotherCode").value ||
            null,

        MotherNumber:
            document.getElementById("eMotherNumber").value
                ? Number(
                    document.getElementById("eMotherNumber").value
                )
                : null,

        Breed:
            document.getElementById("eBreed").value ||
            null
    };


    // ==========================================
    // SAVE
    // ==========================================

    try {

        const response = await fetch(
            "/api/animal/save",
            {
                method: "POST",

                headers: {
                    "Content-Type": "application/json",
                    "app-version": "2"
                },

                body: JSON.stringify(model)
            }
        );


        if (!response.ok) {

            const errorText =
                await response.text();


            showMessage(
                "Save failed: " +
                errorText,
                true
            );

            return;
        }


        // Save status separately so the existing Animal model and save endpoint remain untouched.

        const statusModel = {
            ATCode: model.ATCode,
            GoatStatus: document.getElementById("eGoatStatus").value,
            SoldExpiredDate: document.getElementById("eSoldExpiredDate").value || null,
            SellingAmount: document.getElementById("eSellingAmount").value === ""
                ? null
                : Number(document.getElementById("eSellingAmount").value),
            ExpiredRemark: document.getElementById("eExpiredRemark").value.trim() || null
        };

        const statusResponse = await fetch("/api/animal/status/update", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "app-version": "2"
            },
            body: JSON.stringify(statusModel)
        });

        if (!statusResponse.ok) {
            throw new Error("Status update failed: " + await statusResponse.text());
        }


        // If a new weight was entered, append a new ArticleInfo03 history row.

        const newWeight =
            document.getElementById("eNewWeight").value.trim();

        const weightDate =
            document.getElementById("eWeightRecordDate").value;

        if (newWeight !== "") {

            if (
                !Number.isFinite(Number(newWeight)) ||
                Number(newWeight) < 0 ||
                !weightDate
            ) {
                throw new Error("Enter a valid non-negative weight and its recording date.");
            }

            const weightResponse = await fetch("/api/animal/inspection/save", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "app-version": "2"
                },
                body: JSON.stringify({
                    AnimalNo: Number(document.getElementById("eAnimalNo").value),
                    RecordDate: weightDate,
                    Weight: Number(newWeight),
                    Teeth: null,
                    Nails: null,
                    Hair: null
                })
            });

            if (!weightResponse.ok) {
                throw new Error("Weight save failed: " + await weightResponse.text());
            }
        }


        showMessage(
            "Goat updated successfully.",
            false
        );


        await loadAnimalDetails(
            model.ATCode
        );


        document.getElementById("editCard").style.display =
            "none";

    }
    catch (error) {

        showMessage(
            "Error saving goat: " +
            error.message,
            true
        );
    }
}


// ==========================================
// CANCEL
// ==========================================

function cancelEdit() {

    document.getElementById("editCard").style.display =
        "none";
}
