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
                    </div>
                </td>
            </tr>
        `).join('');
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
        window.location.href = `/MarketingStaff/CampaignDetails/${id}`;
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
            this.load();
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
            this.load();
        } catch (error) {
            Toast.error('Failed to update campaign status');
        }
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
                        <button class="btn btn-sm btn-outline-warning" onclick="Promotions.edit(${p.promotionId})">${Icons.edit}</button>
                        <button class="btn btn-sm btn-outline-${p.isActive ? 'danger' : 'success'}" onclick="Promotions.toggle(${p.promotionId})">
                            ${p.isActive ? Icons.toggleOff : Icons.toggleOn}
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');
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
        Modal.show('promotionModal');
    },

    async toggle(id) {
        try {
            await API.put(`/promotions/${id}/toggle`, {});
            Toast.success('Promotion status updated');
            this.load();
        } catch (error) {
            Toast.error('Failed to update promotion');
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
            this.load();
        } catch (error) {
            Toast.error('Failed to save promotion');
        }
    }
};

// ===========================================
// CUSTOMER SEGMENTS MODULE
// ===========================================
const Segments = {
    data: [],

    async load() {
        try {
            this.data = await API.get('/marketing/segments');
            this.render();
        } catch (error) {
            Toast.error('Failed to load segments');
        }
    },

    render() {
        const container = document.getElementById('segmentsContainer');
        if (!container) return;

        if (!this.data || Object.keys(this.data).length === 0) {
            container.innerHTML = '<div class="text-center py-4">No segments available</div>';
            return;
        }

        const segments = Object.values(this.data);
        container.innerHTML = segments.map(s => `
            <div class="col-md-4 mb-3">
                <div class="card h-100">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start mb-2">
                            <h5 class="card-title mb-0">${s.name}</h5>
                            <span class="badge" style="background-color: ${s.color}">${s.count} customers</span>
                        </div>
                        <p class="card-text text-muted small">${s.description}</p>
                        <button class="btn btn-sm btn-outline-primary" onclick="Segments.viewCustomers('${s.name}')">View Customers</button>
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
    async load() {
        try {
            const [campaigns, promotions] = await Promise.all([
                API.get('/campaigns').catch(() => []),
                API.get('/promotions').catch(() => [])
            ]);

            this.renderCampaignPerformance(campaigns);
            this.renderPromotionUsage(promotions);
        } catch (error) {
            Toast.error('Failed to load analytics');
        }
    },

    renderCampaignPerformance(campaigns) {
        const ctx = document.getElementById('campaignChart')?.getContext('2d');
        if (!ctx) return;

        const active = campaigns.filter(c => c.status === 'Active').length;
        const completed = campaigns.filter(c => c.status === 'Completed').length;
        const scheduled = campaigns.filter(c => c.status === 'Scheduled').length;
        const paused = campaigns.filter(c => c.status === 'Paused').length;

        new Chart(ctx, {
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
        const ctx = document.getElementById('promotionChart')?.getContext('2d');
        if (!ctx) return;

        const labels = promotions.slice(0, 5).map(p => p.promoCode);
        const data = promotions.slice(0, 5).map(p => p.usageCount || 0);

        new Chart(ctx, {
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
        Segments.load();
    } else if (path.includes('/marketingstaff/analytics') || path.endsWith('/analytics')) {
        Analytics.load();
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
