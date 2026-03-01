/**
 * CompuGear Marketing Staff JavaScript
 * Handles Marketing module functionality
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
    delete: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>',
    toggleOn: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="1" y="5" width="22" height="14" rx="7" ry="7"/><circle cx="16" cy="12" r="3"/></svg>',
    toggleOff: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="1" y="5" width="22" height="14" rx="7" ry="7"/><circle cx="8" cy="12" r="3"/></svg>'
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
    percentage(value) {
        return `${parseFloat(value || 0).toFixed(1)}%`;
    },
    statusBadge(status) {
        const colors = {
            'Active': 'success', 'Draft': 'secondary', 'Scheduled': 'info',
            'Paused': 'warning', 'Completed': 'primary', 'Cancelled': 'danger'
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
// CAMPAIGNS MODULE
// ===========================================
const Campaigns = {
    data: [],
    currentId: null,

    async load() {
        try {
            this.data = await API.get('/campaigns');
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load campaigns');
        }
    },

    render() {
        const tbody = document.getElementById('campaignsTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No campaigns found. Create your first campaign!</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(c => `
            <tr>
                <td>
                    <div class="fw-semibold">${c.campaignName}</div>
                    <small class="text-muted">${c.campaignCode}</small>
                </td>
                <td><span class="badge bg-info">${c.type || 'General'}</span></td>
                <td>${Format.statusBadge(c.status)}</td>
                <td>
                    <div>${Format.date(c.startDate)}</div>
                    <small class="text-muted">to ${Format.date(c.endDate)}</small>
                </td>
                <td class="text-end">${Format.currency(c.budget)}</td>
                <td class="text-end">${Format.percentage(c.roi)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Campaigns.view(${c.campaignId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="Campaigns.edit(${c.campaignId})">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-${c.status === 'Active' ? 'danger' : 'success'}" onclick="Campaigns.toggleStatus(${c.campaignId})">
                            ${c.status === 'Active' ? Icons.toggleOff : Icons.toggleOn}
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="Campaigns.delete(${c.campaignId})">${Icons.delete}</button>
                    </div>
                </td>
            </tr>
        `).join('');
        this.applyCurrentFilters();
    },

    updateStats() {
        const total = this.data.length;
        const active = this.data.filter(c => c.status === 'Active').length;
        const budget = this.data.reduce((sum, c) => sum + (c.budget || 0), 0);
        const avgRoi = this.data.length > 0 ? this.data.reduce((sum, c) => sum + (c.roi || 0), 0) / this.data.length : 0;

        if (document.getElementById('totalCampaigns')) document.getElementById('totalCampaigns').textContent = total;
        if (document.getElementById('activeCampaigns')) document.getElementById('activeCampaigns').textContent = active;
        if (document.getElementById('totalBudget')) document.getElementById('totalBudget').textContent = Format.currency(budget);
        if (document.getElementById('avgRoi')) document.getElementById('avgRoi').textContent = Format.percentage(avgRoi);
    },

    view(id) {
        const c = this.data.find(campaign => campaign.campaignId === id);
        if (!c) {
            Toast.error('Campaign not found');
            return;
        }
        
        this.currentId = id;
        const content = document.getElementById('viewCampaignContent');
        if (content) {
            content.innerHTML = `
                <div class="row g-4">
                    <div class="col-12 d-flex justify-content-between align-items-start border-bottom pb-3">
                        <div>
                            <h5 class="mb-1">${c.campaignName}</h5>
                            <small class="text-muted">${c.campaignCode}</small>
                        </div>
                        ${Format.statusBadge(c.status)}
                    </div>
                    <div class="col-md-6">
                        <div class="detail-label">Campaign Type</div>
                        <div class="detail-value"><span class="badge bg-info">${c.type || 'General'}</span></div>
                    </div>
                    <div class="col-md-6">
                        <div class="detail-label">Duration</div>
                        <div class="detail-value">${Format.date(c.startDate)} - ${Format.date(c.endDate)}</div>
                    </div>
                    <div class="col-12">
                        <div class="detail-label">Description</div>
                        <div class="detail-value">${c.description || 'No description provided'}</div>
                    </div>
                    <div class="col-12">
                        <div class="row g-3">
                            <div class="col-md-4">
                                <div class="card bg-light">
                                    <div class="card-body text-center py-3">
                                        <div class="small text-muted">Budget</div>
                                        <h4 class="mb-0 text-primary">${Format.currency(c.budget)}</h4>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <div class="card bg-light">
                                    <div class="card-body text-center py-3">
                                        <div class="small text-muted">Spent</div>
                                        <h4 class="mb-0 text-warning">${Format.currency(c.spent || 0)}</h4>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-4">
                                <div class="card ${(c.roi || 0) >= 0 ? 'bg-success' : 'bg-danger'} text-white">
                                    <div class="card-body text-center py-3">
                                        <div class="small">ROI</div>
                                        <h4 class="mb-0">${Format.percentage(c.roi)}</h4>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        }
        Modal.show('viewCampaignModal');
    },

    edit(id) {
        this.currentId = id;
        const campaign = this.data.find(c => c.campaignId === id);
        if (!campaign) return;
        
        document.getElementById('campaignId').value = id;
        document.getElementById('campaignName').value = campaign.campaignName;
        document.getElementById('campaignType').value = campaign.type;
        document.getElementById('campaignStatus').value = campaign.status;
        document.getElementById('campaignBudget').value = campaign.budget;
        document.getElementById('startDate').value = campaign.startDate?.split('T')[0];
        document.getElementById('endDate').value = campaign.endDate?.split('T')[0];
        
        Modal.show('campaignModal');
    },

    openCreateModal() {
        Modal.reset('campaignForm');
        document.getElementById('campaignId').value = '';
        Modal.show('campaignModal');
    },

    async save() {
        const data = {
            campaignId: document.getElementById('campaignId')?.value || 0,
            campaignName: document.getElementById('campaignName')?.value,
            type: document.getElementById('campaignType')?.value,
            status: document.getElementById('campaignStatus')?.value,
            budget: parseFloat(document.getElementById('campaignBudget')?.value) || 0,
            startDate: document.getElementById('startDate')?.value,
            endDate: document.getElementById('endDate')?.value,
            description: document.getElementById('campaignDescription')?.value
        };

        try {
            if (data.campaignId) {
                await API.put(`/campaigns/${data.campaignId}`, data);
                Toast.success('Campaign updated successfully');
            } else {
                await API.post('/campaigns', data);
                Toast.success('Campaign created successfully');
            }
            Modal.hide('campaignModal');
            await this.load();
        } catch (error) {
            Toast.error('Failed to save campaign');
        }
    },

    async toggleStatus(id) {
        const campaign = this.data.find(c => c.campaignId === id);
        if (!campaign) return;
        
        const newStatus = campaign.status === 'Active' ? 'Paused' : 'Active';
        try {
            await API.put(`/campaigns/${id}/status`, { status: newStatus });
            Toast.success(`Campaign ${newStatus.toLowerCase()}`);
            await this.load();
        } catch (error) {
            Toast.error('Failed to update campaign status');
        }
    },

    async delete(id) {
        if (!confirm('Are you sure you want to delete this campaign?')) return;
        try {
            await API.delete(`/campaigns/${id}`);
            Toast.success('Campaign deleted successfully');
            await this.load();
        } catch (error) {
            Toast.error('Failed to delete campaign');
        }
    },

    applyCurrentFilters() {
        const search = document.getElementById('campaignSearch')?.value || '';
        const status = document.getElementById('campaignStatusFilter')?.value || '';
        const type = document.getElementById('campaignTypeFilter')?.value || '';
        if (search || status || type) {
            this.filter(search, status, type);
        }
    },

    filter(search = '', status = '', type = '') {
        let filtered = this.data;
        
        if (search) {
            const s = search.toLowerCase();
            filtered = filtered.filter(c => 
                c.campaignName?.toLowerCase().includes(s) ||
                c.campaignCode?.toLowerCase().includes(s)
            );
        }
        
        if (status) {
            filtered = filtered.filter(c => c.status === status);
        }
        
        if (type) {
            filtered = filtered.filter(c => c.type === type);
        }
        
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('campaignsTableBody');
        if (!tbody) return;

        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No campaigns found</td></tr>';
            return;
        }

        tbody.innerHTML = data.map(c => `
            <tr>
                <td>
                    <div class="fw-semibold">${c.campaignName}</div>
                    <small class="text-muted">${c.campaignCode}</small>
                </td>
                <td><span class="badge bg-info">${c.type || 'General'}</span></td>
                <td>${Format.statusBadge(c.status)}</td>
                <td>
                    <div>${Format.date(c.startDate)}</div>
                    <small class="text-muted">to ${Format.date(c.endDate)}</small>
                </td>
                <td class="text-end">${Format.currency(c.budget)}</td>
                <td class="text-end">${Format.percentage(c.roi)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Campaigns.view(${c.campaignId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="Campaigns.edit(${c.campaignId})">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-${c.status === 'Active' ? 'danger' : 'success'}" onclick="Campaigns.toggleStatus(${c.campaignId})">
                            ${c.status === 'Active' ? Icons.toggleOff : Icons.toggleOn}
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="Campaigns.delete(${c.campaignId})">${Icons.delete}</button>
                    </div>
                </td>
            </tr>
        `).join('');
    }
};

// ===========================================
// PROMOTIONS MODULE
// ===========================================
const Promotions = {
    data: [],
    currentId: null,

    async load() {
        try {
            this.data = await API.get('/promotions');
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load promotions');
        }
    },

    render() {
        const tbody = document.getElementById('promotionsTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No promotions found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(p => `
            <tr>
                <td><strong>${p.promoCode}</strong></td>
                <td>${p.promoName}</td>
                <td>${p.discountType === 'Percentage' ? Format.percentage(p.discountValue) : Format.currency(p.discountValue)}</td>
                <td>${Format.date(p.startDate)} - ${Format.date(p.endDate)}</td>
                <td>${p.usageCount || 0} / ${p.usageLimit || '∞'}</td>
                <td>${Format.statusBadge(p.isActive ? 'Active' : 'Inactive')}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Promotions.view(${p.promotionId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="Promotions.edit(${p.promotionId})">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-${p.isActive ? 'danger' : 'success'}" onclick="Promotions.toggle(${p.promotionId})">
                            ${p.isActive ? Icons.toggleOff : Icons.toggleOn}
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="Promotions.delete(${p.promotionId})">${Icons.delete}</button>
                    </div>
                </td>
            </tr>
        `).join('');
        this.applyCurrentFilters();
        if (typeof initPagination === 'function') initPagination('promotionsTableBody', 'mktPromotionsPagination', 10);
    },

    updateStats() {
        const total = this.data.length;
        const active = this.data.filter(p => p.isActive).length;
        const totalUsage = this.data.reduce((sum, p) => sum + (p.usageCount || 0), 0);

        if (document.getElementById('totalPromotions')) document.getElementById('totalPromotions').textContent = total;
        if (document.getElementById('activePromotions')) document.getElementById('activePromotions').textContent = active;
        if (document.getElementById('totalUsage')) document.getElementById('totalUsage').textContent = totalUsage;
    },

    edit(id) {
        this.currentId = id;
        const promo = this.data.find(p => p.promotionId === id);
        if (!promo) return;

        document.getElementById('promotionId').value = id;
        document.getElementById('promoCode').value = promo.promoCode || '';
        document.getElementById('promoName').value = promo.promoName || '';
        document.getElementById('discountType').value = promo.discountType || '';
        document.getElementById('discountValue').value = promo.discountValue || '';
        document.getElementById('promoStartDate').value = promo.startDate?.split('T')[0] || '';
        document.getElementById('promoEndDate').value = promo.endDate?.split('T')[0] || '';
        document.getElementById('usageLimit').value = promo.usageLimit || '';

        Modal.show('promotionModal');
    },

    openCreateModal() {
        Modal.reset('promotionForm');
        document.getElementById('promotionId').value = '';
        Modal.show('promotionModal');
    },

    view(id) {
        const p = this.data.find(promo => promo.promotionId === id);
        if (!p) {
            Toast.error('Promotion not found');
            return;
        }
        
        this.currentId = id;
        const content = document.getElementById('viewPromotionContent');
        if (content) {
            content.innerHTML = `
                <div class="row g-4">
                    <div class="col-12 d-flex justify-content-between align-items-start border-bottom pb-3">
                        <div>
                            <h5 class="mb-1">${p.promoName}</h5>
                            <code class="fs-5">${p.promoCode}</code>
                        </div>
                        ${Format.statusBadge(p.isActive ? 'Active' : 'Inactive')}
                    </div>
                    <div class="col-md-6">
                        <div class="detail-label">Discount Type</div>
                        <div class="detail-value">${p.discountType}</div>
                    </div>
                    <div class="col-md-6">
                        <div class="detail-label">Discount Value</div>
                        <div class="detail-value fw-semibold text-success">
                            ${p.discountType === 'Percentage' ? Format.percentage(p.discountValue) : Format.currency(p.discountValue)}
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="detail-label">Validity Period</div>
                        <div class="detail-value">${Format.date(p.startDate)} - ${Format.date(p.endDate)}</div>
                    </div>
                    <div class="col-md-6">
                        <div class="detail-label">Usage</div>
                        <div class="detail-value">${p.usageCount || 0} / ${p.usageLimit || '∞'}</div>
                    </div>
                    ${p.minOrderAmount ? `<div class="col-md-6"><div class="detail-label">Min. Order Amount</div><div class="detail-value">${Format.currency(p.minOrderAmount)}</div></div>` : ''}
                    ${p.description ? `<div class="col-12"><div class="detail-label">Description</div><div class="detail-value">${p.description}</div></div>` : ''}
                </div>
            `;
        }
        Modal.show('viewPromotionModal');
    },

    async toggle(id) {
        try {
            await API.put(`/promotions/${id}/toggle`, {});
            Toast.success('Promotion status updated');
            await this.load();
        } catch (error) {
            Toast.error('Failed to update promotion');
        }
    },

    async delete(id) {
        if (!confirm('Are you sure you want to delete this promotion?')) return;
        try {
            await API.delete(`/promotions/${id}`);
            Toast.success('Promotion deleted successfully');
            await this.load();
        } catch (error) {
            Toast.error('Failed to delete promotion');
        }
    },

    async save() {
        const data = {
            promotionId: document.getElementById('promotionId')?.value || 0,
            promoCode: document.getElementById('promoCode')?.value,
            promoName: document.getElementById('promoName')?.value,
            discountType: document.getElementById('discountType')?.value,
            discountValue: parseFloat(document.getElementById('discountValue')?.value) || 0,
            startDate: document.getElementById('promoStartDate')?.value,
            endDate: document.getElementById('promoEndDate')?.value,
            usageLimit: parseInt(document.getElementById('usageLimit')?.value) || null
        };

        try {
            if (data.promotionId) {
                await API.put(`/promotions/${data.promotionId}`, data);
                Toast.success('Promotion updated');
            } else {
                await API.post('/promotions', data);
                Toast.success('Promotion created');
            }
            Modal.hide('promotionModal');
            await this.load();
        } catch (error) {
            Toast.error('Failed to save promotion');
        }
    },

    applyCurrentFilters() {
        const search = document.getElementById('promotionSearch')?.value || '';
        const status = document.getElementById('promotionStatusFilter')?.value || '';
        if (search || status) {
            this.filter(search, status);
        }
    },

    filter(search = '', status = '') {
        if (typeof resetPagination === 'function') resetPagination('mktPromotionsPagination');
        let filtered = this.data;
        
        if (search) {
            const s = search.toLowerCase();
            filtered = filtered.filter(p => 
                p.promoCode?.toLowerCase().includes(s) ||
                p.promoName?.toLowerCase().includes(s)
            );
        }
        
        if (status) {
            const isActive = status === 'Active';
            filtered = filtered.filter(p => p.isActive === isActive);
        }
        
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('promotionsTableBody');
        if (!tbody) return;

        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4">No promotions found</td></tr>';
            return;
        }

        tbody.innerHTML = data.map(p => `
            <tr>
                <td><strong>${p.promoCode}</strong></td>
                <td>${p.promoName}</td>
                <td>${p.discountType === 'Percentage' ? Format.percentage(p.discountValue) : Format.currency(p.discountValue)}</td>
                <td>${Format.date(p.startDate)} - ${Format.date(p.endDate)}</td>
                <td>${p.usageCount || 0} / ${p.usageLimit || '∞'}</td>
                <td>${Format.statusBadge(p.isActive ? 'Active' : 'Inactive')}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Promotions.view(${p.promotionId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="Promotions.edit(${p.promotionId})">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-${p.isActive ? 'danger' : 'success'}" onclick="Promotions.toggle(${p.promotionId})">
                            ${p.isActive ? Icons.toggleOff : Icons.toggleOn}
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="Promotions.delete(${p.promotionId})">${Icons.delete}</button>
                    </div>
                </td>
            </tr>
        `).join('');
        if (typeof initPagination === 'function') initPagination('promotionsTableBody', 'mktPromotionsPagination', 10);
    }
};

// ===========================================
// CUSTOMER SEGMENTS MODULE
// ===========================================
const Segments = {
    data: [],
    initialized: false,
    loading: false,

    async load() {
        if (this.loading) return;
        this.loading = true;

        try {
            const result = await API.get('/marketing/segments');
            this.data = result || {};
            this.render();
        } catch (error) {
            Toast.error('Failed to load segments');
        } finally {
            this.initialized = true;
            this.loading = false;
        }
    },

    render() {
        const container = document.getElementById('segmentsGrid') || document.getElementById('segmentsContainer');
        if (!container) return;

        if (!this.data || (Array.isArray(this.data) ? this.data.length === 0 : Object.keys(this.data).length === 0)) {
            container.innerHTML = '<div class="text-center py-4">No segments available</div>';
            return;
        }

        const segments = Array.isArray(this.data) ? this.data : Object.values(this.data);
        container.innerHTML = segments.map(s => `
            <div class="col-md-4 mb-3">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start mb-2">
                            <h5 class="card-title mb-0">${s.name || s.segmentName || 'Segment'}</h5>
                            <span class="badge" style="background-color: ${s.color || '#008080'}">${s.count || s.customerCount || 0} customers</span>
                        </div>
                        <p class="card-text text-muted small">${s.description || 'Customer segment'}</p>
                        <button class="btn btn-sm btn-outline-primary" onclick="Segments.viewCustomers('${(s.name || s.segmentName || '').replace(/'/g, "\\'")}')">View Customers</button>
                    </div>
                </div>
            </div>
        `).join('');
    },

    viewCustomers(segmentName) {
        window.location.href = `/MarketingStaff/SegmentCustomers?segment=${encodeURIComponent(segmentName)}`;
    }
};

// ===========================================
// ANALYTICS MODULE
// ===========================================
const Analytics = {
    initialized: false,
    loading: false,
    performanceChart: null,
    campaignTypeChart: null,

    async load() {
        if (this.loading) return;
        this.loading = true;

        try {
            const [campaigns, promotions] = await Promise.all([
                API.get('/campaigns').catch(() => []),
                API.get('/promotions').catch(() => [])
            ]);

            const campaignList = Array.isArray(campaigns) ? campaigns : [];
            const promotionList = Array.isArray(promotions) ? promotions : [];

            this.renderSummaryCards(campaignList, promotionList);
            this.renderCampaignPerformance(campaignList);
            this.renderPromotionUsage(promotionList);
            this.renderTopCampaigns(campaignList);
            this.renderTopPromotions(promotionList);
        } catch (error) {
            Toast.error('Failed to load analytics');
        } finally {
            this.initialized = true;
            this.loading = false;
        }
    },

    renderSummaryCards(campaigns, promotions) {
        const el = id => document.getElementById(id);
        if (el('totalCampaigns')) el('totalCampaigns').textContent = campaigns.length;
        const totalReach = campaigns.reduce((sum, c) => sum + (c.totalReach || 0), 0);
        if (el('totalReach')) el('totalReach').textContent = totalReach >= 1000 ? (totalReach / 1000).toFixed(1) + 'K' : totalReach;
        const totalClicks = campaigns.reduce((sum, c) => sum + (c.clicks || 0), 0);
        const totalImpressions = campaigns.reduce((sum, c) => sum + (c.impressions || 0), 0);
        const engRate = totalImpressions > 0 ? ((totalClicks / totalImpressions) * 100).toFixed(1) : 0;
        if (el('engagementRate')) el('engagementRate').textContent = engRate + '%';
        const totalSpend = campaigns.reduce((sum, c) => sum + (c.actualSpend || 0), 0);
        const totalRevenue = campaigns.reduce((sum, c) => sum + (c.revenue || 0), 0);
        const roi = totalSpend > 0 ? (((totalRevenue - totalSpend) / totalSpend) * 100).toFixed(1) : 0;
        if (el('roi')) el('roi').textContent = roi + '%';
    },

    renderCampaignPerformance(campaigns) {
        const ctx = (document.getElementById('performanceChart') || document.getElementById('campaignChart'))?.getContext('2d');
        if (!ctx) return;

        if (this.performanceChart) {
            this.performanceChart.destroy();
            this.performanceChart = null;
        }

        const active = campaigns.filter(c => c.status === 'Active').length;
        const completed = campaigns.filter(c => c.status === 'Completed').length;
        const scheduled = campaigns.filter(c => c.status === 'Scheduled').length;
        const paused = campaigns.filter(c => c.status === 'Paused').length;

        this.performanceChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: ['Active', 'Completed', 'Scheduled', 'Paused'],
                datasets: [{
                    data: [active, completed, scheduled, paused],
                    backgroundColor: ['#10B981', '#3B82F6', '#F59E0B', '#6B7280']
                }]
            },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { position: 'bottom' } } }
        });
    },

    renderPromotionUsage(promotions) {
        const ctx = (document.getElementById('campaignTypeChart') || document.getElementById('promotionChart'))?.getContext('2d');
        if (!ctx) return;

        if (this.campaignTypeChart) {
            this.campaignTypeChart.destroy();
            this.campaignTypeChart = null;
        }

        const labels = promotions.slice(0, 5).map(p => p.promoCode || p.promotionCode || p.promotionName || 'N/A');
        const data = promotions.slice(0, 5).map(p => p.usageCount || p.timesUsed || 0);

        this.campaignTypeChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Usage Count',
                    data: data,
                    backgroundColor: '#008080'
                }]
            },
            options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: false } } }
        });
    },

    renderTopCampaigns(campaigns) {
        const tbody = document.getElementById('topCampaignsTable');
        if (!tbody) return;
        const sorted = [...campaigns].sort((a, b) => (b.revenue || 0) - (a.revenue || 0)).slice(0, 5);
        if (sorted.length === 0) {
            tbody.innerHTML = '<tr><td colspan="4" class="text-center py-4 text-muted">No campaigns yet</td></tr>';
            return;
        }
        tbody.innerHTML = sorted.map(c => {
            const roi = (c.actualSpend || 0) > 0 ? (((c.revenue || 0) - c.actualSpend) / c.actualSpend * 100).toFixed(1) : 0;
            return `<tr>
                <td>${c.campaignName || 'N/A'}</td>
                <td class="text-center">${(c.totalReach || 0).toLocaleString()}</td>
                <td class="text-center">${c.conversions || 0}</td>
                <td class="text-end">${roi}%</td>
            </tr>`;
        }).join('');
    },

    renderTopPromotions(promotions) {
        const tbody = document.getElementById('topPromotionsTable');
        if (!tbody) return;
        const sorted = [...promotions].sort((a, b) => (b.usageCount || b.timesUsed || 0) - (a.usageCount || a.timesUsed || 0)).slice(0, 5);
        if (sorted.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-center py-4 text-muted">No promotions yet</td></tr>';
            return;
        }
        tbody.innerHTML = sorted.map(p => `<tr>
            <td>${p.promoCode || p.promotionCode || p.promotionName || 'N/A'}</td>
            <td class="text-center">${p.usageCount || p.timesUsed || 0}</td>
            <td class="text-end">₱${(p.revenue || 0).toLocaleString()}</td>
        </tr>`).join('');
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    const path = window.location.pathname.toLowerCase();
    
    if (path.includes('/marketingstaff/campaigns') || path.endsWith('/campaigns')) {
        Campaigns.load();
    } else if (path.includes('/marketingstaff/promotions') || path.endsWith('/promotions')) {
        Promotions.load();
    } else if (path.includes('/marketingstaff/segments') || path.endsWith('/segments')) {
        if (!Segments.initialized) Segments.load();
    } else if (path.includes('/marketingstaff/analytics') || path.endsWith('/analytics')) {
        if (!Analytics.initialized) Analytics.load();
    } else if (path === '/marketingstaff' || path === '/marketingstaff/') {
        // Dashboard
        Promise.all([
            API.get('/campaigns').catch(() => []),
            API.get('/promotions').catch(() => [])
        ]).then(([campaigns, promotions]) => {
            const active = campaigns.filter(c => c.status === 'Active').length;
            const totalBudget = campaigns.reduce((sum, c) => sum + (c.budget || 0), 0);
            
            if (document.getElementById('totalCampaigns')) document.getElementById('totalCampaigns').textContent = campaigns.length;
            if (document.getElementById('activeCampaigns')) document.getElementById('activeCampaigns').textContent = active;
            if (document.getElementById('totalBudget')) document.getElementById('totalBudget').textContent = Format.currency(totalBudget);
            if (document.getElementById('activePromotions')) document.getElementById('activePromotions').textContent = promotions.filter(p => p.isActive).length;
        });
    }
});
