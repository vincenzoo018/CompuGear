/**
 * CompuGear Admin JavaScript - Comprehensive CRUD Operations
 * Handles all admin module functionality with database integration
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
                <div class="toast-body">
                    <i class="bi bi-${type === 'success' ? 'check-circle' : type === 'error' ? 'x-circle' : 'exclamation-circle'} me-2"></i>
                    ${message}
                </div>
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

// Loading Spinner
const Loader = {
    show(element) {
        if (element) {
            element.innerHTML = '<div class="text-center py-5"><div class="spinner-border text-teal" role="status"><span class="visually-hidden">Loading...</span></div></div>';
        }
    },

    hide(element, content = '') {
        if (element) {
            element.innerHTML = content;
        }
    }
};

// API Helper Functions
const API = {
    async request(endpoint, method = 'GET', data = null) {
        const options = {
            method,
            headers: {
                'Content-Type': 'application/json'
            }
        };

        if (data && method !== 'GET') {
            options.body = JSON.stringify(data);
        }

        try {
            const response = await fetch(`${CONFIG.apiBase}${endpoint}`, options);
            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.message || 'An error occurred');
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
        if (includeTime) {
            options.hour = '2-digit';
            options.minute = '2-digit';
        }
        return date.toLocaleDateString(CONFIG.dateFormat, options);
    },

    percentage(value) {
        return `${parseFloat(value || 0).toFixed(1)}%`;
    },

    statusBadge(status, colorMap = {}) {
        const defaultColors = {
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
            'Draft': 'secondary', 'draft': 'secondary',
            'Scheduled': 'info', 'scheduled': 'info',
            'Paused': 'warning', 'paused': 'warning',
            'New': 'primary', 'new': 'primary',
            'Hot': 'danger', 'hot': 'danger',
            'Warm': 'warning', 'warm': 'warning',
            'Cold': 'info', 'cold': 'info',
            'Won': 'success', 'won': 'success',
            'Lost': 'danger', 'lost': 'danger',
            'Delivered': 'success', 'delivered': 'success',
            'Shipped': 'info', 'shipped': 'info',
            'Processing': 'warning', 'processing': 'warning',
            'Confirmed': 'primary', 'confirmed': 'primary'
        };
        const colors = { ...defaultColors, ...colorMap };
        const color = colors[status] || 'secondary';
        return `<span class="badge bg-${color}">${status}</span>`;
    },

    priorityBadge(priority) {
        const colors = {
            'High': 'danger', 'high': 'danger',
            'Medium': 'warning', 'medium': 'warning',
            'Low': 'info', 'low': 'info',
            'Critical': 'dark', 'critical': 'dark',
            'Urgent': 'danger', 'urgent': 'danger'
        };
        const color = colors[priority] || 'secondary';
        return `<span class="badge bg-${color}">${priority}</span>`;
    }
};

// Table Builder
const Table = {
    build(data, columns, options = {}) {
        const { emptyMessage = 'No data available', tableClass = '', rowClick = null } = options;

        if (!data || data.length === 0) {
            return `<div class="text-center text-muted py-5"><i class="bi bi-inbox fs-1"></i><p>${emptyMessage}</p></div>`;
        }

        let html = `<div class="table-responsive"><table class="table table-hover align-middle ${tableClass}">`;
        
        // Header
        html += '<thead class="table-light"><tr>';
        columns.forEach(col => {
            html += `<th class="${col.class || ''}" style="${col.style || ''}">${col.header}</th>`;
        });
        html += '</tr></thead>';

        // Body
        html += '<tbody>';
        data.forEach((row, index) => {
            const clickAttr = rowClick ? `onclick="${rowClick}(${row.id || index})" style="cursor: pointer;"` : '';
            html += `<tr ${clickAttr}>`;
            columns.forEach(col => {
                const value = col.render ? col.render(row) : (row[col.field] || '-');
                html += `<td class="${col.cellClass || ''}">${value}</td>`;
            });
            html += '</tr>';
        });
        html += '</tbody></table></div>';

        return html;
    }

};

// Modal Manager
const Modal = {
    show(modalId) {
        const modalEl = document.getElementById(modalId);
        if (!modalEl) return null;
        
        // Clean up any existing backdrops first
        this.cleanup();
        
        // Get existing instance or create new one
        let modal = bootstrap.Modal.getInstance(modalEl);
        if (!modal) {
            modal = new bootstrap.Modal(modalEl, {
                backdrop: true,
                keyboard: true
            });
        }
        modal.show();
        return modal;
    },

    hide(modalId) {
        const modalEl = document.getElementById(modalId);
        if (!modalEl) return;
        
        const modal = bootstrap.Modal.getInstance(modalEl);
        
        // One-time listener for when modal is fully hidden
        const onHidden = () => {
            modalEl.removeEventListener('hidden.bs.modal', onHidden);
            
            // Dispose the modal instance
            const instance = bootstrap.Modal.getInstance(modalEl);
            if (instance) {
                instance.dispose();
            }
            
            // Force cleanup
            this.cleanup();
        };
        
        if (modal) {
            modalEl.addEventListener('hidden.bs.modal', onHidden);
            modal.hide();
        } else {
            // No instance, just cleanup
            this.cleanup();
        }
        
        // Backup cleanup after animation would complete
        setTimeout(() => this.cleanup(), 500);
    },

    reset(formId) {
        const form = document.getElementById(formId);
        if (form) {
            form.reset();
            form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
        }
    },

    // Force cleanup all modal backdrops
    cleanup() {
        // Remove all backdrop elements
        document.querySelectorAll('.modal-backdrop').forEach(backdrop => {
            backdrop.remove();
        });
        
        // Remove modal-open class and styles from body
        document.body.classList.remove('modal-open');
        document.body.style.removeProperty('overflow');
        document.body.style.removeProperty('padding-right');
        document.body.style.overflow = '';
        document.body.style.paddingRight = '';
    }
};

// Form Validation
const Validate = {
    form(formId) {
        const form = document.getElementById(formId);
        if (!form) return false;

        let isValid = true;
        const requiredFields = form.querySelectorAll('[required]');

        requiredFields.forEach(field => {
            if (!field.value.trim()) {
                field.classList.add('is-invalid');
                isValid = false;
            } else {
                field.classList.remove('is-invalid');
            }
        });

        return isValid;
    },

    email(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    },

    phone(phone) {
        return /^[\d\s\-\+\(\)]+$/.test(phone);
    }
};

// ===========================================
// MARKETING MODULE
// ===========================================
const Marketing = {
    // Campaigns
    campaigns: {
        data: [],
        currentId: null,

        async load() {
            try {
                this.data = await API.get('/campaigns');
                this.render();
            } catch (error) {
                Toast.error('Failed to load campaigns');
            }
        },

        render() {
            const tbody = document.getElementById('campaignsTableBody');
            if (!tbody) return;

            if (this.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No campaigns found. Click "New Campaign" to create one.</td></tr>';
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
                    <td>
                        <div class="progress" style="height: 6px;">
                            <div class="progress-bar bg-teal" style="width: ${Math.min((c.actualSpend / c.budget) * 100, 100)}%"></div>
                        </div>
                        <small class="text-muted">${Format.currency(c.actualSpend)} spent</small>
                    </td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Marketing.campaigns.view(${c.campaignId})" title="View">
                                ${Icons.view}
                            </button>
                            <button class="btn btn-sm btn-outline-warning" onclick="Marketing.campaigns.edit(${c.campaignId})" title="Edit">
                                ${Icons.edit}
                            </button>
                            <button class="btn btn-sm btn-outline-${c.status === 'Active' ? 'danger' : 'success'}" onclick="Marketing.campaigns.toggleStatus(${c.campaignId})" title="${c.status === 'Active' ? 'Deactivate' : 'Activate'}">
                                ${c.status === 'Active' ? Icons.toggleOff : Icons.toggleOn}
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');

            this.updateStats();
        },

        updateStats() {
            const total = this.data.length;
            const active = this.data.filter(c => c.status === 'Active').length;
            const totalBudget = this.data.reduce((sum, c) => sum + (c.budget || 0), 0);
            const avgRoi = this.data.length > 0 ? this.data.reduce((sum, c) => sum + (c.roi || 0), 0) / this.data.length : 0;

            document.getElementById('totalCampaigns')?.textContent && (document.getElementById('totalCampaigns').textContent = total);
            document.getElementById('activeCampaigns')?.textContent && (document.getElementById('activeCampaigns').textContent = active);
            document.getElementById('totalBudget')?.textContent && (document.getElementById('totalBudget').textContent = Format.currency(totalBudget));
            document.getElementById('avgRoi')?.textContent && (document.getElementById('avgRoi').textContent = Format.percentage(avgRoi));
        },

        showModal(campaign = null) {
            Modal.reset('campaignForm');
            document.getElementById('campaignModalTitle').textContent = campaign ? 'Edit Campaign' : 'New Campaign';
            document.getElementById('campaignId').value = campaign?.campaignId || '';

            if (campaign) {
                document.getElementById('campaignName').value = campaign.campaignName || '';
                document.getElementById('campaignType').value = campaign.type || '';
                document.getElementById('campaignDescription').value = campaign.description || '';
                document.getElementById('campaignStartDate').value = campaign.startDate?.split('T')[0] || '';
                document.getElementById('campaignEndDate').value = campaign.endDate?.split('T')[0] || '';
                document.getElementById('campaignBudget').value = campaign.budget || '';
                document.getElementById('campaignTarget').value = campaign.targetSegment || '';
            }

            Modal.show('campaignModal');
        },

        async save() {
            if (!Validate.form('campaignForm')) {
                Toast.error('Please fill all required fields');
                return;
            }

            const id = document.getElementById('campaignId').value;
            const data = {
                campaignName: document.getElementById('campaignName').value,
                type: document.getElementById('campaignType').value,
                description: document.getElementById('campaignDescription').value,
                startDate: document.getElementById('campaignStartDate').value,
                endDate: document.getElementById('campaignEndDate').value,
                budget: parseFloat(document.getElementById('campaignBudget').value) || 0,
                targetSegment: document.getElementById('campaignTarget').value,
                status: 'Draft'
            };

            try {
                if (id) {
                    await API.put(`/campaigns/${id}`, data);
                    Toast.success('Campaign updated successfully');
                } else {
                    await API.post('/campaigns', data);
                    Toast.success('Campaign created successfully');
                }
                Modal.hide('campaignModal');
                this.load();
            } catch (error) {
                Toast.error(error.message || 'Failed to save campaign');
            }
        },

        view(id) {
            const campaign = this.data.find(c => c.campaignId === id);
            if (!campaign) {
                Toast.error('Campaign not found');
                return;
            }
            
            this.currentId = id;
            document.getElementById('viewCampaignContent').innerHTML = `
                <div class="row g-4">
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Campaign Name</h6>
                        <p class="fw-semibold">${campaign.campaignName}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Code</h6>
                        <p>${campaign.campaignCode}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Type</h6>
                        <p><span class="badge bg-info">${campaign.type || 'General'}</span></p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Status</h6>
                        <p>${Format.statusBadge(campaign.status)}</p>
                    </div>
                    <div class="col-12">
                        <h6 class="text-muted mb-1">Description</h6>
                        <p>${campaign.description || '-'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Duration</h6>
                        <p>${Format.date(campaign.startDate)} - ${Format.date(campaign.endDate)}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Target Segment</h6>
                        <p>${campaign.targetSegment || 'All Customers'}</p>
                    </div>
                    <div class="col-md-4">
                        <div class="card bg-light">
                            <div class="card-body text-center">
                                <h6 class="text-muted">Budget</h6>
                                <h4 class="text-teal">${Format.currency(campaign.budget)}</h4>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card bg-light">
                            <div class="card-body text-center">
                                <h6 class="text-muted">Spent</h6>
                                <h4 class="text-warning">${Format.currency(campaign.actualSpend)}</h4>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <div class="card bg-light">
                            <div class="card-body text-center">
                                <h6 class="text-muted">ROI</h6>
                                <h4 class="text-success">${Format.percentage(campaign.roi)}</h4>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            Modal.show('viewCampaignModal');
        },

        edit(id) {
            const campaign = this.data.find(c => c.campaignId === id);
            if (!campaign) {
                Toast.error('Campaign not found');
                return;
            }
            this.currentId = id;
            this.showModal(campaign);
        },

        async toggleStatus(id) {
            const campaign = this.data.find(c => c.campaignId === id);
            if (!campaign) return;

            const newStatus = campaign.status === 'Active' ? 'Paused' : 'Active';
            
            try {
                await API.put(`/campaigns/${id}`, { ...campaign, status: newStatus });
                Toast.success(`Campaign ${newStatus === 'Active' ? 'activated' : 'paused'}`);
                this.load();
            } catch (error) {
                Toast.error('Failed to update campaign status');
            }
        },

        async delete(id) {
            if (!confirm('Are you sure you want to delete this campaign?')) return;

            try {
                await API.delete(`/campaigns/${id}`);
                Toast.success('Campaign deleted successfully');
                this.load();
            } catch (error) {
                Toast.error('Failed to delete campaign');
            }
        }
    },

    // Promotions
    promotions: {
        data: [],
        currentId: null,

        async load() {
            try {
                this.data = await API.get('/promotions');
                this.render();
            } catch (error) {
                Toast.error('Failed to load promotions');
            }
        },

        render() {
            const tbody = document.getElementById('promotionsTableBody');
            if (!tbody) return;

            if (this.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No promotions found. Click "New Promotion" to create one.</td></tr>';
                return;
            }

            tbody.innerHTML = this.data.map(p => `
                <tr data-deployed="${p.isActive}">
                    <td>
                        <div class="fw-semibold">${p.promotionName}</div>
                        <code class="small bg-light px-2 py-1 rounded">${p.promotionCode}</code>
                    </td>
                    <td><span class="badge bg-info">${p.discountType}</span></td>
                    <td class="text-end fw-semibold text-success">
                        ${p.discountType === 'Percentage' ? Format.percentage(p.discountValue) : 
                          p.discountType === 'FreeShipping' ? '<span class="badge bg-warning">Free Shipping</span>' :
                          Format.currency(p.discountValue)}
                    </td>
                    <td>
                        <div>${Format.date(p.startDate)}</div>
                        <small class="text-muted">to ${Format.date(p.endDate)}</small>
                    </td>
                    <td class="text-center">${p.timesUsed || 0} / ${p.usageLimit || '∞'}</td>
                    <td class="text-center">
                        <button class="btn btn-sm deploy-btn ${p.isActive ? 'deployed' : 'not-deployed'}" 
                                onclick="Marketing.promotions.toggleVisibility(${p.promotionId})"
                                title="${p.isActive ? 'Currently visible to customers - Click to hide' : 'Hidden from customers - Click to deploy'}">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="white" stroke-width="2" class="me-1">
                                ${p.isActive ? 
                                    '<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>' : 
                                    '<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/>'}
                            </svg>
                            ${p.isActive ? 'Deployed' : 'Deploy'}
                        </button>
                    </td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Marketing.promotions.view(${p.promotionId})" title="View">
                                ${Icons.view}
                            </button>
                            <button class="btn btn-sm btn-outline-warning" onclick="Marketing.promotions.edit(${p.promotionId})" title="Edit">
                                ${Icons.edit}
                            </button>
                            <button class="btn btn-sm btn-outline-${p.isActive ? 'danger' : 'success'}" onclick="Marketing.promotions.toggleStatus(${p.promotionId})" title="${p.isActive ? 'Deactivate' : 'Activate'}">
                                ${p.isActive ? Icons.toggleOff : Icons.toggleOn}
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');

            // Update count badge
            const countBadge = document.getElementById('promotionCount');
            if (countBadge) countBadge.textContent = this.data.length + ' promotions';

            this.updateStats();
        },

        updateStats() {
            const total = this.data.length;
            const active = this.data.filter(p => p.isActive).length;
            const deployed = this.data.filter(p => p.isShowInCustomer).length;
            const totalUsage = this.data.reduce((sum, p) => sum + (p.timesUsed || 0), 0);

            document.getElementById('totalPromotions')?.textContent && (document.getElementById('totalPromotions').textContent = total);
            document.getElementById('activePromotions')?.textContent && (document.getElementById('activePromotions').textContent = active);
            document.getElementById('deployedPromotions')?.textContent && (document.getElementById('deployedPromotions').textContent = deployed);
            document.getElementById('totalUsage')?.textContent && (document.getElementById('totalUsage').textContent = totalUsage);
        },

        showModal(promotion = null) {
            Modal.reset('promotionForm');
            document.getElementById('promotionModalTitle').textContent = promotion ? 'Edit Promotion' : 'New Promotion';
            document.getElementById('promotionId').value = promotion?.promotionId || '';

            if (promotion) {
                document.getElementById('promotionCode').value = promotion.promotionCode || '';
                document.getElementById('promotionName').value = promotion.promotionName || '';
                document.getElementById('promotionDescription').value = promotion.description || '';
                document.getElementById('discountType').value = promotion.discountType || '';
                document.getElementById('discountValue').value = promotion.discountValue || '';
                document.getElementById('minOrderAmount').value = promotion.minOrderAmount || '';
                document.getElementById('maxDiscount').value = promotion.maxDiscountAmount || '';
                document.getElementById('promotionStartDate').value = promotion.startDate?.split('T')[0] || '';
                document.getElementById('promotionEndDate').value = promotion.endDate?.split('T')[0] || '';
                document.getElementById('usageLimit').value = promotion.usageLimit || '';
                document.getElementById('promotionActive').checked = promotion.isActive;
            }

            Modal.show('promotionModal');
        },

        async save() {
            if (!Validate.form('promotionForm')) {
                Toast.error('Please fill all required fields');
                return;
            }

            const id = document.getElementById('promotionId').value;
            const data = {
                promotionCode: document.getElementById('promotionCode').value,
                promotionName: document.getElementById('promotionName').value,
                description: document.getElementById('promotionDescription').value,
                discountType: document.getElementById('discountType').value,
                discountValue: parseFloat(document.getElementById('discountValue').value) || 0,
                minOrderAmount: parseFloat(document.getElementById('minOrderAmount').value) || 0,
                maxDiscountAmount: parseFloat(document.getElementById('maxDiscount').value) || 0,
                startDate: document.getElementById('promotionStartDate').value,
                endDate: document.getElementById('promotionEndDate').value,
                usageLimit: parseInt(document.getElementById('usageLimit').value) || null,
                isActive: document.getElementById('promotionActive').checked
            };

            try {
                if (id) {
                    await API.put(`/promotions/${id}`, data);
                    Toast.success('Promotion updated successfully');
                } else {
                    await API.post('/promotions', data);
                    Toast.success('Promotion created successfully');
                }
                Modal.hide('promotionModal');
                this.load();
            } catch (error) {
                Toast.error(error.message || 'Failed to save promotion');
            }
        },

        view(id) {
            const p = this.data.find(promo => promo.promotionId === id);
            if (!p) {
                Toast.error('Promotion not found');
                return;
            }
            
            this.currentId = id;
            document.getElementById('viewPromotionContent').innerHTML = `
                <div class="row g-4">
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Promotion Name</h6>
                        <p class="fw-semibold">${p.promotionName}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Code</h6>
                        <p><code class="bg-light px-2 py-1 rounded">${p.promotionCode}</code></p>
                    </div>
                    <div class="col-12">
                        <h6 class="text-muted mb-1">Description</h6>
                        <p>${p.description || '-'}</p>
                    </div>
                    <div class="col-md-4">
                        <h6 class="text-muted mb-1">Discount Type</h6>
                        <p><span class="badge bg-info">${p.discountType}</span></p>
                    </div>
                    <div class="col-md-4">
                        <h6 class="text-muted mb-1">Discount Value</h6>
                        <p class="fw-bold text-teal fs-5">${p.discountType === 'Percentage' ? Format.percentage(p.discountValue) : Format.currency(p.discountValue)}</p>
                    </div>
                    <div class="col-md-4">
                        <h6 class="text-muted mb-1">Status</h6>
                        <p>${p.isActive ? '<span class="badge bg-success">Active</span>' : '<span class="badge bg-secondary">Inactive</span>'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Minimum Order</h6>
                        <p>${Format.currency(p.minOrderAmount)}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Max Discount</h6>
                        <p>${p.maxDiscountAmount ? Format.currency(p.maxDiscountAmount) : 'No limit'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Valid Period</h6>
                        <p>${Format.date(p.startDate)} - ${Format.date(p.endDate)}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted mb-1">Usage</h6>
                        <p>${p.timesUsed || 0} / ${p.usageLimit || '∞'}</p>
                    </div>
                </div>
            `;
            Modal.show('viewPromotionModal');
        },

        edit(id) {
            const promotion = this.data.find(p => p.promotionId === id);
            if (!promotion) {
                Toast.error('Promotion not found');
                return;
            }
            this.currentId = id;
            this.showModal(promotion);
        },

        async toggleVisibility(id) {
            try {
                const result = await API.put(`/promotions/${id}/toggle`);
                Toast.success(result.message);
                this.load();
            } catch (error) {
                Toast.error('Failed to toggle promotion visibility');
            }
        },

        async toggleStatus(id) {
            try {
                const promotion = this.data.find(p => p.promotionId === id);
                if (!promotion) return;
                
                const newStatus = !promotion.isActive;
                await API.put(`/promotions/${id}/status`, { isActive: newStatus });
                Toast.success(`Promotion ${newStatus ? 'activated' : 'deactivated'} successfully`);
                this.load();
            } catch (error) {
                Toast.error('Failed to update promotion status');
            }
        },

        async delete(id) {
            if (!confirm('Are you sure you want to delete this promotion?')) return;

            try {
                await API.delete(`/promotions/${id}`);
                Toast.success('Promotion deleted successfully');
                this.load();
            } catch (error) {
                Toast.error('Failed to delete promotion');
            }
        }
    }
};

// ===========================================
// CUSTOMERS MODULE
// ===========================================
const Customers = {
    data: [],
    categories: [],
    currentId: null,

    async load() {
        try {
            [this.data, this.categories] = await Promise.all([
                API.get('/customers'),
                API.get('/customer-categories')
            ]);
            this.render();
        } catch (error) {
            Toast.error('Failed to load customers');
        }
    },

    render() {
        const tbody = document.getElementById('customersTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No customers found. Click "Add Customer" to create one.</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(c => `
            <tr>
                <td>
                    <div class="d-flex align-items-center">
                        <div class="avatar-circle me-3">${(c.firstName?.[0] || '') + (c.lastName?.[0] || '')}</div>
                        <div>
                            <div class="fw-semibold">${c.fullName}</div>
                            <small class="text-muted">${c.customerCode}</small>
                        </div>
                    </div>
                </td>
                <td>
                    <div>${c.email}</div>
                    <small class="text-muted">${c.phone || '-'}</small>
                </td>
                <td><span class="badge bg-info">${c.categoryName}</span></td>
                <td>${Format.statusBadge(c.status)}</td>
                <td class="text-center">${c.totalOrders}</td>
                <td class="text-end">${Format.currency(c.totalSpent)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Customers.view(${c.customerId})" title="View">
                            ${Icons.view}
                        </button>
                        <button class="btn btn-sm btn-outline-warning" onclick="Customers.edit(${c.customerId})" title="Edit">
                            ${Icons.edit}
                        </button>
                        <button class="btn btn-sm btn-outline-${c.status === 'Active' ? 'danger' : 'success'}" onclick="Customers.toggleStatus(${c.customerId})" title="${c.status === 'Active' ? 'Deactivate' : 'Activate'}">
                            ${c.status === 'Active' ? Icons.toggleOff : Icons.toggleOn}
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');

        this.updateStats();
    },

    updateStats() {
        const total = this.data.length;
        const active = this.data.filter(c => c.status === 'Active').length;
        const totalSpent = this.data.reduce((sum, c) => sum + (c.totalSpent || 0), 0);

        document.getElementById('totalCustomers')?.textContent && (document.getElementById('totalCustomers').textContent = total);
        document.getElementById('activeCustomers')?.textContent && (document.getElementById('activeCustomers').textContent = active);
        document.getElementById('totalRevenue')?.textContent && (document.getElementById('totalRevenue').textContent = Format.currency(totalSpent));
    },

    showModal(customer = null) {
        Modal.reset('customerForm');
        const modalTitle = document.getElementById('modalTitle');
        if (modalTitle) modalTitle.textContent = customer ? 'Edit Customer' : 'Add New Customer';
        document.getElementById('customerId').value = customer?.customerId || '';

        if (customer) {
            document.getElementById('firstName').value = customer.firstName || '';
            document.getElementById('lastName').value = customer.lastName || '';
            document.getElementById('companyName').value = customer.companyName || '';
            document.getElementById('categoryId').value = customer.categoryId || '';
            document.getElementById('email').value = customer.email || '';
            document.getElementById('phone').value = customer.phone || '';
            document.getElementById('street').value = customer.billingAddress || '';
            document.getElementById('city').value = customer.billingCity || '';
            document.getElementById('province').value = customer.billingState || '';
            document.getElementById('postalCode').value = customer.billingZipCode || '';
            document.getElementById('creditLimit').value = customer.creditLimit || 0;
            document.getElementById('isActive').value = customer.isActive ? 'true' : 'false';
            document.getElementById('notes').value = customer.notes || '';
        }

        Modal.show('customerModal');
    },

    async save() {
        const id = document.getElementById('customerId').value;
        const data = {
            firstName: document.getElementById('firstName').value,
            lastName: document.getElementById('lastName').value,
            companyName: document.getElementById('companyName').value,
            categoryId: parseInt(document.getElementById('categoryId').value) || null,
            email: document.getElementById('email').value,
            phone: document.getElementById('phone').value,
            billingAddress: document.getElementById('street').value,
            billingCity: document.getElementById('city').value,
            billingState: document.getElementById('province').value,
            billingZipCode: document.getElementById('postalCode').value,
            creditLimit: parseFloat(document.getElementById('creditLimit').value) || 0,
            isActive: document.getElementById('isActive').value === 'true',
            notes: document.getElementById('notes').value
        };

        try {
            if (id) {
                await API.put(`/customers/${id}`, data);
                Toast.success('Customer updated successfully');
            } else {
                await API.post('/customers', data);
                Toast.success('Customer added successfully');
            }
            Modal.hide('customerModal');
            this.load();
        } catch (error) {
            Toast.error(error.message || 'Failed to save customer');
        }
    },

    view(id) {
        const c = this.data.find(customer => customer.customerId === id);
        if (!c) {
            Toast.error('Customer not found');
            return;
        }
        
        this.currentId = id;
        document.getElementById('viewCustomerContent').innerHTML = `
            <div class="text-center mb-4">
                <div class="avatar-circle avatar-lg mx-auto mb-3">${(c.firstName?.[0] || '') + (c.lastName?.[0] || '')}</div>
                <h4>${c.firstName} ${c.lastName}</h4>
                <p class="text-muted">${c.customerCode}</p>
                ${Format.statusBadge(c.status)}
            </div>
            <div class="row g-3">
                <div class="col-md-6">
                    <h6 class="text-muted">Email</h6>
                    <p>${c.email}</p>
                </div>
                <div class="col-md-6">
                    <h6 class="text-muted">Phone</h6>
                    <p>${c.phone || '-'}</p>
                </div>
                <div class="col-12">
                    <h6 class="text-muted">Address</h6>
                    <p>${c.billingAddress || '-'}, ${c.billingCity || ''}</p>
                </div>
                <div class="col-md-4">
                    <div class="card bg-light">
                        <div class="card-body text-center">
                            <h6 class="text-muted">Total Orders</h6>
                            <h4 class="text-teal">${c.totalOrders || 0}</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card bg-light">
                        <div class="card-body text-center">
                            <h6 class="text-muted">Total Spent</h6>
                            <h4 class="text-success">${Format.currency(c.totalSpent)}</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card bg-light">
                        <div class="card-body text-center">
                            <h6 class="text-muted">Loyalty Points</h6>
                            <h4 class="text-warning">${c.loyaltyPoints || 0}</h4>
                        </div>
                    </div>
                </div>
            </div>
        `;
        Modal.show('viewCustomerModal');
    },

    edit(id) {
        const customer = this.data.find(c => c.customerId === id);
        if (!customer) {
            Toast.error('Customer not found');
            return;
        }
        this.currentId = id;
        this.showModal(customer);
    },

    async toggleStatus(id) {
        try {
            const customer = this.data.find(c => c.customerId === id);
            if (!customer) return;
            
            const newStatus = customer.status === 'Active' ? 'Inactive' : 'Active';
            await API.put(`/customers/${id}/status`, { status: newStatus });
            Toast.success(`Customer ${newStatus === 'Active' ? 'activated' : 'deactivated'} successfully`);
            this.load();
        } catch (error) {
            Toast.error('Failed to update customer status');
        }
    }
};

// ===========================================
// INVENTORY MODULE
// ===========================================
const Inventory = {
    products: {
        data: [],
        categories: [],
        brands: [],
        currentId: null,

        async load() {
            try {
                [this.data, this.categories, this.brands] = await Promise.all([
                    API.get('/products'),
                    API.get('/product-categories'),
                    API.get('/brands')
                ]);
                this.render();
            } catch (error) {
                Toast.error('Failed to load products');
            }
        },

        render() {
            const tbody = document.getElementById('productsTableBody');
            if (!tbody) return;

            if (this.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4 text-muted">No products found. Click "Add Product" to create one.</td></tr>';
                return;
            }

            tbody.innerHTML = this.data.map(p => `
                <tr>
                    <td>
                        <div class="d-flex align-items-center">
                            <img src="${p.mainImageUrl || '/img/placeholder.png'}" class="product-thumb me-3" alt="">
                            <div>
                                <div class="fw-semibold">${p.productName}</div>
                                <small class="text-muted">${p.sku || p.productCode}</small>
                            </div>
                        </div>
                    </td>
                    <td>${p.categoryName || '-'}</td>
                    <td>${p.brandName || '-'}</td>
                    <td class="text-end">${Format.currency(p.costPrice)}</td>
                    <td class="text-end">${Format.currency(p.sellingPrice)}</td>
                    <td class="text-center">
                        <span class="badge ${p.stockQuantity <= p.reorderLevel ? 'bg-danger' : p.stockQuantity <= p.reorderLevel * 2 ? 'bg-warning text-dark' : 'bg-success'}">
                            ${p.stockQuantity}
                        </span>
                    </td>
                    <td>${Format.statusBadge(p.status)}</td>
                    <td class="text-center">
                        ${p.isFeatured ? '<i class="bi bi-star-fill text-warning"></i>' : ''}
                        ${p.isOnSale ? '<span class="badge bg-danger">Sale</span>' : ''}
                    </td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Inventory.products.view(${p.productId})" title="View">
                                ${Icons.view}
                            </button>
                            <button class="btn btn-sm btn-outline-warning" onclick="Inventory.products.edit(${p.productId})" title="Edit">
                                ${Icons.edit}
                            </button>
                            <button class="btn btn-sm btn-outline-${p.status === 'Active' ? 'danger' : 'success'}" onclick="Inventory.products.toggleStatus(${p.productId})" title="${p.status === 'Active' ? 'Deactivate' : 'Activate'}">
                                ${p.status === 'Active' ? Icons.toggleOff : Icons.toggleOn}
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');

            this.updateStats();
        },

        updateStats() {
            const total = this.data.length;
            const lowStock = this.data.filter(p => p.stockQuantity <= p.reorderLevel).length;
            const totalValue = this.data.reduce((sum, p) => sum + (p.stockQuantity * p.costPrice), 0);

            document.getElementById('totalProducts')?.textContent && (document.getElementById('totalProducts').textContent = total);
            document.getElementById('lowStockCount')?.textContent && (document.getElementById('lowStockCount').textContent = lowStock);
            document.getElementById('inventoryValue')?.textContent && (document.getElementById('inventoryValue').textContent = Format.currency(totalValue));
        },

        showModal(product = null) {
            Modal.reset('productForm');
            const modalTitle = document.getElementById('modalTitle');
            if (modalTitle) modalTitle.textContent = product ? 'Edit Product' : 'Add New Product';
            document.getElementById('productId').value = product?.productId || '';

            if (product) {
                document.getElementById('productName').value = product.productName || '';
                document.getElementById('productCode').value = product.sku || product.productCode || '';
                document.getElementById('description').value = product.shortDescription || '';
                document.getElementById('categoryId').value = product.categoryId || '';
                document.getElementById('brandId').value = product.brandId || '';
                document.getElementById('costPrice').value = product.costPrice || '';
                document.getElementById('sellingPrice').value = product.sellingPrice || '';
                document.getElementById('stockQuantity').value = product.stockQuantity || 0;
                document.getElementById('reorderLevel').value = product.reorderLevel || 10;
                document.getElementById('isActive').value = product.isActive ? 'true' : 'false';
            }

            Modal.show('productModal');
        },

        async save() {
            const id = document.getElementById('productId').value;
            const data = {
                productName: document.getElementById('productName').value,
                sku: document.getElementById('productCode').value,
                shortDescription: document.getElementById('description').value,
                categoryId: parseInt(document.getElementById('categoryId').value) || null,
                brandId: parseInt(document.getElementById('brandId').value) || null,
                costPrice: parseFloat(document.getElementById('costPrice').value) || 0,
                sellingPrice: parseFloat(document.getElementById('sellingPrice').value) || 0,
                stockQuantity: parseInt(document.getElementById('stockQuantity').value) || 0,
                reorderLevel: parseInt(document.getElementById('reorderLevel').value) || 10,
                isActive: document.getElementById('isActive').value === 'true'
            };

            try {
                if (id) {
                    await API.put(`/products/${id}`, data);
                    Toast.success('Product updated successfully');
                } else {
                    await API.post('/products', data);
                    Toast.success('Product added successfully');
                }
                Modal.hide('productModal');
                this.load();
            } catch (error) {
                Toast.error(error.message || 'Failed to save product');
            }
        },

        view(id) {
            const p = this.data.find(product => product.productId === id);
            if (!p) {
                Toast.error('Product not found');
                return;
            }
            
            this.currentId = id;
            document.getElementById('viewProductContent').innerHTML = `
                <div class="row g-4">
                    <div class="col-md-4 text-center">
                        <img src="${p.mainImageUrl || '/img/placeholder.png'}" class="img-fluid rounded mb-3" alt="">
                        ${p.isFeatured ? '<span class="badge bg-warning">Featured</span>' : ''}
                        ${p.isOnSale ? '<span class="badge bg-danger">On Sale</span>' : ''}
                    </div>
                    <div class="col-md-8">
                        <h4>${p.productName}</h4>
                        <p class="text-muted">${p.productCode || '-'} | SKU: ${p.sku || '-'}</p>
                        <p>${p.shortDescription || ''}</p>
                        <div class="row mt-3">
                            <div class="col-6">
                                <h6 class="text-muted">Cost Price</h6>
                                <h5>${Format.currency(p.costPrice)}</h5>
                            </div>
                            <div class="col-6">
                                <h6 class="text-muted">Selling Price</h6>
                                <h5 class="text-teal">${Format.currency(p.sellingPrice)}</h5>
                            </div>
                        </div>
                        <div class="row mt-3">
                            <div class="col-6">
                                <h6 class="text-muted">Stock</h6>
                                <h5 class="${p.stockQuantity <= p.reorderLevel ? 'text-danger' : 'text-success'}">${p.stockQuantity} units</h5>
                            </div>
                            <div class="col-6">
                                <h6 class="text-muted">Status</h6>
                                ${Format.statusBadge(p.status)}
                            </div>
                        </div>
                    </div>
                </div>
            `;
            Modal.show('viewProductModal');
        },

        edit(id) {
            const product = this.data.find(p => p.productId === id);
            if (!product) {
                Toast.error('Product not found');
                return;
            }
            this.currentId = id;
            this.showModal(product);
        },

        updateStock(id) {
            const product = this.data.find(p => p.productId === id);
            if (!product) return;

            Modal.reset('stockForm');
            document.getElementById('stockProductId').value = id;
            document.getElementById('stockProductName').textContent = product.productName;
            document.getElementById('currentStock').textContent = product.stockQuantity;
            document.getElementById('newStock').value = product.stockQuantity;

            Modal.show('stockModal');
        },

        async saveStock() {
            const id = document.getElementById('stockProductId').value;
            const data = {
                newQuantity: parseInt(document.getElementById('newStock').value) || 0,
                transactionType: document.getElementById('stockTransactionType').value,
                notes: document.getElementById('stockNotes').value
            };

            try {
                await API.put(`/products/${id}/stock`, data);
                Toast.success('Stock updated successfully');
                Modal.hide('stockModal');
                this.load();
            } catch (error) {
                Toast.error('Failed to update stock');
            }
        },

        async delete(id) {
            if (!confirm('Are you sure you want to delete this product?')) return;

            try {
                await API.delete(`/products/${id}`);
                Toast.success('Product deleted successfully');
                this.load();
            } catch (error) {
                Toast.error('Failed to delete product');
            }
        },

        async toggleStatus(id) {
            try {
                const product = this.data.find(p => p.productId === id);
                if (!product) return;
                
                const newStatus = product.status === 'Active' ? 'Inactive' : 'Active';
                await API.put(`/products/${id}/status`, { status: newStatus });
                Toast.success(`Product ${newStatus === 'Active' ? 'activated' : 'deactivated'} successfully`);
                this.load();
            } catch (error) {
                Toast.error('Failed to update product status');
            }
        }
    }
};

// ===========================================
// SALES MODULE
// ===========================================
const Sales = {
    orders: {
        data: [],
        currentId: null,

        async load() {
            try {
                this.data = await API.get('/orders');
                this.render();
            } catch (error) {
                Toast.error('Failed to load orders');
            }
        },

        render() {
            const tbody = document.getElementById('ordersTableBody');
            if (!tbody) return;

            if (this.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No orders found.</td></tr>';
                return;
            }

            tbody.innerHTML = this.data.map(o => `
                <tr>
                    <td>
                        <div class="fw-semibold">${o.orderNumber}</div>
                        <small class="text-muted">${Format.date(o.orderDate, true)}</small>
                    </td>
                    <td>${o.customerName || 'Guest'}</td>
                    <td class="text-center">${o.itemCount}</td>
                    <td class="text-end">${Format.currency(o.totalAmount)}</td>
                    <td>${Format.statusBadge(o.orderStatus)}</td>
                    <td>${Format.statusBadge(o.paymentStatus)}</td>
                    <td>${o.paymentMethod || '-'}</td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Sales.orders.view(${o.orderId})" title="View">
                                ${Icons.view}
                            </button>
                            <button class="btn btn-sm btn-outline-warning" onclick="Sales.orders.edit(${o.orderId})" title="Edit">
                                ${Icons.edit}
                            </button>
                            <button class="btn btn-sm btn-outline-${o.orderStatus === 'Cancelled' ? 'success' : 'danger'}" onclick="Sales.orders.toggleStatus(${o.orderId})" title="${o.orderStatus === 'Cancelled' ? 'Activate' : 'Cancel'}">
                                ${o.orderStatus === 'Cancelled' ? Icons.toggleOn : Icons.toggleOff}
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');

            this.updateStats();
        },

        updateStats() {
            const total = this.data.length;
            const pending = this.data.filter(o => o.orderStatus === 'Pending').length;
            const totalRevenue = this.data.filter(o => o.paymentStatus === 'Paid').reduce((sum, o) => sum + o.totalAmount, 0);

            document.getElementById('totalOrders')?.textContent && (document.getElementById('totalOrders').textContent = total);
            document.getElementById('pendingOrders')?.textContent && (document.getElementById('pendingOrders').textContent = pending);
            document.getElementById('totalSales')?.textContent && (document.getElementById('totalSales').textContent = Format.currency(totalRevenue));
        },

        view(id) {
            const o = this.data.find(order => order.orderId === id);
            if (!o) {
                Toast.error('Order not found');
                return;
            }
            
            this.currentId = id;
            let itemsHtml = '';
            if (o.items && o.items.length > 0) {
                itemsHtml = `
                    <h6 class="mt-4 mb-3">Order Items</h6>
                    <table class="table table-sm">
                        <thead><tr><th>Product</th><th class="text-center">Qty</th><th class="text-end">Price</th><th class="text-end">Total</th></tr></thead>
                        <tbody>
                            ${o.items.map(i => `<tr>
                                <td>${i.productName || '-'}</td>
                                <td class="text-center">${i.quantity}</td>
                                <td class="text-end">${Format.currency(i.unitPrice)}</td>
                                <td class="text-end">${Format.currency(i.totalPrice)}</td>
                            </tr>`).join('')}
                        </tbody>
                    </table>
                `;
            }

            document.getElementById('viewOrderContent').innerHTML = `
                <div class="row g-3">
                    <div class="col-md-6">
                        <h6 class="text-muted">Order Number</h6>
                        <p class="fw-semibold">${o.orderNumber}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Order Date</h6>
                        <p>${Format.date(o.orderDate, true)}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Customer</h6>
                        <p>${o.customerName || 'Guest'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Status</h6>
                        <p>${Format.statusBadge(o.orderStatus)} ${Format.statusBadge(o.paymentStatus)}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Payment Method</h6>
                        <p>${o.paymentMethod || '-'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Shipping</h6>
                        <p>${o.shippingMethod || '-'}</p>
                    </div>
                    ${itemsHtml}
                    <div class="col-12">
                        <hr>
                        <div class="row">
                            <div class="col-md-3">
                                <h6 class="text-muted">Subtotal</h6>
                                <p>${Format.currency(o.subtotal || 0)}</p>
                            </div>
                            <div class="col-md-3">
                                <h6 class="text-muted">Discount</h6>
                                <p class="text-danger">-${Format.currency(o.discountAmount || 0)}</p>
                            </div>
                            <div class="col-md-3">
                                <h6 class="text-muted">Tax</h6>
                                <p>${Format.currency(o.taxAmount || 0)}</p>
                            </div>
                            <div class="col-md-3">
                                <h6 class="text-muted">Total</h6>
                                <h5 class="text-teal">${Format.currency(o.totalAmount)}</h5>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            Modal.show('viewOrderModal');
        },

        edit(id) {
            const order = this.data.find(o => o.orderId === id);
            if (!order) {
                Toast.error('Order not found');
                return;
            }
            
            this.currentId = id;
            Modal.reset('orderForm');
            document.getElementById('orderId').value = order.orderId;
            document.getElementById('orderStatus').value = order.orderStatus;
            document.getElementById('paymentStatus').value = order.paymentStatus;
            document.getElementById('paymentMethod').value = order.paymentMethod || '';
            document.getElementById('shippingMethod').value = order.shippingMethod || '';
            document.getElementById('trackingNumber').value = order.trackingNumber || '';
            document.getElementById('orderNotes').value = order.notes || '';
            Modal.show('orderModal');
        },

        async save() {
            const id = document.getElementById('orderId').value;
            const data = {
                orderStatus: document.getElementById('orderStatus').value,
                paymentStatus: document.getElementById('paymentStatus').value,
                paymentMethod: document.getElementById('paymentMethod').value,
                shippingMethod: document.getElementById('shippingMethod').value,
                trackingNumber: document.getElementById('trackingNumber').value,
                notes: document.getElementById('orderNotes').value
            };

            try {
                await API.put(`/orders/${id}`, data);
                Toast.success('Order updated successfully');
                Modal.hide('orderModal');
                this.load();
            } catch (error) {
                Toast.error('Failed to update order');
            }
        },

        updateStatus(id) {
            const order = this.data.find(o => o.orderId === id);
            if (!order) return;

            document.getElementById('statusOrderId').value = id;
            document.getElementById('statusOrderNumber').textContent = order.orderNumber;
            document.getElementById('currentOrderStatus').textContent = order.orderStatus;
            document.getElementById('newOrderStatus').value = order.orderStatus;

            Modal.show('statusModal');
        },

        async saveStatus() {
            const id = document.getElementById('statusOrderId').value;
            const data = {
                status: document.getElementById('newOrderStatus').value,
                notes: document.getElementById('statusNotes').value
            };

            try {
                await API.put(`/orders/${id}/status`, data);
                Toast.success('Order status updated');
                Modal.hide('statusModal');
                this.load();
            } catch (error) {
                Toast.error('Failed to update status');
            }
        },

        async delete(id) {
            if (!confirm('Are you sure you want to delete this order?')) return;

            try {
                await API.delete(`/orders/${id}`);
                Toast.success('Order deleted successfully');
                this.load();
            } catch (error) {
                Toast.error('Failed to delete order');
            }
        },

        async toggleStatus(id) {
            try {
                const order = this.data.find(o => o.orderId === id);
                if (!order) return;
                
                const newStatus = order.orderStatus === 'Cancelled' ? 'Pending' : 'Cancelled';
                await API.put(`/orders/${id}/status`, { status: newStatus });
                Toast.success(`Order ${newStatus === 'Cancelled' ? 'cancelled' : 'reactivated'} successfully`);
                this.load();
            } catch (error) {
                Toast.error('Failed to update order status');
            }
        }
    },

    leads: {
        data: [],
        currentId: null,

        async load() {
            try {
                this.data = await API.get('/leads');
                this.render();
            } catch (error) {
                Toast.error('Failed to load leads');
            }
        },

        render() {
            const tbody = document.getElementById('leadsTableBody');
            if (!tbody) return;

            if (this.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No leads found. Click "Add Lead" to create one.</td></tr>';
                return;
            }

            tbody.innerHTML = this.data.map(l => `
                <tr>
                    <td>
                        <div class="fw-semibold">${l.firstName} ${l.lastName}</div>
                        <small class="text-muted">${l.leadCode}</small>
                    </td>
                    <td>${l.email || '-'}</td>
                    <td>${l.phone || '-'}</td>
                    <td>${l.companyName || '-'}</td>
                    <td><span class="badge bg-info">${l.source || '-'}</span></td>
                    <td>${Format.statusBadge(l.status)}</td>
                    <td>${Format.priorityBadge(l.priority)}</td>
                    <td class="text-end">${Format.currency(l.estimatedValue)}</td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Sales.leads.view(${l.leadId})" title="View">
                                ${Icons.view}
                            </button>
                            <button class="btn btn-sm btn-outline-warning" onclick="Sales.leads.edit(${l.leadId})" title="Edit">
                                ${Icons.edit}
                            </button>
                            <button class="btn btn-sm btn-outline-${l.status === 'Active' || l.status === 'Hot' || l.status === 'Warm' ? 'danger' : 'success'}" onclick="Sales.leads.toggleStatus(${l.leadId})" title="${l.status === 'Active' || l.status === 'Hot' || l.status === 'Warm' ? 'Deactivate' : 'Activate'}">
                                ${(l.status === 'Active' || l.status === 'Hot' || l.status === 'Warm') ? Icons.toggleOff : Icons.toggleOn}
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');

            this.updateStats();
        },

        updateStats() {
            const total = this.data.length;
            const hot = this.data.filter(l => l.status === 'Hot').length;
            const totalValue = this.data.reduce((sum, l) => sum + (l.estimatedValue || 0), 0);

            document.getElementById('totalLeads')?.textContent && (document.getElementById('totalLeads').textContent = total);
            document.getElementById('hotLeads')?.textContent && (document.getElementById('hotLeads').textContent = hot);
            document.getElementById('totalPipeline')?.textContent && (document.getElementById('totalPipeline').textContent = Format.currency(totalValue));
        },

        showModal(lead = null) {
            Modal.reset('leadForm');
            document.getElementById('leadModalTitle').textContent = lead ? 'Edit Lead' : 'Add New Lead';
            document.getElementById('leadId').value = lead?.leadId || '';

            if (lead) {
                // Combine firstName and lastName for display
                document.getElementById('leadName').value = (lead.firstName || '') + (lead.lastName ? ' ' + lead.lastName : '');
                document.getElementById('leadCompany').value = lead.companyName || '';
                document.getElementById('leadEmail').value = lead.email || '';
                document.getElementById('leadPhone').value = lead.phone || '';
                document.getElementById('leadSource').value = lead.source || '';
                document.getElementById('leadStatus').value = lead.status || 'New';
                document.getElementById('leadValue').value = lead.estimatedValue || '';
                document.getElementById('leadNotes').value = lead.notes || '';
            }

            Modal.show('leadModal');
        },

        async save() {
            const id = document.getElementById('leadId').value;
            
            // Split name into first and last
            const fullName = document.getElementById('leadName').value.trim();
            const nameParts = fullName.split(' ');
            const firstName = nameParts[0] || '';
            const lastName = nameParts.slice(1).join(' ') || '';

            const data = {
                firstName: firstName,
                lastName: lastName,
                email: document.getElementById('leadEmail').value,
                phone: document.getElementById('leadPhone').value,
                companyName: document.getElementById('leadCompany').value,
                source: document.getElementById('leadSource').value,
                status: document.getElementById('leadStatus').value,
                estimatedValue: parseFloat(document.getElementById('leadValue').value) || 0,
                notes: document.getElementById('leadNotes').value
            };

            try {
                if (id) {
                    await API.put(`/leads/${id}`, data);
                    Toast.success('Lead updated successfully');
                } else {
                    await API.post('/leads', data);
                    Toast.success('Lead added successfully');
                }
                Modal.hide('leadModal');
                this.load();
            } catch (error) {
                Toast.error(error.message || 'Failed to save lead');
            }
        },

        view(id) {
            const l = this.data.find(lead => lead.leadId === id);
            if (!l) {
                Toast.error('Lead not found');
                return;
            }
            
            this.currentId = id;
            document.getElementById('viewLeadContent').innerHTML = `
                <div class="row g-3">
                    <div class="col-md-6">
                        <h6 class="text-muted">Name</h6>
                        <p class="fw-semibold">${l.firstName} ${l.lastName}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Code</h6>
                        <p>${l.leadCode}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Email</h6>
                        <p>${l.email || '-'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Phone</h6>
                        <p>${l.phone || '-'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Company</h6>
                        <p>${l.companyName || '-'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Source</h6>
                        <p><span class="badge bg-info">${l.source || '-'}</span></p>
                    </div>
                    <div class="col-md-4">
                        <h6 class="text-muted">Status</h6>
                        <p>${Format.statusBadge(l.status)}</p>
                    </div>
                    <div class="col-md-4">
                        <h6 class="text-muted">Priority</h6>
                        <p>${Format.priorityBadge(l.priority)}</p>
                    </div>
                    <div class="col-md-4">
                        <h6 class="text-muted">Est. Value</h6>
                        <p class="fw-bold text-teal">${Format.currency(l.estimatedValue)}</p>
                    </div>
                    <div class="col-12">
                        <h6 class="text-muted">Notes</h6>
                        <p>${l.notes || '-'}</p>
                    </div>
                </div>
            `;
            Modal.show('viewLeadModal');
        },

        edit(id) {
            const lead = this.data.find(l => l.leadId === id);
            if (!lead) {
                Toast.error('Lead not found');
                return;
            }
            this.currentId = id;
            this.showModal(lead);
        },

        async convert(id) {
            if (!confirm('Convert this lead to a customer?')) return;

            try {
                const result = await API.put(`/leads/${id}/convert`);
                Toast.success(result.message);
                this.load();
            } catch (error) {
                Toast.error('Failed to convert lead');
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

        async toggleStatus(id) {
            try {
                const lead = this.data.find(l => l.leadId === id);
                if (!lead) return;
                
                const isActive = lead.status === 'Active' || lead.status === 'Hot' || lead.status === 'Warm';
                const newStatus = isActive ? 'Cold' : 'Active';
                await API.put(`/leads/${id}/status`, { status: newStatus });
                Toast.success(`Lead ${isActive ? 'deactivated' : 'activated'} successfully`);
                this.load();
            } catch (error) {
                Toast.error('Failed to update lead status');
            }
        }
    }
};

// ===========================================
// SUPPORT MODULE
// ===========================================
const Support = {
    tickets: {
        data: [],
        categories: [],
        currentId: null,

        async load() {
            try {
                [this.data, this.categories] = await Promise.all([
                    API.get('/tickets'),
                    API.get('/ticket-categories')
                ]);
                this.render();
            } catch (error) {
                Toast.error('Failed to load tickets');
            }
        },

        render() {
            const tbody = document.getElementById('ticketsTableBody');
            if (!tbody) return;

            if (this.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No tickets found.</td></tr>';
                return;
            }

            tbody.innerHTML = this.data.map(t => `
                <tr>
                    <td>
                        <div class="fw-semibold">${t.ticketNumber}</div>
                        <small class="text-muted">${Format.date(t.createdAt, true)}</small>
                    </td>
                    <td>${t.customerName || t.contactEmail}</td>
                    <td><span class="badge bg-info">${t.categoryName || '-'}</span></td>
                    <td>
                        <div class="text-truncate" style="max-width: 200px;">${t.subject}</div>
                    </td>
                    <td>${Format.priorityBadge(t.priority)}</td>
                    <td>${Format.statusBadge(t.status)}</td>
                    <td>${t.firstResponseAt ? Format.date(t.firstResponseAt, true) : '<span class="text-warning">Awaiting</span>'}</td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Support.tickets.view(${t.ticketId})" title="View">
                                ${Icons.view}
                            </button>
                            <button class="btn btn-sm btn-outline-warning" onclick="Support.tickets.edit(${t.ticketId})" title="Edit">
                                ${Icons.edit}
                            </button>
                            <button class="btn btn-sm btn-outline-${t.status === 'Closed' ? 'success' : 'danger'}" onclick="Support.tickets.toggleStatus(${t.ticketId})" title="${t.status === 'Closed' ? 'Reopen' : 'Close'}">
                                ${t.status === 'Closed' ? Icons.toggleOn : Icons.toggleOff}
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');

            this.updateStats();
        },

        updateStats() {
            const total = this.data.length;
            const open = this.data.filter(t => t.status === 'Open').length;
            const inProgress = this.data.filter(t => t.status === 'In Progress').length;
            const resolved = this.data.filter(t => t.status === 'Resolved' || t.status === 'Closed').length;

            document.getElementById('totalTickets')?.textContent && (document.getElementById('totalTickets').textContent = total);
            document.getElementById('openTickets')?.textContent && (document.getElementById('openTickets').textContent = open);
            document.getElementById('inProgressTickets')?.textContent && (document.getElementById('inProgressTickets').textContent = inProgress);
            document.getElementById('resolvedTickets')?.textContent && (document.getElementById('resolvedTickets').textContent = resolved);
        },

        showModal(ticket = null) {
            Modal.reset('ticketForm');
            const modalTitle = document.getElementById('modalTitle');
            if (modalTitle) modalTitle.textContent = ticket ? 'Edit Ticket' : 'Create New Ticket';
            document.getElementById('ticketId').value = ticket?.ticketId || '';

            if (ticket) {
                document.getElementById('ticketSubject').value = ticket.subject || '';
                document.getElementById('ticketDescription').value = ticket.description || '';
                document.getElementById('ticketCategory').value = ticket.categoryId || '';
                document.getElementById('ticketPriority').value = ticket.priority || 'Medium';
                document.getElementById('ticketCustomer').value = ticket.customerId || '';
                document.getElementById('ticketAssignee').value = ticket.assignedTo || '';
            }

            Modal.show('ticketModal');
        },

        async save() {
            const id = document.getElementById('ticketId').value;
            const data = {
                subject: document.getElementById('ticketSubject').value,
                description: document.getElementById('ticketDescription').value,
                customerId: parseInt(document.getElementById('ticketCustomer').value) || null,
                categoryId: document.getElementById('ticketCategory').value || null,
                priority: document.getElementById('ticketPriority').value,
                assignedTo: parseInt(document.getElementById('ticketAssignee').value) || null
            };

            try {
                if (id) {
                    await API.put(`/tickets/${id}`, data);
                    Toast.success('Ticket updated successfully');
                } else {
                    await API.post('/tickets', data);
                    Toast.success('Ticket created successfully');
                }
                Modal.hide('ticketModal');
                this.load();
            } catch (error) {
                Toast.error(error.message || 'Failed to save ticket');
            }
        },

        view(id) {
            const t = this.data.find(ticket => ticket.ticketId === id);
            if (!t) {
                Toast.error('Ticket not found');
                return;
            }
            
            this.currentId = id;
            let messagesHtml = '';
            if (t.messages && t.messages.length > 0) {
                messagesHtml = `
                    <h6 class="mt-4 mb-3">Messages</h6>
                    <div class="ticket-messages">
                        ${t.messages.map(m => `
                            <div class="message ${m.senderType === 'Staff' ? 'staff' : 'customer'} mb-3 p-3 rounded ${m.senderType === 'Staff' ? 'bg-teal-light' : 'bg-light'}">
                                <div class="d-flex justify-content-between mb-2">
                                    <strong>${m.senderName || m.senderType}</strong>
                                    <small class="text-muted">${Format.date(m.createdAt, true)}</small>
                                </div>
                                <p class="mb-0">${m.message}</p>
                            </div>
                        `).join('')}
                    </div>
                `;
            }

            document.getElementById('viewTicketContent').innerHTML = `
                <div class="row g-3">
                    <div class="col-md-6">
                        <h6 class="text-muted">Ticket Number</h6>
                        <p class="fw-semibold">${t.ticketNumber}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Created</h6>
                        <p>${Format.date(t.createdAt, true)}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Customer</h6>
                        <p>${t.customerName || t.contactName || t.contactEmail || '-'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Category</h6>
                        <p>${t.categoryName || '-'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Priority</h6>
                        <p>${Format.priorityBadge(t.priority)}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Status</h6>
                        <p>${Format.statusBadge(t.status)}</p>
                    </div>
                    <div class="col-12">
                        <h6 class="text-muted">Subject</h6>
                        <p class="fw-semibold">${t.subject}</p>
                    </div>
                    <div class="col-12">
                        <h6 class="text-muted">Description</h6>
                        <p>${t.description || '-'}</p>
                    </div>
                    ${messagesHtml}
                </div>
            `;
            Modal.show('viewTicketModal');
        },

        edit(id) {
            const ticket = this.data.find(t => t.ticketId === id);
            if (!ticket) {
                Toast.error('Ticket not found');
                return;
            }
            this.currentId = id;
            this.showModal(ticket);
        },

        reply(id) {
            const ticket = this.data.find(t => t.ticketId === id);
            if (!ticket) return;

            Modal.reset('replyForm');
            document.getElementById('replyTicketId').value = id;
            document.getElementById('replyTicketNumber').textContent = ticket.ticketNumber;

            Modal.show('replyModal');
        },

        async sendReply() {
            const id = document.getElementById('replyTicketId').value;
            const message = document.getElementById('replyMessage').value;

            if (!message.trim()) {
                Toast.error('Please enter a message');
                return;
            }

            try {
                await API.post(`/tickets/${id}/reply`, { message, senderName: 'Support Staff' });
                Toast.success('Reply sent successfully');
                Modal.hide('replyModal');
                this.load();
            } catch (error) {
                Toast.error('Failed to send reply');
            }
        },

        async delete(id) {
            if (!confirm('Are you sure you want to delete this ticket?')) return;

            try {
                await API.delete(`/tickets/${id}`);
                Toast.success('Ticket deleted successfully');
                this.load();
            } catch (error) {
                Toast.error('Failed to delete ticket');
            }
        },

        async toggleStatus(id) {
            try {
                const ticket = this.data.find(t => t.ticketId === id);
                if (!ticket) return;
                
                const newStatus = ticket.status === 'Closed' ? 'Open' : 'Closed';
                await API.put(`/tickets/${id}/status`, { status: newStatus });
                Toast.success(`Ticket ${newStatus === 'Closed' ? 'closed' : 'reopened'} successfully`);
                this.load();
            } catch (error) {
                Toast.error('Failed to update ticket status');
            }
        }
    }
};

// ===========================================
// USERS MODULE
// ===========================================
const Users = {
    data: [],
    roles: [],
    currentId: null,

    async load() {
        try {
            [this.data, this.roles] = await Promise.all([
                API.get('/users'),
                API.get('/roles')
            ]);
            this.render();
        } catch (error) {
            Toast.error('Failed to load users');
        }
    },

    render() {
        const tbody = document.getElementById('usersTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">No users found. Click "Add User" to create one.</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(u => `
            <tr>
                <td>
                    <div class="d-flex align-items-center">
                        <div class="avatar-circle me-3">${(u.firstName?.[0] || '') + (u.lastName?.[0] || '')}</div>
                        <div>
                            <div class="fw-semibold">${u.fullName || (u.firstName + ' ' + u.lastName)}</div>
                            <small class="text-muted">@${u.username}</small>
                        </div>
                    </div>
                </td>
                <td>${u.email}</td>
                <td><span class="badge bg-info">${u.roleName || 'Staff'}</span></td>
                <td>
                    <span class="badge ${u.isActive ? 'bg-success' : 'bg-secondary'}">${u.isActive ? 'Active' : 'Inactive'}</span>
                </td>
                <td>${u.lastLoginAt ? Format.date(u.lastLoginAt, true) : 'Never'}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="Users.view(${u.userId})" title="View">
                            ${Icons.view}
                        </button>
                        <button class="btn btn-sm btn-outline-warning" onclick="Users.edit(${u.userId})" title="Edit">
                            ${Icons.edit}
                        </button>
                        <button class="btn btn-sm btn-outline-${u.isActive ? 'danger' : 'success'}" onclick="Users.toggleStatus(${u.userId})" title="${u.isActive ? 'Deactivate' : 'Activate'}">
                            ${u.isActive ? Icons.toggleOff : Icons.toggleOn}
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');

        this.updateStats();
    },

    updateStats() {
        const total = this.data.length;
        const active = this.data.filter(u => u.isActive).length;

        document.getElementById('totalUsers')?.textContent && (document.getElementById('totalUsers').textContent = total);
        document.getElementById('activeUsers')?.textContent && (document.getElementById('activeUsers').textContent = active);
    },

    showModal(user = null) {
        Modal.reset('userForm');
        const modalTitle = document.getElementById('modalTitle');
        if (modalTitle) modalTitle.textContent = user ? 'Edit User' : 'Add New User';
        document.getElementById('userId').value = user?.userId || '';

        // Show/hide password fields based on edit mode
        const passwordSection = document.getElementById('passwordSection');
        const passwordField = document.getElementById('password');
        const confirmPasswordField = document.getElementById('confirmPassword');
        
        if (user) {
            // Edit mode - password optional
            if (passwordField) passwordField.removeAttribute('required');
            if (confirmPasswordField) confirmPasswordField.removeAttribute('required');
            document.getElementById('passwordLabel').textContent = 'New Password (leave blank to keep current)';
        } else {
            // Create mode - password required
            if (passwordField) passwordField.setAttribute('required', 'required');
            if (confirmPasswordField) confirmPasswordField.setAttribute('required', 'required');
            document.getElementById('passwordLabel').textContent = 'Password';
        }

        if (user) {
            document.getElementById('firstName').value = user.firstName || '';
            document.getElementById('lastName').value = user.lastName || '';
            document.getElementById('username').value = user.username || '';
            document.getElementById('email').value = user.email || '';
            document.getElementById('phone').value = user.phone || '';
            document.getElementById('roleId').value = user.roleId || '';
            document.getElementById('isActive').value = user.isActive ? 'true' : 'false';
        }

        Modal.show('userModal');
    },

    async save() {
        const id = document.getElementById('userId').value;
        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;

        // Validate password match
        if (password && password !== confirmPassword) {
            Toast.error('Passwords do not match');
            return;
        }

        // Validate required for new user
        if (!id && !password) {
            Toast.error('Password is required for new users');
            return;
        }

        const data = {
            firstName: document.getElementById('firstName').value,
            lastName: document.getElementById('lastName').value,
            username: document.getElementById('username').value,
            email: document.getElementById('email').value,
            phone: document.getElementById('phone').value,
            roleId: parseInt(document.getElementById('roleId').value) || null,
            isActive: document.getElementById('isActive').value === 'true'
        };

        // Only include password if provided
        if (password) {
            data.password = password;
        }

        try {
            if (id) {
                await API.put(`/users/${id}`, data);
                Toast.success('User updated successfully');
            } else {
                await API.post('/users', data);
                Toast.success('User added successfully');
            }
            Modal.hide('userModal');
            this.load();
        } catch (error) {
            Toast.error(error.message || 'Failed to save user');
        }
    },

    view(id) {
        const u = this.data.find(user => user.userId === id);
        if (!u) {
            Toast.error('User not found');
            return;
        }
        
        this.currentId = id;
        document.getElementById('viewUserContent').innerHTML = `
            <div class="text-center mb-4">
                <div class="avatar-circle avatar-lg mx-auto mb-3">${(u.firstName?.[0] || '') + (u.lastName?.[0] || '')}</div>
                <h4>${u.firstName} ${u.lastName}</h4>
                <p class="text-muted">@${u.username}</p>
                <span class="badge ${u.isActive ? 'bg-success' : 'bg-secondary'}">${u.isActive ? 'Active' : 'Inactive'}</span>
            </div>
            <div class="row g-3">
                <div class="col-md-6">
                    <h6 class="text-muted">Email</h6>
                    <p>${u.email}</p>
                </div>
                <div class="col-md-6">
                    <h6 class="text-muted">Phone</h6>
                    <p>${u.phone || '-'}</p>
                </div>
                <div class="col-md-6">
                    <h6 class="text-muted">Role</h6>
                    <p><span class="badge bg-info">${u.roleName || '-'}</span></p>
                </div>
                <div class="col-md-6">
                    <h6 class="text-muted">Last Login</h6>
                    <p>${u.lastLoginAt ? Format.date(u.lastLoginAt, true) : 'Never'}</p>
                </div>
            </div>
        `;
        Modal.show('viewUserModal');
    },

    edit(id) {
        const user = this.data.find(u => u.userId === id);
        if (!user) {
            Toast.error('User not found');
            return;
        }
        this.currentId = id;
        this.showModal(user);
    },

    async toggleStatus(id) {
        try {
            const result = await API.put(`/users/${id}/toggle-status`);
            Toast.success(result.message);
            this.load();
        } catch (error) {
            Toast.error('Failed to toggle user status');
        }
    },

    async delete(id) {
        if (!confirm('Are you sure you want to delete this user?')) return;

        try {
            await API.delete(`/users/${id}`);
            Toast.success('User deleted successfully');
            this.load();
        } catch (error) {
            Toast.error('Failed to delete user');
        }
    }
};

// ===========================================
// BILLING MODULE
// ===========================================
const Billing = {
    invoices: {
        data: [],
        currentId: null,

        async load() {
            try {
                this.data = await API.get('/invoices');
                this.render();
            } catch (error) {
                Toast.error('Failed to load invoices');
            }
        },

        render() {
            const tbody = document.getElementById('invoicesTableBody');
            if (!tbody) return;

            if (this.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No invoices found.</td></tr>';
                return;
            }

            tbody.innerHTML = this.data.map(i => `
                <tr>
                    <td>
                        <div class="fw-semibold">${i.invoiceNumber}</div>
                        <small class="text-muted">${Format.date(i.invoiceDate)}</small>
                    </td>
                    <td>${i.customerName || '-'}</td>
                    <td>${Format.date(i.dueDate)}</td>
                    <td class="text-end">${Format.currency(i.totalAmount)}</td>
                    <td class="text-end text-success">${Format.currency(i.paidAmount)}</td>
                    <td class="text-end ${i.balance > 0 ? 'text-danger' : ''}">${Format.currency(i.balance)}</td>
                    <td>${Format.statusBadge(i.status)}</td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Billing.invoices.view(${i.invoiceId})" title="View">
                                ${Icons.view}
                            </button>
                            <button class="btn btn-sm btn-outline-warning" onclick="Billing.invoices.edit(${i.invoiceId})" title="Edit">
                                ${Icons.edit}
                            </button>
                            <button class="btn btn-sm btn-outline-${(i.status === 'Cancelled' || i.status === 'Void') ? 'success' : 'danger'}" onclick="Billing.invoices.toggleStatus(${i.invoiceId})" title="${(i.status === 'Cancelled' || i.status === 'Void') ? 'Activate' : 'Void'}">
                                ${(i.status === 'Cancelled' || i.status === 'Void') ? Icons.toggleOn : Icons.toggleOff}
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');

            this.updateStats();
        },

        updateStats() {
            const total = this.data.reduce((sum, i) => sum + i.totalAmount, 0);
            const paid = this.data.reduce((sum, i) => sum + i.paidAmount, 0);
            const outstanding = this.data.reduce((sum, i) => sum + i.balance, 0);
            const overdue = this.data.filter(i => i.status === 'Overdue' || (i.balance > 0 && new Date(i.dueDate) < new Date())).length;

            document.getElementById('totalBilled')?.textContent && (document.getElementById('totalBilled').textContent = Format.currency(total));
            document.getElementById('totalPaid')?.textContent && (document.getElementById('totalPaid').textContent = Format.currency(paid));
            document.getElementById('totalOutstanding')?.textContent && (document.getElementById('totalOutstanding').textContent = Format.currency(outstanding));
            document.getElementById('overdueCount')?.textContent && (document.getElementById('overdueCount').textContent = overdue);
        },

        showModal(invoice = null) {
            Modal.reset('invoiceForm');
            const modalTitle = document.getElementById('modalTitle');
            if (modalTitle) modalTitle.textContent = invoice ? 'Edit Invoice' : 'Create New Invoice';
            document.getElementById('invoiceId').value = invoice?.invoiceId || '';

            // Set default dates
            const today = new Date().toISOString().split('T')[0];
            const dueDate = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
            document.getElementById('invoiceDate').value = invoice?.invoiceDate?.split('T')[0] || today;
            document.getElementById('dueDate').value = invoice?.dueDate?.split('T')[0] || dueDate;

            if (invoice) {
                document.getElementById('invoiceCustomer').value = invoice.customerId || '';
                document.getElementById('billingAddress').value = invoice.billingAddress || '';
                document.getElementById('paymentTerms').value = invoice.paymentTerms || 'NET30';
                document.getElementById('invoiceNotes').value = invoice.notes || '';
            }

            Modal.show('invoiceModal');
        },

        async save() {
            const id = document.getElementById('invoiceId').value;
            
            // Collect line items
            const items = [];
            const rows = document.querySelectorAll('#invoiceItemsBody tr');
            rows.forEach(row => {
                const inputs = row.querySelectorAll('input');
                const description = inputs[0]?.value?.trim();
                const unitPrice = parseFloat(inputs[1]?.value) || 0;
                const quantity = parseInt(inputs[2]?.value) || 0;
                
                if (description && quantity > 0) {
                    items.push({ description, unitPrice, quantity, totalPrice: unitPrice * quantity });
                }
            });

            if (items.length === 0) {
                Toast.error('Please add at least one line item');
                return;
            }

            const subtotal = items.reduce((sum, item) => sum + item.totalPrice, 0);
            const taxAmount = subtotal * 0.12;
            const totalAmount = subtotal + taxAmount;

            const data = {
                customerId: parseInt(document.getElementById('invoiceCustomer').value) || null,
                invoiceDate: document.getElementById('invoiceDate').value,
                dueDate: document.getElementById('dueDate').value,
                paymentTerms: document.getElementById('paymentTerms').value,
                billingAddress: document.getElementById('billingAddress').value,
                notes: document.getElementById('invoiceNotes').value,
                subtotal: subtotal,
                taxAmount: taxAmount,
                totalAmount: totalAmount,
                status: 'Pending',
                items: items
            };

            try {
                if (id) {
                    await API.put(`/invoices/${id}`, data);
                    Toast.success('Invoice updated successfully');
                } else {
                    await API.post('/invoices', data);
                    Toast.success('Invoice created successfully');
                }
                Modal.hide('invoiceModal');
                this.load();
            } catch (error) {
                Toast.error(error.message || 'Failed to save invoice');
            }
        },

        view(id) {
            const i = this.data.find(inv => inv.invoiceId === id);
            if (!i) {
                Toast.error('Invoice not found');
                return;
            }
            
            this.currentId = id;
            let itemsHtml = '';
            if (i.items && i.items.length > 0) {
                itemsHtml = `
                    <h6 class="mt-4 mb-3">Invoice Items</h6>
                    <table class="table table-sm">
                        <thead><tr><th>Description</th><th class="text-center">Qty</th><th class="text-end">Price</th><th class="text-end">Total</th></tr></thead>
                        <tbody>
                            ${i.items.map(item => `<tr>
                                <td>${item.description}</td>
                                <td class="text-center">${item.quantity}</td>
                                <td class="text-end">${Format.currency(item.unitPrice)}</td>
                                <td class="text-end">${Format.currency(item.totalPrice)}</td>
                            </tr>`).join('')}
                        </tbody>
                    </table>
                `;
            }

            document.getElementById('viewInvoiceContent').innerHTML = `
                <div class="row g-3">
                    <div class="col-md-6">
                        <h6 class="text-muted">Invoice Number</h6>
                        <p class="fw-semibold">${i.invoiceNumber}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Status</h6>
                        <p>${Format.statusBadge(i.status)}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Customer</h6>
                        <p>${i.customerName || '-'}</p>
                    </div>
                    <div class="col-md-6">
                        <h6 class="text-muted">Due Date</h6>
                        <p>${Format.date(i.dueDate)}</p>
                    </div>
                    ${itemsHtml}
                    <div class="col-12">
                        <hr>
                        <div class="row text-end">
                            <div class="col-md-3">
                                <h6 class="text-muted">Subtotal</h6>
                                <p>${Format.currency(i.subtotal || 0)}</p>
                            </div>
                            <div class="col-md-3">
                                <h6 class="text-muted">Tax</h6>
                                <p>${Format.currency(i.taxAmount || 0)}</p>
                            </div>
                            <div class="col-md-3">
                                <h6 class="text-muted">Total</h6>
                                <h5 class="text-teal">${Format.currency(i.totalAmount)}</h5>
                            </div>
                            <div class="col-md-3">
                                <h6 class="text-muted">Balance</h6>
                                <h5 class="${i.balance > 0 ? 'text-danger' : 'text-success'}">${Format.currency(i.balance)}</h5>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            Modal.show('viewInvoiceModal');
        },

        recordPayment(id) {
            const invoice = this.data.find(i => i.invoiceId === id);
            if (!invoice) return;

            Modal.reset('paymentForm');
            document.getElementById('paymentInvoiceId').value = id;
            document.getElementById('paymentInvoiceNumber').textContent = invoice.invoiceNumber;
            document.getElementById('paymentBalance').textContent = Format.currency(invoice.balance);
            document.getElementById('paymentAmount').value = invoice.balance;
            document.getElementById('paymentAmount').max = invoice.balance;

            Modal.show('paymentModal');
        },

        async savePayment() {
            const invoiceId = document.getElementById('paymentInvoiceId').value;
            const data = {
                invoiceId: parseInt(invoiceId),
                amount: parseFloat(document.getElementById('paymentAmount').value) || 0,
                paymentMethod: document.getElementById('paymentMethod').value,
                transactionReference: document.getElementById('paymentReference').value,
                notes: document.getElementById('paymentNotes').value,
                status: 'Completed'
            };

            if (data.amount <= 0) {
                Toast.error('Please enter a valid amount');
                return;
            }

            try {
                await API.post('/payments', data);
                Toast.success('Payment recorded successfully');
                Modal.hide('paymentModal');
                this.load();
            } catch (error) {
                Toast.error('Failed to record payment');
            }
        },

        print(id) {
            window.open(`/Billing/Print/${id}`, '_blank');
        },

        async delete(id) {
            if (!confirm('Are you sure you want to delete this invoice?')) return;

            try {
                await API.delete(`/invoices/${id}`);
                Toast.success('Invoice deleted successfully');
                this.load();
            } catch (error) {
                Toast.error('Failed to delete invoice');
            }
        },

        edit(id) {
            const invoice = this.data.find(i => i.invoiceId === id);
            if (!invoice) {
                Toast.error('Invoice not found');
                return;
            }
            this.currentId = id;
            this.showModal(invoice);
        },

        async toggleStatus(id) {
            try {
                const invoice = this.data.find(i => i.invoiceId === id);
                if (!invoice) return;
                
                const isVoid = invoice.status === 'Cancelled' || invoice.status === 'Void';
                const newStatus = isVoid ? 'Unpaid' : 'Void';
                await API.put(`/invoices/${id}/status`, { status: newStatus });
                Toast.success(`Invoice ${isVoid ? 'reactivated' : 'voided'} successfully`);
                this.load();
            } catch (error) {
                Toast.error('Failed to update invoice status');
            }
        }
    },

    payments: {
        data: [],

        async load() {
            try {
                this.data = await API.get('/payments');
                this.render();
            } catch (error) {
                Toast.error('Failed to load payments');
            }
        },

        render() {
            const tbody = document.getElementById('paymentsTableBody');
            if (!tbody) return;

            if (this.data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No payments found.</td></tr>';
                return;
            }

            tbody.innerHTML = this.data.map(p => `
                <tr>
                    <td>
                        <div class="fw-semibold">${p.paymentNumber}</div>
                        <small class="text-muted">${Format.date(p.paymentDate, true)}</small>
                    </td>
                    <td>${p.customerName || '-'}</td>
                    <td>${p.invoiceNumber || '-'}</td>
                    <td class="text-end fw-semibold text-success">${Format.currency(p.amount)}</td>
                    <td><span class="badge bg-info">${p.paymentMethod}</span></td>
                    <td>${Format.statusBadge(p.status)}</td>
                    <td>${p.transactionReference || '-'}</td>
                </tr>
            `).join('');
        }
    }
};

// ===========================================
// CUSTOMER PORTAL (for fetching promotions)
// ===========================================
const CustomerPortal = {
    async loadPromotions() {
        try {
            const promotions = await API.get('/promotions/active');
            const container = document.getElementById('promotionsContainer');
            if (!container) return;

            if (promotions.length === 0) {
                container.innerHTML = '<div class="col-12 text-center py-5 text-muted">No active promotions at the moment.</div>';
                return;
            }

            container.innerHTML = promotions.map(p => `
                <div class="col-md-6 col-lg-4 mb-4">
                    <div class="card h-100 promotion-card">
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-start mb-3">
                                <span class="badge bg-danger">${p.discountType === 'Percentage' ? Format.percentage(p.discountValue) + ' OFF' : Format.currency(p.discountValue) + ' OFF'}</span>
                                <small class="text-muted">Valid until ${Format.date(p.endDate)}</small>
                            </div>
                            <h5 class="card-title">${p.promotionName}</h5>
                            <p class="card-text text-muted">${p.description || ''}</p>
                            <div class="promo-code mt-3">
                                <small class="text-muted">Use code:</small>
                                <div class="code-box bg-light p-2 rounded mt-1 d-flex justify-content-between align-items-center">
                                    <code class="fw-bold">${p.promotionCode}</code>
                                    <button class="btn btn-sm btn-outline-teal" onclick="CustomerPortal.copyCode('${p.promotionCode}')">
                                        <i class="bi bi-clipboard"></i>
                                    </button>
                                </div>
                            </div>
                            ${p.minOrderAmount > 0 ? `<small class="text-muted mt-2 d-block">Min. order: ${Format.currency(p.minOrderAmount)}</small>` : ''}
                        </div>
                    </div>
                </div>
            `).join('');
        } catch (error) {
            console.error('Failed to load promotions:', error);
        }
    },

    copyCode(code) {
        navigator.clipboard.writeText(code).then(() => {
            Toast.success('Promo code copied to clipboard!');
        }).catch(() => {
            Toast.error('Failed to copy code');
        });
    }
};

// ===========================================
// INITIALIZATION
// ===========================================
document.addEventListener('DOMContentLoaded', () => {
    // Global modal backdrop cleanup - ensures backdrops are removed when modals are hidden
    document.addEventListener('hidden.bs.modal', function (event) {
        // Immediate cleanup
        Modal.cleanup();
    });
    
    // Also listen for hide event as backup
    document.addEventListener('hide.bs.modal', function (event) {
        // Schedule cleanup
        setTimeout(() => Modal.cleanup(), 350);
    });
    
    // Emergency escape: click on backdrop to remove it if stuck
    document.addEventListener('click', function(event) {
        if (event.target.classList.contains('modal-backdrop')) {
            Modal.cleanup();
        }
    });
    
    // Periodic cleanup check - removes orphaned backdrops
    setInterval(() => {
        const openModals = document.querySelectorAll('.modal.show');
        const backdrops = document.querySelectorAll('.modal-backdrop');
        if (openModals.length === 0 && backdrops.length > 0) {
            Modal.cleanup();
        }
    }, 1000);

    // Add custom styles
    const style = document.createElement('style');
    style.textContent = `
        .text-teal { color: #008080 !important; }
        .bg-teal { background-color: #008080 !important; }
        .bg-teal-light { background-color: rgba(0, 128, 128, 0.1) !important; }
        .btn-teal { background-color: #008080; border-color: #008080; color: white; }
        .btn-teal:hover { background-color: #006666; border-color: #006666; color: white; }
        .btn-outline-teal { color: #008080; border-color: #008080; }
        .btn-outline-teal:hover { background-color: #008080; color: white; }
        .spinner-border.text-teal { color: #008080 !important; }
        
        .avatar-circle {
            width: 40px;
            height: 40px;
            border-radius: 50%;
            background: linear-gradient(135deg, #008080, #00a0a0);
            color: white;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: 600;
            font-size: 14px;
        }
        .avatar-circle.avatar-lg {
            width: 80px;
            height: 80px;
            font-size: 24px;
        }
        
        .product-thumb {
            width: 50px;
            height: 50px;
            object-fit: cover;
            border-radius: 8px;
            background: #f8f9fa;
        }
        
        .promotion-card {
            border: none;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            transition: transform 0.2s, box-shadow 0.2s;
        }
        .promotion-card:hover {
            transform: translateY(-4px);
            box-shadow: 0 4px 16px rgba(0,0,0,0.15);
        }
        
        .ticket-messages .message.staff {
            border-left: 3px solid #008080;
        }
        .ticket-messages .message.customer {
            border-left: 3px solid #ff6b35;
        }
        
        .table th {
            font-weight: 600;
            font-size: 0.875rem;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            color: #6c757d;
        }
        
        .progress {
            border-radius: 10px;
        }
        .progress-bar.bg-teal {
            background-color: #008080 !important;
        }
        
        .code-box code {
            font-size: 1rem;
            color: #008080;
        }
    `;
    document.head.appendChild(style);
});

// Export for global access
window.Toast = Toast;
window.API = API;
window.Format = Format;
window.Modal = Modal;
window.Icons = Icons;
window.Marketing = Marketing;
window.Customers = Customers;
window.Inventory = Inventory;
window.Sales = Sales;
window.Support = Support;
window.Users = Users;
window.Billing = Billing;
window.CustomerPortal = CustomerPortal;
window.Billing = Billing;
window.CustomerPortal = CustomerPortal;
