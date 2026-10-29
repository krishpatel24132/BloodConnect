document.addEventListener('DOMContentLoaded', function () {
    // Dark / light theme toggle, remembered in a cookie (read by the server in _Layout)
    var btn = document.getElementById('themeToggle');
    if (btn) {
        btn.addEventListener('click', function () {
            var html = document.documentElement;
            var next = html.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
            html.setAttribute('data-bs-theme', next);
            document.cookie = 'theme=' + next + '; path=/; max-age=31536000; SameSite=Lax';
        });
    }

    // Mobile number fields accept digits only
    document.querySelectorAll('input[inputmode="numeric"]').forEach(function (input) {
        input.addEventListener('input', function () {
            input.value = input.value.replace(/\D/g, '');
        });
    });

    // Stop double submits: disable the submit button once a valid POST form is sent
    document.querySelectorAll('form[method="post"]').forEach(function (form) {
        form.addEventListener('submit', function () {
            if (window.jQuery && jQuery.fn.valid && !jQuery(form).valid()) { return; }
            var submit = form.querySelector('button[type="submit"]');
            if (submit) {
                submit.disabled = true;
                submit.dataset.label = submit.innerHTML;
                submit.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Please wait...';
            }
        });
    });

    // Back button: put the buttons back to normal
    window.addEventListener('pageshow', function (e) {
        if (!e.persisted) { return; }
        document.querySelectorAll('button[type="submit"][data-label]').forEach(function (b) {
            b.disabled = false;
            b.innerHTML = b.dataset.label;
        });
    });

    // Auto-dismiss flash messages after 5 seconds
    setTimeout(function () {
        document.querySelectorAll('.alert-dismissible').forEach(function (a) {
            if (window.bootstrap) { bootstrap.Alert.getOrCreateInstance(a).close(); }
        });
    }, 5000);
});
