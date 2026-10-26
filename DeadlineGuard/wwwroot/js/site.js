// DeadlineGuard Client Scripts

document.addEventListener('DOMContentLoaded', function () {
    // 1. Sidebar Toggle
    const sidebarToggle = document.getElementById('sidebarToggle');
    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function (e) {
            e.preventDefault();
            const wrapper = document.getElementById('sidebar-wrapper');
            if (wrapper) {
                wrapper.classList.toggle('toggled');
            }
        });
    }

    // 2. Initialize Bootstrap Toasts
    const toastElList = [].slice.call(document.querySelectorAll('.toast'));
    toastElList.map(function (toastEl) {
        const toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 5000 });
        toast.show();
    });

    // 3. Live Notification Count Check (every 45s)
    function updateNotificationCount() {
        const badge = document.getElementById('navbarNotificationBadge');
        if (!badge) return;

        fetch('/Notifications/GetUnreadCount')
            .then(res => res.json())
            .then(data => {
                if (data && typeof data.count === 'number') {
                    if (data.count > 0) {
                        badge.textContent = data.count > 99 ? '99+' : data.count;
                        badge.style.display = 'inline-block';
                    } else {
                        badge.style.display = 'none';
                    }
                }
            })
            .catch(() => { /* silent ignore for background poll */ });
    }

    if (document.getElementById('navbarNotificationBadge')) {
        updateNotificationCount();
        setInterval(updateNotificationCount, 45000);
    }

    // 4. Password Visibility Toggle
    document.querySelectorAll('.password-toggle-btn, .dg-password-toggle').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = targetId ? document.getElementById(targetId) : (this.closest('.dg-input-wrapper') || this.closest('.input-group'))?.querySelector('input');
            const icon = this.querySelector('i');
            if (input) {
                if (input.type === 'password') {
                    input.type = 'text';
                    if (icon) {
                        icon.classList.remove('bi-eye-slash', 'bi-eye');
                        icon.classList.add('bi-eye');
                    }
                } else {
                    input.type = 'password';
                    if (icon) {
                        icon.classList.remove('bi-eye', 'bi-eye-slash');
                        icon.classList.add('bi-eye-slash');
                    }
                }
            }
        });
    });

    // 5. Auth Form Submission Feedback
    const authForms = document.querySelectorAll('.auth-form, .dg-auth-form');
    authForms.forEach(function (form) {
        form.addEventListener('submit', function () {
            // Check jQuery validation if loaded
            if (window.jQuery && typeof jQuery(form).valid === 'function') {
                if (!jQuery(form).valid()) return;
            }
            const submitBtn = form.querySelector('.btn-auth-submit');
            if (submitBtn) {
                const spinner = submitBtn.querySelector('.btn-spinner');
                const text = submitBtn.querySelector('.btn-text');
                if (spinner) spinner.classList.remove('d-none');
                if (text) text.classList.add('opacity-75');
            }
        });
    });
});
