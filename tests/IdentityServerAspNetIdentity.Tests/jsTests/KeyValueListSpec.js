describe('Add Key Values to list Tests', function () {
    beforeEach(function () {
        // Runs before all tests in this block
        document.querySelector('#working_area').innerHTML = `
        <ul class="list-group" id="propertyContainer">
    <li>
        <div id="addScope_Properties" class="list-group-item d-flex">
            <input class="col-2" type="text" id="newScope_PropertiesKey" placeholder="Enter Key">
            <input class="col-8" type="text" id="newScope_PropertiesValue" placeholder="Enter Value">
            <button class="col-2 btn btn-outline-primary float-right" type="button" onclick="AddKeyValueItem('propertyContainer','Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue')">
                <i class="bi bi-plus"></i>
            </button>
        </div>
    </li>
    <li class="list-group-item d-flex">
        <label class="control-label col-2" for="Scope_Properties0__Key">w</label>
        <input class="d-none" type="text" value="w" id="Scope_Properties0__Key" name="Scope.Properties[0].Key">
        <input class="col-8" type="text" value="e" id="Scope_Properties0__Value" name="Scope.Properties[0].Value">
        <span class="field-validation-valid" data-valmsg-for="Scope.Properties[0].Value" data-valmsg-replace="true"></span>
        <button class="col-2 btn btn-outline-danger delete float-right" onclick="removeItem('propertyContainer','Scope.Properties',0)">
            <i class="bi bi-x"></i>
        </button>
    </li>
    <li class="list-group-item d-flex">
        <label for="Scope_Properties1__Key" class="control-label col-2">a</label>
        <input class="d-none" type="text" id="Scope_Properties1__Key" name="Scope.Properties[1].Key" value="a">
        <input class="col-8 text-box single-line" type="text" id="Scope_Properties1__Value" name="Scope.Properties[1].Value" value="a">
        <span class="field-validation-valid" data-valmsg-replace="true" data-valmsg-for="Scope.Properties[1].Value"></span>
        <button type="button" onclick="removeItem('propertyContainer', 'Scope.Properties',1)" class="col-2 btn btn-outline-danger delete float-right">
            <i class="bi bi-x"></i>
        </button>
    </li>
    <li class="list-group-item d-flex">
        <label for="Scope_Properties2__Key" class="control-label col-2">b</label>
        <input class="d-none" type="text" id="Scope_Properties2__Key" name="Scope.Properties[2].Key" value="b">
        <input class="col-8 text-box single-line" type="text" id="Scope_Properties2__Value" name="Scope.Properties[2].Value" value="b">
        <span class="field-validation-valid" data-valmsg-replace="true" data-valmsg-for="Scope.Properties[2].Value"></span>
        <button type="button" onclick="removeItem('propertyContainer', 'Scope.Properties',2)" class="col-2 btn btn-outline-danger delete float-right">
            <i class="bi bi-x"></i>
        </button>
    </li>
    <li class="list-group-item d-flex">
        <label for="Scope_Properties3__Key" class="control-label col-2">c</label>
        <input class="d-none" type="text" id="Scope_Properties3__Key" name="Scope.Properties[3].Key" value="c">
        <input class="col-8 text-box single-line" type="text" id="Scope_Properties3__Value" name="Scope.Properties[3].Value" value="c">
        <span class="field-validation-valid" data-valmsg-replace="true" data-valmsg-for="Scope.Properties[3].Value"></span>
        <button type="button" onclick="removeItem('propertyContainer', 'Scope.Properties',3)" class="col-2 btn btn-outline-danger delete float-right">
            <i class="bi bi-x"></i>
        </button>
    </li>
    <li class="list-group-item d-flex">
        <label for="Scope_Properties4__Key" class="control-label col-2">d</label>
        <input class="d-none" type="text" id="Scope_Properties4__Key" name="Scope.Properties[4].Key" value="d">
        <input class="col-8 text-box single-line" type="text" id="Scope_Properties4__Value" name="Scope.Properties[4].Value" value="d">
        <span class="field-validation-valid" data-valmsg-replace="true" data-valmsg-for="Scope.Properties[4].Value"></span>
        <button type="button" onclick="removeItem('propertyContainer', 'Scope.Properties',4)" class="col-2 btn btn-outline-danger delete float-right">
            <i class="bi bi-x"></i>
        </button>
    </li>
    <li class="list-group-item d-flex">
        <label for="Scope_Properties5__Key" class="control-label col-2">e</label>
        <input class="d-none" type="text" id="Scope_Properties5__Key" name="Scope.Properties[5].Key" value="e">
        <input class="col-8 text-box single-line" type="text" id="Scope_Properties5__Value" name="Scope.Properties[5].Value" value="e">
        <span class="field-validation-valid" data-valmsg-replace="true" data-valmsg-for="Scope.Properties[5].Value"></span>
        <button type="button" onclick="removeItem('propertyContainer', 'Scope.Properties',5)" class="col-2 btn btn-outline-danger delete float-right">
            <i class="bi bi-x"></i>
        </button>
    </li>
    <li class="list-group-item d-flex">
        <label for="Scope_Properties6__Key" class="control-label col-2">f</label>
        <input class="d-none" type="text" id="Scope_Properties6__Key" name="Scope.Properties[6].Key" value="f">
        <input class="col-8 text-box single-line" type="text" id="Scope_Properties6__Value" name="Scope.Properties[6].Value" value="f">
        <span class="field-validation-valid" data-valmsg-replace="true" data-valmsg-for="Scope.Properties[6].Value"></span>
        <button type="button" onclick="removeItem('propertyContainer', 'Scope.Properties',6)" class="col-2 btn btn-outline-danger delete float-right">
            <i class="bi bi-x"></i>
        </button>
    </li>
</ul>

`;
        
    });
    
    afterEach(function () {
        document.querySelector('#working_area').innerHTML = '';
    } );

    it("should add a new list item when given valid input", function() {
        // Given
        const key = 'testKey';
        const value = 'testValue';

        // Mock the input elements and set their values
        document.getElementById('newScope_PropertiesKey').value = key;
        document.getElementById('newScope_PropertiesValue').value = value;

        // Execute the function
        AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

        // Check if the new item was added
        const newKey = document.querySelector(`input[value="${key}"]`);
        const newValue = document.querySelector(`input[value="${value}"]`);

        expect(newKey).not.toBeNull();
        expect(newValue).not.toBeNull();
    });

    it("should not add a new list item when key already exists", function() {

        // Save the original function to a variable
        var originalAlert = window.alert;

        // Mock the alert function
        window.alert = function (message) {
            window.latestAlertMessage = message; // Save the latest alert message
        };
        
        // Given
        const existingKey = 'w';
        const value = 'testValue';

        // Mock the input elements and set their values
        document.getElementById('newScope_PropertiesKey').value = existingKey;
        document.getElementById('newScope_PropertiesValue').value = value;

        const initialLength = document.querySelectorAll('#propertyContainer li').length;

        // Execute the function
        AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

        const finalLength = document.querySelectorAll('#propertyContainer li').length;

        // The length should remain the same as no new item should be added
        expect(finalLength).toEqual(initialLength);

        expect(window.latestAlertMessage).toBe("Key " + existingKey + " already exists.") ;

        window.alert = originalAlert; // Restore the original function
    });

    it("should not add a new list item when key is empty", function() {
        var originalAlert = window.alert;

        // Mock the alert function
        window.alert = function (message) {
            window.latestAlertMessage = message; // Save the latest alert message
        };
        // Given
        const key = ''; // empty key
        const value = 'testValue';

        // Mock the input elements and set their values
        document.getElementById('newScope_PropertiesKey').value = key;
        document.getElementById('newScope_PropertiesValue').value = value;

        const initialLength = document.querySelectorAll('#propertyContainer li').length;

        // Execute the function
        AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

        const finalLength = document.querySelectorAll('#propertyContainer li').length;

        // The length should remain the same as no new item should be added
        expect(finalLength).toEqual(initialLength);
        //        alert("Key and value cannot be empty.");
        expect(window.latestAlertMessage).toBe("Key and value cannot be empty.") ;
        window.alert = originalAlert; // Restore the original function
    });


    describe("Check attributes", function() {

        it("should create a new list item with the correct class name", function() {
            const key = 'g';
            const value = 'g';

            document.getElementById('newScope_PropertiesKey').value = key;
            document.getElementById('newScope_PropertiesValue').value = value;

            AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

            const newEntry = document.querySelector(`#propertyContainer li:last-child`);
            expect(newEntry.className).toBe('list-group-item d-flex');
        });

        it("should set the correct attributes and content for the label", function() {
            const key = 'g';
            const value = 'g';

            document.getElementById('newScope_PropertiesKey').value = key;
            document.getElementById('newScope_PropertiesValue').value = value;

            AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

            const label = document.querySelector(`#propertyContainer li:last-child label`);
            expect(label.getAttribute('for')).toBe('Scope_Properties7__Key');
            expect(label.className).toBe('control-label col-2');
            expect(label.innerText).toBe('g');
        });

        it("should set the correct attributes for the key input", function() {
            const key = 'g';
            const value = 'g';

            document.getElementById('newScope_PropertiesKey').value = key;
            document.getElementById('newScope_PropertiesValue').value = value;

            AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

            const inputKey = document.querySelector(`#propertyContainer li:last-child input.d-none`);
            expect(inputKey.id).toBe('Scope_Properties7__Key');
            expect(inputKey.getAttribute('name')).toBe('Scope.Properties[7].Key');
            expect(inputKey.getAttribute('value')).toBe('g');
        });

        it("should set the correct attributes for the value input", function() {
            const key = 'g';
            const value = 'g';

            document.getElementById('newScope_PropertiesKey').value = key;
            document.getElementById('newScope_PropertiesValue').value = value;

            AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

            const inputValue = document.querySelector(`#propertyContainer li:last-child input.col-8`);
            expect(inputValue.id).toBe('Scope_Properties7__Value');
            expect(inputValue.getAttribute('name')).toBe('Scope.Properties[7].Value');
            expect(inputValue.getAttribute('value')).toBe('g');
        });

        it("should set the correct attributes for the validation span", function() {
            const key = 'g';
            const value = 'g';

            document.getElementById('newScope_PropertiesKey').value = key;
            document.getElementById('newScope_PropertiesValue').value = value;

            AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

            const span = document.querySelector(`#propertyContainer li:last-child span.field-validation-valid`);
            expect(span.getAttribute('data-valmsg-replace')).toBe('true');
            expect(span.getAttribute('data-valmsg-for')).toBe('Scope.Properties[7].Value');
        });

        it("should set the correct attributes for the remove button", function() {
            const key = 'g';
            const value = 'g';

            document.getElementById('newScope_PropertiesKey').value = key;
            document.getElementById('newScope_PropertiesValue').value = value;

            AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

            const button = document.querySelector(`#propertyContainer li:last-child button.col-2`);
            expect(button.getAttribute('onclick')).toBe("removeItem('propertyContainer', 'Scope.Properties',7)");
            expect(button.className).toBe('col-2 btn btn-outline-danger delete float-right');
        });

        
        it("should have the correct icon in the remove button", function() {
            const key = 'g';
            const value = 'g';

            document.getElementById('newScope_PropertiesKey').value = key;
            document.getElementById('newScope_PropertiesValue').value = value;

            AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

            const icon = document.querySelector(`#propertyContainer li:last-child button.col-2 i`);
            expect(icon.className).toBe('bi bi-x');
        });
    });

    function getRemoveButtonByIndex(index) {
        const buttons = document.querySelectorAll('button.delete');
        for (let button of buttons) {
            const onClickValue = button.getAttribute('onclick').replace(/\s+/g, '');
            const expectedOnClickValue = `removeItem('propertyContainer','Scope.Properties',${index})`;
            if (onClickValue === expectedOnClickValue) {
                return button;
            }
        }
        return null;
    }

    function getKeyValuePairsFromUL(ulElementId) {
        const ulElement = document.getElementById(ulElementId);
        if (!ulElement) return [];

        const lis = ulElement.querySelectorAll('li');
        let keyValuePairs = [];

        lis.forEach(li => {
            const keyInput = li.querySelector(`input[id$='__Key']`);
            const valueInput = li.querySelector(`input[id$='__Value']`);

            if (keyInput && valueInput) {
                keyValuePairs.push({
                    key: keyInput.value,
                    value: valueInput.value
                });
            }
        });

        return keyValuePairs;
    }
    

    describe("removeItem function", function() {


        
        it("should remove an item from the list when the remove button is clicked", function() {
            // get an array of all inputs for the "Scope.Properties
            const firstKeyInput = document.querySelector(`input[id^='Scope_Properties0__Key']`);
            const firstValueInput = document.querySelector(`input[id^='Scope_Properties0__Value']`);



            const removeButton = getRemoveButtonByIndex(0);
            removeButton.click();


            const updatedfirstKeyInput = document.querySelector(`input[id^='Scope_Properties0__Key']`);
            const updatedfirstValueInput = document.querySelector(`input[id^='Scope_Properties0__Value']`);            

            
            expect(updatedfirstKeyInput.value).not.toBe(firstKeyInput.value);
            expect(updatedfirstValueInput.value).not.toBe(firstValueInput.value);


        });

        it("should reindex the items correctly after an item is removed", function() {
            // Get the remove button for the first item and trigger a click event
            const removeButton = getRemoveButtonByIndex(1);

            removeButton.click();

            // Get all the list items after the removal
            const allItems = document.querySelectorAll('#propertyContainer li');

            // Filter the items that contain inputs with the name "Scope_Properties"
            const items = Array.from(allItems   ).filter(item =>
                item.querySelector(`input[name^='Scope.Properties']`)
            );

            // Check if each item has been reindexed correctly
            items.forEach((item, index) => {
                const keyInput = item.querySelector(`input[id^='Scope_Properties'][id$='__Key']`);
                const valueInput = item.querySelector(`input[id^='Scope_Properties'][id$='__Value']`);
                const span = item.querySelector('span.field-validation-valid');
                const removeBtn = item.querySelector('button.delete');

                expect(keyInput.id).toBe(`Scope_Properties${index}__Key`);
                expect(keyInput.getAttribute('name')).toBe(`Scope.Properties[${index}].Key`);

                expect(valueInput.id).toBe(`Scope_Properties${index}__Value`);
                expect(valueInput.getAttribute('name')).toBe(`Scope.Properties[${index}].Value`);

                expect(span.getAttribute('data-valmsg-for')).toBe(`Scope.Properties[${index}].Value`);

                // Checking if the remove button's onclick attribute has been reindexed correctly
                expect(removeBtn).not.toBeNull();
                // Remove all whitespace
                const onClickValue = removeBtn.getAttribute('onclick').replace(/\s+/g, '');

                // Compare the cleaned up string with the expected value
                expect(onClickValue).toBe(`removeItem('propertyContainer','Scope.Properties',${index})`);

            });
        });


        it("should decrease the list length by one after removal", function() {
            const initialLength = document.querySelectorAll('#propertyContainer li').length;

            // Get the remove button for the first item and trigger a click event
            const removeButton = getRemoveButtonByIndex(0);
            removeButton.click();

            const finalLength = document.querySelectorAll('#propertyContainer li').length;
            expect(finalLength).toEqual(initialLength - 1);
        });
    });


    describe("Ensuring existing entries remain unchanged", function() {

        it("should not alter existing entries when a new entry is added", function() {
            const key = 'g';
            const value = 'g';

            document.getElementById('newScope_PropertiesKey').value = key;
            document.getElementById('newScope_PropertiesValue').value = value;

            const keyValueArray = getKeyValuePairsFromUL('propertyContainer');

            AddKeyValueItem('propertyContainer', 'Scope.Properties', 'newScope_PropertiesKey', 'newScope_PropertiesValue');

            const newKeyValueArray = getKeyValuePairsFromUL('propertyContainer');
            expect(newKeyValueArray.length).toBe(keyValueArray.length + 1);
            for (let i = 0; i < keyValueArray.length; i++) {
                expect(newKeyValueArray[i].key).toBe(keyValueArray[i].key);
                expect(newKeyValueArray[i].value).toBe(keyValueArray[i].value);
            }

        });

        it("should not alter existing entries when an entry is removed", function() {
            // Capture the state of existing entries before removing one
            const originalPairs = getKeyValuePairsFromUL('propertyContainer');

            // Simulate removing the second entry
            const removeButton = getRemoveButtonByIndex(1);
            removeButton.click();

            // Get the updated key-value pairs
            const updatedPairs = getKeyValuePairsFromUL('propertyContainer');

            // Ensure that the updated list is one less than the original
            expect(updatedPairs.length).toBe(originalPairs.length - 1);

            // Ensure that the removed key-value pair isn't in the updated list
            const removedPair = originalPairs[1];
            const pairFoundInUpdatedPairs = updatedPairs.find(pair => pair.key === removedPair.key && pair.value === removedPair.value);
            expect(pairFoundInUpdatedPairs).toBeUndefined();

            // Ensure all other key-value pairs remain unchanged
            originalPairs.forEach((pair, index) => {
                // Skip the removed pair
                if (index === 1) return;

                // Adjust the index for comparison, since we removed a pair
                const adjustedIndex = index > 1 ? index - 1 : index;

                expect(updatedPairs[adjustedIndex].key).toBe(pair.key);
                expect(updatedPairs[adjustedIndex].value).toBe(pair.value);
            });
        });


    });




});