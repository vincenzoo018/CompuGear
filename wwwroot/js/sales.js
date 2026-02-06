/**
 * CompuGear Sales Staff JavaScript
 * Handles Sales staff module functionality with limited access
 */

// Global Configuration
const CONFIG = {
    currency: '₱',
    dateFormat: 'en-PH',
    apiBase: '/api'
};

// SVG Icons for action buttons
const Icons = {
    view: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>',
    edit: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>',
    toggleOn: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="1" y="5" width="22" height="14" rx="7" ry="7"/><circle cx="16" cy="12" r="3"/></svg>',
    toggleOff: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="1" y="5" width="22" height="14" rx="7" ry="7"/><circle cx="8" cy="12" r="3"/></svg>'
};

// Toast Notification System
const Toast = {
    container: null,

    init() {
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.id = 'toast-container';
            this.container.className = 'toast-container position-fixed top-0 end-0 p-3';
            this.container.style.zIndex = '9999';
            document.body.appendChild(this.container);
        }
    },

    show(message, type = 'success', duration = 4000) {
        this.init();
        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white border-0 show`;
        toast.style.backgroundColor = type === 'success' ? '#008080' : type === 'error' ? '#dc3545' : type === 'warning' ? '#ff6b35' : '#008080';
        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">${message}</div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" onclick="this.parentElement.parentElement.remove()"></button>
            </div>
        `;
        this.container.appendChild(toast);
        setTimeout(() => toast.remove(), duration);
    },

    success(message) { this.show(message, 'success'); },
    error(message) { this.show(message, 'error'); },
    warning(message) { this.show(message, 'warning'); }
};

// API Helper Functions
const API = {
    async request(endpoint, method = 'GET', data = null) {
        const options = {
            method,
            headers: { 'Content-Type': 'application/json' }
        };

        if (data && method !== 'GET') {
            options.body = JSON.stringify(data);
        }

        try {
            const response = await fetch(`${CONFIG.apiBase}${endpoint}`, options);
            const result = await response.json();
            if (!response.ok) throw new Error(result.message || 'An error occurred');
            return result;
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    },

    get(endpoint) { return this.request(endpoint, 'GET'); },
    post(endpoint, data) { return this.request(endpoint, 'POST', data); },
    put(endpoint, data) { return this.request(endpoint, 'PUT', data); },
    delete(endpoint) { return this.request(endpoint, 'DELETE'); }
};

// Format Helpers
const Format = {
    currency(amount) {
        return `${CONFIG.currency}${parseFloat(amount || 0).toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    },

    date(dateString, includeTime = false) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        const options = { year: 'numeric', month: 'short', day: 'numeric' };
        if (includeTime) {
            options.hour = '2-digit';
            options.minute = '2-digit';
        }
        return date.toLocaleDateString(CONFIG.dateFormat, options);
    },

    statusBadge(status) {
        const colors = {
            'Active': 'success', 'Completed': 'info', 'Pending': 'warning',
            'Cancelled': 'danger', 'Processing': 'primary', 'Shipped': 'info',
            'Delivered': 'success', 'New': 'primary', 'Contacted': 'info',
            'Qualified': 'warning', 'Won': 'success', 'Lost': 'danger'
        };
        const color = colors[status] || 'secondary';
        return `<span class="badge bg-${color}">${status}</span>`;
    }
};

// Modal Manager
const Modal = {
    show(modalId) {
        const modalEl = document.getElementById(modalId);
        if (!modalEl) return null;
        let modal = bootstrap.Modal.getInstance(modalEl);
        if (!modal) modal = new bootstrap.Modal(modalEl);
        modal.show();
        return modal;
    },

    hide(modalId) {
        const modalEl = document.getElementById(modalId);
        if (!modalEl) return;
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();
        document.querySelectorAll('.modal-backdrop').forEach(b => b.remove());
        document.body.classList.remove('modal-open');
        document.body.style.overflow = '';
    },

    reset(formId) {
        const form = document.getElementById(formId);
        if (form) {
            form.reset();
            form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
        }
    }
};

// ===========================================
// SALES STAFF MODULE - Customer Management
// ===========================================
const SalesCustomers = {
    data: [],
    currentId: null,

    async load() {
        try {
            this.data = await API.get('/customers');
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load customers');
        }
    },

    render() {
        const tbody = document.getElementById('customersTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No customers found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(c => `
            <tr>
                <td><strong>${c.customerCode}</strong></td>
                <td>
                    <div class="fw-semibold">${c.firstName} ${c.lastName}</div>
                    <small class="text-muted">${c.email}</small>
                </td>
                <td>${c.phone || '-'}</td>
                <td>${c.categoryName || 'Standard'}</td>
                <td>${Format.currency(c.totalSpent)}</td>
                <td>${Format.statusBadge(c.status)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="SalesCustomers.view(${c.customerId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="SalesCustomers.edit(${c.customerId})">${Icons.edit}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const total = this.data.length;
        const active = this.data.filter(c => c.status === 'Active').length;
        const totalSpent = this.data.reduce((sum, c) => sum + (c.totalSpent || 0), 0);

        if (document.getElementById('totalCustomers')) document.getElementById('totalCustomers').textContent = total;
        if (document.getElementById('activeCustomers')) document.getElementById('activeCustomers').textContent = active;
        if (document.getElementById('totalRevenue')) document.getElementById('totalRevenue').textContent = Format.currency(totalSpent);
    },

    view(id) {
        window.location.href = `/SalesStaff/CustomerProfile/${id}`;
    },

    edit(id) {
        this.currentId = id;
        const customer = this.data.find(c => c.customerId === id);
        if (!customer) return;
        // Populate modal fields
        document.getElementById('customerId').value = id;
        document.getElementById('firstName').value = customer.firstName;
        document.getElementById('lastName').value = customer.lastName;
        document.getElementById('email').value = customer.email;
        document.getElementById('phone').value = customer.phone || '';
        Modal.show('customerModal');
    },

    async save() {
        const data = {
            customerId: document.getElementById('customerId').value || 0,
            firstName: document.getElementById('firstName').value,
            lastName: document.getElementById('lastName').value,
            email: document.getElementById('email').value,
            phone: document.getElementById('phone').value
        };

        try {
            if (data.customerId) {
                await API.put(`/customers/${data.customerId}`, data);
                Toast.success('Customer updated successfully');
            } else {
                await API.post('/customers', data);
                Toast.success('Customer created successfully');
            }
            Modal.hide('customerModal');
            this.load();
        } catch (error) {
            Toast.error('Failed to save customer');
        }
    }
};

// ===========================================
// SALES STAFF MODULE - Orders
// ===========================================
const SalesOrders = {
    data: [],
    currentId: null,

    async load() {
        try {
            this.data = await API.get('/orders');
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load orders');
        }
    },

    render() {
        const tbody = document.getElementById('ordersTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No orders found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(o => `
            <tr>
                <td><strong>${o.orderNumber}</strong></td>
                <td>${o.customerName || 'N/A'}</td>
                <td>${Format.date(o.orderDate)}</td>
                <td>${o.itemCount || 0} items</td>
                <td>${Format.currency(o.totalAmount)}</td>
                <td>${Format.statusBadge(o.orderStatus)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="SalesOrders.view(${o.orderId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="SalesOrders.edit(${o.orderId})">${Icons.edit}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const total = this.data.length;
        const pending = this.data.filter(o => o.orderStatus === 'Pending').length;
        const processing = this.data.filter(o => o.orderStatus === 'Processing').length;
        const completed = this.data.filter(o => o.orderStatus === 'Completed' || o.orderStatus === 'Delivered').length;
        const revenue = this.data.reduce((sum, o) => sum + (o.totalAmount || 0), 0);

        if (document.getElementById('totalOrders')) document.getElementById('totalOrders').textContent = total;
        if (document.getElementById('pendingOrders')) document.getElementById('pendingOrders').textContent = pending;
        if (document.getElementById('processingOrders')) document.getElementById('processingOrders').textContent = processing;
        if (document.getElementById('completedOrders')) document.getElementById('completedOrders').textContent = completed;
        if (document.getElementById('totalRevenue')) document.getElementById('totalRevenue').textContent = Format.currency(revenue);
    },

    view(id) {
        window.location.href = `/SalesStaff/OrderDetails/${id}`;
    },

    edit(id) {
        this.currentId = id;
        const order = this.data.find(o => o.orderId === id);
        if (!order) return;
        Modal.show('orderModal');
    },

    async updateStatus(id, status) {
        try {
            await API.put(`/orders/${id}/status`, { status });
            Toast.success('Order status updated');
            this.load();
        } catch (error) {
            Toast.error('Failed to update order status');
        }
    }
};

// ===========================================
// SALES STAFF MODULE - Leads
// ===========================================
const SalesLeads = {
    data: [],
    currentId: null,

    async load() {
        try {
            this.data = await API.get('/leads');
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load leads');
        }
    },

    render() {
        const tbody = document.getElementById('leadsTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No leads found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(l => `
            <tr>
                <td><strong>${l.leadCode}</strong></td>
                <td>
                    <div class="fw-semibold">${l.firstName} ${l.lastName}</div>
                    <small class="text-muted">${l.email || ''}</small>
                </td>
                <td>${l.companyName || '-'}</td>
                <td>${l.source || '-'}</td>
                <td>${Format.currency(l.estimatedValue)}</td>
                <td>${Format.statusBadge(l.status)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="SalesLeads.view(${l.leadId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="SalesLeads.edit(${l.leadId})">${Icons.edit}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const total = this.data.length;
        const newLeads = this.data.filter(l => l.status === 'New').length;
        const qualified = this.data.filter(l => l.status === 'Qualified').length;
        const won = this.data.filter(l => l.status === 'Won').length;

        if (document.getElementById('totalLeads')) document.getElementById('totalLeads').textContent = total;
        if (document.getElementById('newLeads')) document.getElementById('newLeads').textContent = newLeads;
        if (document.getElementById('qualifiedLeads')) document.getElementById('qualifiedLeads').textContent = qualified;
        if (document.getElementById('wonLeads')) document.getElementById('wonLeads').textContent = won;
    },

    view(id) {
        const lead = this.data.find(l => l.leadId === id);
        if (!lead) return;
        Modal.show('leadViewModal');
    },

    edit(id) {
        this.currentId = id;
        const lead = this.data.find(l => l.leadId === id);
        if (!lead) return;
        Modal.show('leadModal');
    },

    async save() {
        const data = {
            leadId: document.getElementById('leadId')?.value || 0,
            firstName: document.getElementById('leadFirstName')?.value,
            lastName: document.getElementById('leadLastName')?.value,
            email: document.getElementById('leadEmail')?.value,
            phone: document.getElementById('leadPhone')?.value,
            companyName: document.getElementById('leadCompany')?.value,
            source: document.getElementById('leadSource')?.value,
            status: document.getElementById('leadStatus')?.value,
            estimatedValue: parseFloat(document.getElementById('leadValue')?.value) || 0
        };

        try {
            if (data.leadId) {
                await API.put(`/leads/${data.leadId}`, data);
                Toast.success('Lead updated successfully');
            } else {
                await API.post('/leads', data);
                Toast.success('Lead created successfully');
            }
            Modal.hide('leadModal');
            this.load();
        } catch (error) {
            Toast.error('Failed to save lead');
        }
    }
};

// ===========================================
// SALES STAFF MODULE - Products (Read Only)
// ===========================================
const SalesProducts = {
    data: [],

    async load() {
        try {
            this.data = await API.get('/products');
            this.render();
        } catch (error) {
            Toast.error('Failed to load products');
        }
    },

    render() {
        const tbody = document.getElementById('productsTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">No products found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(p => `
            <tr>
                <td><strong>${p.productCode}</strong></td>
                <td>
                    <div class="fw-semibold">${p.productName}</div>
                    <small class="text-muted">${p.categoryName || ''}</small>
                </td>
                <td>${p.brandName || '-'}</td>
                <td>${Format.currency(p.sellingPrice)}</td>
                <td>
                    <span class="badge ${p.stockQuantity > 10 ? 'bg-success' : p.stockQuantity > 0 ? 'bg-warning' : 'bg-danger'}">
                        ${p.stockQuantity} in stock
                    </span>
                </td>
                <td>${Format.statusBadge(p.status)}</td>
            </tr>
        `).join('');
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    // Check current page and load appropriate data
    const path = window.location.pathname.toLowerCase();
    
    if (path.includes('/salesstaff/customers') || path.includes('/salesstaff') && path.endsWith('/customers')) {
        SalesCustomers.load();
    } else if (path.includes('/salesstaff/orders') || path.includes('/salesstaff') && path.endsWith('/orders')) {
        SalesOrders.load();
    } else if (path.includes('/salesstaff/leads') || path.includes('/salesstaff') && path.endsWith('/leads')) {
        SalesLeads.load();
    } else if (path.includes('/salesstaff/products') || path.includes('/salesstaff') && path.endsWith('/products')) {
        SalesProducts.load();
    } else if (path === '/salesstaff' || path === '/salesstaff/') {
        // Dashboard - load all stats
        Promise.all([
            API.get('/customers').catch(() => []),
            API.get('/orders').catch(() => []),
            API.get('/leads').catch(() => [])
        ]).then(([customers, orders, leads]) => {
            // Update dashboard stats
            if (document.getElementById('totalCustomers')) document.getElementById('totalCustomers').textContent = customers.length;
            if (document.getElementById('totalOrders')) document.getElementById('totalOrders').textContent = orders.length;
            if (document.getElementById('totalLeads')) document.getElementById('totalLeads').textContent = leads.length;
            
            const revenue = orders.reduce((sum, o) => sum + (o.totalAmount || 0), 0);
            if (document.getElementById('totalRevenue')) document.getElementById('totalRevenue').textContent = Format.currency(revenue);
        });
    }
});
