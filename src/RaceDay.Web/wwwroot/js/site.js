(function () {
    "use strict";

    document.querySelectorAll("[data-image-input]").forEach(function (input) {
        var preview = document.getElementById(input.getAttribute("data-image-input"));
        var maxBytes = parseInt(input.getAttribute("data-max-bytes") || "5242880", 10);
        var zone = input.closest(".rd-upload");

        input.addEventListener("change", function () {
            var file = input.files && input.files[0];
            input.setCustomValidity("");
            if (!file) {
                return;
            }
            if (!/^image\/(jpeg|png|gif|webp)$/.test(file.type)) {
                input.setCustomValidity("Choose a JPEG, PNG, GIF or WebP image.");
            } else if (file.size > maxBytes) {
                input.setCustomValidity("The image must be " + Math.round(maxBytes / 1048576) + " MB or smaller.");
            }
            input.reportValidity();
            if (preview && !input.validationMessage) {
                preview.src = URL.createObjectURL(file);
                preview.classList.remove("d-none");
            }
        });

        if (zone) {
            ["dragenter", "dragover"].forEach(function (name) {
                zone.addEventListener(name, function () { zone.classList.add("is-dragging"); });
            });
            ["dragleave", "drop"].forEach(function (name) {
                zone.addEventListener(name, function () { zone.classList.remove("is-dragging"); });
            });
        }
    });

    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            if (!window.confirm(form.getAttribute("data-confirm"))) {
                event.preventDefault();
            }
        });
    });

    document.querySelectorAll("form[method='post']").forEach(function (form) {
        form.addEventListener("submit", function () {
            if (window.jQuery && window.jQuery(form).valid && !window.jQuery(form).valid()) {
                return;
            }
            form.querySelectorAll("button[type='submit']").forEach(function (button) {
                window.setTimeout(function () { button.disabled = true; }, 0);
            });
        });
    });

    var province = document.querySelector("[data-province-select]");
    if (province) {
        var centres = {
            "Eastern Cape": [-32.2968, 26.4194], "Free State": [-28.4541, 26.7968], "Gauteng": [-26.2041, 28.0473],
            "KwaZulu-Natal": [-29.8587, 31.0218], "Limpopo": [-23.9045, 29.4689], "Mpumalanga": [-25.4753, 30.9694],
            "North West": [-25.6551, 25.6066], "Northern Cape": [-28.7282, 24.7499], "Western Cape": [-33.9249, 18.4241]
        };
        var lat = document.getElementById(province.getAttribute("data-lat"));
        var lng = document.getElementById(province.getAttribute("data-lng"));
        province.addEventListener("change", function () {
            var centre = centres[province.value];
            if (centre && lat && lng && !lat.value && !lng.value) {
                lat.value = centre[0].toFixed(6);
                lng.value = centre[1].toFixed(6);
            }
        });
    }

    document.querySelectorAll("[data-demo-email]").forEach(function (button) {
        button.addEventListener("click", function () {
            var email = document.getElementById("Email");
            var password = document.getElementById("Password");
            if (email && password) {
                email.value = button.getAttribute("data-demo-email");
                password.value = button.getAttribute("data-demo-password");
                password.focus();
            }
        });
    });
})();
