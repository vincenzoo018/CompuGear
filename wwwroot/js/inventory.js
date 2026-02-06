/**
 * CompuGear Inventory Staff JavaScript
 * Handles Inventory module functionality
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
    add: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="16"/><line x1="8" y1="12" x2="16" y2="12"/></svg>',
    minus: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="8" y1="12" x2="16" y2="12"/></svg>',
    alert: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>'
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
    number(value) {
        return parseInt(value || 0).toLocaleString();
    },
    stockStatus(current, min) {
        if (current <= 0) return '<span class="badge bg-danger">Out of Stock</span>';
        if (current <= min) return '<span class="badge bg-warning">Low Stock</span>';
        return '<span class="badge bg-success">In Stock</span>';
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
// PRODUCTS MODULE
// ===========================================
const Products = {
    data: [],
    currentId: null,

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
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4">No products found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(p => `
            <tr>
                <td><strong>${p.sku}</strong></td>
                <td>
                    <div class="d-flex align-items-center">
                        <img src="${p.imageUrl || '/images/products/default.png'}" class="me-2 rounded" width="40" height="40" style="object-fit: cover;">
                        <div>
                            <div class="fw-semibold">${p.productName}</div>
                            <small class="text-muted">${p.categoryName || 'Uncategorized'}</small>
                        </div>
                    </div>
                </td>
                <td class="text-end">${Format.currency(p.price)}</td>
                <td class="text-center">${Format.number(p.stockQuantity)}</td>
                <td class="text-center">${Format.number(p.reorderLevel || 10)}</td>
                <td>${Format.stockStatus(p.stockQuantity, p.reorderLevel || 10)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Products.view(${p.productId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="Products.edit(${p.productId})">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-success" onclick="Products.adjustStock(${p.productId}, 'add')">${Icons.add}</button>
                        <button class="btn btn-sm btn-outline-danger" onclick="Products.adjustStock(${p.productId}, 'subtract')">${Icons.minus}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const totalProducts = this.data.length;
        const totalValue = this.data.reduce((sum, p) => sum + ((p.price || 0) * (p.stockQuantity || 0)), 0);
        const lowStock = this.data.filter(p => p.stockQuantity > 0 && p.stockQuantity <= (p.reorderLevel || 10)).length;
        const outOfStock = this.data.filter(p => (p.stockQuantity || 0) <= 0).length;

        if (document.getElementById('totalProducts')) document.getElementById('totalProducts').textContent = Format.number(totalProducts);
        if (document.getElementById('inventoryValue')) document.getElementById('inventoryValue').textContent = Format.currency(totalValue);
        if (document.getElementById('lowStockCount')) document.getElementById('lowStockCount').textContent = Format.number(lowStock);
        if (document.getElementById('outOfStockCount')) document.getElementById('outOfStockCount').textContent = Format.number(outOfStock);
    },

    view(id) {
        window.location.href = `/InventoryStaff/ProductDetails/${id}`;
    },

    edit(id) {
        this.currentId = id;
        const product = this.data.find(p => p.productId === id);
        if (!product) return;

        document.getElementById('productId').value = product.productId;
        document.getElementById('productName').value = product.productName;
        document.getElementById('productSku').value = product.sku;
        document.getElementById('productPrice').value = product.price;
        document.getElementById('productStock').value = product.stockQuantity;
        document.getElementById('productReorderLevel').value = product.reorderLevel || 10;
        document.getElementById('productCategory').value = product.categoryId;
        document.getElementById('productDescription').value = product.description || '';

        Modal.show('productModal');
    },

    adjustStock(id, type) {
        this.currentId = id;
        const product = this.data.find(p => p.productId === id);
        if (!product) return;

        document.getElementById('adjustProductId').value = id;
        document.getElementById('adjustProductName').textContent = product.productName;
        document.getElementById('adjustCurrentStock').textContent = Format.number(product.stockQuantity);
        document.getElementById('adjustType').value = type;
        document.getElementById('adjustQuantity').value = '';
        document.getElementById('adjustReason').value = '';

        Modal.show('stockAdjustmentModal');
    },

    async saveAdjustment() {
        const data = {
            productId: document.getElementById('adjustProductId').value,
            type: document.getElementById('adjustType').value,
            quantity: parseInt(document.getElementById('adjustQuantity').value) || 0,
            reason: document.getElementById('adjustReason').value
        };

        if (data.quantity <= 0) {
            Toast.error('Please enter a valid quantity');
            return;
        }

        try {
            await API.post('/stock-adjustments', data);
            Toast.success('Stock adjusted successfully');
            Modal.hide('stockAdjustmentModal');
            this.load();
        } catch (error) {
            Toast.error('Failed to adjust stock');
        }
    },

    async save() {
        const data = {
            productId: document.getElementById('productId').value || null,
            productName: document.getElementById('productName').value,
            sku: document.getElementById('productSku').value,
            price: parseFloat(document.getElementById('productPrice').value) || 0,
            stockQuantity: parseInt(document.getElementById('productStock').value) || 0,
            reorderLevel: parseInt(document.getElementById('productReorderLevel').value) || 10,
            categoryId: document.getElementById('productCategory').value,
            description: document.getElementById('productDescription').value
        };

        try {
            if (data.productId) {
                await API.put(`/products/${data.productId}`, data);
            } else {
                await API.post('/products', data);
            }
            Toast.success('Product saved successfully');
            Modal.hide('productModal');
            this.load();
        } catch (error) {
            Toast.error('Failed to save product');
        }
    }
};

// ===========================================
// STOCK LEVELS MODULE
// ===========================================
const StockLevels = {
    data: [],

    async load() {
        try {
            this.data = await API.get('/products');
            this.render();
            this.renderChart();
        } catch (error) {
            Toast.error('Failed to load stock levels');
        }
    },

    render() {
        const tbody = document.getElementById('stockTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">No products found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(p => {
            const percentage = Math.min((p.stockQuantity / (p.reorderLevel * 3 || 30)) * 100, 100);
            const barColor = p.stockQuantity <= 0 ? 'danger' : p.stockQuantity <= p.reorderLevel ? 'warning' : 'success';
            return `
            <tr>
                <td><strong>${p.sku}</strong></td>
                <td>${p.productName}</td>
                <td class="text-center">${Format.number(p.stockQuantity)}</td>
                <td class="text-center">${Format.number(p.reorderLevel || 10)}</td>
                <td style="width: 200px;">
                    <div class="progress" style="height: 8px;">
                        <div class="progress-bar bg-${barColor}" style="width: ${percentage}%"></div>
                    </div>
                </td>
                <td>${Format.stockStatus(p.stockQuantity, p.reorderLevel || 10)}</td>
            </tr>
        `}).join('');
    },

    renderChart() {
        const ctx = document.getElementById('stockChart')?.getContext('2d');
        if (!ctx) return;

        const inStock = this.data.filter(p => p.stockQuantity > (p.reorderLevel || 10)).length;
        const lowStock = this.data.filter(p => p.stockQuantity > 0 && p.stockQuantity <= (p.reorderLevel || 10)).length;
        const outOfStock = this.data.filter(p => (p.stockQuantity || 0) <= 0).length;

        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['In Stock', 'Low Stock', 'Out of Stock'],
                datasets: [{
                    data: [inStock, lowStock, outOfStock],
                    backgroundColor: ['#10B981', '#F59E0B', '#EF4444']
                }]
            },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
        });
    }
};

// ===========================================
// STOCK ALERTS MODULE
// ===========================================
const StockAlerts = {
    data: [],

    async load() {
        try {
            const products = await API.get('/products');
            this.data = products.filter(p => p.stockQuantity <= (p.reorderLevel || 10));
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load stock alerts');
        }
    },

    render() {
        const tbody = document.getElementById('alertsTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-success"><i class="bi bi-check-circle me-2"></i>No stock alerts - All products are well stocked!</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(p => `
            <tr class="${p.stockQuantity <= 0 ? 'table-danger' : 'table-warning'}">
                <td><strong>${p.sku}</strong></td>
                <td>${p.productName}</td>
                <td class="text-center">${Format.number(p.stockQuantity)}</td>
                <td class="text-center">${Format.number(p.reorderLevel || 10)}</td>
                <td>
                    ${p.stockQuantity <= 0 
                        ? '<span class="badge bg-danger"><i class="bi bi-exclamation-circle me-1"></i>OUT OF STOCK</span>'
                        : '<span class="badge bg-warning"><i class="bi bi-exclamation-triangle me-1"></i>LOW STOCK</span>'
                    }
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const lowStock = this.data.filter(p => p.stockQuantity > 0).length;
        const outOfStock = this.data.filter(p => p.stockQuantity <= 0).length;

        if (document.getElementById('lowStockAlerts')) document.getElementById('lowStockAlerts').textContent = Format.number(lowStock);
        if (document.getElementById('outOfStockAlerts')) document.getElementById('outOfStockAlerts').textContent = Format.number(outOfStock);
        if (document.getElementById('totalAlerts')) document.getElementById('totalAlerts').textContent = Format.number(this.data.length);
    }
};

// ===========================================
// SUPPLIERS MODULE
// ===========================================
const Suppliers = {
    data: [],
    currentId: null,

    async load() {
        try {
            this.data = await API.get('/suppliers');
            this.render();
        } catch (error) {
            Toast.error('Failed to load suppliers');
        }
    },

    render() {
        const tbody = document.getElementById('suppliersTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">No suppliers found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(s => `
            <tr>
                <td><strong>${s.supplierCode || s.supplierId}</strong></td>
                <td>
                    <div class="fw-semibold">${s.supplierName}</div>
                    <small class="text-muted">${s.contactPerson || ''}</small>
                </td>
                <td>${s.email || '-'}</td>
                <td>${s.phone || '-'}</td>
                <td><span class="badge bg-${s.isActive ? 'success' : 'secondary'}">${s.isActive ? 'Active' : 'Inactive'}</span></td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Suppliers.view(${s.supplierId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="Suppliers.edit(${s.supplierId})">${Icons.edit}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    },

    view(id) {
        window.location.href = `/InventoryStaff/SupplierDetails/${id}`;
    },

    edit(id) {
        this.currentId = id;
        const supplier = this.data.find(s => s.supplierId === id);
        if (!supplier) return;

        document.getElementById('supplierId').value = supplier.supplierId;
        document.getElementById('supplierName').value = supplier.supplierName;
        document.getElementById('supplierContact').value = supplier.contactPerson || '';
        document.getElementById('supplierEmail').value = supplier.email || '';
        document.getElementById('supplierPhone').value = supplier.phone || '';
        document.getElementById('supplierAddress').value = supplier.address || '';
        document.getElementById('supplierActive').checked = supplier.isActive;

        Modal.show('supplierModal');
    },

    async save() {
        const data = {
            supplierId: document.getElementById('supplierId').value || null,
            supplierName: document.getElementById('supplierName').value,
            contactPerson: document.getElementById('supplierContact').value,
            email: document.getElementById('supplierEmail').value,
            phone: document.getElementById('supplierPhone').value,
            address: document.getElementById('supplierAddress').value,
            isActive: document.getElementById('supplierActive').checked
        };

        try {
            if (data.supplierId) {
                await API.put(`/suppliers/${data.supplierId}`, data);
            } else {
                await API.post('/suppliers', data);
            }
            Toast.success('Supplier saved successfully');
            Modal.hide('supplierModal');
            this.load();
        } catch (error) {
            Toast.error('Failed to save supplier');
        }
    }
};

// ===========================================
// INVENTORY REPORTS MODULE
// ===========================================
const InventoryReports = {
    async load() {
        try {
            const products = await API.get('/products');
            this.renderCategoryChart(products);
            this.renderValueChart(products);
            this.updateSummary(products);
        } catch (error) {
            Toast.error('Failed to load inventory reports');
        }
    },

    renderCategoryChart(products) {
        const ctx = document.getElementById('categoryChart')?.getContext('2d');
        if (!ctx) return;

        const categories = {};
        products.forEach(p => {
            const cat = p.categoryName || 'Uncategorized';
            categories[cat] = (categories[cat] || 0) + 1;
        });

        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: Object.keys(categories),
                datasets: [{
                    label: 'Products',
                    data: Object.values(categories),
                    backgroundColor: '#008080'
                }]
            },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
        });
    },

    renderValueChart(products) {
        const ctx = document.getElementById('valueChart')?.getContext('2d');
        if (!ctx) return;

        const categories = {};
        products.forEach(p => {
            const cat = p.categoryName || 'Uncategorized';
            categories[cat] = (categories[cat] || 0) + ((p.price || 0) * (p.stockQuantity || 0));
        });

        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: Object.keys(categories),
                datasets: [{
                    data: Object.values(categories),
                    backgroundColor: ['#008080', '#10B981', '#3B82F6', '#F59E0B', '#EF4444', '#8B5CF6', '#EC4899']
                }]
            },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
        });
    },

    updateSummary(products) {
        const totalProducts = products.length;
        const totalStock = products.reduce((sum, p) => sum + (p.stockQuantity || 0), 0);
        const totalValue = products.reduce((sum, p) => sum + ((p.price || 0) * (p.stockQuantity || 0)), 0);
        const avgValue = totalProducts > 0 ? totalValue / totalProducts : 0;

        if (document.getElementById('totalProducts')) document.getElementById('totalProducts').textContent = Format.number(totalProducts);
        if (document.getElementById('totalStock')) document.getElementById('totalStock').textContent = Format.number(totalStock);
        if (document.getElementById('totalValue')) document.getElementById('totalValue').textContent = Format.currency(totalValue);
        if (document.getElementById('avgValue')) document.getElementById('avgValue').textContent = Format.currency(avgValue);
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    const path = window.location.pathname.toLowerCase();
    
    if (path.includes('/inventorystaff/products') || path.endsWith('/products')) {
        Products.load();
    } else if (path.includes('/inventorystaff/stock') || path.endsWith('/stock')) {
        StockLevels.load();
    } else if (path.includes('/inventorystaff/alerts') || path.endsWith('/alerts')) {
        StockAlerts.load();
    } else if (path.includes('/inventorystaff/suppliers') || path.endsWith('/suppliers')) {
        Suppliers.load();
    } else if (path.includes('/inventorystaff/reports') || path.endsWith('/reports')) {
        InventoryReports.load();
    } else if (path === '/inventorystaff' || path === '/inventorystaff/') {
        // Dashboard
        API.get('/products').then(products => {
            const totalProducts = products.length;
            const totalValue = products.reduce((sum, p) => sum + ((p.price || 0) * (p.stockQuantity || 0)), 0);
            const lowStock = products.filter(p => p.stockQuantity > 0 && p.stockQuantity <= (p.reorderLevel || 10)).length;
            const outOfStock = products.filter(p => (p.stockQuantity || 0) <= 0).length;

            if (document.getElementById('totalProducts')) document.getElementById('totalProducts').textContent = Format.number(totalProducts);
            if (document.getElementById('inventoryValue')) document.getElementById('inventoryValue').textContent = Format.currency(totalValue);
            if (document.getElementById('lowStockCount')) document.getElementById('lowStockCount').textContent = Format.number(lowStock);
            if (document.getElementById('outOfStockCount')) document.getElementById('outOfStockCount').textContent = Format.number(outOfStock);
        }).catch(() => {});
    }
});
