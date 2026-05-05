define(["loading", "globalize", "emby-input", "emby-button", "emby-select"], function (loading, globalize) {
    "use strict";

    function populateUsers(config) {
        var usersList = document.getElementById("usersList");
        var userSelect = document.getElementById("userSelect");

        ApiClient.getUsers().then(function (users) {
            userSelect.innerHTML = '<option value="">Select a user...</option>';
            users.forEach(function (user) {
                var option = document.createElement("option");
                option.value = user.Id;
                option.textContent = user.Name;
                userSelect.appendChild(option);
            });
        });

        if (!config.PopfeedUsers || config.PopfeedUsers.length === 0) {
            usersList.innerHTML = "<p>No users configured.</p>";
            return;
        }

        usersList.innerHTML = "";
        config.PopfeedUsers.forEach(function (user, index) {
            ApiClient.getUser(user.LinkedMbUserId).then(function (jellyfinUser) {
                var tvOptions = [];
                if (user.PostEachEpisode) {
                    tvOptions.push("Post each episode");
                }
                if (user.PostOnSeasonComplete) {
                    tvOptions.push("Post on season complete");
                }
                var tvOptionsText = tvOptions.length > 0 ? tvOptions.join(", ") : "None";

                var userItem = document.createElement("div");
                userItem.className = "user-item";
                userItem.innerHTML = `
                    <div>
                        <strong>${jellyfinUser.Name}</strong><br>
                        <small>Bluesky: ${user.BlueskyHandle || "Not configured"}</small><br>
                        <small>Mark on Popfeed: ${user.MarkWatchedOnPopfeed ? "Yes" : "No"}</small><br>
                        <small>TV Options: ${tvOptionsText}</small><br>
                        <small>Post to Bluesky: ${user.PostToBluesky ? "Yes" : "No"}</small>
                    </div>
                    <button class="delete-btn" data-index="${index}">Delete</button>
                `;
                usersList.appendChild(userItem);
            });
        });

        document.querySelectorAll(".delete-btn").forEach(function (btn) {
            btn.addEventListener("click", function () {
                var index = parseInt(this.getAttribute("data-index"));
                deleteUser(index);
            });
        });
    }

    function saveUser(formData) {
        var config = Dashboard.getPluginConfig();
        var users = config.PopfeedUsers || [];

        var newUser = {
            LinkedMbUserId: formData.get("userSelect"),
            BlueskyHandle: document.getElementById("blueskyHandle").value,
            BlueskyAppPassword: document.getElementById("blueskyAppPassword").value,
            MarkWatchedOnPopfeed: document.getElementById("markWatchedOnPopfeed").checked,
            PostEachEpisode: document.getElementById("postEachEpisode").checked,
            PostOnSeasonComplete: document.getElementById("postOnSeasonComplete").checked,
            PostToBluesky: document.getElementById("postToBluesky").checked,
            ExtraLogging: document.getElementById("extraLogging").checked
        };

        users.push(newUser);
        config.PopfeedUsers = users;

        ApiClient.updatePluginConfiguration("a1b2c3d4-e5f6-7890-abcd-ef1234567890", config).then(function () {
            Dashboard.navigate("pluginmanager");
        });
    }

    function deleteUser(index) {
        var config = Dashboard.getPluginConfig();
        config.PopfeedUsers.splice(index, 1);
        ApiClient.updatePluginConfiguration("a1b2c3d4-e5f6-7890-abcd-ef1234567890", config).then(function () {
            populateUsers(config);
        });
    }

    function testConnection() {
        var blueskyHandle = document.getElementById("blueskyHandle").value;
        var blueskyAppPassword = document.getElementById("blueskyAppPassword").value;
        var resultDiv = document.getElementById("testResult");

        if (!blueskyHandle || !blueskyAppPassword) {
            resultDiv.className = "test-result error";
            resultDiv.textContent = "Please enter Bluesky handle and app password";
            return;
        }

        resultDiv.className = "test-result";
        resultDiv.textContent = "Testing connection...";

        var testUser = {
            BlueskyHandle: blueskyHandle,
            BlueskyAppPassword: blueskyAppPassword
        };

        var xhr = new XMLHttpRequest();
        xhr.open("POST", "https://atproto.com/xrpc/com.atproto.server.createSession", true);
        xhr.setRequestHeader("Content-Type", "application/json");
        xhr.onload = function() {
            if (xhr.status === 200) {
                resultDiv.className = "test-result success";
                resultDiv.textContent = "Connection successful!";
            } else {
                resultDiv.className = "test-result error";
                resultDiv.textContent = "Connection failed. Please check your credentials.";
            }
        };
        xhr.onerror = function() {
            resultDiv.className = "test-result error";
            resultDiv.textContent = "Connection failed. Network error.";
        };
        xhr.send(JSON.stringify(testUser));
    }

    function loadConfig() {
        ApiClient.getPluginConfiguration("a1b2c3d4-e5f6-7890-abcd-ef1234567890").then(function (config) {
            populateUsers(config);
        });
    }

    function init() {
        var form = document.getElementById("popfeedUserForm");
        var testBtn = document.getElementById("testConnectionBtn");

        form.addEventListener("submit", function (e) {
            e.preventDefault();

            var config = Dashboard.getPluginConfig();
            var users = config.PopfeedUsers || [];
            var userSelect = document.getElementById("userSelect").value;

            var userExists = users.some(function (u) {
                return u.LinkedMbUserId === userSelect;
            });

            if (userExists) {
                alert("This user is already configured. Delete the existing configuration first.");
                return;
            }

            saveUser();
        });

        testBtn.addEventListener("click", testConnection);

        loadConfig();
    }

    document.addEventListener("domready", function () {
        init();
    });
});