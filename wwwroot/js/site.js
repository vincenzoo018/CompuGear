// CompuGear CRM - JavaScript

// Immediately apply sidebar state from preloaded data (before DOMContentLoaded)
(function() {
    var openSections = window.__sidebarOpenSections || {};
    
    // Wait for sidebar to exist, then apply state immediately
    function applySidebarState() {
        var sidebar = document.getElementById('sidebar');
        if (!sidebar) {
            // Sidebar not in DOM yet, try again on next frame
            requestAnimationFrame(applySidebarState);
            return;
        }
        
        // Restore collapsed state
        if (localStorage.getItem('sidebarCollapsed') === 'true') {
            sidebar.classList.add('collapsed');
        }
        
        // Apply expanded classes to sections based on stored state
        var allNavItems = sidebar.querySelectorAll('.nav-item');
        allNavItems.forEach(function(navItem) {
            var textEl = navItem.querySelector('.nav-item-text');
            var key = textEl ? textEl.textContent.trim() : '';
            
            // If localStorage says this section should be open, expand it
            if (key && openSections[key] === true) {
                navItem.classList.add('expanded');
            }
            // Also expand if it contains the active link
            if (navItem.querySelector('.nav-submenu .nav-link.active')) {
                navItem.classList.add('expanded');
            }
        });
        
        // Remove loading state and re-enable transitions
        document.body.classList.remove('sidebar-loading');
        var preloadStyle = document.getElementById('sidebar-preload-style');
        if (preloadStyle) preloadStyle.remove();
    }
    
    // Start checking immediately
    if (document.readyState === 'loading') {
        // DOM not ready yet, wait for it
        document.addEventListener('DOMContentLoaded', applySidebarState);
    } else {
        applySidebarState();
    }
})();

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

    // Submenu Toggle with localStorage persistence
    const navToggles = document.querySelectorAll('.nav-link-toggle');
    
    // Get stored open sections (or empty object)
    function getOpenSections() {
        try {
            return JSON.parse(localStorage.getItem('sidebarOpenSections') || '{}');
        } catch (e) { return {}; }
    }
    
    function saveOpenSections(sections) {
        localStorage.setItem('sidebarOpenSections', JSON.stringify(sections));
    }
    
    // Build a key from the nav-item's toggle text
    function getSectionKey(navItem) {
        var textEl = navItem.querySelector('.nav-item-text');
        return textEl ? textEl.textContent.trim() : '';
    }
    
    // Sidebar state was already restored by the immediate script above
    // Just update localStorage if active sections changed
    var openSections = getOpenSections();
    var allNavItems = document.querySelectorAll('.nav-item');
    allNavItems.forEach(function (navItem) {
        // Keep sections expanded if they contain the active link
        if (navItem.querySelector('.nav-submenu .nav-link.active')) {
            var key = getSectionKey(navItem);
            if (key) { openSections[key] = true; }
        }
    });
    saveOpenSections(openSections);
    
    // Handle toggle clicks and persist state
    navToggles.forEach(function (toggle) {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            const navItem = this.closest('.nav-item');
            navItem.classList.toggle('expanded');
            
            // Save state
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
    setInterval(loadNotifications, 15000);
    
    // Play notification sound for new alerts
    setupNotificationSound();
}

function setupNotificationSound() {
    // Create audio element for notification sound
    window.notificationSound = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1tcGNqbnFqb3N1dHZ3d3Z2dnZ2d3h4d3d3d3d2dXV0dHNycXBvb25tbGtqaWhnZmVkY2JhYF9eXVxbWllYV1ZVVFNSUVBPTk1MS0pJSEdGRURDQkFAP0A/QD9APz8/Pz8/Pz8/Pz8/Pz8/Pz8/');
}

async function loadNotifications() {
    try {
        const response = await fetch('/api/products');
        if (!response.ok) return;
        
        const result = await response.json();
        const products = Array.isArray(result) ? result : (Array.isArray(result?.data) ? result.data : []);
        
        // Get low stock products (threshold 15)
        const lowStockProducts = products.filter(p => p.stockQuantity <= LOW_STOCK_THRESHOLD);

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

// Show toast notification (if Toast not available)
function showToast(message, type = 'info') {
    if (typeof Toast !== 'undefined') {
        Toast[type](message);
    } else {
        console.log(`[${type.toUpperCase()}] ${message}`);
    }
}