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
    payment: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="1" y="4" width="22" height="16" rx="2" ry="2"/><line x1="1" y1="10" x2="23" y2="10"/></svg>',
    download: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>'
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
    date(dateString) {
        if (!dateString) return '-';
        return new Date(dateString).toLocaleDateString(CONFIG.dateFormat, { year: 'numeric', month: 'short', day: 'numeric' });
    },
    statusBadge(status) {
        const colors = {
            'Paid': 'success', 'Paid/Confirmed': 'primary', 'Pending': 'warning', 'Overdue': 'danger',
            'Draft': 'secondary', 'Partial': 'info', 'Cancelled': 'dark',
            'Completed': 'success', 'Processing': 'info', 'Refunded': 'warning',
            'Void': 'dark', 'Active': 'success'
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
// INVOICES MODULE (Billing Staff - Limited Access)
// ===========================================
const Invoices = {
    data: [],
    currentId: null,

    async load() {
        try {
            const [invoices, orders] = await Promise.all([
                API.get('/invoices'),
                API.get('/orders')
            ]);

            const invoiceList = Array.isArray(invoices) ? invoices : [];
            const orderList = Array.isArray(orders) ? orders : [];

            const existingOrderIds = new Set(invoiceList.filter(i => i.orderId).map(i => i.orderId));
            const derivedOrderRows = orderList
                .filter(o => (o.orderStatus === 'Pending' || o.orderStatus === 'Confirmed') && !existingOrderIds.has(o.orderId))
                .map(o => {
                    const paidAmount = o.paidAmount || 0;
                    const totalAmount = o.totalAmount || 0;
                    const balanceDue = Math.max(0, totalAmount - paidAmount);
                    const derivedStatus = o.paymentStatus === 'Paid'
                        ? 'Paid'
                        : (paidAmount > 0 ? 'Partial' : 'Pending');

                    return {
                        invoiceId: -o.orderId,
                        invoiceNumber: `ORD-${o.orderNumber}`,
                        orderId: o.orderId,
                        orderNumber: o.orderNumber,
                        customerId: o.customerId,
                        customerName: o.customerName,
                        invoiceDate: o.orderDate,
                        dueDate: o.orderDate,
                        totalAmount: totalAmount,
                        paidAmount: paidAmount,
                        balanceDue: balanceDue,
                        subtotal: o.subtotal || totalAmount,
                        taxAmount: o.taxAmount || 0,
                        shippingAmount: o.shippingAmount || 0,
                        discountAmount: o.discountAmount || 0,
                        status: derivedStatus,
                        isOrderDerived: true,
                        orderStatus: o.orderStatus,
                        paymentStatus: o.paymentStatus,
                        paymentMethod: o.paymentMethod,
                        shippingMethod: o.shippingMethod,
                        trackingNumber: o.trackingNumber,
                        confirmedAt: o.confirmedAt,
                        notes: o.notes,
                        items: o.items || []
                    };
                });

            this.data = [...invoiceList, ...derivedOrderRows];
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
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4">No invoices found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(i => `
            <tr>
                <td>
                    <div class="fw-semibold">${i.invoiceNumber}</div>
                    <small class="text-muted">${Format.date(i.invoiceDate)}</small>
                </td>
                <td>${i.customerName || 'N/A'}</td>
                <td>${Format.date(i.dueDate)}</td>
                <td class="text-end">${Format.currency(i.totalAmount)}</td>
                <td class="text-end text-success">${Format.currency(i.paidAmount)}</td>
                <td class="text-end ${(i.balance || 0) > 0 ? 'text-danger' : ''}">${Format.currency(i.balance || (i.totalAmount - (i.paidAmount || 0)))}</td>
                <td>${Format.statusBadge(i.status)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Invoices.view(${i.invoiceId})" title="View">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-info" onclick="Invoices.print(${i.invoiceId})" title="Print">${Icons.print}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const total = this.data.reduce((sum, i) => sum + (i.totalAmount || 0), 0);
        const paid = this.data.filter(i => i.status === 'Paid' || i.status === 'Paid/Confirmed').reduce((sum, i) => sum + (i.totalAmount || 0), 0);
        const pending = this.data.filter(i => i.status === 'Pending' || i.status === 'Partial').reduce((sum, i) => sum + ((i.totalAmount || 0) - (i.paidAmount || 0)), 0);
        const overdue = this.data.filter(i => i.status === 'Overdue' || (!['Paid', 'Paid/Confirmed', 'Cancelled', 'Void'].includes(i.status) && new Date(i.dueDate) < new Date())).reduce((sum, i) => sum + ((i.totalAmount || 0) - (i.paidAmount || 0)), 0);

        if (document.getElementById('totalInvoiced')) document.getElementById('totalInvoiced').textContent = Format.currency(total);
        if (document.getElementById('totalPaid')) document.getElementById('totalPaid').textContent = Format.currency(paid);
        if (document.getElementById('totalPending')) document.getElementById('totalPending').textContent = Format.currency(pending);
        if (document.getElementById('totalOverdue')) document.getElementById('totalOverdue').textContent = Format.currency(overdue);
    },

    async view(id) {
        if (id < 0) {
            const orderId = Math.abs(id);
            const order = await API.get(`/orders/${orderId}`);
            this.currentId = id;
            const content = document.getElementById('viewInvoiceContent');
            if (!content) return;

            const orderItems = Array.isArray(order.orderItems) ? order.orderItems : [];
            const itemsHtml = orderItems.length > 0
                ? `<div class="col-12 mt-3"><h6 class="mb-2">Order Items</h6><div class="table-responsive"><table class="table table-sm table-bordered"><thead class="table-light"><tr><th>Description</th><th class="text-center">Qty</th><th class="text-end">Unit Price</th><th class="text-end">Amount</th></tr></thead><tbody>${orderItems.map(item => `<tr><td>${item.productName}${item.productCode ? ` <small class="text-muted">(${item.productCode})</small>` : ''}</td><td class="text-center">${item.quantity}</td><td class="text-end">${Format.currency(item.unitPrice)}</td><td class="text-end">${Format.currency(item.totalPrice)}</td></tr>`).join('')}</tbody></table></div></div>`
                : '';

            const total = order.totalAmount || 0;
            const paid = order.paidAmount || 0;
            const balance = Math.max(0, total - paid);
            const status = order.paymentStatus === 'Paid' ? 'Paid' : (paid > 0 ? 'Partial' : 'Pending');

            content.innerHTML = `
                <div class="row g-4">
                    <div class="col-12 d-flex justify-content-between align-items-start border-bottom pb-3">
                        <div>
                            <h5 class="mb-1">Order ${order.orderNumber}</h5>
                            <small class="text-muted">Order Date: ${Format.date(order.orderDate)}</small>
                        </div>
                        ${Format.statusBadge(status)}
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Customer</h6>
                        <div class="fw-semibold">${order.customer?.firstName || ''} ${order.customer?.lastName || ''}</div>
                        <div class="text-muted small">${order.customer?.email || ''}</div>
                        <div class="text-muted small">${order.customer?.phone || ''}</div>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Order Details</h6>
                        <div><small class="text-muted">Order Status:</small> ${Format.statusBadge(order.orderStatus || 'Pending')}</div>
                        <div><small class="text-muted">Payment Status:</small> ${Format.statusBadge(order.paymentStatus || 'Pending')}</div>
                        <div><small class="text-muted">Payment Method:</small> ${order.paymentMethod || '-'}</div>
                        <div><small class="text-muted">Shipping Method:</small> ${order.shippingMethod || '-'}</div>
                        <div><small class="text-muted">Tracking #:</small> ${order.trackingNumber || '-'}</div>
                    </div>
                    ${itemsHtml}
                    <div class="col-12">
                        <div class="row g-3 mt-2">
                            <div class="col-md-3"><div class="card bg-light"><div class="card-body text-center py-3"><div class="small text-muted">Subtotal</div><h5 class="mb-0">${Format.currency(order.subtotal || total)}</h5></div></div></div>
                            <div class="col-md-3"><div class="card bg-light"><div class="card-body text-center py-3"><div class="small text-muted">Tax</div><h5 class="mb-0">${Format.currency(order.taxAmount || 0)}</h5></div></div></div>
                            <div class="col-md-3"><div class="card bg-primary text-white"><div class="card-body text-center py-3"><div class="small">Total</div><h4 class="mb-0">${Format.currency(total)}</h4></div></div></div>
                            <div class="col-md-3"><div class="card ${balance > 0 ? 'bg-danger' : 'bg-success'} text-white"><div class="card-body text-center py-3"><div class="small">Balance</div><h4 class="mb-0">${Format.currency(balance)}</h4></div></div></div>
                        </div>
                    </div>
                    ${order.notes ? `<div class="col-12"><h6 class="text-muted">Notes</h6><p>${order.notes}</p></div>` : ''}
                </div>`;

            Modal.show('viewInvoiceModal');
            return;
        }

        try {
            const result = await API.get(`/invoices/${id}/pdf`);
            const inv = result.data || result;
            this.currentId = id;
            const content = document.getElementById('viewInvoiceContent');
            if (!content) return;

            let itemsHtml = '';
            if (inv.items && inv.items.length > 0) {
                itemsHtml = `
                    <div class="col-12 mt-3">
                        <h6 class="mb-2">Invoice Items</h6>
                        <div class="table-responsive">
                            <table class="table table-sm table-bordered">
                                <thead class="table-light">
                                    <tr><th>Description</th><th class="text-center">Qty</th><th class="text-end">Unit Price</th><th class="text-end">Amount</th></tr>
                                </thead>
                                <tbody>
                                    ${inv.items.map(item => `<tr>
                                        <td>${item.description}${item.productCode ? ` <small class="text-muted">(${item.productCode})</small>` : ''}</td>
                                        <td class="text-center">${item.quantity}</td>
                                        <td class="text-end">${Format.currency(item.unitPrice)}</td>
                                        <td class="text-end">${Format.currency(item.totalPrice)}</td>
                                    </tr>`).join('')}
                                </tbody>
                            </table>
                        </div>
                    </div>`;
            }

            let paymentsHtml = '<p class="text-muted">No payments recorded yet.</p>';
            if (inv.payments && inv.payments.length > 0) {
                paymentsHtml = `<div class="table-responsive"><table class="table table-sm">
                    <thead><tr><th>Payment #</th><th>Date</th><th>Method</th><th class="text-end">Amount</th><th>Status</th></tr></thead>
                    <tbody>${inv.payments.map(p => `<tr>
                        <td>${p.paymentNumber || '-'}</td>
                        <td>${Format.date(p.paymentDate)}</td>
                        <td><span class="badge bg-info">${p.paymentMethodType || '-'}</span></td>
                        <td class="text-end text-success">${Format.currency(p.amount)}</td>
                        <td>${Format.statusBadge(p.status || 'Completed')}</td>
                    </tr>`).join('')}</tbody></table></div>`;
            }

            content.innerHTML = `
                <div class="row g-4">
                    <div class="col-12 d-flex justify-content-between align-items-start border-bottom pb-3">
                        <div>
                            <h5 class="mb-1">${inv.invoiceNumber}</h5>
                            <small class="text-muted">Invoice Date: ${Format.date(inv.invoiceDate)}</small>
                            ${inv.orderNumber ? `<br><small class="text-muted">Order: ${inv.orderNumber}</small>` : ''}
                        </div>
                        ${Format.statusBadge(inv.status)}
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Bill To</h6>
                        <div class="fw-semibold">${inv.customerName || inv.billingName || 'N/A'}</div>
                        <div class="text-muted small">${inv.billingAddress || ''}</div>
                        ${inv.billingCity ? `<div class="text-muted small">${inv.billingCity}${inv.billingState ? ', ' + inv.billingState : ''} ${inv.billingZipCode || ''}</div>` : ''}
                        ${inv.customerEmail || inv.billingEmail ? `<div class="text-muted small">${inv.customerEmail || inv.billingEmail}</div>` : ''}
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Invoice Details</h6>
                        <div><small class="text-muted">Due Date:</small> ${Format.date(inv.dueDate)}</div>
                        <div><small class="text-muted">Payment Terms:</small> ${inv.paymentTerms || 'Net 30'}</div>
                    </div>
                    ${itemsHtml}
                    <div class="col-12">
                        <div class="row g-3 mt-2">
                            <div class="col-md-3"><div class="card bg-light"><div class="card-body text-center py-3"><div class="small text-muted">Subtotal</div><h5 class="mb-0">${Format.currency(inv.subtotal || inv.totalAmount)}</h5></div></div></div>
                            <div class="col-md-3"><div class="card bg-light"><div class="card-body text-center py-3"><div class="small text-muted">Tax</div><h5 class="mb-0">${Format.currency(inv.taxAmount || 0)}</h5></div></div></div>
                            <div class="col-md-3"><div class="card ${inv.status === 'Paid' ? 'bg-success' : 'bg-primary'} text-white"><div class="card-body text-center py-3"><div class="small">Total</div><h4 class="mb-0">${Format.currency(inv.totalAmount)}</h4></div></div></div>
                            <div class="col-md-3"><div class="card ${(inv.balanceDue || 0) > 0 ? 'bg-danger' : 'bg-success'} text-white"><div class="card-body text-center py-3"><div class="small">Balance</div><h4 class="mb-0">${Format.currency(inv.balanceDue || 0)}</h4></div></div></div>
                        </div>
                    </div>
                    <div class="col-12 mt-3"><h6 class="mb-2">Payment History</h6>${paymentsHtml}</div>
                    ${inv.notes ? `<div class="col-12"><h6 class="text-muted">Notes</h6><p>${inv.notes}</p></div>` : ''}
                </div>`;
            Modal.show('viewInvoiceModal');
        } catch (error) {
            const inv = this.data.find(i => i.invoiceId === id);
            if (!inv) { Toast.error('Invoice not found'); return; }
            this.currentId = id;
            const content = document.getElementById('viewInvoiceContent');
            if (content) {
                content.innerHTML = `<div class="row g-4">
                    <div class="col-12 d-flex justify-content-between align-items-start border-bottom pb-3">
                        <div><h5 class="mb-1">${inv.invoiceNumber}</h5><small class="text-muted">Invoice Date: ${Format.date(inv.invoiceDate)}</small></div>
                        ${Format.statusBadge(inv.status)}
                    </div>
                    <div class="col-md-6"><div class="detail-label">Customer</div><div class="detail-value fw-semibold">${inv.customerName || 'N/A'}</div></div>
                    <div class="col-md-6"><div class="detail-label">Due Date</div><div class="detail-value">${Format.date(inv.dueDate)}</div></div>
                    <div class="col-12"><div class="row g-3 mt-2">
                        <div class="col-md-4"><div class="card bg-light"><div class="card-body text-center py-3"><div class="small text-muted">Total</div><h5 class="mb-0">${Format.currency(inv.totalAmount)}</h5></div></div></div>
                        <div class="col-md-4"><div class="card bg-light"><div class="card-body text-center py-3"><div class="small text-muted">Paid</div><h5 class="mb-0 text-success">${Format.currency(inv.paidAmount)}</h5></div></div></div>
                        <div class="col-md-4"><div class="card ${(inv.balance || 0) > 0 ? 'bg-danger' : 'bg-success'} text-white"><div class="card-body text-center py-3"><div class="small">Balance</div><h5 class="mb-0">${Format.currency(inv.balance || (inv.totalAmount - (inv.paidAmount || 0)))}</h5></div></div></div>
                    </div></div>
                </div>`;
            }
            Modal.show('viewInvoiceModal');
        }
    },

    async print(id) {
        if (id < 0) {
            try {
                const orderId = Math.abs(id);
                const order = await API.get(`/orders/${orderId}`);
                const derivedInvoice = {
                    invoiceNumber: `ORD-${order.orderNumber}`,
                    customerName: order.customer ? `${order.customer.firstName || ''} ${order.customer.lastName || ''}`.trim() : 'N/A',
                    billingAddress: order.billingAddress || order.shippingAddress || '',
                    billingCity: order.billingCity || order.shippingCity || '',
                    billingState: order.billingState || order.shippingState || '',
                    billingZipCode: order.billingZipCode || order.shippingZipCode || '',
                    customerEmail: order.customer?.email || '',
                    invoiceDate: order.orderDate,
                    dueDate: order.orderDate,
                    paymentTerms: 'Order Based',
                    orderNumber: order.orderNumber,
                    status: order.paymentStatus === 'Paid' ? 'Paid' : ((order.paidAmount || 0) > 0 ? 'Partial' : 'Pending'),
                    subtotal: order.subtotal || order.totalAmount || 0,
                    discountAmount: order.discountAmount || 0,
                    taxAmount: order.taxAmount || 0,
                    shippingAmount: order.shippingAmount || 0,
                    totalAmount: order.totalAmount || 0,
                    paidAmount: order.paidAmount || 0,
                    balanceDue: Math.max(0, (order.totalAmount || 0) - (order.paidAmount || 0)),
                    notes: order.notes || '',
                    items: Array.isArray(order.orderItems) ? order.orderItems.map(i => ({
                        description: i.productName,
                        productCode: i.productCode,
                        quantity: i.quantity,
                        unitPrice: i.unitPrice,
                        totalPrice: i.totalPrice
                    })) : [],
                    payments: []
                };

                const inv = derivedInvoice;

                let itemsRows = '';
                if (inv.items && inv.items.length > 0) {
                    itemsRows = inv.items.map(item => `<tr>
                        <td style="padding:8px;border:1px solid #ddd;">${item.description}${item.productCode ? ' (' + item.productCode + ')' : ''}</td>
                        <td style="padding:8px;border:1px solid #ddd;text-align:center;">${item.quantity}</td>
                        <td style="padding:8px;border:1px solid #ddd;text-align:right;">${Format.currency(item.unitPrice)}</td>
                        <td style="padding:8px;border:1px solid #ddd;text-align:right;">${Format.currency(item.totalPrice)}</td>
                    </tr>`).join('');
                }

                const w = window.open('', '_blank');
                w.document.write(`<!DOCTYPE html><html><head><title>Invoice ${inv.invoiceNumber}</title>
                <style>
                    body{font-family:Arial,sans-serif;margin:0;padding:20px;color:#333}
                    .header{display:flex;justify-content:space-between;align-items:flex-start;border-bottom:3px solid #008080;padding-bottom:20px;margin-bottom:30px}
                    .company{font-size:28px;font-weight:bold;color:#008080}
                    .inv-title{font-size:24px;color:#333;text-align:right}
                    .inv-num{font-size:14px;color:#666}
                    .bill-section{display:flex;justify-content:space-between;margin-bottom:30px}
                    .bill-to,.inv-details{width:48%}
                    .bill-to h3,.inv-details h3{color:#008080;margin-bottom:10px;font-size:14px;text-transform:uppercase}
                    table{width:100%;border-collapse:collapse}
                    th{background:#008080;color:white;padding:10px 8px;text-align:left}
                    td{padding:8px;border:1px solid #ddd}
                    .totals{margin-top:20px;text-align:right}
                    .totals table{width:300px;margin-left:auto}
                    .totals td{border:none;padding:5px 8px}
                    .total-row{font-size:18px;font-weight:bold;color:#008080;border-top:2px solid #008080!important}
                    .footer{margin-top:40px;padding-top:15px;border-top:1px solid #ddd;text-align:center;color:#999;font-size:12px}
                </style></head><body>
                    <div class="header"><div><div class="company">CompuGear</div><div style="color:#666">Computer Parts & Solutions</div></div>
                    <div style="text-align:right"><div class="inv-title">ORDER INVOICE DETAILS</div><div class="inv-num">${inv.invoiceNumber}</div></div></div>
                    <div class="bill-section">
                        <div class="bill-to"><h3>Customer</h3><strong>${inv.customerName || 'N/A'}</strong><br>${inv.billingAddress ? inv.billingAddress + '<br>' : ''}${inv.customerEmail || ''}</div>
                        <div class="inv-details"><h3>Details</h3><table style="width:100%;border:none"><tr><td style="border:none;padding:3px 0;color:#666">Order Date:</td><td style="border:none;padding:3px 0;text-align:right">${Format.date(inv.invoiceDate)}</td></tr><tr><td style="border:none;padding:3px 0;color:#666">Order Ref:</td><td style="border:none;padding:3px 0;text-align:right">${inv.orderNumber || '-'}</td></tr></table></div>
                    </div>
                    <table><thead><tr><th>Description</th><th style="text-align:center;width:80px">Qty</th><th style="text-align:right;width:120px">Unit Price</th><th style="text-align:right;width:120px">Amount</th></tr></thead>
                    <tbody>${itemsRows || '<tr><td colspan="4" style="text-align:center;padding:20px;border:1px solid #ddd">No items</td></tr>'}</tbody></table>
                    <div class="totals"><table>
                        <tr><td style="color:#666">Total:</td><td style="text-align:right">${Format.currency(inv.totalAmount)}</td></tr>
                        <tr><td style="color:#666">Paid:</td><td style="text-align:right;color:#10B981">${Format.currency(inv.paidAmount)}</td></tr>
                        <tr style="font-weight:bold"><td>Balance Due:</td><td style="text-align:right;color:${(inv.balanceDue||0)>0?'#dc3545':'#10B981'}">${Format.currency(inv.balanceDue)}</td></tr>
                    </table></div>
                    <div class="footer"><p>Generated on ${new Date().toLocaleDateString('en-PH')}</p></div>
                </body></html>`);
                w.document.close();
                return;
            } catch (error) {
                Toast.error('Failed to generate order invoice details for printing');
                return;
            }
        }

        try {
            const result = await API.get(`/invoices/${id}/pdf`);
            const inv = result.data || result;

            let itemsRows = '';
            if (inv.items && inv.items.length > 0) {
                itemsRows = inv.items.map(item => `<tr>
                    <td style="padding:8px;border:1px solid #ddd;">${item.description}${item.productCode ? ' (' + item.productCode + ')' : ''}</td>
                    <td style="padding:8px;border:1px solid #ddd;text-align:center;">${item.quantity}</td>
                    <td style="padding:8px;border:1px solid #ddd;text-align:right;">${Format.currency(item.unitPrice)}</td>
                    <td style="padding:8px;border:1px solid #ddd;text-align:right;">${Format.currency(item.totalPrice)}</td>
                </tr>`).join('');
            }

            let paymentsSection = '';
            if (inv.payments && inv.payments.length > 0) {
                paymentsSection = `
                    <h3 style="margin-top:30px;color:#008080;">Payment History</h3>
                    <table style="width:100%;border-collapse:collapse;margin-top:10px;">
                        <thead><tr style="background:#f8f9fa;">
                            <th style="padding:8px;border:1px solid #ddd;">Payment #</th>
                            <th style="padding:8px;border:1px solid #ddd;">Date</th>
                            <th style="padding:8px;border:1px solid #ddd;">Method</th>
                            <th style="padding:8px;border:1px solid #ddd;text-align:right;">Amount</th>
                        </tr></thead>
                        <tbody>${inv.payments.map(p => `<tr>
                            <td style="padding:8px;border:1px solid #ddd;">${p.paymentNumber || '-'}</td>
                            <td style="padding:8px;border:1px solid #ddd;">${Format.date(p.paymentDate)}</td>
                            <td style="padding:8px;border:1px solid #ddd;">${p.paymentMethodType || '-'}</td>
                            <td style="padding:8px;border:1px solid #ddd;text-align:right;">${Format.currency(p.amount)}</td>
                        </tr>`).join('')}</tbody>
                    </table>`;
            }

            const w = window.open('', '_blank');
            w.document.write(`<!DOCTYPE html><html><head><title>Invoice ${inv.invoiceNumber}</title>
            <style>
                body{font-family:Arial,sans-serif;margin:0;padding:20px;color:#333}
                .header{display:flex;justify-content:space-between;align-items:flex-start;border-bottom:3px solid #008080;padding-bottom:20px;margin-bottom:30px}
                .company{font-size:28px;font-weight:bold;color:#008080}
                .inv-title{font-size:24px;color:#333;text-align:right}
                .inv-num{font-size:14px;color:#666}
                .bill-section{display:flex;justify-content:space-between;margin-bottom:30px}
                .bill-to,.inv-details{width:48%}
                .bill-to h3,.inv-details h3{color:#008080;margin-bottom:10px;font-size:14px;text-transform:uppercase}
                table{width:100%;border-collapse:collapse}
                th{background:#008080;color:white;padding:10px 8px;text-align:left}
                td{padding:8px;border:1px solid #ddd}
                .totals{margin-top:20px;text-align:right}
                .totals table{width:300px;margin-left:auto}
                .totals td{border:none;padding:5px 8px}
                .total-row{font-size:18px;font-weight:bold;color:#008080;border-top:2px solid #008080!important}
                .footer{margin-top:40px;padding-top:15px;border-top:1px solid #ddd;text-align:center;color:#999;font-size:12px}
                .status{display:inline-block;padding:4px 12px;border-radius:12px;font-size:12px;font-weight:bold}
                .s-Paid{background:#d1fae5;color:#065f46}.s-Pending{background:#fef3c7;color:#92400e}.s-Overdue{background:#fee2e2;color:#991b1b}.s-Partial{background:#dbeafe;color:#1e40af}
                @media print{body{margin:0}.no-print{display:none}}
            </style></head><body>
                <div class="header"><div><div class="company">CompuGear</div><div style="color:#666">Computer Parts & Solutions</div></div>
                <div style="text-align:right"><div class="inv-title">INVOICE</div><div class="inv-num">${inv.invoiceNumber}</div><div><span class="status s-${inv.status}">${inv.status}</span></div></div></div>
                <div class="bill-section">
                    <div class="bill-to"><h3>Bill To</h3><strong>${inv.customerName || inv.billingName || 'N/A'}</strong><br>${inv.billingAddress ? inv.billingAddress + '<br>' : ''}${inv.billingCity ? inv.billingCity + (inv.billingState ? ', ' + inv.billingState : '') + ' ' + (inv.billingZipCode || '') + '<br>' : ''}${inv.customerEmail || inv.billingEmail || ''}</div>
                    <div class="inv-details"><h3>Invoice Details</h3><table style="width:100%;border:none"><tr><td style="border:none;padding:3px 0;color:#666">Invoice Date:</td><td style="border:none;padding:3px 0;text-align:right">${Format.date(inv.invoiceDate)}</td></tr><tr><td style="border:none;padding:3px 0;color:#666">Due Date:</td><td style="border:none;padding:3px 0;text-align:right">${Format.date(inv.dueDate)}</td></tr><tr><td style="border:none;padding:3px 0;color:#666">Payment Terms:</td><td style="border:none;padding:3px 0;text-align:right">${inv.paymentTerms || 'Net 30'}</td></tr>${inv.orderNumber ? `<tr><td style="border:none;padding:3px 0;color:#666">Order Ref:</td><td style="border:none;padding:3px 0;text-align:right">${inv.orderNumber}</td></tr>` : ''}</table></div>
                </div>
                <table><thead><tr><th>Description</th><th style="text-align:center;width:80px">Qty</th><th style="text-align:right;width:120px">Unit Price</th><th style="text-align:right;width:120px">Amount</th></tr></thead>
                <tbody>${itemsRows || '<tr><td colspan="4" style="text-align:center;padding:20px;border:1px solid #ddd">No items</td></tr>'}</tbody></table>
                <div class="totals"><table>
                    <tr><td style="color:#666">Subtotal:</td><td style="text-align:right">${Format.currency(inv.subtotal)}</td></tr>
                    ${inv.discountAmount > 0 ? `<tr><td style="color:#666">Discount:</td><td style="text-align:right;color:#dc3545">-${Format.currency(inv.discountAmount)}</td></tr>` : ''}
                    <tr><td style="color:#666">VAT (12%):</td><td style="text-align:right">${Format.currency(inv.taxAmount)}</td></tr>
                    ${inv.shippingAmount > 0 ? `<tr><td style="color:#666">Shipping:</td><td style="text-align:right">${Format.currency(inv.shippingAmount)}</td></tr>` : ''}
                    <tr class="total-row"><td>Total:</td><td style="text-align:right">${Format.currency(inv.totalAmount)}</td></tr>
                    <tr><td style="color:#666">Paid:</td><td style="text-align:right;color:#10B981">${Format.currency(inv.paidAmount)}</td></tr>
                    <tr style="font-weight:bold"><td>Balance Due:</td><td style="text-align:right;color:${(inv.balanceDue||0)>0?'#dc3545':'#10B981'}">${Format.currency(inv.balanceDue)}</td></tr>
                </table></div>
                ${paymentsSection}
                ${inv.notes?`<div style="margin-top:30px"><h3 style="color:#008080">Notes</h3><p>${inv.notes}</p></div>`:''}
                <div class="footer"><p>Thank you for your business!</p><p>CompuGear - Computer Parts & Solutions | Generated on ${new Date().toLocaleDateString('en-PH')}</p></div>
                <div class="no-print" style="text-align:center;margin-top:20px"><button onclick="window.print()" style="padding:10px 30px;background:#008080;color:white;border:none;border-radius:5px;cursor:pointer;font-size:16px">Print Invoice</button></div>
            </body></html>`);
            w.document.close();
        } catch (error) {
            Toast.error('Failed to generate invoice for printing');
        }
    },

    filter(search = '', status = '') {
        let filtered = this.data;
        if (search) { const s = search.toLowerCase(); filtered = filtered.filter(i => i.invoiceNumber?.toLowerCase().includes(s) || i.customerName?.toLowerCase().includes(s)); }
        if (status) { filtered = filtered.filter(i => i.status === status); }
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('invoicesTableBody');
        if (!tbody) return;
        if (data.length === 0) { tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4">No invoices match your criteria</td></tr>'; return; }
        tbody.innerHTML = data.map(i => `<tr>
            <td><div class="fw-semibold">${i.invoiceNumber}</div><small class="text-muted">${Format.date(i.invoiceDate)}</small></td>
            <td>${i.customerName || 'N/A'}</td><td>${Format.date(i.dueDate)}</td>
            <td class="text-end">${Format.currency(i.totalAmount)}</td>
            <td class="text-end text-success">${Format.currency(i.paidAmount)}</td>
            <td class="text-end ${(i.balance||0)>0?'text-danger':''}">${Format.currency(i.balance||(i.totalAmount-(i.paidAmount||0)))}</td>
            <td>${Format.statusBadge(i.status)}</td>
            <td class="text-center"><div class="btn-group">
                <button class="btn btn-sm btn-outline-primary" onclick="Invoices.view(${i.invoiceId})" title="View">${Icons.view}</button>
                <button class="btn btn-sm btn-outline-info" onclick="Invoices.print(${i.invoiceId})" title="Print">${Icons.print}</button>
                </div></td>
        </tr>`).join('');
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
        } catch (error) { Toast.error('Failed to load payments'); }
    },

    render() {
        const tbody = document.getElementById('paymentsTableBody');
        if (!tbody) return;
        if (this.data.length === 0) { tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No payments found</td></tr>'; return; }
        tbody.innerHTML = this.data.map(p => `<tr>
            <td><strong>${p.paymentNumber || 'PAY-' + p.paymentId}</strong></td>
            <td>${p.invoiceNumber || 'N/A'}</td>
            <td>${p.customerName || 'N/A'}</td>
            <td>${Format.date(p.paymentDate)}</td>
            <td class="text-end fw-semibold text-success">${Format.currency(p.amount)}</td>
            <td><span class="badge bg-info">${p.paymentMethodType || p.paymentMethod || '-'}</span></td>
            <td>${Format.statusBadge(p.status || 'Completed')}</td>
        </tr>`).join('');
    },

    updateStats() {
        const today = new Date().toDateString();
        const todayTotal = this.data.filter(p => new Date(p.paymentDate).toDateString() === today).reduce((sum, p) => sum + (p.amount || 0), 0);
        const totalPayments = this.data.reduce((sum, p) => sum + (p.amount || 0), 0);
        if (document.getElementById('todayPayments')) document.getElementById('todayPayments').textContent = Format.currency(todayTotal);
        if (document.getElementById('totalPayments')) document.getElementById('totalPayments').textContent = Format.currency(totalPayments);
        if (document.getElementById('paymentCount')) document.getElementById('paymentCount').textContent = this.data.length;
    },

    filter(search = '', method = '') {
        let filtered = this.data;
        if (search) { const s = search.toLowerCase(); filtered = filtered.filter(p => (p.paymentNumber||'').toLowerCase().includes(s)||(p.invoiceNumber||'').toLowerCase().includes(s)||(p.customerName||'').toLowerCase().includes(s)); }
        if (method) { filtered = filtered.filter(p => p.paymentMethod === method); }
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('paymentsTableBody');
        if (!tbody) return;
        if (data.length === 0) { tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No payments match your criteria</td></tr>'; return; }
        tbody.innerHTML = data.map(p => `<tr>
            <td><strong>${p.paymentNumber || 'PAY-' + p.paymentId}</strong></td>
            <td>${p.invoiceNumber || 'N/A'}</td>
            <td>${p.customerName || 'N/A'}</td>
            <td>${Format.date(p.paymentDate)}</td>
            <td class="text-end fw-semibold text-success">${Format.currency(p.amount)}</td>
            <td><span class="badge bg-info">${p.paymentMethodType || p.paymentMethod || '-'}</span></td>
            <td>${Format.statusBadge(p.status || 'Completed')}</td>
        </tr>`).join('');
    }
};

// ===========================================
// CUSTOMER ACCOUNTS MODULE
// ===========================================
const CustomerAccounts = {
    data: [],

    async load() {
        try { this.data = await API.get('/customers'); this.render(); }
        catch (error) { Toast.error('Failed to load customer accounts'); }
    },

    render() {
        const tbody = document.getElementById('accountsTableBody');
        if (!tbody) return;
        if (this.data.length === 0) { tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">No accounts found</td></tr>'; return; }
        tbody.innerHTML = this.data.map(c => `<tr>
            <td><strong>${c.customerCode}</strong></td>
            <td><div class="fw-semibold">${c.firstName} ${c.lastName}</div><small class="text-muted">${c.email}</small></td>
            <td>${c.totalOrders || 0}</td>
            <td class="text-end">${Format.currency(c.totalSpent)}</td>
            <td class="text-end">${Format.currency(c.balance || 0)}</td>
            <td class="text-center"><button class="btn btn-sm btn-outline-primary" onclick="CustomerAccounts.viewStatement(${c.customerId})">${Icons.view}</button></td>
        </tr>`).join('');
    },

    async viewStatement(id) {
        const c = this.data.find(customer => customer.customerId === id);
        if (!c) { Toast.error('Customer not found'); return; }

        let invoicesHtml = '<p class="text-muted">No invoices found.</p>';
        try {
            const invoices = await API.get('/invoices');
            const ci = invoices.filter(i => i.customerId === id);
            if (ci.length > 0) {
                invoicesHtml = `<div class="table-responsive mt-3"><table class="table table-sm">
                    <thead><tr><th>Invoice #</th><th>Date</th><th class="text-end">Amount</th><th class="text-end">Paid</th><th>Status</th></tr></thead>
                    <tbody>${ci.map(i => `<tr><td>${i.invoiceNumber}</td><td>${Format.date(i.invoiceDate)}</td><td class="text-end">${Format.currency(i.totalAmount)}</td><td class="text-end text-success">${Format.currency(i.paidAmount)}</td><td>${Format.statusBadge(i.status)}</td></tr>`).join('')}</tbody>
                </table></div>`;
            }
        } catch (e) { console.error(e); }
        
        const content = document.getElementById('viewAccountContent');
        if (content) {
            content.innerHTML = `<div class="row g-4">
                <div class="col-12 text-center border-bottom pb-3"><h5 class="mb-1">${c.firstName} ${c.lastName}</h5><small class="text-muted">${c.customerCode}</small></div>
                <div class="col-md-6"><div class="detail-label">Email</div><div class="detail-value">${c.email}</div></div>
                <div class="col-md-6"><div class="detail-label">Phone</div><div class="detail-value">${c.phone || '-'}</div></div>
                <div class="col-12"><div class="row g-3">
                    <div class="col-md-4"><div class="card bg-light"><div class="card-body text-center py-3"><div class="small text-muted">Total Orders</div><h4 class="mb-0 text-primary">${c.totalOrders || 0}</h4></div></div></div>
                    <div class="col-md-4"><div class="card bg-light"><div class="card-body text-center py-3"><div class="small text-muted">Total Spent</div><h4 class="mb-0 text-success">${Format.currency(c.totalSpent)}</h4></div></div></div>
                    <div class="col-md-4"><div class="card ${(c.balance||0)>0?'bg-warning':'bg-success'} text-white"><div class="card-body text-center py-3"><div class="small">Balance</div><h4 class="mb-0">${Format.currency(c.balance || 0)}</h4></div></div></div>
                </div></div>
                <div class="col-12"><h6 class="mb-2">Invoice History</h6>${invoicesHtml}</div>
            </div>`;
        }
        Modal.show('viewAccountModal');
    },

    filter(search = '') {
        let filtered = this.data;
        if (search) { const s = search.toLowerCase(); filtered = filtered.filter(c => (c.firstName+' '+c.lastName).toLowerCase().includes(s)||c.email?.toLowerCase().includes(s)||c.customerCode?.toLowerCase().includes(s)); }
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('accountsTableBody');
        if (!tbody) return;
        if (data.length === 0) { tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">No accounts found</td></tr>'; return; }
        tbody.innerHTML = data.map(c => `<tr>
            <td><strong>${c.customerCode}</strong></td>
            <td><div class="fw-semibold">${c.firstName} ${c.lastName}</div><small class="text-muted">${c.email}</small></td>
            <td>${c.totalOrders || 0}</td>
            <td class="text-end">${Format.currency(c.totalSpent)}</td>
            <td class="text-end">${Format.currency(c.balance || 0)}</td>
            <td class="text-center"><button class="btn btn-sm btn-outline-primary" onclick="CustomerAccounts.viewStatement(${c.customerId})">${Icons.view}</button></td>
        </tr>`).join('');
    }
};

// ===========================================
// FINANCIAL REPORTS MODULE
// ===========================================
const FinancialReports = {
    revenueChart: null,
    paymentMethodsChart: null,
    invoiceStatusChart: null,
    reportData: null,

    async load(period = 'month') {
        try {
            const result = await API.get(`/reports/financial?period=${period}`);
            this.reportData = result.data || result;
            this.updateSummary();
            this.renderRevenueChart();
            this.renderPaymentMethodsChart();
            this.renderInvoiceStatusChart();
            this.renderTopCustomers();
        } catch (error) {
            console.error('Failed to load financial reports:', error);
            await this.loadFallback();
        }
    },

    async loadFallback() {
        try {
            const [invoices, payments, orders] = await Promise.all([
                API.get('/invoices').catch(() => []),
                API.get('/payments').catch(() => []),
                API.get('/orders').catch(() => [])
            ]);
            const totalRevenue = orders.reduce((sum, o) => sum + (o.totalAmount || 0), 0);
            const totalInvoiced = invoices.reduce((sum, i) => sum + (i.totalAmount || 0), 0);
            const totalCollected = payments.reduce((sum, p) => sum + (p.amount || 0), 0);
            const outstanding = totalInvoiced - totalCollected;
            if (document.getElementById('totalRevenue')) document.getElementById('totalRevenue').textContent = Format.currency(totalRevenue);
            if (document.getElementById('totalInvoiced')) document.getElementById('totalInvoiced').textContent = Format.currency(totalInvoiced);
            if (document.getElementById('totalCollected')) document.getElementById('totalCollected').textContent = Format.currency(totalCollected);
            if (document.getElementById('outstanding')) document.getElementById('outstanding').textContent = Format.currency(outstanding > 0 ? outstanding : 0);

            // Fallback charts
            const ctx1 = document.getElementById('revenueChart')?.getContext('2d');
            if (ctx1) {
                if (this.revenueChart) this.revenueChart.destroy();
                const md = Array(12).fill(0); const cy = new Date().getFullYear();
                orders.forEach(o => { const d = new Date(o.orderDate); if (d.getFullYear() === cy) md[d.getMonth()] += o.totalAmount || 0; });
                this.revenueChart = new Chart(ctx1, { type:'line', data:{ labels:['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'], datasets:[{label:'Revenue',data:md,borderColor:'#008080',backgroundColor:'rgba(0,128,128,0.1)',fill:true,tension:0.4}]}, options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{display:false}},scales:{y:{beginAtZero:true,ticks:{callback:v=>'₱'+(v/1000).toFixed(0)+'k'}}}}});
            }
            const ctx2 = document.getElementById('paymentMethodsChart')?.getContext('2d');
            if (ctx2) {
                if (this.paymentMethodsChart) this.paymentMethodsChart.destroy();
                const methods = {}; payments.forEach(p => { const m = p.paymentMethod||'Other'; methods[m]=(methods[m]||0)+(p.amount||0); });
                this.paymentMethodsChart = new Chart(ctx2, { type:'doughnut', data:{labels:Object.keys(methods),datasets:[{data:Object.values(methods),backgroundColor:['#008080','#10B981','#3B82F6','#F59E0B','#EF4444']}]}, options:{responsive:true,maintainAspectRatio:false,plugins:{legend:{position:'bottom'}}}});
            }
        } catch (error) { Toast.error('Failed to load reports'); }
    },

    updateSummary() {
        const d = this.reportData;
        if (!d) return;
        if (document.getElementById('totalRevenue')) document.getElementById('totalRevenue').textContent = Format.currency(d.totalRevenue);
        if (document.getElementById('totalInvoiced')) document.getElementById('totalInvoiced').textContent = Format.currency(d.totalInvoiced);
        if (document.getElementById('totalCollected')) document.getElementById('totalCollected').textContent = Format.currency(d.totalCollected);
        if (document.getElementById('outstanding')) document.getElementById('outstanding').textContent = Format.currency(d.outstanding);
    },

    renderRevenueChart() {
        const ctx = document.getElementById('revenueChart')?.getContext('2d');
        if (!ctx || !this.reportData) return;
        if (this.revenueChart) this.revenueChart.destroy();
        const d = this.reportData;
        this.revenueChart = new Chart(ctx, {
            type: 'line',
            data: { labels: ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'],
                datasets: [
                    { label:'Revenue', data:d.monthlyRevenue, borderColor:'#008080', backgroundColor:'rgba(0,128,128,0.1)', fill:true, tension:0.4 },
                    { label:'Collected', data:d.monthlyCollected, borderColor:'#10B981', backgroundColor:'rgba(16,185,129,0.05)', fill:true, tension:0.4, borderDash:[5,5] }
                ]
            },
            options: { responsive:true, maintainAspectRatio:false, plugins:{legend:{position:'top'}}, scales:{y:{beginAtZero:true,ticks:{callback:v=>'₱'+(v/1000).toFixed(0)+'k'}}} }
        });
    },

    renderPaymentMethodsChart() {
        const ctx = document.getElementById('paymentMethodsChart')?.getContext('2d');
        if (!ctx || !this.reportData) return;
        if (this.paymentMethodsChart) this.paymentMethodsChart.destroy();
        const methods = this.reportData.paymentMethods || [];
        this.paymentMethodsChart = new Chart(ctx, {
            type: 'doughnut',
            data: { labels: methods.map(m => m.method||'Other'), datasets: [{ data: methods.map(m => m.amount), backgroundColor: ['#008080','#10B981','#3B82F6','#F59E0B','#EF4444','#8B5CF6'] }] },
            options: { responsive:true, maintainAspectRatio:false, plugins:{legend:{position:'bottom'}} }
        });
    },

    renderInvoiceStatusChart() {
        const ctx = document.getElementById('invoiceStatusChart')?.getContext('2d');
        if (!ctx || !this.reportData) return;
        if (this.invoiceStatusChart) this.invoiceStatusChart.destroy();
        const statuses = this.reportData.invoiceStatusBreakdown || [];
        const colors = {'Paid':'#10B981','Pending':'#F59E0B','Overdue':'#EF4444','Draft':'#6B7280','Cancelled':'#374151','Void':'#9CA3AF','Partial':'#3B82F6'};
        this.invoiceStatusChart = new Chart(ctx, {
            type: 'doughnut',
            data: { labels: statuses.map(s=>`${s.status} (${s.count})`), datasets: [{ data: statuses.map(s=>s.amount), backgroundColor: statuses.map(s=>colors[s.status]||'#6B7280') }] },
            options: { responsive:true, maintainAspectRatio:false, plugins:{legend:{position:'bottom'}} }
        });
    },

    renderTopCustomers() {
        const tbody = document.getElementById('topCustomersBody');
        if (!tbody || !this.reportData) return;
        const customers = this.reportData.topCustomers || [];
        if (customers.length === 0) { tbody.innerHTML = '<tr><td colspan="4" class="text-center py-3">No data</td></tr>'; return; }
        tbody.innerHTML = customers.map((c, i) => `<tr>
            <td><span class="badge bg-${i===0?'warning':i===1?'secondary':'light text-dark'}">#${i+1}</span></td>
            <td class="fw-semibold">${c.name}</td>
            <td class="text-center">${c.totalOrders || 0}</td>
            <td class="text-end text-success fw-semibold">${Format.currency(c.totalSpent)}</td>
        </tr>`).join('');
    },

    exportPDF() {
        const d = this.reportData;
        const w = window.open('', '_blank');
        let html = '';
        if (d) {
            html = `
                <div style="display:flex;gap:20px;margin-bottom:30px">
                    <div style="flex:1;background:#f0fdfa;padding:15px;border-radius:8px;text-align:center"><div style="color:#666;font-size:12px">Total Revenue</div><div style="font-size:20px;font-weight:bold;color:#008080">${Format.currency(d.totalRevenue)}</div></div>
                    <div style="flex:1;background:#f0fdf4;padding:15px;border-radius:8px;text-align:center"><div style="color:#666;font-size:12px">Total Collected</div><div style="font-size:20px;font-weight:bold;color:#10B981">${Format.currency(d.totalCollected)}</div></div>
                    <div style="flex:1;background:#fffbeb;padding:15px;border-radius:8px;text-align:center"><div style="color:#666;font-size:12px">Outstanding</div><div style="font-size:20px;font-weight:bold;color:#F59E0B">${Format.currency(d.outstanding)}</div></div>
                    <div style="flex:1;background:#eff6ff;padding:15px;border-radius:8px;text-align:center"><div style="color:#666;font-size:12px">Total Invoiced</div><div style="font-size:20px;font-weight:bold;color:#3B82F6">${Format.currency(d.totalInvoiced)}</div></div>
                </div>
                <h3 style="color:#008080;margin-bottom:10px">Invoice Status Breakdown</h3>
                <table style="width:100%;border-collapse:collapse;margin-bottom:30px"><thead><tr style="background:#008080;color:white"><th style="padding:8px">Status</th><th style="padding:8px;text-align:center">Count</th><th style="padding:8px;text-align:right">Amount</th></tr></thead>
                <tbody>${(d.invoiceStatusBreakdown||[]).map(s=>`<tr><td style="padding:8px;border:1px solid #ddd">${s.status}</td><td style="padding:8px;border:1px solid #ddd;text-align:center">${s.count}</td><td style="padding:8px;border:1px solid #ddd;text-align:right">${Format.currency(s.amount)}</td></tr>`).join('')}</tbody></table>
                <h3 style="color:#008080;margin-bottom:10px">Payment Methods</h3>
                <table style="width:100%;border-collapse:collapse;margin-bottom:30px"><thead><tr style="background:#008080;color:white"><th style="padding:8px">Method</th><th style="padding:8px;text-align:center">Count</th><th style="padding:8px;text-align:right">Amount</th></tr></thead>
                <tbody>${(d.paymentMethods||[]).map(m=>`<tr><td style="padding:8px;border:1px solid #ddd">${m.method||'Other'}</td><td style="padding:8px;border:1px solid #ddd;text-align:center">${m.count}</td><td style="padding:8px;border:1px solid #ddd;text-align:right">${Format.currency(m.amount)}</td></tr>`).join('')}</tbody></table>
                <h3 style="color:#008080;margin-bottom:10px">Top Customers</h3>
                <table style="width:100%;border-collapse:collapse"><thead><tr style="background:#008080;color:white"><th style="padding:8px">#</th><th style="padding:8px">Customer</th><th style="padding:8px;text-align:center">Orders</th><th style="padding:8px;text-align:right">Total Spent</th></tr></thead>
                <tbody>${(d.topCustomers||[]).map((c,i)=>`<tr><td style="padding:8px;border:1px solid #ddd">${i+1}</td><td style="padding:8px;border:1px solid #ddd">${c.name}</td><td style="padding:8px;border:1px solid #ddd;text-align:center">${c.totalOrders||0}</td><td style="padding:8px;border:1px solid #ddd;text-align:right">${Format.currency(c.totalSpent)}</td></tr>`).join('')}</tbody></table>`;
        }
        w.document.write(`<!DOCTYPE html><html><head><title>Financial Report - CompuGear</title><style>body{font-family:Arial,sans-serif;margin:0;padding:20px;color:#333}.header{border-bottom:3px solid #008080;padding-bottom:15px;margin-bottom:25px}.company{font-size:28px;font-weight:bold;color:#008080}.report-title{font-size:18px;color:#666;margin-top:5px}.footer{margin-top:40px;padding-top:15px;border-top:1px solid #ddd;text-align:center;color:#999;font-size:12px}@media print{body{margin:0}.no-print{display:none}}</style></head><body>
            <div class="header"><div class="company">CompuGear</div><div class="report-title">Financial Report - Generated ${new Date().toLocaleDateString('en-PH',{year:'numeric',month:'long',day:'numeric'})}</div></div>
            ${html}
            <div class="footer">CompuGear - Computer Parts & Solutions | Financial Report</div>
            <div class="no-print" style="text-align:center;margin-top:20px"><button onclick="window.print()" style="padding:10px 30px;background:#008080;color:white;border:none;border-radius:5px;cursor:pointer;font-size:16px">Print Report</button></div>
        </body></html>`);
        w.document.close();
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    const path = window.location.pathname.toLowerCase();
    if (path.includes('/billingstaff/invoices')) Invoices.load();
    else if (path.includes('/billingstaff/payments')) Payments.load();
    else if (path.includes('/billingstaff/accounts')) CustomerAccounts.load();
    else if (path.includes('/billingstaff/reports')) FinancialReports.load();
});
