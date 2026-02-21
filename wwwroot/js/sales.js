/**
 * CompuGear Sales Staff JavaScript - Full CRUD Operations
 * Consistent with Admin portal styling and functionality
 */

// Global Configuration
const CONFIG = {
    currency: '₱',
    dateFormat: 'en-PH',
    apiBase: '/api'
};

// SVG Icons
const Icons = {
    view: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>',
    edit: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>',
    toggleOn: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="1" y="5" width="22" height="14" rx="7" ry="7"/><circle cx="16" cy="12" r="3"/></svg>',
    toggleOff: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="1" y="5" width="22" height="14" rx="7" ry="7"/><circle cx="8" cy="12" r="3"/></svg>',
    add: '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>',
    approve: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>',
    reject: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>',
    update: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/></svg>',
    delete: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>'
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
        toast.className = 'toast align-items-center text-white border-0 show';
        toast.style.backgroundColor = type === 'success' ? '#008080' : type === 'error' ? '#dc3545' : type === 'warning' ? '#ff6b35' : '#008080';
        toast.innerHTML = `<div class="d-flex"><div class="toast-body"><i class="bi bi-${type === 'success' ? 'check-circle' : type === 'error' ? 'x-circle' : 'exclamation-circle'} me-2"></i>${message}</div><button type="button" class="btn-close btn-close-white me-2 m-auto" onclick="this.parentElement.parentElement.remove()"></button></div>`;
        this.container.appendChild(toast);
        setTimeout(() => toast.remove(), duration);
    },
    success(msg) { this.show(msg, 'success'); },
    error(msg) { this.show(msg, 'error'); },
    warning(msg) { this.show(msg, 'warning'); }
};

// API Helper
const API = {
    async request(endpoint, method = 'GET', data = null) {
        const options = { method, headers: { 'Content-Type': 'application/json' } };
        if (data && method !== 'GET') options.body = JSON.stringify(data);
        try {
            const response = await fetch(`${CONFIG.apiBase}${endpoint}`, options);
            const result = await response.json();
            if (!response.ok) throw new Error(result.message || 'An error occurred');
            // Handle wrapped response format {success: true, data: [...]}
            if (result && typeof result === 'object' && 'data' in result) {
                return result.data;
            }
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
        if (includeTime) { options.hour = '2-digit'; options.minute = '2-digit'; }
        return date.toLocaleDateString(CONFIG.dateFormat, options);
    },
    statusBadge(status, colorMap = {}) {
        const colors = {
            'Active': 'success', 'active': 'success',
            'Inactive': 'secondary', 'inactive': 'secondary',
            'Pending': 'warning', 'pending': 'warning',
            'Completed': 'info', 'completed': 'info',
            'Cancelled': 'danger', 'cancelled': 'danger',
            'Open': 'primary', 'open': 'primary',
            'Closed': 'secondary', 'closed': 'secondary',
            'In Progress': 'info', 'in progress': 'info',
            'Resolved': 'success', 'resolved': 'success',
            'Paid': 'success', 'paid': 'success',
            'Unpaid': 'danger', 'unpaid': 'danger',
            'Partial': 'warning', 'partial': 'warning',
            'New': 'primary', 'new': 'primary',
            'Hot': 'danger', 'hot': 'danger',
            'Warm': 'warning', 'warm': 'warning',
            'Cold': 'info', 'cold': 'info',
            'Won': 'success', 'won': 'success',
            'Lost': 'danger', 'lost': 'danger',
            'Qualified': 'info', 'qualified': 'info',
            'Confirmed': 'primary', 'confirmed': 'primary',
            'Processing': 'info', 'processing': 'info',
            'Delivered': 'success', 'delivered': 'success',
            'Shipped': 'info', 'shipped': 'info',
            'Draft': 'secondary', 'draft': 'secondary',
            ...colorMap
        };
        const color = colors[status] || 'secondary';
        return `<span class="badge bg-${color}">${status || 'Unknown'}</span>`;
    }
};

// Modal Helper
const Modal = {
    show(id) {
        const el = document.getElementById(id);
        if (el) {
            const m = bootstrap.Modal.getInstance(el) || new bootstrap.Modal(el);
            m.show();
        }
    },
    hide(id) {
        const el = document.getElementById(id);
        if (el) {
            const m = bootstrap.Modal.getInstance(el);
            if (m) m.hide();
        }
    }
};

// ===========================================
// SALES STAFF MODULE - Customers
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
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No customers found.</td></tr>';
            this.updateStats();
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
                <td class="text-center">${c.totalOrders || 0}</td>
                <td class="text-end">${Format.currency(c.totalSpent)}</td>
                <td class="text-center">${Format.statusBadge(c.status)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="SalesCustomers.view(${c.customerId})" title="View">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="SalesCustomers.edit(${c.customerId})" title="Edit">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-${c.status === 'Active' ? 'danger' : 'success'}" onclick="SalesCustomers.toggleStatus(${c.customerId})" title="${c.status === 'Active' ? 'Deactivate' : 'Activate'}">
                            ${c.status === 'Active' ? Icons.toggleOff : Icons.toggleOn}
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="SalesCustomers.delete(${c.customerId})" title="Delete">${Icons.delete}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const total = this.data.length;
        const active = this.data.filter(c => c.status === 'Active').length;
        const newCustomers = this.data.filter(c => {
            const created = new Date(c.createdAt);
            const now = new Date();
            return (now - created) < 30 * 24 * 60 * 60 * 1000;
        }).length;
        const revenue = this.data.reduce((sum, c) => sum + (c.totalSpent || 0), 0);

        const el = (id, val) => { const e = document.getElementById(id); if (e) e.textContent = val; };
        el('totalCustomers', total);
        el('activeCustomers', active);
        el('newCustomers', newCustomers);
        el('totalRevenue', Format.currency(revenue));
        el('customerCount', total + ' customers');
    },

    view(id) {
        const c = this.data.find(customer => customer.customerId === id);
        if (!c) { Toast.error('Customer not found'); return; }

        this.currentId = id;
        const content = document.getElementById('viewCustomerContent');
        if (content) {
            content.innerHTML = `
                <div class="text-center mb-4">
                    <div class="mx-auto mb-3" style="width: 80px; height: 80px; font-size: 2rem; background: linear-gradient(135deg, #008080, #006666); color: white; border-radius: 50%; display: flex; align-items: center; justify-content: center;">
                        ${(c.firstName?.[0] || '') + (c.lastName?.[0] || '')}
                    </div>
                    <h4>${c.firstName} ${c.lastName}</h4>
                    <p class="text-muted">${c.customerCode}</p>
                    ${Format.statusBadge(c.status)}
                </div>
                <div class="row g-3">
                    <div class="col-md-6"><div class="detail-label">Email</div><div class="detail-value">${c.email}</div></div>
                    <div class="col-md-6"><div class="detail-label">Phone</div><div class="detail-value">${c.phone || '-'}</div></div>
                    <div class="col-md-6"><div class="detail-label">Company</div><div class="detail-value">${c.companyName || '-'}</div></div>
                    <div class="col-md-6"><div class="detail-label">Category</div><div class="detail-value">${c.categoryName || 'Standard'}</div></div>
                    <div class="col-12"><div class="detail-label">Address</div><div class="detail-value">${c.billingAddress || '-'}${c.billingCity ? ', ' + c.billingCity : ''}</div></div>
                    <div class="col-md-4">
                        <div class="card bg-light"><div class="card-body text-center py-3"><div class="detail-label">Total Orders</div><h4 class="mb-0 text-primary">${c.totalOrders || 0}</h4></div></div>
                    </div>
                    <div class="col-md-4">
                        <div class="card bg-light"><div class="card-body text-center py-3"><div class="detail-label">Total Spent</div><h4 class="mb-0 text-success">${Format.currency(c.totalSpent)}</h4></div></div>
                    </div>
                    <div class="col-md-4">
                        <div class="card bg-light"><div class="card-body text-center py-3"><div class="detail-label">Credit Limit</div><h4 class="mb-0 text-warning">${Format.currency(c.creditLimit)}</h4></div></div>
                    </div>
                </div>`;
        }
        Modal.show('viewCustomerModal');
    },

    openCreate() {
        this.currentId = null;
        document.getElementById('customerModalTitle').textContent = 'Add New Customer';
        document.getElementById('customerForm').reset();
        document.getElementById('customerId').value = '';
        Modal.show('customerModal');
    },

    edit(id) {
        this.currentId = id;
        const c = this.data.find(cust => cust.customerId === id);
        if (!c) return;

        document.getElementById('customerModalTitle').textContent = 'Edit Customer';
        document.getElementById('customerId').value = id;
        document.getElementById('firstName').value = c.firstName || '';
        document.getElementById('lastName').value = c.lastName || '';
        document.getElementById('email').value = c.email || '';
        document.getElementById('phone').value = c.phone || '';
        document.getElementById('companyName').value = c.companyName || '';
        document.getElementById('billingAddress').value = c.billingAddress || '';
        document.getElementById('billingCity').value = c.billingCity || '';
        document.getElementById('customerNotes').value = c.notes || '';
        Modal.show('customerModal');
    },

    async save() {
        const data = {
            customerId: parseInt(document.getElementById('customerId').value) || 0,
            firstName: document.getElementById('firstName').value,
            lastName: document.getElementById('lastName').value,
            email: document.getElementById('email').value,
            phone: document.getElementById('phone').value,
            companyName: document.getElementById('companyName').value || '',
            billingAddress: document.getElementById('billingAddress').value || '',
            billingCity: document.getElementById('billingCity').value || '',
            notes: document.getElementById('customerNotes').value || ''
        };

        if (!data.firstName || !data.lastName || !data.email) {
            Toast.error('Please fill in required fields');
            return;
        }

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
            Toast.error(error.message || 'Failed to save customer');
        }
    },

    async toggleStatus(id) {
        try {
            const result = await API.put(`/customers/${id}/toggle-status`);
            Toast.success(result.message || 'Status updated');
            this.load();
        } catch (error) {
            Toast.error('Failed to update status');
        }
    },

    async delete(id) {
        if (!confirm('Are you sure you want to delete this customer?')) return;
        try {
            await API.delete(`/customers/${id}`);
            Toast.success('Customer deleted successfully');
            this.load();
        } catch (error) {
            Toast.error('Failed to delete customer');
        }
    },

    filter(search = '', status = '') {
        let filtered = this.data;
        if (search) {
            const s = search.toLowerCase();
            filtered = filtered.filter(c =>
                (c.firstName + ' ' + c.lastName).toLowerCase().includes(s) ||
                c.email?.toLowerCase().includes(s) ||
                c.customerCode?.toLowerCase().includes(s) ||
                c.phone?.toLowerCase().includes(s) ||
                c.companyName?.toLowerCase().includes(s)
            );
        }
        if (status) filtered = filtered.filter(c => c.status === status);
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('customersTableBody');
        if (!tbody) return;
        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4">No customers found</td></tr>';
            return;
        }
        tbody.innerHTML = data.map(c => `
            <tr>
                <td><strong>${c.customerCode}</strong></td>
                <td><div class="fw-semibold">${c.firstName} ${c.lastName}</div><small class="text-muted">${c.email}</small></td>
                <td>${c.phone || '-'}</td>
                <td>${c.categoryName || 'Standard'}</td>
                <td class="text-center">${c.totalOrders || 0}</td>
                <td class="text-end">${Format.currency(c.totalSpent)}</td>
                <td class="text-center">${Format.statusBadge(c.status)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="SalesCustomers.view(${c.customerId})" title="View">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="SalesCustomers.edit(${c.customerId})" title="Edit">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-${c.status === 'Active' ? 'danger' : 'success'}" onclick="SalesCustomers.toggleStatus(${c.customerId})" title="${c.status === 'Active' ? 'Deactivate' : 'Activate'}">
                            ${c.status === 'Active' ? Icons.toggleOff : Icons.toggleOn}
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="SalesCustomers.delete(${c.customerId})" title="Delete">${Icons.delete}</button>
                    </div>
                </td>
            </tr>
        `).join('');
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
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No orders found.</td></tr>';
            this.updateStats();
            return;
        }

        tbody.innerHTML = this.data.map(o => {
            const actions = `<button class="btn btn-sm btn-outline-primary" onclick="SalesOrders.view(${o.orderId})" title="View">${Icons.view}</button>`;

            return `<tr>
                <td>
                    <div class="fw-semibold">${o.orderNumber}</div>
                    <small class="text-muted">${Format.date(o.orderDate, true)}</small>
                </td>
                <td>${o.customerName || 'Guest'}</td>
                <td>${Format.date(o.orderDate)}</td>
                <td class="text-center">${o.itemCount || 0}</td>
                <td class="text-end">${Format.currency(o.totalAmount)}</td>
                <td class="text-center">${Format.statusBadge(o.orderStatus)}</td>
                <td class="text-center">${Format.statusBadge(o.paymentStatus)}</td>
                <td class="text-center">
                    <div class="btn-group">${actions}</div>
                </td>
            </tr>`;
        }).join('');
        this.updateStats();
    },

    updateStats() {
        const total = this.data.length;
        const pending = this.data.filter(o => o.orderStatus === 'Pending').length;
        const processing = this.data.filter(o => o.orderStatus === 'Processing' || o.orderStatus === 'Confirmed').length;
        const completed = this.data.filter(o => o.orderStatus === 'Completed' || o.orderStatus === 'Delivered').length;
        const revenue = this.data.reduce((sum, o) => sum + (o.totalAmount || 0), 0);

        const el = (id, val) => { const e = document.getElementById(id); if (e) e.textContent = val; };
        el('totalOrders', total);
        el('pendingOrders', pending);
        el('processingOrders', processing);
        el('completedOrders', completed);
        el('totalRevenue', Format.currency(revenue));
        el('orderCount', total + ' orders');
        el('orderPaginationInfo', `Showing 1 to ${total} of ${total} orders`);
    },

    view(id) {
        const o = this.data.find(order => order.orderId === id);
        if (!o) { Toast.error('Order not found'); return; }

        this.currentId = id;
        const content = document.getElementById('viewOrderContent');
        if (content) {
            const items = o.items || [];
            content.innerHTML = `
                <div class="row mb-4">
                    <div class="col-md-4">
                        <div class="p-3 bg-light rounded">
                            <h6 class="text-muted mb-2">Customer</h6>
                            <div class="fw-bold">${o.customerName || 'Guest'}</div>
                            <div class="text-muted small">${[o.shippingAddress, o.shippingCity].filter(Boolean).join(', ') || '-'}</div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="p-3 bg-light rounded">
                            <h6 class="text-muted mb-2">Order Info</h6>
                            <div><span class="text-muted">Date:</span> ${Format.date(o.orderDate, true)}</div>
                            <div><span class="text-muted">Method:</span> ${o.paymentMethod || '-'}</div>
                            <div><span class="text-muted">Reference:</span> ${o.paymentReference || '-'}</div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="p-3 bg-light rounded">
                            <h6 class="text-muted mb-2">Status</h6>
                            <div class="mb-1"><span class="text-muted">Order:</span> ${Format.statusBadge(o.orderStatus)}</div>
                            <div><span class="text-muted">Payment:</span> ${Format.statusBadge(o.paymentStatus)}</div>
                        </div>
                    </div>
                </div>
                <h6>Order Items</h6>
                <div class="table-responsive">
                    <table class="table table-sm table-bordered">
                        <thead class="table-light">
                            <tr><th>Product</th><th class="text-end">Unit Price</th><th class="text-center">Qty</th><th class="text-end">Subtotal</th></tr>
                        </thead>
                        <tbody>${items.length > 0 ? items.map(i => `<tr>
                            <td>${i.productName || '-'}</td>
                            <td class="text-end">${Format.currency(i.unitPrice)}</td>
                            <td class="text-center">${i.quantity}</td>
                            <td class="text-end">${Format.currency(i.totalPrice)}</td>
                        </tr>`).join('') : `<tr><td colspan="4" class="text-center text-muted">${o.itemCount} item(s)</td></tr>`}</tbody>
                        <tfoot>
                            <tr><td colspan="3" class="text-end"><strong>Subtotal:</strong></td><td class="text-end">${Format.currency(o.subtotal || o.totalAmount)}</td></tr>
                            <tr><td colspan="3" class="text-end">VAT (12%):</td><td class="text-end">${Format.currency(o.taxAmount || 0)}</td></tr>
                            <tr class="table-primary"><td colspan="3" class="text-end"><strong>Total:</strong></td><td class="text-end"><strong>${Format.currency(o.totalAmount)}</strong></td></tr>
                        </tfoot>
                    </table>
                </div>`;
        }
        Modal.show('viewOrderModal');
    },

    openUpdateStatus(id) {
        Toast.warning('Sales Staff has view-only access for orders.');
    },

    async confirmStatusUpdate() {
        Toast.warning('Sales Staff has view-only access for orders.');
    },

    filter(search = '', status = '') {
        let filtered = this.data;
        if (search) {
            const s = search.toLowerCase();
            filtered = filtered.filter(o =>
                o.orderNumber?.toLowerCase().includes(s) ||
                o.customerName?.toLowerCase().includes(s)
            );
        }
        if (status) filtered = filtered.filter(o => o.orderStatus === status);
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('ordersTableBody');
        if (!tbody) return;
        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No orders match your criteria.</td></tr>';
            return;
        }
        tbody.innerHTML = data.map(o => {
            const actions = `<button class="btn btn-sm btn-outline-primary" onclick="SalesOrders.view(${o.orderId})" title="View">${Icons.view}</button>`;
            return `<tr>
                <td><div class="fw-semibold">${o.orderNumber}</div><small class="text-muted">${Format.date(o.orderDate, true)}</small></td>
                <td>${o.customerName || 'Guest'}</td>
                <td>${Format.date(o.orderDate)}</td>
                <td class="text-center">${o.itemCount || 0}</td>
                <td class="text-end">${Format.currency(o.totalAmount)}</td>
                <td class="text-center">${Format.statusBadge(o.orderStatus)}</td>
                <td class="text-center">${Format.statusBadge(o.paymentStatus)}</td>
                <td class="text-center"><div class="btn-group">${actions}</div></td>
            </tr>`;
        }).join('');
    },

    async updateStatusDirect(id, status) {
        Toast.warning('Sales Staff has view-only access for orders.');
    },

    printOrder() {
        const id = this.currentId;
        if (!id) return;
        const o = this.data.find(ord => ord.orderId === id);
        if (!o) return;
        const items = o.items || [];
        
        const html = `<html><head><title>Order ${o.orderNumber}</title>
            <style>body{font-family:'Segoe UI',Arial,sans-serif;margin:40px;color:#333}.header{display:flex;justify-content:space-between;border-bottom:3px solid #008080;padding-bottom:20px;margin-bottom:30px}.company-logo img{height:50px}.badge{background:#008080;color:white;padding:8px 20px;border-radius:5px;font-size:18px}table{width:100%;border-collapse:collapse;margin-bottom:20px}th{background:#008080;color:white;padding:10px;text-align:left}td{padding:10px;border-bottom:1px solid #eee}.total-row td{font-weight:bold;font-size:18px;color:#008080;border-top:2px solid #008080}</style></head><body>
            <div class="header">
                <div class="company-logo">
                    <img src="${window.location.origin}/images/compugearlogo.png" alt="CompuGear" onerror="this.style.display='none';document.getElementById('fallback-title').style.display='block'">
                    <h1 id="fallback-title" style="display:none;color:#008080;margin:0;">CompuGear</h1>
                    <p>Computer & Gear Solutions</p>
                </div>
                <div><span class="badge">${o.orderNumber}</span></div>
            </div>
            <table><thead><tr><th>Product</th><th>Qty</th><th>Price</th><th>Total</th></tr></thead>
            <tbody>${items.map(i => `<tr><td>${i.productName}</td><td>${i.quantity}</td><td>${Format.currency(i.unitPrice)}</td><td>${Format.currency(i.totalPrice)}</td></tr>`).join('')}</tbody></table>
            <p style="text-align:right;font-size:1.2em"><strong>Total: ${Format.currency(o.totalAmount)}</strong></p>
            <script>window.onload=function(){window.print();window.close();}<\/script></body></html>`;
            
        var printWindow = window.open('', '_blank');
        printWindow.document.write(html);
        printWindow.document.close();
    },

    exportOrders() {
        const rows = [['Order #', 'Customer', 'Date', 'Items', 'Amount', 'Order Status', 'Payment Status']];
        this.data.forEach(o => {
            rows.push([o.orderNumber, o.customerName || '', new Date(o.orderDate).toLocaleDateString(), o.itemCount, o.totalAmount.toFixed(2), o.orderStatus, o.paymentStatus]);
        });
        const csv = rows.map(r => r.map(c => `"${c}"`).join(',')).join('\n');
        const blob = new Blob([csv], { type: 'text/csv' });
        const a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = `orders_${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        Toast.success('Orders exported!');
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
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No leads found.</td></tr>';
            this.updateStats();
            return;
        }

        tbody.innerHTML = this.data.map(l => `
            <tr>
                <td><strong>${l.leadCode || '-'}</strong></td>
                <td>
                    <div class="fw-semibold">${l.firstName} ${l.lastName}</div>
                    <small class="text-muted">${l.email || ''}</small>
                </td>
                <td>${l.companyName || '-'}</td>
                <td>${l.source || '-'}</td>
                <td class="text-end">${Format.currency(l.estimatedValue)}</td>
                <td class="text-center">${Format.statusBadge(l.status)}</td>
                <td class="text-center">${Format.date(l.createdAt)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="SalesLeads.view(${l.leadId})" title="View">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="SalesLeads.edit(${l.leadId})" title="Edit">${Icons.edit}</button>
                        ${l.status !== 'Won' && l.status !== 'Lost' ? `<button class="btn btn-sm btn-outline-success" onclick="SalesLeads.convert(${l.leadId})" title="Convert to Customer">${Icons.approve}</button>` : ''}
                        <button class="btn btn-sm btn-outline-${l.status === 'Inactive' ? 'success' : 'danger'}" onclick="SalesLeads.toggleStatus(${l.leadId})" title="${l.status === 'Inactive' ? 'Activate' : 'Deactivate'}">
                            ${l.status === 'Inactive' ? Icons.toggleOn : Icons.toggleOff}
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="SalesLeads.delete(${l.leadId})" title="Delete">${Icons.delete}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const total = this.data.length;
        const newLeads = this.data.filter(l => l.status === 'New').length;
        const qualified = this.data.filter(l => l.status === 'Qualified' || l.status === 'Hot').length;
        const won = this.data.filter(l => l.status === 'Won').length;
        const pipeline = this.data.filter(l => l.status !== 'Won' && l.status !== 'Lost' && l.status !== 'Inactive').reduce((s, l) => s + (l.estimatedValue || 0), 0);

        const el = (id, val) => { const e = document.getElementById(id); if (e) e.textContent = val; };
        el('totalLeads', total);
        el('newLeads', newLeads);
        el('qualifiedLeads', qualified);
        el('wonLeads', won);
        el('pipelineValue', Format.currency(pipeline));
        el('leadCount', total + ' leads');
    },

    view(id) {
        const l = this.data.find(lead => lead.leadId === id);
        if (!l) { Toast.error('Lead not found'); return; }
        this.currentId = id;
        const content = document.getElementById('viewLeadContent');
        if (content) {
            content.innerHTML = `
                <div class="row g-4">
                    <div class="col-12 text-center border-bottom pb-3">
                        <h5 class="mb-1">${l.firstName} ${l.lastName}</h5>
                        <small class="text-muted">${l.leadCode}</small>
                        <div class="mt-2">${Format.statusBadge(l.status)}</div>
                    </div>
                    <div class="col-md-6"><div class="detail-label">Email</div><div class="detail-value">${l.email || '-'}</div></div>
                    <div class="col-md-6"><div class="detail-label">Phone</div><div class="detail-value">${l.phone || '-'}</div></div>
                    <div class="col-md-6"><div class="detail-label">Company</div><div class="detail-value">${l.companyName || '-'}</div></div>
                    <div class="col-md-6"><div class="detail-label">Source</div><div class="detail-value">${l.source || '-'}</div></div>
                    <div class="col-md-6"><div class="detail-label">Estimated Value</div><div class="detail-value fw-semibold text-success">${Format.currency(l.estimatedValue)}</div></div>
                    <div class="col-md-6"><div class="detail-label">Created Date</div><div class="detail-value">${Format.date(l.createdAt)}</div></div>
                    <div class="col-12"><div class="detail-label">Notes</div><div class="detail-value">${l.notes || 'No notes available'}</div></div>
                </div>`;
        }
        Modal.show('viewLeadModal');
    },

    openCreate() {
        this.currentId = null;
        document.getElementById('leadModalTitle').textContent = 'Add New Lead';
        document.getElementById('leadForm').reset();
        document.getElementById('leadId').value = '';
        Modal.show('leadModal');
    },

    edit(id) {
        this.currentId = id;
        const l = this.data.find(lead => lead.leadId === id);
        if (!l) return;

        document.getElementById('leadModalTitle').textContent = 'Edit Lead';
        document.getElementById('leadId').value = id;
        document.getElementById('leadFirstName').value = l.firstName || '';
        document.getElementById('leadLastName').value = l.lastName || '';
        document.getElementById('leadEmail').value = l.email || '';
        document.getElementById('leadPhone').value = l.phone || '';
        document.getElementById('leadCompany').value = l.companyName || '';
        document.getElementById('leadSource').value = l.source || '';
        document.getElementById('leadStatus').value = l.status || 'New';
        document.getElementById('leadValue').value = l.estimatedValue || '';
        document.getElementById('leadNotes').value = l.notes || '';
        Modal.show('leadModal');
    },

    async save() {
        const data = {
            leadId: parseInt(document.getElementById('leadId').value) || 0,
            firstName: document.getElementById('leadFirstName').value,
            lastName: document.getElementById('leadLastName').value,
            email: document.getElementById('leadEmail').value,
            phone: document.getElementById('leadPhone').value,
            companyName: document.getElementById('leadCompany').value || '',
            source: document.getElementById('leadSource').value || '',
            status: document.getElementById('leadStatus').value || 'New',
            estimatedValue: parseFloat(document.getElementById('leadValue').value) || 0,
            notes: document.getElementById('leadNotes').value || ''
        };

        if (!data.firstName || !data.lastName) {
            Toast.error('Please fill in required fields');
            return;
        }

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
            Toast.error(error.message || 'Failed to save lead');
        }
    },

    async convert(id) {
        if (!confirm('Convert this lead to a customer? This action cannot be undone.')) return;
        try {
            const result = await API.put(`/leads/${id}/convert`);
            Toast.success(result.message || 'Lead converted to customer');
            this.load();
        } catch (error) {
            Toast.error(error.message || 'Failed to convert lead');
        }
    },

    async toggleStatus(id) {
        try {
            const result = await API.put(`/leads/${id}/toggle-status`);
            Toast.success(result.message || 'Status updated');
            this.load();
        } catch (error) {
            Toast.error('Failed to update status');
        }
    },

    async delete(id) {
        if (!confirm('Are you sure you want to delete this lead?')) return;
        try {
            await API.delete(`/leads/${id}`);
            Toast.success('Lead deleted successfully');
            this.load();
        } catch (error) {
            Toast.error('Failed to delete lead');
        }
    },

    filter(search = '', status = '', source = '') {
        let filtered = this.data;
        if (search) {
            const s = search.toLowerCase();
            filtered = filtered.filter(l =>
                (l.firstName + ' ' + l.lastName).toLowerCase().includes(s) ||
                l.email?.toLowerCase().includes(s) ||
                l.leadCode?.toLowerCase().includes(s) ||
                l.companyName?.toLowerCase().includes(s)
            );
        }
        if (status) filtered = filtered.filter(l => l.status === status);
        if (source) filtered = filtered.filter(l => l.source === source);
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('leadsTableBody');
        if (!tbody) return;
        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4">No leads found</td></tr>';
            return;
        }
        tbody.innerHTML = data.map(l => `
            <tr>
                <td><strong>${l.leadCode || '-'}</strong></td>
                <td><div class="fw-semibold">${l.firstName} ${l.lastName}</div><small class="text-muted">${l.email || ''}</small></td>
                <td>${l.companyName || '-'}</td>
                <td>${l.source || '-'}</td>
                <td class="text-end">${Format.currency(l.estimatedValue)}</td>
                <td class="text-center">${Format.statusBadge(l.status)}</td>
                <td class="text-center">${Format.date(l.createdAt)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="SalesLeads.view(${l.leadId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="SalesLeads.edit(${l.leadId})">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-${l.status === 'Inactive' ? 'success' : 'danger'}" onclick="SalesLeads.toggleStatus(${l.leadId})">
                            ${l.status === 'Inactive' ? Icons.toggleOn : Icons.toggleOff}
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="SalesLeads.delete(${l.leadId})" title="Delete">${Icons.delete}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    }
};

// ===========================================
// SALES STAFF MODULE - Products (Read-Only with View)
// ===========================================
const SalesProducts = {
    data: [],

    async load() {
        try {
            this.data = await API.get('/products');
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load products');
        }
    },

    render() {
        const tbody = document.getElementById('productsTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No products found.</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(p => `
            <tr>
                <td><strong>${p.productCode || '-'}</strong></td>
                <td>
                    <div class="fw-semibold">${p.productName}</div>
                    <small class="text-muted">${p.categoryName || ''}</small>
                </td>
                <td>${p.brandName || '-'}</td>
                <td class="text-end">${Format.currency(p.sellingPrice)}</td>
                <td class="text-center">
                    <span class="badge ${p.stockQuantity > 10 ? 'bg-success' : p.stockQuantity > 0 ? 'bg-warning' : 'bg-danger'}">
                        ${p.stockQuantity} in stock
                    </span>
                </td>
                <td class="text-center">${Format.statusBadge(p.status)}</td>
                <td class="text-center">
                    <button class="btn btn-sm btn-outline-primary" onclick="SalesProducts.view(${p.productId})" title="View Details">${Icons.view}</button>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const total = this.data.length;
        const inStock = this.data.filter(p => p.stockQuantity > 10).length;
        const lowStock = this.data.filter(p => p.stockQuantity > 0 && p.stockQuantity <= 10).length;
        const outOfStock = this.data.filter(p => p.stockQuantity === 0).length;

        const el = (id, val) => { const e = document.getElementById(id); if (e) e.textContent = val; };
        el('totalProducts', total);
        el('inStockProducts', inStock);
        el('lowStockProducts', lowStock);
        el('outOfStockProducts', outOfStock);
    },

    view(id) {
        const p = this.data.find(prod => prod.productId === id);
        if (!p) { Toast.error('Product not found'); return; }

        const content = document.getElementById('viewProductContent');
        if (content) {
            content.innerHTML = `
                <div class="row g-4">
                    <div class="col-12 text-center border-bottom pb-3">
                        <h5 class="mb-1">${p.productName}</h5>
                        <small class="text-muted">${p.productCode}</small>
                        <div class="mt-2">${Format.statusBadge(p.status)}</div>
                    </div>
                    <div class="col-md-6"><div class="detail-label">Category</div><div class="detail-value">${p.categoryName || '-'}</div></div>
                    <div class="col-md-6"><div class="detail-label">Brand</div><div class="detail-value">${p.brandName || '-'}</div></div>
                    <div class="col-md-6"><div class="detail-label">Cost Price</div><div class="detail-value">${Format.currency(p.costPrice)}</div></div>
                    <div class="col-md-6"><div class="detail-label">Selling Price</div><div class="detail-value fw-semibold text-success">${Format.currency(p.sellingPrice)}</div></div>
                    <div class="col-md-4">
                        <div class="card bg-light"><div class="card-body text-center py-3"><div class="detail-label">Stock</div><h4 class="mb-0 ${p.stockQuantity > 10 ? 'text-success' : p.stockQuantity > 0 ? 'text-warning' : 'text-danger'}">${p.stockQuantity}</h4></div></div>
                    </div>
                    <div class="col-md-4">
                        <div class="card bg-light"><div class="card-body text-center py-3"><div class="detail-label">Reorder Level</div><h4 class="mb-0 text-info">${p.reorderLevel || 0}</h4></div></div>
                    </div>
                    <div class="col-md-4">
                        <div class="card bg-light"><div class="card-body text-center py-3"><div class="detail-label">Margin</div><h4 class="mb-0 text-primary">${p.sellingPrice && p.costPrice ? ((p.sellingPrice - p.costPrice) / p.sellingPrice * 100).toFixed(1) + '%' : '-'}</h4></div></div>
                    </div>
                    <div class="col-12"><div class="detail-label">Description</div><div class="detail-value">${p.description || 'No description available'}</div></div>
                </div>`;
        }
        Modal.show('viewProductModal');
    },

    filter(search = '', status = '') {
        let filtered = this.data;
        if (search) {
            const s = search.toLowerCase();
            filtered = filtered.filter(p =>
                p.productName?.toLowerCase().includes(s) ||
                p.productCode?.toLowerCase().includes(s) ||
                p.categoryName?.toLowerCase().includes(s) ||
                p.brandName?.toLowerCase().includes(s)
            );
        }
        if (status === 'in-stock') filtered = filtered.filter(p => p.stockQuantity > 10);
        else if (status === 'low-stock') filtered = filtered.filter(p => p.stockQuantity > 0 && p.stockQuantity <= 10);
        else if (status === 'out-of-stock') filtered = filtered.filter(p => p.stockQuantity === 0);

        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('productsTableBody');
        if (!tbody) return;
        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No products found</td></tr>';
            return;
        }
        tbody.innerHTML = data.map(p => `
            <tr>
                <td><strong>${p.productCode || '-'}</strong></td>
                <td><div class="fw-semibold">${p.productName}</div><small class="text-muted">${p.categoryName || ''}</small></td>
                <td>${p.brandName || '-'}</td>
                <td class="text-end">${Format.currency(p.sellingPrice)}</td>
                <td class="text-center"><span class="badge ${p.stockQuantity > 10 ? 'bg-success' : p.stockQuantity > 0 ? 'bg-warning' : 'bg-danger'}">${p.stockQuantity} in stock</span></td>
                <td class="text-center">${Format.statusBadge(p.status)}</td>
                <td class="text-center"><button class="btn btn-sm btn-outline-primary" onclick="SalesProducts.view(${p.productId})">${Icons.view}</button></td>
            </tr>
        `).join('');
    }
};

// ===========================================
// PDF Report Generator
// ===========================================
const SalesReportPDF = {
    generate(orders, period, periodLabel) {
        const now = new Date();
        const filteredOrders = this.filterByPeriod(orders, period);
        const totalRevenue = filteredOrders.reduce((s, o) => s + (o.totalAmount || 0), 0);
        const avgOrder = filteredOrders.length > 0 ? totalRevenue / filteredOrders.length : 0;
        const completedOrders = filteredOrders.filter(o => o.orderStatus === 'Completed' || o.orderStatus === 'Delivered').length;

        const html = `<html><head><title>CompuGear Sales Report - ${periodLabel}</title>
            <style>
                *{margin:0;padding:0;box-sizing:border-box}
                body{font-family:'Segoe UI',Arial,sans-serif;margin:0;padding:40px;color:#333;font-size:12px}
                .header{display:flex;justify-content:space-between;align-items:center;border-bottom:3px solid #008080;padding-bottom:20px;margin-bottom:30px}
                .company-logo img { height: 50px; margin-bottom: 5px; }
                .report-title{text-align:right}
                .report-title h2{color:#008080;font-size:20px}
                .report-title p{color:#666}
                .summary-cards{display:flex;gap:20px;margin-bottom:30px}
                .summary-card{flex:1;background:#f8f9fa;border-radius:8px;padding:15px;text-align:center;border-left:4px solid #008080}
                .summary-card .value{font-size:24px;font-weight:bold;color:#008080}
                .summary-card .label{color:#666;font-size:11px;text-transform:uppercase}
                table{width:100%;border-collapse:collapse;margin-bottom:20px;font-size:11px}
                th{background:#008080;color:white;padding:10px 8px;text-align:left;font-weight:600}
                td{padding:8px;border-bottom:1px solid #e0e0e0}
                tr:nth-child(even){background:#f8f9fa}
                .text-end{text-align:right}
                .text-center{text-align:center}
                .badge{padding:3px 8px;border-radius:4px;font-size:10px;font-weight:600;color:white}
                .bg-success{background:#198754}.bg-warning{background:#ffc107;color:#333}.bg-danger{background:#dc3545}.bg-info{background:#0dcaf0;color:#333}.bg-primary{background:#0d6efd}
                .footer{margin-top:30px;padding-top:15px;border-top:2px solid #e0e0e0;text-align:center;color:#666;font-size:10px}
                .total-row{font-weight:bold;background:#e8f5f5!important}
                @media print{body{margin:15px;padding:15px}.no-print{display:none}}
            </style></head><body>
            <div class="header">
                <div class="company">
                    <div class="company-logo">
                        <img src="${window.location.origin}/images/compugearlogo.png" alt="CompuGear" onerror="this.outerHTML='<h1 style=\\'color:#008080;font-size:28px;margin-bottom:5px\\'>CompuGear</h1>'">
                    </div>
                    <p>Computer & Gear Solutions</p>
                </div>
                <div class="report-title"><h2>Sales Report</h2><p>Period: ${periodLabel}</p><p>Generated: ${now.toLocaleDateString('en-PH', { year: 'numeric', month: 'long', day: 'numeric' })}</p></div>
            </div>
            <div class="summary-cards">
                <div class="summary-card"><div class="value">${filteredOrders.length}</div><div class="label">Total Orders</div></div>
                <div class="summary-card"><div class="value">${Format.currency(totalRevenue)}</div><div class="label">Total Revenue</div></div>
                <div class="summary-card"><div class="value">${Format.currency(avgOrder)}</div><div class="label">Avg Order Value</div></div>
                <div class="summary-card"><div class="value">${completedOrders}</div><div class="label">Completed Orders</div></div>
            </div>
            <h3 style="color:#008080;margin-bottom:15px">Order Details</h3>
            <table><thead><tr><th>Order #</th><th>Customer</th><th>Date</th><th class="text-center">Items</th><th class="text-end">Amount</th><th class="text-center">Status</th><th class="text-center">Payment</th></tr></thead>
            <tbody>
                ${filteredOrders.map(o => `<tr>
                    <td><strong>${o.orderNumber}</strong></td>
                    <td>${o.customerName || 'Guest'}</td>
                    <td>${new Date(o.orderDate).toLocaleDateString('en-PH')}</td>
                    <td class="text-center">${o.itemCount || 0}</td>
                    <td class="text-end">${Format.currency(o.totalAmount)}</td>
                    <td class="text-center"><span class="badge ${o.orderStatus === 'Completed' || o.orderStatus === 'Delivered' ? 'bg-success' : o.orderStatus === 'Pending' ? 'bg-warning' : o.orderStatus === 'Cancelled' ? 'bg-danger' : 'bg-info'}">${o.orderStatus}</span></td>
                    <td class="text-center"><span class="badge ${o.paymentStatus === 'Paid' ? 'bg-success' : o.paymentStatus === 'Pending' ? 'bg-warning' : 'bg-danger'}">${o.paymentStatus || 'Pending'}</span></td>
                </tr>`).join('')}
                <tr class="total-row"><td colspan="4" class="text-end"><strong>Grand Total:</strong></td><td class="text-end"><strong>${Format.currency(totalRevenue)}</strong></td><td colspan="2"></td></tr>
            </tbody></table>
            <div class="footer"><p>CompuGear Sales Report - Generated on ${now.toLocaleString('en-PH')} - Confidential</p></div>
            <script>window.onload=function(){window.print();window.close();}<\/script></body></html>`;
            
        var printWindow = window.open('', '_blank');
        printWindow.document.write(html);
        printWindow.document.close();
    },

    filterByPeriod(orders, period) {
        const now = new Date();
        const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        switch (period) {
            case 'day': return orders.filter(o => new Date(o.orderDate) >= startOfToday);
            case 'week': { const d = new Date(startOfToday); d.setDate(d.getDate() - d.getDay()); return orders.filter(o => new Date(o.orderDate) >= d); }
            case 'month': return orders.filter(o => new Date(o.orderDate) >= new Date(now.getFullYear(), now.getMonth(), 1));
            case 'year': return orders.filter(o => new Date(o.orderDate) >= new Date(now.getFullYear(), 0, 1));
            default: return orders;
        }
    }
};

// Export for global access
window.Toast = Toast;
window.API = API;
window.Format = Format;
window.Modal = Modal;
window.Icons = Icons;
window.SalesCustomers = SalesCustomers;
window.SalesOrders = SalesOrders;
window.SalesLeads = SalesLeads;
window.SalesProducts = SalesProducts;
window.SalesReportPDF = SalesReportPDF;
