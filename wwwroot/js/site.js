// CompuGear CRM - JavaScript

// =====================================================
// Sidebar State Restoration (runs immediately, no flicker)
// =====================================================
(function () {
    // Read pre-cached state from inline <script> in layout head
    var openSections = window.__sidebarOpenSections || {};
    var wasCollapsed = window.__sidebarCollapsed || false;

    function applySidebarState() {
        var sidebar = document.getElementById('sidebar');
        if (!sidebar) {
            // Sidebar not in DOM yet, retry on next frame
            requestAnimationFrame(applySidebarState);
            return;
        }

        // Restore collapsed state instantly (transitions suppressed by sidebar-loading)
        if (wasCollapsed) {
            sidebar.classList.add('collapsed');
        }

        // Expand sections from localStorage + any with active links
        var allNavItems = sidebar.querySelectorAll('.nav-item');
        allNavItems.forEach(function (navItem) {
            var textEl = navItem.querySelector('.nav-item-text');
            var key = textEl ? textEl.textContent.trim() : '';

            if (key && openSections[key] === true) {
                navItem.classList.add('expanded');
            }
            if (navItem.querySelector('.nav-submenu .nav-link.active')) {
                navItem.classList.add('expanded');
            }
        });

        // Double-rAF: wait for the browser to paint the restored state,
        // THEN re-enable CSS transitions by removing sidebar-loading.
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                document.body.classList.remove('sidebar-loading');
                var preloadStyle = document.getElementById('sidebar-preload-style');
                if (preloadStyle) preloadStyle.remove();
            });
        });
    }

    // Start as soon as possible
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', applySidebarState);
    } else {
        applySidebarState();
    }
})();

document.addEventListener('DOMContentLoaded', function () {
    // Sidebar Toggle (collapse/expand entire sidebar)
    const sidebar = document.getElementById('sidebar');
    const sidebarToggle = document.getElementById('sidebarToggle');

    if (sidebarToggle) {
        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('collapsed');
            localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
            // Close all submenus when collapsing
            if (sidebar.classList.contains('collapsed')) {
                document.querySelectorAll('.nav-item.expanded').forEach(function(item) {
                    item.classList.remove('expanded');
                });
            }
        });
    }

    // Add data-title attributes for collapsed tooltips
    document.querySelectorAll('.sidebar-nav .nav-item > .nav-link').forEach(function(link) {
        var textEl = link.querySelector('.nav-item-text');
        if (textEl) {
            link.setAttribute('data-title', textEl.textContent.trim());
        }
    });
    // Also add to sidebar-footer links
    document.querySelectorAll('.sidebar-footer .nav-link').forEach(function(link) {
        var textEl = link.querySelector('.nav-item-text');
        if (textEl) {
            link.setAttribute('data-title', textEl.textContent.trim());
        }
    });

    // ---- Submenu Toggle with localStorage ----
    function getOpenSections() {
        try {
            return JSON.parse(localStorage.getItem('sidebarOpenSections') || '{}');
        } catch (e) { return {}; }
    }

    function saveOpenSections(sections) {
        localStorage.setItem('sidebarOpenSections', JSON.stringify(sections));
    }

    function getSectionKey(navItem) {
        var textEl = navItem.querySelector('.nav-item-text');
        return textEl ? textEl.textContent.trim() : '';
    }

    // Persist active section so the NEXT page load keeps it open
    var openSections = getOpenSections();
    document.querySelectorAll('.nav-item').forEach(function (navItem) {
        if (navItem.querySelector('.nav-submenu .nav-link.active')) {
            var key = getSectionKey(navItem);
            if (key) { openSections[key] = true; }
        }
    });
    saveOpenSections(openSections);

    // Handle toggle clicks - when collapsed, navigate to first submenu link
    document.querySelectorAll('.nav-link-toggle').forEach(function (toggle) {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            
            // If sidebar is collapsed, navigate to the first submenu item
            if (sidebar && sidebar.classList.contains('collapsed')) {
                var submenu = this.closest('.nav-item').querySelector('.nav-submenu');
                if (submenu) {
                    var firstLink = submenu.querySelector('.nav-link');
                    if (firstLink && firstLink.href) {
                        window.location.href = firstLink.href;
                        return;
                    }
                }
            }
            
            var navItem = this.closest('.nav-item');
            navItem.classList.toggle('expanded');

            var key = getSectionKey(navItem);
            if (key) {
                var sections = getOpenSections();
                sections[key] = navItem.classList.contains('expanded');
                saveOpenSections(sections);
            }
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

    // Initialize Dark Mode
    initDarkMode();
    
    // Initialize Notifications
    initNotifications();
});

// =====================================================
// Dark Mode System
// =====================================================

function initDarkMode() {
    const darkModeBtn = document.getElementById('darkModeToggle') || document.querySelector('.topbar-btn[title="Dark Mode"]');
    
    // Restore dark mode preference
    const isDarkMode = localStorage.getItem('darkMode') === 'true';
    if (isDarkMode) {
        document.documentElement.classList.add('dark-mode');
        document.body.classList.add('dark-mode');
        updateDarkModeIcon(true);
    }
    
    if (darkModeBtn) {
        darkModeBtn.addEventListener('click', toggleDarkMode);
    }
}

function toggleDarkMode() {
    const isDark = document.body.classList.toggle('dark-mode');
    document.documentElement.classList.toggle('dark-mode', isDark);
    localStorage.setItem('darkMode', isDark);
    updateDarkModeIcon(isDark);
    
    // Show toast notification
    if (typeof Toast !== 'undefined') {
        Toast.info(isDark ? 'Dark mode enabled' : 'Light mode enabled');
    }
}

function updateDarkModeIcon(isDark) {
    const darkModeBtn = document.getElementById('darkModeToggle') || document.querySelector('.topbar-btn[title="Dark Mode"]');
    if (darkModeBtn) {
        darkModeBtn.innerHTML = isDark 
            ? `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <circle cx="12" cy="12" r="5"/><line x1="12" y1="1" x2="12" y2="3"/><line x1="12" y1="21" x2="12" y2="23"/>
                <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"/><line x1="18.36" y1="18.36" x2="19.78" y2="19.78"/>
                <line x1="1" y1="12" x2="3" y2="12"/><line x1="21" y1="12" x2="23" y2="12"/>
                <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"/><line x1="18.36" y1="5.64" x2="19.78" y2="4.22"/>
               </svg>`
            : `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/>
               </svg>`;
    }
}

// =====================================================
// Notification System (Real-time)
// =====================================================

const LOW_STOCK_THRESHOLD = 15;
let notifications = [];
let notificationSocket = null;

async function initNotifications() {
    await loadNotifications();
    // Refresh notifications every 15 seconds for real-time updates
    setInterval(loadNotifications, 120000);
    
    // Play notification sound for new alerts
    setupNotificationSound();
}

function setupNotificationSound() {
    // Create audio element for notification sound
    window.notificationSound = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1tcGNqbnFqb3N1dHZ3d3Z2dnZ2d3h4d3d3d3d2dXV0dHNycXBvb25tbGtqaWhnZmVkY2JhYF9eXVxbWllYV1ZVVFNSUVBPTk1MS0pJSEdGRURDQkFAP0A/QD9APz8/Pz8/Pz8/Pz8/Pz8/Pz8/');
}

async function loadNotifications() {
    try {
        const response = await fetch('/api/stock-alerts');
        if (!response.ok) return;
        
        const result = await response.json();
        const products = result.data || [];
        
        // Get low stock products (threshold 15)
        const lowStockProducts = products; // Already filtered server-side

        const previousCount = notifications.length;
        const readMap = new Map((notifications || []).map(n => [n.productId, !!n.read]));

        notifications = lowStockProducts.map(p => ({
            id: p.productId,
            productId: p.productId,
            type: p.stockQuantity === 0 ? 'danger' : p.stockQuantity <= 5 ? 'warning' : 'info',
            title: p.stockQuantity === 0 ? 'Out of Stock' : p.stockQuantity <= 5 ? 'Critical Stock' : 'Low Stock',
            message: `${p.productName} has only ${p.stockQuantity} units left`,
            productName: p.productName,
            stockQuantity: p.stockQuantity,
            sku: p.sku || 'N/A',
            time: 'Just now',
            read: readMap.get(p.productId) === true
        }));
        
        // Play sound if new notifications
        if (notifications.length > previousCount && previousCount > 0) {
            playNotificationSound();
        }
        
        updateNotificationUI();
        
        // Update any page-specific notification badges
        updatePageNotificationBadges(lowStockProducts.length);
        
    } catch (error) {
        console.error('Failed to load notifications:', error);
    }
}

function playNotificationSound() {
    if (window.notificationSound) {
        window.notificationSound.play().catch(() => {});
    }
}

function updatePageNotificationBadges(count) {
    // Update any inventory-related badges on the page
    const inventoryBadges = document.querySelectorAll('[data-notification-type="inventory"]');
    inventoryBadges.forEach(badge => {
        badge.textContent = count;
        badge.style.display = count > 0 ? 'inline-flex' : 'none';
    });
}

function updateNotificationUI() {
    const badge = document.getElementById('notificationBadge');
    const list = document.getElementById('notificationList');
    
    if (!badge || !list) return;
    
    const notificationCount = notifications.length;
    
    // Update badge with animation
    if (notificationCount > 0) {
        badge.textContent = notificationCount > 99 ? '99+' : notificationCount;
        badge.style.display = 'flex';
        badge.classList.add('pulse');
        setTimeout(() => badge.classList.remove('pulse'), 1000);
    } else {
        badge.style.display = 'none';
    }
    
    // Update notification list
    if (notifications.length === 0) {
        list.innerHTML = `
            <div class="text-center py-4 text-muted">
                <svg width="32" height="32" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" class="mb-2">
                    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"/>
                    <path d="M13.73 21a2 2 0 0 1-3.46 0"/>
                </svg>
                <p class="mb-0 small">All products are well stocked!</p>
            </div>
        `;
    } else {
        list.innerHTML = notifications.slice(0, 10).map(n => {
            const iconColor = n.type === 'danger' ? '#dc3545' : n.type === 'warning' ? '#ff6b35' : '#008080';
            const bgColor = n.type === 'danger' ? '#f8d7da' : n.type === 'warning' ? '#fff3cd' : '#d1e7dd';
            
            return `
                <a href="/Inventory/Alerts" class="dropdown-item notification-item py-2 px-3 ${n.read ? 'read' : ''}" style="white-space: normal;">
                    <div class="d-flex align-items-start gap-2">
                        <div class="notification-icon flex-shrink-0" style="width: 36px; height: 36px; border-radius: 50%; background: ${bgColor}; display: flex; align-items: center; justify-content: center;">
                            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="${iconColor}" stroke-width="2">
                                <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/>
                                <line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>
                            </svg>
                        </div>
                        <div class="flex-grow-1">
                            <div class="fw-semibold small" style="color: ${iconColor};">${n.title}</div>
                            <div class="small text-muted">${n.message}</div>
                            <div class="small text-muted mt-1">${n.time}</div>
                        </div>
                    </div>
                </a>
            `;
        }).join('');
    }
}

function markAllRead() {
    notifications.forEach(n => n.read = true);
    updateNotificationUI();
    
    if (typeof Toast !== 'undefined') {
        Toast.success('All notifications marked as read');
    }
}

// Global notification function that can be called from any view
function addNotification(title, message, type = 'info') {
    notifications.unshift({
        id: Date.now(),
        type: type,
        title: title,
        message: message,
        time: 'Just now',
        read: false
    });
    updateNotificationUI();
    playNotificationSound();
}

// =====================================================
// GLOBAL TOAST NOTIFICATION SYSTEM
// Unified across entire system — no need for per-page definitions
// =====================================================
function showToast(message, type) {
    type = type || 'info';

    // Ensure container exists
    var container = document.querySelector('.toast-container.position-fixed');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container position-fixed';
        container.setAttribute('aria-live', 'polite');
        container.setAttribute('aria-atomic', 'true');
        document.body.appendChild(container);
    }

    var icons = {
        success: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>',
        error: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>',
        warning: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
        info: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>',
        danger: '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>'
    };

    var titles = { success: 'Success', error: 'Error', warning: 'Warning', info: 'Info', danger: 'Error' };
    var toastType = type === 'danger' ? 'error' : type;
    var cssClass = 'toast-' + type;

    var toastEl = document.createElement('div');
    toastEl.className = 'toast ' + cssClass;
    toastEl.setAttribute('role', 'alert');
    toastEl.innerHTML =
        '<div class="toast-header">' +
            (icons[type] || icons.info) +
            '<strong class="ms-2 me-auto">' + (titles[type] || 'Notice') + '</strong>' +
            '<button type="button" class="btn-close btn-close-sm" data-bs-dismiss="toast" aria-label="Close"></button>' +
        '</div>' +
        '<div class="toast-body">' + message + '</div>';

    container.appendChild(toastEl);

    // Use Bootstrap Toast if available, else manual
    if (typeof bootstrap !== 'undefined' && bootstrap.Toast) {
        var bsToast = new bootstrap.Toast(toastEl, { delay: 4000 });
        bsToast.show();
        toastEl.addEventListener('hidden.bs.toast', function() { toastEl.remove(); });
    } else {
        toastEl.style.display = 'block';
        toastEl.style.opacity = '1';
        setTimeout(function() {
            toastEl.style.transition = 'opacity 0.3s';
            toastEl.style.opacity = '0';
            setTimeout(function() { toastEl.remove(); }, 350);
        }, 4000);
    }
}

// Global Toast object — replaces per-page duplicates
var Toast = {
    success: function(msg) { showToast(msg, 'success'); },
    error: function(msg) { showToast(msg, 'error'); },
    warning: function(msg) { showToast(msg, 'warning'); },
    info: function(msg) { showToast(msg, 'info'); }
};

// Backward-compatible aliases used by layout showSuccess/showError
function showSuccess(msg) { showToast(msg, 'success'); }
function showError(msg) { showToast(msg, 'error'); }

// =====================================================
// Universal Table Pagination
// =====================================================
// Usage: call initPagination(tbodyId, footerId, pageSize) after rendering rows
// It reads all <tr> in the tbody, shows only `pageSize` at a time, and renders
// Prev/Next controls into the footer container.  Call it again after re-rendering.

function initPagination(tbodyId, footerId, pageSize) {
    pageSize = pageSize || 10;
    var tbody = document.getElementById(tbodyId);
    var footer = document.getElementById(footerId);
    if (!tbody || !footer) return;

    var rows = Array.from(tbody.querySelectorAll('tr'));
    var total = rows.length;

    // If only 1 row and it has a colspan (empty-state row), hide pagination
    if (total <= 1 && rows[0] && rows[0].querySelector('td[colspan]')) {
        footer.innerHTML = '';
        return;
    }

    var totalPages = Math.ceil(total / pageSize);
    var currentPage = parseInt(footer.dataset.currentPage) || 1;
    if (currentPage > totalPages) currentPage = totalPages;
    if (currentPage < 1) currentPage = 1;
    footer.dataset.currentPage = currentPage;

    // Show / hide rows
    rows.forEach(function(tr, i) {
        var start = (currentPage - 1) * pageSize;
        tr.style.display = (i >= start && i < start + pageSize) ? '' : 'none';
    });

    // Render controls
    var startItem = (currentPage - 1) * pageSize + 1;
    var endItem = Math.min(currentPage * pageSize, total);

    if (total <= pageSize) {
        footer.innerHTML = '<span class="text-muted" style="font-size:0.85rem;">Showing ' + total + ' of ' + total + ' entries</span>';
        return;
    }

    footer.innerHTML =
        '<span class="text-muted" style="font-size:0.85rem;">Showing ' + startItem + '–' + endItem + ' of ' + total + ' entries</span>' +
        '<div class="d-flex gap-2">' +
            '<button class="btn btn-sm btn-outline-secondary pg-prev"' + (currentPage <= 1 ? ' disabled' : '') + '>← Prev</button>' +
            '<button class="btn btn-sm btn-outline-secondary pg-next"' + (currentPage >= totalPages ? ' disabled' : '') + '>Next →</button>' +
        '</div>';

    var prevBtn = footer.querySelector('.pg-prev');
    var nextBtn = footer.querySelector('.pg-next');
    if (prevBtn) prevBtn.onclick = function() { footer.dataset.currentPage = currentPage - 1; initPagination(tbodyId, footerId, pageSize); };
    if (nextBtn) nextBtn.onclick = function() { footer.dataset.currentPage = currentPage + 1; initPagination(tbodyId, footerId, pageSize); };
}

// Reset pagination to page 1 (call before re-rendering rows)
function resetPagination(footerId) {
    var footer = document.getElementById(footerId);
    if (footer) footer.dataset.currentPage = '1';
}

// Auto-paginate: watch a tbody for content changes and apply pagination automatically
// Usage: autoPaginate('myTableBody', 'myPagination', 10)
function autoPaginate(tbodyId, footerId, pageSize) {
    pageSize = pageSize || 10;
    var tbody = document.getElementById(tbodyId);
    if (!tbody) return;
    var observer = new MutationObserver(function() {
        initPagination(tbodyId, footerId, pageSize);
    });
    observer.observe(tbody, { childList: true });
    // Run once immediately if tbody already has content
    if (tbody.children.length > 0) initPagination(tbodyId, footerId, pageSize);
}

// =====================================================
// GLOBAL PDF EXPORT UTILITY
// Usage: exportTableToPDF('Report Title', 'tableBodyId', ['Col1','Col2',...], [0,1,2,...colIndexes])
// Or: exportDataToPDF('Report Title', headers, rows)
// =====================================================

function exportTableToPDF(title, tbodyId, headers, colIndexes) {
    var tbody = document.getElementById(tbodyId);
    if (!tbody) {
        showToast('No data to export', 'warning');
        return;
    }
    var allRows = tbody.querySelectorAll('tr');
    if (allRows.length === 0) {
        showToast('No data to export', 'warning');
        return;
    }

    var rows = [];
    allRows.forEach(function(tr) {
        if (tr.style.display === 'none') return;
        var cells = tr.querySelectorAll('td');
        var row = [];
        if (colIndexes && colIndexes.length > 0) {
            colIndexes.forEach(function(i) {
                row.push(cells[i] ? cells[i].textContent.trim() : '');
            });
        } else {
            // Use all columns except the last one (actions)
            for (var i = 0; i < cells.length - 1; i++) {
                row.push(cells[i].textContent.trim());
            }
        }
        rows.push(row);
    });

    exportDataToPDF(title, headers, rows);
}

function exportDataToPDF(title, headers, rows) {
    var now = new Date();
    var dateStr = now.toLocaleDateString('en-PH', { year: 'numeric', month: 'long', day: 'numeric' });
    var timeStr = now.toLocaleTimeString('en-PH', { hour: '2-digit', minute: '2-digit' });

    var html = '<!DOCTYPE html><html><head><meta charset="utf-8"><title>' + title + '</title>';
    html += '<style>';
    html += 'body { font-family: "Segoe UI", Arial, sans-serif; margin: 0; padding: 30px; color: #1e293b; }';
    html += '.report-header { text-align: center; margin-bottom: 30px; padding-bottom: 20px; border-bottom: 3px solid #008080; }';
    html += '.report-header img { width: 60px; height: 60px; margin-bottom: 10px; }';
    html += '.report-header h1 { font-size: 22px; color: #008080; margin: 5px 0; }';
    html += '.report-header p { font-size: 12px; color: #64748b; margin: 3px 0; }';
    html += 'table { width: 100%; border-collapse: collapse; margin-top: 15px; font-size: 11px; }';
    html += 'th { background: #008080; color: white; padding: 10px 8px; text-align: left; font-weight: 600; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px; }';
    html += 'td { padding: 8px; border-bottom: 1px solid #e2e8f0; color: #334155; }';
    html += 'tr:nth-child(even) { background: #f8fafc; }';
    html += 'tr:hover { background: #f0fdfa; }';
    html += '.report-footer { text-align: center; margin-top: 30px; padding-top: 15px; border-top: 1px solid #e2e8f0; font-size: 10px; color: #94a3b8; }';
    html += '.summary-row { font-weight: 700; background: #f0fdfa !important; }';
    html += '@media print { body { padding: 15px; } @page { margin: 1cm; size: landscape; } }';
    html += '</style></head><body>';

    html += '<div class="report-header">';
    html += '<img src="' + window.location.origin + '/images/compugear-logo-v7.png" alt="CompuGear">';
    html += '<h1>' + title + '</h1>';
    html += '<p>Generated on ' + dateStr + ' at ' + timeStr + '</p>';
    html += '<p>CompuGear Enterprise Resource Planning System</p>';
    html += '</div>';

    html += '<table><thead><tr>';
    headers.forEach(function(h) { html += '<th>' + h + '</th>'; });
    html += '</tr></thead><tbody>';

    rows.forEach(function(row) {
        html += '<tr>';
        row.forEach(function(cell) { html += '<td>' + cell + '</td>'; });
        html += '</tr>';
    });

    html += '</tbody></table>';

    html += '<div class="report-footer">';
    html += '<p>Total Records: ' + rows.length + '</p>';
    html += '<p>\u00A9 ' + now.getFullYear() + ' CompuGear ERP — Confidential</p>';
    html += '</div>';
    html += '</body></html>';

    // Direct download via hidden iframe - no redirect
    var iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.right = '0';
    iframe.style.bottom = '0';
    iframe.style.width = '0';
    iframe.style.height = '0';
    iframe.style.border = 'none';
    document.body.appendChild(iframe);

    var doc = iframe.contentWindow.document;
    doc.open();
    doc.write(html);
    doc.close();

    iframe.contentWindow.onload = function() {
        setTimeout(function() {
            iframe.contentWindow.focus();
            iframe.contentWindow.print();
            setTimeout(function() {
                document.body.removeChild(iframe);
            }, 1000);
        }, 500);
    };

    showToast('PDF report generated — use Save as PDF in the print dialog', 'success');
}

// Export stat cards data to PDF
function exportStatsToPDF(title, statsData) {
    var headers = ['Metric', 'Value'];
    var rows = [];
    statsData.forEach(function(s) {
        rows.push([s.label, s.value]);
    });
    exportDataToPDF(title, headers, rows);
}