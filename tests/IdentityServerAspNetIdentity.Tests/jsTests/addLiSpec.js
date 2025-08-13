let lastMessage = "";
describe("addLiToUl", function () {

     function RemoveFunction(value) {
         lastMessage= value;
    }
    beforeEach(function () {
         lastMessage = "";

        document.querySelector('#working_area').innerHTML = `
                <ul class="list-group" id="Scope.UserClaims[]">
                    <li class="list-group-item d-flex">
                        <input type="text" id="26b678fb-362e-4076-a178-c17855937879" class="col-10" placeholder="Add new item">
                        <button type="button" class="col-2 btn btn-outline-primary float-right" onclick="addButtonPressed(this, '26b678fb-362e-4076-a178-c17855937879', 'Scope.UserClaims[]', 'addUserClaim')"><i class="bi bi-plus"></i></button>
                    </li>
                    <li class="list-group-item">
                        <input type="hidden" value="a" name="Scope.UserClaims[]">
                        <label class="col-10">a</label>
                        <button type="button" class="col-2 btn btn-outline-danger delete float-right" onclick="removeButtonPressed(this,${RemoveFunction})"><i class="bi bi-x"></i></button>
                    </li>
                    <li class="list-group-item">
                        <input type="hidden" value="b" name="Scope.UserClaims[]">
                        <label class="col-10">b</label>
                        <button type="button" class="col-2 btn btn-outline-danger delete float-right" onclick="removeButtonPressed(this,${RemoveFunction})"><i class="bi bi-x"></i></button>
                    </li>
                    <li class="list-group-item">
                        <input type="hidden" value="c" name="Scope.UserClaims[]">
                        <label class="col-10">c</label>
                        <button type="button" class="col-2 btn btn-outline-danger delete float-right" onclick="removeButtonPressed(this,${RemoveFunction})"><i class="bi bi-x"></i></button>
                    </li>
                    <li class="list-group-item">
                        <input type="hidden" value="d" name="Scope.UserClaims[]">
                        <label class="col-10">d</label>
                        <button type="button" class="col-2 btn btn-outline-danger delete float-right" onclick="removeButtonPressed(this,${RemoveFunction})"><i class="bi bi-x"></i></button>
                    </li>
                </ul>
    `;

        // Attach event listener to buttons
        // document.querySelectorAll('.delete').forEach(function(button) {
        //     button.addEventListener('click', function() {
        //         removeButtonPressed(this,RemoveFunction);
        //     });
        // });
    });

    afterEach(function () {
        // Clean up the HTML structure after each test
        document.querySelector('#working_area').innerHTML = '';
    });


    it("adds a new li element with the correct content", function () {
        var list = document.getElementById('Scope.UserClaims[]');
        var input = document.getElementById("26b678fb-362e-4076-a178-c17855937879");
        var value = "z";
        input.value = value;
        addButtonPressed(input, "26b678fb-362e-4076-a178-c17855937879", "Scope.UserClaims[]", "addUserClaim");


        var newLi = list.querySelector("li:last-child");

        // remove whitespace from newLi
        newLi.innerHTML = newLi.innerHTML.trim();

        expect(newLi.classList.contains("list-group-item")).toBeTruthy();
        // there should be an input element with the correct name and value

        expect(newLi.querySelector("input[name='Scope.UserClaims[]']").value).toBe("z");
        expect(newLi.querySelector("input[name='Scope.UserClaims[]']").type).toBe("hidden");


        expect(newLi.querySelector("label").textContent).toBe("z");
        expect(newLi.querySelector("label").classList.contains("col-10")).toBeTruthy();

        expect(newLi.querySelector("button").innerHTML).toBe("<i class=\"bi bi-x\"></i>");

        expect(newLi.querySelector("button").classList.contains("col-2")).toBeTruthy();
        expect(newLi.querySelector("button").classList.contains("btn")).toBeTruthy();
        expect(newLi.querySelector("button").classList.contains("btn-outline-danger")).toBeTruthy();
        expect(newLi.querySelector("button").classList.contains("delete")).toBeTruthy();
        expect(newLi.querySelector("button").classList.contains("float-right")).toBeTruthy();
        expect(newLi.querySelector("button").type).toBe("button");

    });


    it("alert when there is a duplicate", function () {
        // Save the original function to a variable
        var originalAlert = window.alert;

        // Mock the alert function
        window.alert = function (message) {
            window.latestAlertMessage = message; // Save the latest alert message
        };

        var value = "a";  // 'a' already exists in the list
        var input = document.getElementById("26b678fb-362e-4076-a178-c17855937879");
        input.value = value;
        addButtonPressed(input, "26b678fb-362e-4076-a178-c17855937879", "Scope.UserClaims[]", "addUserClaim");

        // Check that alert was called by checking that the latestAlertMessage was set
        expect(window.latestAlertMessage).toBe("Duplicate entry: " + value);

        // Restore the original alert function
        window.alert = originalAlert;
    });

    it("should not add a new item if it already exists", function () {
        var originalAlert = window.alert;

        // Mock the alert function
        window.alert = function (message) {
            window.latestAlertMessage = message; // Save the latest alert message
        };

        var list = document.getElementById('Scope.UserClaims[]');
        var input = document.getElementById("26b678fb-362e-4076-a178-c17855937879");
        var value = "a";  // 'a' already exists in the list
        input.value = value;
        addButtonPressed(input, "26b678fb-362e-4076-a178-c17855937879", "Scope.UserClaims[]", "addUserClaim");

        var allListItems = list.querySelectorAll("li.list-group-item");
        var newItemCount = 0;

        // Count how many times the new value appears in the list
        for (var i = 1; i < allListItems.length; i++) {
            if (allListItems[i].querySelector("input[name='Scope.UserClaims[]']").value === value) {
                newItemCount++;
            }
        }

        // Since 'a' already exists, it should only appear once
        expect(newItemCount).toBe(1);
        window.alert = originalAlert;
    });
    
    it("should not add a new item if it is empty", function () {
        var originalAlert = window.alert;
        
        // Mock the alert function
        // Mock the alert function
        window.alert = function (message) {
            window.latestAlertMessage = message; // Save the latest alert message
        };

        var list = document.getElementById('Scope.UserClaims[]');
        var initialLength = document.querySelectorAll('#propertyContainer li').length;
        var input = document.getElementById("26b678fb-362e-4076-a178-c17855937879");
        var value = "";  
        input.value = value;
        addButtonPressed(input, "26b678fb-362e-4076-a178-c17855937879", "Scope.UserClaims[]", "addUserClaim");

        const finalLength = document.querySelectorAll('#propertyContainer li').length;

        // The length should remain the same as no new item should be added
        expect(finalLength).toEqual(initialLength);

        expect(window.latestAlertMessage).toBe("Value cannot be empty.") ;
        window.alert = originalAlert; // Restore the original function


    });



        it("should remove the first data li element when its delete button is pressed", function () {
        var list = document.getElementById('Scope.UserClaims[]');
        var firstLi = list.querySelector("li:nth-child(2)");
        var deleteButton = firstLi.querySelector("button");
        deleteButton.click();

        // Check that the first li element was removed
        expect(list.querySelector("li:nth-child(2)").outerHTML).not.toEqual(firstLi.outerHTML);

        // Check that the second li element is now the first li element
        expect(list.querySelector("li:nth-child(2)").querySelector("label").textContent).toBe("b");

        // Check that the label of firstli is no longer in the list
        var allListItemsLabelsAsArray = Array.from(list.querySelectorAll("li label"));
        expect(allListItemsLabelsAsArray).not.toContain("a");

    });

    it("should remove a middle li element when its delete button is pressed", function () {
        var list = document.getElementById('Scope.UserClaims[]');
        var liToRemove = list.querySelector("li:nth-child(3)");
        var labelToRemove = liToRemove.querySelector("label").textContent;

        var deleteButton = liToRemove.querySelector("button");
        // simulate a click on the delete button
        deleteButton.click();


        // Check that the li element was removed
        expect(list.querySelector("li:nth-child(3)").outerHTML).not.toEqual(liToRemove.outerHTML);

        // Check that the label of liToRemove is no longer in the list
        var allListItemsLabelsAsArray = Array.from(list.querySelectorAll("li label"));
        expect(allListItemsLabelsAsArray).not.toContain(labelToRemove);


    });

    it("should remove the last li element when its delete button is pressed", function () {
        var list = document.getElementById('Scope.UserClaims[]');
        var lastLi = list.querySelector("li:last-child");
        var deleteButton = lastLi.querySelector("button");
        deleteButton.click();

        // Check that the last li element was removed
        expect(list.querySelector("li:last-child").outerHTML).not.toEqual(lastLi.outerHTML);
    });
    
    it("should only delete one item when its delete button is pressed", function () {
        var list = document.getElementById('Scope.UserClaims[]');
        var liToRemove = list.querySelector("li:nth-child(3)");
        var labelToRemove = liToRemove.querySelector("label").textContent;

        var listCount = list.querySelectorAll("li").length;
        var deleteButton = liToRemove.querySelector("button");
        // simulate a click on the delete button
        deleteButton.click();

        // Check that the li element was removed
        var updatedListCount = list.querySelectorAll("li").length;
        expect(updatedListCount).toBe(listCount - 1);

    });
        
    

    
    it("should pass the correct value to the passed function on the delete button onclick", function () {
       
        var list = document.getElementById('Scope.UserClaims[]');
        var liToRemove = list.querySelector("li:nth-child(3)");
        var labelToRemove = liToRemove.querySelector("label").textContent;

        var deleteButton = liToRemove.querySelector("button");

        // create a spy for RemoveFunction
        // simulate a click on the delete button
        deleteButton.click();

        // Check that the spy was called with the correct value
        expect(lastMessage).toBe( labelToRemove);
    });
});
    