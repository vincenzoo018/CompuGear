/**
 * CompuGear Billing Staff JavaScript
 * Handles Billing and Accounting module functionality
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
    print: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="6 9 6 2 18 2 18 9"/><path d="M6 18H4a2 2 0 0 1-2-2v-5a2 2 0 0 1 2-2h16a2 2 0 0 1 2 2v5a2 2 0 0 1-2 2h-2"/><rect x="6" y="14" width="12" height="8"/></svg>',
    payment: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="1" y="4" width="22" height="16" rx="2" ry="2"/><line x1="1" y1="10" x2="23" y2="10"/></svg>'
};

// Toast System
const Toast = {
    container: null,
    init() {
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.className = 'toast-container position-fixed top-0 end-0 p-3';
            this.container.style.zIndex = '9999';
            document.body.appendChild(this.container);
        }
    },
    show(message, type = 'success', duration = 4000) {
        this.init();
        const toast = document.createElement('div');
        toast.className = 'toast align-items-center text-white border-0 show';
        toast.style.backgroundColor = type === 'success' ? '#008080' : type === 'error' ? '#dc3545' : '#ff6b35';
        toast.innerHTML = `<div class="d-flex"><div class="toast-body">${message}</div><button type="button" class="btn-close btn-close-white me-2 m-auto" onclick="this.parentElement.parentElement.remove()"></button></div>`;
        this.container.appendChild(toast);
        setTimeout(() => toast.remove(), duration);
    },
    success(message) { this.show(message, 'success'); },
    error(message) { this.show(message, 'error'); },
    warning(message) { this.show(message, 'warning'); }
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
            return result;
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    },
    get(endpoint) { return this.request(endpoint, 'GET'); },
    post(endpoint, data) { return this.request(endpoint, 'POST', data); },
    put(endpoint, data) { return this.request(endpoint, 'PUT', data); }
};

// Format Helpers
const Format = {
    currency(amount) {
        return `${CONFIG.currency}${parseFloat(amount || 0).toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    },
    date(dateString) {
        if (!dateString) return '-';
        return new Date(dateString).toLocaleDateString(CONFIG.dateFormat, { year: 'numeric', month: 'short', day: 'numeric' });
    },
    statusBadge(status) {
        const colors = {
            'Paid': 'success', 'Pending': 'warning', 'Overdue': 'danger',
            'Draft': 'secondary', 'Partial': 'info', 'Cancelled': 'dark',
            'Completed': 'success', 'Processing': 'info', 'Refunded': 'warning'
        };
        return `<span class="badge bg-${colors[status] || 'secondary'}">${status}</span>`;
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
        if (form) { form.reset(); form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid')); }
    }
};

// ===========================================
// INVOICES MODULE
// ===========================================
const Invoices = {
    data: [],
    currentId: null,

    async load() {
        try {
            this.data = await API.get('/invoices');
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load invoices');
        }
    },

    render() {
        const tbody = document.getElementById('invoicesTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No invoices found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(i => `
            <tr>
                <td><strong>${i.invoiceNumber}</strong></td>
                <td>${i.customerName || 'N/A'}</td>
                <td>${Format.date(i.invoiceDate)}</td>
                <td>${Format.date(i.dueDate)}</td>
                <td class="text-end">${Format.currency(i.totalAmount)}</td>
                <td>${Format.statusBadge(i.status)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Invoices.view(${i.invoiceId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="Invoices.edit(${i.invoiceId})">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-info" onclick="Invoices.print(${i.invoiceId})">${Icons.print}</button>
                        ${i.status !== 'Paid' ? `<button class="btn btn-sm btn-outline-success" onclick="Invoices.recordPayment(${i.invoiceId})">${Icons.payment}</button>` : ''}
                    </div>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const total = this.data.reduce((sum, i) => sum + (i.totalAmount || 0), 0);
        const paid = this.data.filter(i => i.status === 'Paid').reduce((sum, i) => sum + (i.totalAmount || 0), 0);
        const pending = this.data.filter(i => i.status === 'Pending').reduce((sum, i) => sum + (i.totalAmount || 0), 0);
        const overdue = this.data.filter(i => i.status === 'Overdue').reduce((sum, i) => sum + (i.totalAmount || 0), 0);

        if (document.getElementById('totalInvoiced')) document.getElementById('totalInvoiced').textContent = Format.currency(total);
        if (document.getElementById('totalPaid')) document.getElementById('totalPaid').textContent = Format.currency(paid);
        if (document.getElementById('totalPending')) document.getElementById('totalPending').textContent = Format.currency(pending);
        if (document.getElementById('totalOverdue')) document.getElementById('totalOverdue').textContent = Format.currency(overdue);
    },

    view(id) {
        window.location.href = `/BillingStaff/InvoiceDetails/${id}`;
    },

    edit(id) {
        this.currentId = id;
        const invoice = this.data.find(i => i.invoiceId === id);
        if (!invoice) return;
        Modal.show('invoiceModal');
    },

    print(id) {
        window.open(`/BillingStaff/PrintInvoice/${id}`, '_blank');
    },

    recordPayment(id) {
        this.currentId = id;
        const invoice = this.data.find(i => i.invoiceId === id);
        if (!invoice) return;
        
        document.getElementById('paymentInvoiceId').value = id;
        document.getElementById('paymentAmount').value = invoice.totalAmount - (invoice.paidAmount || 0);
        Modal.show('paymentModal');
    },

    async savePayment() {
        const data = {
            invoiceId: document.getElementById('paymentInvoiceId').value,
            amount: parseFloat(document.getElementById('paymentAmount').value) || 0,
            paymentMethod: document.getElementById('paymentMethod').value,
            reference: document.getElementById('paymentReference').value,
            notes: document.getElementById('paymentNotes').value
        };

        try {
            await API.post('/payments', data);
            Toast.success('Payment recorded successfully');
            Modal.hide('paymentModal');
            this.load();
        } catch (error) {
            Toast.error('Failed to record payment');
        }
    },

    async create() {
        const data = {
            customerId: document.getElementById('invoiceCustomer').value,
            dueDate: document.getElementById('invoiceDueDate').value,
            items: this.getInvoiceItems(),
            notes: document.getElementById('invoiceNotes').value
        };

        try {
            await API.post('/invoices', data);
            Toast.success('Invoice created successfully');
            Modal.hide('invoiceModal');
            this.load();
        } catch (error) {
            Toast.error('Failed to create invoice');
        }
    },

    getInvoiceItems() {
        const items = [];
        document.querySelectorAll('.invoice-item-row').forEach(row => {
            items.push({
                description: row.querySelector('.item-description').value,
                quantity: parseInt(row.querySelector('.item-quantity').value) || 1,
                unitPrice: parseFloat(row.querySelector('.item-price').value) || 0
            });
        });
        return items;
    }
};

// ===========================================
// PAYMENTS MODULE
// ===========================================
const Payments = {
    data: [],

    async load() {
        try {
            this.data = await API.get('/payments');
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load payments');
        }
    },

    render() {
        const tbody = document.getElementById('paymentsTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">No payments found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(p => `
            <tr>
                <td><strong>${p.paymentNumber || p.paymentId}</strong></td>
                <td>${p.invoiceNumber || 'N/A'}</td>
                <td>${p.customerName || 'N/A'}</td>
                <td>${Format.date(p.paymentDate)}</td>
                <td class="text-end">${Format.currency(p.amount)}</td>
                <td><span class="badge bg-info">${p.paymentMethod}</span></td>
            </tr>
        `).join('');
    },

    updateStats() {
        const today = new Date().toDateString();
        const todayPayments = this.data.filter(p => new Date(p.paymentDate).toDateString() === today);
        const todayTotal = todayPayments.reduce((sum, p) => sum + (p.amount || 0), 0);
        const totalPayments = this.data.reduce((sum, p) => sum + (p.amount || 0), 0);

        if (document.getElementById('todayPayments')) document.getElementById('todayPayments').textContent = Format.currency(todayTotal);
        if (document.getElementById('totalPayments')) document.getElementById('totalPayments').textContent = Format.currency(totalPayments);
        if (document.getElementById('paymentCount')) document.getElementById('paymentCount').textContent = this.data.length;
    }
};

// ===========================================
// CUSTOMER ACCOUNTS MODULE
// ===========================================
const CustomerAccounts = {
    data: [],

    async load() {
        try {
            this.data = await API.get('/customers');
            this.render();
        } catch (error) {
            Toast.error('Failed to load customer accounts');
        }
    },

    render() {
        const tbody = document.getElementById('accountsTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">No accounts found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(c => `
            <tr>
                <td><strong>${c.customerCode}</strong></td>
                <td>
                    <div class="fw-semibold">${c.firstName} ${c.lastName}</div>
                    <small class="text-muted">${c.email}</small>
                </td>
                <td>${c.totalOrders || 0}</td>
                <td class="text-end">${Format.currency(c.totalSpent)}</td>
                <td class="text-end">${Format.currency(c.balance || 0)}</td>
                <td class="text-center">
                    <button class="btn btn-sm btn-outline-primary" onclick="CustomerAccounts.viewStatement(${c.customerId})">${Icons.view}</button>
                </td>
            </tr>
        `).join('');
    },

    viewStatement(id) {
        window.location.href = `/BillingStaff/CustomerStatement/${id}`;
    }
};

// ===========================================
// FINANCIAL REPORTS MODULE
// ===========================================
const FinancialReports = {
    async load() {
        try {
            const [invoices, payments, orders] = await Promise.all([
                API.get('/invoices').catch(() => []),
                API.get('/payments').catch(() => []),
                API.get('/orders').catch(() => [])
            ]);

            this.renderRevenueChart(orders);
            this.renderPaymentMethodsChart(payments);
            this.updateSummary(invoices, payments, orders);
        } catch (error) {
            Toast.error('Failed to load reports');
        }
    },

    renderRevenueChart(orders) {
        const ctx = document.getElementById('revenueChart')?.getContext('2d');
        if (!ctx) return;

        const monthlyData = Array(12).fill(0);
        const currentYear = new Date().getFullYear();

        orders.forEach(o => {
            const date = new Date(o.orderDate);
            if (date.getFullYear() === currentYear) {
                monthlyData[date.getMonth()] += o.totalAmount || 0;
            }
        });

        new Chart(ctx, {
            type: 'line',
            data: {
                labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
                datasets: [{
                    label: 'Revenue',
                    data: monthlyData,
                    borderColor: '#008080',
                    backgroundColor: 'rgba(0, 128, 128, 0.1)',
                    fill: true,
                    tension: 0.4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true, ticks: { callback: v => '₱' + (v/1000).toFixed(0) + 'k' } } }
            }
        });
    },

    renderPaymentMethodsChart(payments) {
        const ctx = document.getElementById('paymentMethodsChart')?.getContext('2d');
        if (!ctx) return;

        const methods = {};
        payments.forEach(p => {
            const method = p.paymentMethod || 'Other';
            methods[method] = (methods[method] || 0) + (p.amount || 0);
        });

        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: Object.keys(methods),
                datasets: [{
                    data: Object.values(methods),
                    backgroundColor: ['#008080', '#10B981', '#3B82F6', '#F59E0B', '#EF4444']
                }]
            },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
        });
    },

    updateSummary(invoices, payments, orders) {
        const totalRevenue = orders.reduce((sum, o) => sum + (o.totalAmount || 0), 0);
        const totalInvoiced = invoices.reduce((sum, i) => sum + (i.totalAmount || 0), 0);
        const totalCollected = payments.reduce((sum, p) => sum + (p.amount || 0), 0);
        const outstanding = totalInvoiced - totalCollected;

        if (document.getElementById('totalRevenue')) document.getElementById('totalRevenue').textContent = Format.currency(totalRevenue);
        if (document.getElementById('totalInvoiced')) document.getElementById('totalInvoiced').textContent = Format.currency(totalInvoiced);
        if (document.getElementById('totalCollected')) document.getElementById('totalCollected').textContent = Format.currency(totalCollected);
        if (document.getElementById('outstanding')) document.getElementById('outstanding').textContent = Format.currency(outstanding);
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    const path = window.location.pathname.toLowerCase();
    
    if (path.includes('/billingstaff/invoices') || path.endsWith('/invoices')) {
        Invoices.load();
    } else if (path.includes('/billingstaff/payments') || path.endsWith('/payments')) {
        Payments.load();
    } else if (path.includes('/billingstaff/accounts') || path.endsWith('/accounts')) {
        CustomerAccounts.load();
    } else if (path.includes('/billingstaff/reports') || path.endsWith('/reports')) {
        FinancialReports.load();
    } else if (path === '/billingstaff' || path === '/billingstaff/') {
        // Dashboard
        Promise.all([
            API.get('/invoices').catch(() => []),
            API.get('/payments').catch(() => []),
            API.get('/orders').catch(() => [])
        ]).then(([invoices, payments, orders]) => {
            const totalRevenue = orders.reduce((sum, o) => sum + (o.totalAmount || 0), 0);
            const pending = invoices.filter(i => i.status === 'Pending').reduce((sum, i) => sum + (i.totalAmount || 0), 0);
            const paid = invoices.filter(i => i.status === 'Paid').reduce((sum, i) => sum + (i.totalAmount || 0), 0);
            
            if (document.getElementById('totalRevenue')) document.getElementById('totalRevenue').textContent = Format.currency(totalRevenue);
            if (document.getElementById('pendingAmount')) document.getElementById('pendingAmount').textContent = Format.currency(pending);
            if (document.getElementById('paidAmount')) document.getElementById('paidAmount').textContent = Format.currency(paid);
            if (document.getElementById('invoiceCount')) document.getElementById('invoiceCount').textContent = invoices.length;
        });
    }
});
