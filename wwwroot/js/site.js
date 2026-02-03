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

    // Initialize Notifications
    initNotifications();
});

// Notification System
const LOW_STOCK_THRESHOLD = 15;
let notifications = [];

async function initNotifications() {
    await loadNotifications();
    // Refresh notifications every 30 seconds
    setInterval(loadNotifications, 30000);
}

async function loadNotifications() {
    try {
        const response = await fetch('/api/products');
        const products = await response.json();
        
        // Get low stock products (threshold 15)
        const lowStockProducts = products.filter(p => p.stockQuantity <= LOW_STOCK_THRESHOLD);
        
        notifications = lowStockProducts.map(p => ({
            id: p.productId,
            type: p.stockQuantity === 0 ? 'danger' : p.stockQuantity <= 5 ? 'warning' : 'info',
            title: p.stockQuantity === 0 ? 'Out of Stock' : p.stockQuantity <= 5 ? 'Critical Stock' : 'Low Stock',
            message: `${p.productName} has only ${p.stockQuantity} units left`,
            time: 'Just now',
            read: false
        }));
        
        updateNotificationUI();
    } catch (error) {
        console.error('Failed to load notifications:', error);
    }
}

function updateNotificationUI() {
    const badge = document.getElementById('notificationBadge');
    const list = document.getElementById('notificationList');
    
    if (!badge || !list) return;
    
    const unreadCount = notifications.filter(n => !n.read).length;
    
    // Update badge
    if (unreadCount > 0) {
        badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
        badge.style.display = 'flex';
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
                <p class="mb-0">No new notifications</p>
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
}

