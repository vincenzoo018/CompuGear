// CompuGear CRM - JavaScript

document.addEventListener('DOMContentLoaded', function () {
    // Sidebar Toggle
    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('collapsed');
            // Save state to localStorage
            localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
        });

        // Restore sidebar state
        if (localStorage.getItem('sidebarCollapsed') === 'true') {
            sidebar.classList.add('collapsed');
        }
    }

    // Submenu Toggle
    const navToggles = document.querySelectorAll('.nav-link-toggle');
    navToggles.forEach(function (toggle) {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            const navItem = this.closest('.nav-item');
            
            // Close other expanded items (optional - for accordion behavior)
            // const allItems = document.querySelectorAll('.nav-item.expanded');
            // allItems.forEach(item => {
            //     if (item !== navItem) item.classList.remove('expanded');
            // });

            navItem.classList.toggle('expanded');
        });
    });

    // Mobile menu toggle
    const mobileMenuBtn = document.querySelector('.mobile-menu-btn');
    if (mobileMenuBtn) {
        mobileMenuBtn.addEventListener('click', function () {
            sidebar.classList.toggle('mobile-open');
        });
    }

    // Close sidebar on mobile when clicking outside
    document.addEventListener('click', function (e) {
        if (window.innerWidth <= 768) {
            if (!sidebar.contains(e.target) && !e.target.closest('.mobile-menu-btn')) {
                sidebar.classList.remove('mobile-open');
            }
        }
    });

    // Search functionality
    const searchInputs = document.querySelectorAll('.search-input input, .topbar-search input');
    searchInputs.forEach(function (input) {
        input.addEventListener('input', function () {
            const searchTerm = this.value.toLowerCase();
            const table = this.closest('.card')?.querySelector('.data-table');
            if (table) {
                const rows = table.querySelectorAll('tbody tr');
                rows.forEach(function (row) {
                    const text = row.textContent.toLowerCase();
                    row.style.display = text.includes(searchTerm) ? '' : 'none';
                });
            }
        });
    });

    // Action button dropdown (simple toggle)
    const actionBtns = document.querySelectorAll('.action-btn');
    actionBtns.forEach(function (btn) {
        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            // You can add dropdown menu functionality here
            console.log('Action clicked');
        });
    });

    // Add animation class to cards on scroll
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    document.querySelectorAll('.card, .stat-card').forEach(function (el) {
        observer.observe(el);
    });
});

