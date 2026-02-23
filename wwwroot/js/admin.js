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
            'Paid/Confirmed': 'primary', 'paid/confirmed': 'primary',
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
    },

    // Show a dynamic modal with custom title and HTML body
    showDynamic(title, bodyHtml) {
        let dynamicModal = document.getElementById('dynamicViewModal');
        if (!dynamicModal) {
            dynamicModal = document.createElement('div');
            dynamicModal.id = 'dynamicViewModal';
            dynamicModal.className = 'modal fade';
            dynamicModal.tabIndex = -1;
            dynamicModal.innerHTML = `
                <div class="modal-dialog modal-lg modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content">
                        <div class="modal-header" style="background: linear-gradient(135deg, #008080, #006666); color: white;">
                            <h5 class="modal-title" id="dynamicModalTitle"></h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body" id="dynamicModalBody"></div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        </div>
                    </div>
                </div>`;
            document.body.appendChild(dynamicModal);
        }
        document.getElementById('dynamicModalTitle').textContent = title;
        document.getElementById('dynamicModalBody').innerHTML = bodyHtml;
        this.show('dynamicViewModal');
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
        allData: [],
        currentId: null,

        async load() {
            try {
                this.allData = await API.get('/campaigns');
                this.data = (this.allData || []).filter(c => (c.status || '').toLowerCase() !== 'paused');
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
            const source = Array.isArray(this.allData) ? this.allData : this.data;
            const total = source.length;
            const active = source.filter(c => c.status === 'Active').length;
            const totalBudget = source.reduce((sum, c) => sum + (c.budget || 0), 0);
            const avgRoi = source.length > 0 ? source.reduce((sum, c) => sum + (c.roi || 0), 0) / source.length : 0;

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
        },

        filter(search, status) {
            let filtered = this.data;
            
            if (search) {
                filtered = filtered.filter(c => 
                    (c.campaignName || '').toLowerCase().includes(search) ||
                    (c.campaignCode || '').toLowerCase().includes(search)
                );
            }
            
            if (status) {
                filtered = filtered.filter(c => c.status === status);
            }
            
            this.renderFiltered(filtered);
        },

        renderFiltered(data) {
            const tbody = document.getElementById('campaignsTableBody');
            if (!tbody) return;

            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No campaigns match your search criteria.</td></tr>';
                document.getElementById('totalCampaigns')?.textContent && (document.getElementById('totalCampaigns').textContent = '0');
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

            document.getElementById('totalCampaigns')?.textContent && (document.getElementById('totalCampaigns').textContent = data.length);
        }
    },

    // Promotions
    promotions: {
        data: [],
        allData: [],
        currentId: null,

        async load() {
            try {
                const promotions = await API.get('/promotions');
                this.allData = Array.isArray(promotions) ? promotions : [];
                this.data = this.allData.filter(p => p.isActive !== false);
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
            const source = Array.isArray(this.allData) && this.allData.length > 0 ? this.allData : this.data;
            const total = source.length;
            const active = source.filter(p => p.isActive).length;
            const deployed = source.filter(p => p.isShowInCustomer).length;
            const totalUsage = source.reduce((sum, p) => sum + (p.timesUsed || 0), 0);

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
        },

        filter(search, status) {
            let filtered = this.data;
            
            if (search) {
                filtered = filtered.filter(p => 
                    (p.promotionName || '').toLowerCase().includes(search) ||
                    (p.promotionCode || '').toLowerCase().includes(search)
                );
            }
            
            if (status === 'Active') {
                filtered = filtered.filter(p => p.isActive === true);
            } else if (status === 'Inactive') {
                filtered = filtered.filter(p => p.isActive === false);
            }
            
            this.renderFiltered(filtered);
        },

        renderFiltered(data) {
            const tbody = document.getElementById('promotionsTableBody');
            if (!tbody) return;

            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No promotions match your search criteria.</td></tr>';
                document.getElementById('promotionCount')?.textContent && (document.getElementById('promotionCount').textContent = '0 promotions');
                return;
            }

            tbody.innerHTML = data.map(p => `
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

            document.getElementById('promotionCount')?.textContent && (document.getElementById('promotionCount').textContent = data.length + ' promotions');
        }
    }
};

// ===========================================
// CUSTOMERS MODULE
// ===========================================
const Customers = {
    data: [],
    allData: [],
    categories: [],
    currentId: null,

    async load() {
        try {
            const [customers, categories] = await Promise.all([
                API.get('/customers'),
                API.get('/customer-categories')
            ]);
            this.allData = Array.isArray(customers) ? customers : [];
            this.data = this.allData.filter(c => (c.status || '').toLowerCase() === 'active');
            this.categories = categories;
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
        const source = Array.isArray(this.allData) ? this.allData : this.data;
        const total = source.length;
        const active = source.filter(c => c.status === 'Active').length;
        const totalSpent = source.reduce((sum, c) => sum + (c.totalSpent || 0), 0);

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
            await API.put(`/customers/${id}/toggle-status`, {});
            Toast.success(`Customer ${newStatus === 'Active' ? 'activated' : 'deactivated'} successfully`);
            this.load();
        } catch (error) {
            Toast.error('Failed to update customer status');
        }
    },

    filter(search, type, status) {
        let filtered = this.data;
        
        if (search) {
            filtered = filtered.filter(c => 
                (c.fullName || '').toLowerCase().includes(search) ||
                (c.firstName || '').toLowerCase().includes(search) ||
                (c.lastName || '').toLowerCase().includes(search) ||
                (c.email || '').toLowerCase().includes(search) ||
                (c.customerCode || '').toLowerCase().includes(search) ||
                (c.phone || '').toLowerCase().includes(search) ||
                (c.companyName || '').toLowerCase().includes(search)
            );
        }
        
        if (type) {
            filtered = filtered.filter(c => c.categoryName === type);
        }
        
        if (status === 'active') {
            filtered = filtered.filter(c => c.status === 'Active');
        }
        
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('customersTableBody');
        if (!tbody) return;

        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center py-4 text-muted">No customers match your search criteria.</td></tr>';
            document.getElementById('customerCount').textContent = '0 customers';
            return;
        }

        tbody.innerHTML = data.map(c => `
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

        document.getElementById('customerCount').textContent = data.length + ' customers';
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
    }
};

// ===========================================
// INVENTORY MODULE
// ===========================================
const Inventory = {
    products: {
        data: [],
        allData: [],
        categories: [],
        brands: [],
        currentId: null,
        currentImageUrl: '',

        async load() {
            try {
                const [products, categories, brands] = await Promise.all([
                    API.get('/products'),
                    API.get('/product-categories'),
                    API.get('/brands')
                ]);
                this.allData = Array.isArray(products) ? products : [];
                this.data = this.allData.filter(p => (p.status || '').toLowerCase() === 'active');
                this.categories = categories;
                this.brands = brands;
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
                            ${p.mainImageUrl ? 
                                `<img src="${p.mainImageUrl}" class="product-thumb me-3" alt="${p.productName}" onerror="this.onerror=null; this.src='/images/placeholder.png'; this.classList.add('product-image-placeholder');">` :
                                `<div class="product-image-placeholder me-3">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/>
                                    </svg>
                                </div>`
                            }
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
            const source = Array.isArray(this.allData) && this.allData.length > 0 ? this.allData : this.data;
            const total = source.length;
            const active = source.filter(p => (p.status || '').toLowerCase() === 'active');
            const inStock = active.filter(p => p.stockQuantity > p.reorderLevel).length;
            const lowStock = active.filter(p => p.stockQuantity > 0 && p.stockQuantity <= p.reorderLevel).length;
            const outOfStock = active.filter(p => p.stockQuantity <= 0).length;

            document.getElementById('totalProducts')?.textContent && (document.getElementById('totalProducts').textContent = active.length);
            document.getElementById('inStockProducts')?.textContent && (document.getElementById('inStockProducts').textContent = inStock);
            document.getElementById('lowStockProducts')?.textContent && (document.getElementById('lowStockProducts').textContent = lowStock);
            document.getElementById('outOfStockProducts')?.textContent && (document.getElementById('outOfStockProducts').textContent = outOfStock);
        },

        showModal(product = null) {
            Modal.reset('productForm');
            const modalTitle = document.getElementById('modalTitle');
            if (modalTitle) modalTitle.textContent = product ? 'Edit Product' : 'Add New Product';
            document.getElementById('productId').value = product?.productId || '';

            // Reset image upload area
            const imagePreviewArea = document.getElementById('imagePreviewArea');
            const productImageUrl = document.getElementById('productImageUrl');
            
            if (product) {
                document.getElementById('productName').value = product.productName || '';
                document.getElementById('productCode').value = product.sku || product.productCode || '';
                document.getElementById('description').value = product.shortDescription || '';
                document.getElementById('categoryId').value = product.categoryId || '';
                document.getElementById('brandId').value = product.brandId || '';
                if (document.getElementById('supplierId')) {
                    document.getElementById('supplierId').value = product.supplierId || '';
                }
                document.getElementById('costPrice').value = product.costPrice || '';
                document.getElementById('sellingPrice').value = product.sellingPrice || '';
                document.getElementById('stockQuantity').value = product.stockQuantity || 0;
                document.getElementById('reorderLevel').value = product.reorderLevel || 10;
                document.getElementById('isActive').value = product.isActive ? 'true' : 'false';

                // Show existing image if available
                if (product.mainImageUrl && imagePreviewArea) {
                    productImageUrl.value = product.mainImageUrl;
                    this.currentImageUrl = product.mainImageUrl;
                    imagePreviewArea.innerHTML = `
                        <div class="image-preview-container">
                            <img src="${product.mainImageUrl}" class="image-preview" alt="Product Image">
                            <button type="button" class="image-preview-remove" onclick="removeImage(event)">×</button>
                        </div>
                        <p class="mt-2 mb-0 text-muted small">Click to change image</p>
                    `;
                } else if (imagePreviewArea) {
                    productImageUrl.value = '';
                    this.currentImageUrl = '';
                    imagePreviewArea.innerHTML = `
                        <svg class="image-upload-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/>
                        </svg>
                        <p class="mb-1 text-muted">Click to upload or drag and drop</p>
                        <small class="text-muted">PNG, JPG, GIF up to 5MB</small>
                    `;
                }
            } else if (imagePreviewArea) {
                productImageUrl.value = '';
                this.currentImageUrl = '';
                imagePreviewArea.innerHTML = `
                    <svg class="image-upload-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/>
                    </svg>
                    <p class="mb-1 text-muted">Click to upload or drag and drop</p>
                    <small class="text-muted">PNG, JPG, GIF up to 5MB</small>
                `;
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
                supplierId: parseInt(document.getElementById('supplierId')?.value) || null,
                costPrice: parseFloat(document.getElementById('costPrice').value) || 0,
                sellingPrice: parseFloat(document.getElementById('sellingPrice').value) || 0,
                stockQuantity: parseInt(document.getElementById('stockQuantity').value) || 0,
                reorderLevel: parseInt(document.getElementById('reorderLevel').value) || 10,
                isActive: document.getElementById('isActive').value === 'true',
                mainImageUrl: document.getElementById('productImageUrl')?.value || this.currentImageUrl || ''
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
        },

        filter(search, category, brand, stock) {
            let filtered = this.data;
            
            if (search) {
                filtered = filtered.filter(p => 
                    (p.productName || '').toLowerCase().includes(search) ||
                    (p.sku || '').toLowerCase().includes(search) ||
                    (p.productCode || '').toLowerCase().includes(search)
                );
            }
            
            if (category) {
                filtered = filtered.filter(p => p.categoryId == category || (p.categoryName || '').toLowerCase() === category.toLowerCase());
            }
            
            if (brand) {
                filtered = filtered.filter(p => p.brandId == brand || (p.brandName || '').toLowerCase() === brand.toLowerCase());
            }
            
            if (stock) {
                if (stock === 'instock') {
                    filtered = filtered.filter(p => p.stockQuantity > p.reorderLevel);
                } else if (stock === 'low') {
                    filtered = filtered.filter(p => p.stockQuantity > 0 && p.stockQuantity <= p.reorderLevel);
                } else if (stock === 'outofstock') {
                    filtered = filtered.filter(p => p.stockQuantity <= 0);
                }
            }
            
            this.renderFiltered(filtered);
        },

        renderFiltered(data) {
            const tbody = document.getElementById('productsTableBody');
            if (!tbody) return;

            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4 text-muted">No products match your search criteria.</td></tr>';
                document.getElementById('totalProducts')?.textContent && (document.getElementById('totalProducts').textContent = '0');
                return;
            }

            tbody.innerHTML = data.map(p => `
                <tr>
                    <td>
                        <div class="d-flex align-items-center">
                            ${p.mainImageUrl ? 
                                `<img src="${p.mainImageUrl}" class="product-thumb me-3" alt="${p.productName}" onerror="this.onerror=null; this.src='/images/placeholder.png'; this.classList.add('product-image-placeholder');">` :
                                `<div class="product-image-placeholder me-3">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                        <rect x="3" y="3" width="18" height="18" rx="2" ry="2"/><circle cx="8.5" cy="8.5" r="1.5"/><polyline points="21 15 16 10 5 21"/>
                                    </svg>
                                </div>`
                            }
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

            document.getElementById('totalProducts')?.textContent && (document.getElementById('totalProducts').textContent = data.length);
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
                tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4 text-muted">No orders found.</td></tr>';
                this.updateStats();
                return;
            }

            tbody.innerHTML = this.data.map(o => {
                let actions = `<button class="btn btn-sm btn-outline-primary" onclick="Sales.orders.view(${o.orderId})" title="View">${Icons.view}</button>`;

                if (o.orderStatus === 'Pending') {
                    actions += `
                        <button class="btn btn-sm btn-success" onclick="Sales.orders.approve(${o.orderId})" title="Approve">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>
                        </button>
                        <button class="btn btn-sm btn-danger" onclick="Sales.orders.reject(${o.orderId})" title="Reject">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                        </button>`;
                } else if (o.orderStatus !== 'Cancelled' && o.orderStatus !== 'Completed') {
                    actions += `
                        <button class="btn btn-sm btn-outline-info" onclick="Sales.orders.updateStatus(${o.orderId})" title="Update Status">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/></svg>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="Sales.orders.updateStatusDirect(${o.orderId}, 'Cancelled')" title="Cancel">
                            ${Icons.toggleOff}
                        </button>`;
                } else if (o.orderStatus === 'Cancelled') {
                    actions += `<button class="btn btn-sm btn-outline-secondary" disabled title="Cancelled">${Icons.toggleOff}</button>`;
                }

                return `<tr>
                    <td>
                        <div class="fw-semibold">${o.orderNumber}</div>
                        <small class="text-muted">${Format.date(o.orderDate, true)}</small>
                    </td>
                    <td>${o.customerName || 'Guest'}</td>
                    <td>${Format.date(o.orderDate)}</td>
                    <td class="text-center">${o.itemCount}</td>
                    <td class="text-end">${Format.currency(o.totalAmount)}</td>
                    <td class="text-center">${Format.statusBadge(o.orderStatus)}</td>
                    <td class="text-center">${Format.statusBadge(o.paymentStatus)}</td>
                    <td class="text-center">${(o.paymentMethod || '-').toString().toUpperCase()}</td>
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
            const completed = this.data.filter(o => o.orderStatus === 'Completed').length;

            const el = (id, val) => { const e = document.getElementById(id); if (e) e.textContent = val; };
            el('totalOrders', total);
            el('pendingOrders', pending);
            el('processingOrders', processing);
            el('completedOrders', completed);
            el('orderCount', total + ' orders');
            el('orderPaginationInfo', `Showing 1 to ${total} of ${total} orders`);
        },

        view(id) {
            const o = this.data.find(order => order.orderId === id);
            if (!o) { Toast.error('Order not found'); return; }

            this.currentId = id;
            
            // Set modal title
            const titleEl = document.getElementById('viewOrderNumber');
            if (titleEl) titleEl.textContent = o.orderNumber;

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
                    </div>
                    ${o.notes ? `<div class="mt-3"><h6>Notes</h6><p class="text-muted mb-0">${o.notes}</p></div>` : ''}
                `;
            }

            Modal.show('viewOrderModal');
        },

        async approve(id) {
            if (!confirm('Approve this order? This will confirm the order, generate an invoice, and create a PayMongo payment link.')) return;
            try {
                const result = await API.put('/orders/' + id + '/approve');
                Toast.success(result.message || 'Order approved successfully!');
                await this.load();
                if (result.checkoutUrl) {
                    this.showPaymentLinkModal(result.checkoutUrl, result.orderNumber);
                }
            } catch (error) {
                Toast.error(error.message || 'Failed to approve order');
            }
        },

        async reject(id) {
            const o = this.data.find(order => order.orderId === id);
            if (!o) return;
            this.currentId = id;
            
            const el = document.getElementById('rejectOrderNumber');
            if(el) el.textContent = o.orderNumber;
            
            const reason = document.getElementById('rejectReason');
            if(reason) reason.value = '';
            
            Modal.show('rejectOrderModal');
        },

        async confirmReject() {
            if (!this.currentId) return;
            const reasons = document.getElementById('rejectReason');
            const note = reasons ? reasons.value : 'Rejected by admin';
            
            try {
                await API.put('/orders/' + this.currentId + '/reject', { status: 'Cancelled', notes: note });
                Toast.success('Order rejected');
                Modal.hide('rejectOrderModal');
                this.load();
            } catch (error) {
                Toast.error(error.message || 'Failed to reject order');
            }
        },

        async updateStatusDirect(id, status) {
            if (!confirm(`Set status to ${status}?`)) return;
            try {
                await API.put('/orders/' + id + '/status', { status: status, notes: `Status updated to ${status} by Admin` });
                Toast.success('Status updated');
                this.load();
            } catch (error) {
                Toast.error('Failed to update status');
            }
        },
        
        updateStatus(id) {
            const o = this.data.find(order => order.orderId === id);
            if (!o) return;
            this.currentId = id;
            
            const el = document.getElementById('statusOrderNumber');
            if(el) el.textContent = o.orderNumber;
            
            const badge = document.getElementById('currentStatus');
            if(badge) badge.innerHTML = Format.statusBadge(o.orderStatus);
            
            const select = document.getElementById('newStatus');
            if(select) select.value = o.orderStatus;
            
            const notes = document.getElementById('statusNotes');
            if(notes) notes.value = '';
            
            Modal.show('updateStatusModal');
        },

        async confirmStatusUpdate() {
            if (!this.currentId) return;
            try {
                const statusEl = document.getElementById('newStatus');
                const notesEl = document.getElementById('statusNotes');
                
                await API.put('/orders/' + this.currentId + '/status', { 
                    status: statusEl ? statusEl.value : 'Processing', 
                    notes: notesEl ? notesEl.value : '' 
                });
                Toast.success('Status updated');
                Modal.hide('updateStatusModal');
                this.load();
            } catch (error) {
                Toast.error('Failed to update status');
            }
        },

        showPaymentLinkModal(url, orderNumber) {
            const el = document.getElementById('paymentLinkOrderNumber');
            if(el) el.textContent = orderNumber || '';
            const input = document.getElementById('paymentLinkUrl');
            if(input) input.value = url;
            const anchor = document.getElementById('paymentLinkAnchor');
            if(anchor) anchor.href = url;
            Modal.show('paymentLinkModal');
        },

        print() {
            const id = this.currentId;
            if (!id) return;
            const o = this.data.find(ord => ord.orderId === id);
            if (!o) return;
            const items = o.items || [];
            
            const html = `<html><head><title>Order ${o.orderNumber}</title>
                <style>body{font-family:'Segoe UI',Arial,sans-serif;margin:40px;color:#333}.header{display:flex;justify-content:space-between;border-bottom:3px solid #008080;padding-bottom:20px;margin-bottom:30px}.company-logo img{height:60px;width:auto;}.badge{background:#008080;color:white;padding:8px 20px;border-radius:5px;font-size:18px}table{width:100%;border-collapse:collapse;margin-bottom:20px}th{background:#008080;color:white;padding:10px;text-align:left}td{padding:10px;border-bottom:1px solid #eee}.total-row td{font-weight:bold;font-size:18px;color:#008080;border-top:2px solid #008080}</style></head><body>
                <div class="header">
                    <div class="company-logo">
                        <img src="${window.location.origin}/images/compugearlogo.png" alt="CompuGear" onerror="this.onerror=null;this.src='${window.location.origin}/images/compugearlogo.png';">
                        <div style="font-size: 1.2rem; font-weight: bold; color: #008080; margin-top: 5px;">CompuGear</div>
                    </div>
                    <div><span class="badge">${o.orderNumber}</span></div>
                </div>
                <div style="margin-bottom: 20px;">
                    <strong>Customer:</strong> ${o.customerName || 'Guest'}<br>
                    <strong>Date:</strong> ${Format.date(o.orderDate, true)}<br>
                    <strong>Payment:</strong> ${o.paymentMethod || '-'} (${o.paymentStatus})
                </div>
                <table><thead><tr><th>Product</th><th>Qty</th><th>Price</th><th>Total</th></tr></thead>
                <tbody>${items.map(i => `<tr><td>${i.productName}</td><td>${i.quantity}</td><td>${Format.currency(i.unitPrice)}</td><td>${Format.currency(i.totalPrice)}</td></tr>`).join('')}</tbody></table>
                <p style="text-align:right;font-size:1.2em"><strong>Total: ${Format.currency(o.totalAmount)}</strong></p>
                <script>window.onload=function(){window.print();window.close();}<\/script></body></html>`;
                
            var printWindow = window.open('', '_blank');
            printWindow.document.write(html);
            printWindow.document.close();
        },

        filter(search, status) {
            let filtered = this.data;

            if (search) {
                filtered = filtered.filter(o =>
                    (o.orderNumber || '').toLowerCase().includes(search) ||
                    (o.customerName || '').toLowerCase().includes(search)
                );
            }

            if (status) {
                filtered = filtered.filter(o => o.orderStatus === status);
            }

            this.renderFiltered(filtered);
        },

        renderFiltered(data) {
            const tbody = document.getElementById('ordersTableBody');
            if (!tbody) return;

            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4 text-muted">No orders match your criteria.</td></tr>';
                document.getElementById('orderCount').textContent = '0 orders';
                return;
            }

            tbody.innerHTML = data.map(o => {
                let actions = `<button class="btn btn-sm btn-outline-primary" onclick="Sales.orders.view(${o.orderId})" title="View">${Icons.view}</button>`;

                if (o.orderStatus === 'Pending') {
                    actions += `
                        <button class="btn btn-sm btn-success" onclick="Sales.orders.approve(${o.orderId})" title="Approve">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>
                        </button>
                        <button class="btn btn-sm btn-danger" onclick="Sales.orders.reject(${o.orderId})" title="Reject">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                        </button>`;
                } else if (o.orderStatus !== 'Cancelled' && o.orderStatus !== 'Completed') {
                    actions += `
                        <button class="btn btn-sm btn-outline-info" onclick="Sales.orders.updateStatus(${o.orderId})" title="Update Status">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="23 4 23 10 17 10"/><path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"/></svg>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="Sales.orders.updateStatusDirect(${o.orderId}, 'Cancelled')" title="Cancel">
                            ${Icons.toggleOff}
                        </button>`;
                } else if (o.orderStatus === 'Cancelled') {
                    actions += `<button class="btn btn-sm btn-outline-secondary" disabled>${Icons.toggleOff}</button>`;
                }

                return `<tr>
                    <td>
                        <div class="fw-semibold">${o.orderNumber}</div>
                        <small class="text-muted">${Format.date(o.orderDate, true)}</small>
                    </td>
                    <td>${o.customerName || 'Guest'}</td>
                    <td>${Format.date(o.orderDate)}</td>
                    <td class="text-center">${o.itemCount}</td>
                    <td class="text-end">${Format.currency(o.totalAmount)}</td>
                    <td class="text-center">${Format.statusBadge(o.orderStatus)}</td>
                    <td class="text-center">${Format.statusBadge(o.paymentStatus)}</td>
                    <td class="text-center">${(o.paymentMethod || '-').toString().toUpperCase()}</td>
                    <td class="text-center">
                        <div class="btn-group">${actions}</div>
                    </td>
                </tr>`;
            }).join('');

            document.getElementById('orderCount').textContent = data.length + ' orders';
        }
    },

    leads: {
        data: [],
        allData: [],
        currentId: null,

        async load() {
            try {
                this.allData = await API.get('/leads');
                this.data = (this.allData || []).filter(l => (l.status || '').toLowerCase() !== 'inactive');
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
            const source = Array.isArray(this.allData) ? this.allData : this.data;
            const total = source.length;
            const hot = source.filter(l => l.status === 'Hot').length;
            const totalValue = source.reduce((sum, l) => sum + (l.estimatedValue || 0), 0);

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
                await API.put(`/leads/${id}/toggle-status`, {});
                Toast.success(`Lead ${isActive ? 'deactivated' : 'activated'} successfully`);
                this.load();
            } catch (error) {
                Toast.error('Failed to update lead status');
            }
        }
    },

    reports: {
        data: [],
        period: 'month',
        chart: null,

        normalizeStatus(status) {
            return (status || '').toString().trim().toLowerCase();
        },

        // Revenue reflects only after approval (Confirmed) and beyond.
        isRevenueStatus(status) {
            const s = this.normalizeStatus(status);
            return s === 'confirmed' || s === 'processing' || s === 'completed' || s === 'delivered' || s === 'shipped';
        },

        async load() {
            const setText = (id, txt) => { const el = document.getElementById(id); if(el) el.textContent = txt; };
            setText('revenueInfo', 'Loading data...');
            
            console.log('Loading sales report...');
            try {
                // Set a timeout for the API call to prevent infinite hanging
                const controller = new AbortController();
                const timeoutId = setTimeout(() => controller.abort(), 10000); // 10s timeout
                
                // We can't easily add signal to API.get without modifying it, so we'll just race it
                // or assume API.get works. For now, let's just proceed.
                // Actually, let's just use the existing API.get but handle the UI state better.
                
                const result = await API.get('/orders');
                console.log('Sales report data loaded:', result);
                this.data = Array.isArray(result) ? result : (result.data || []);
                
                if (!Array.isArray(this.data)) {
                    console.warn('API returned non-array data, defaulting to empty array.');
                    this.data = [];
                }
                
                this.update();
            } catch (error) {
                console.error('Failed to load sales report:', error);
                Toast.error('Failed to load sales report');
                this.data = [];
                this.update();
            }
        },

        setPeriod(period) {
            this.period = period;
            
            // Update UI buttons
            const btns = document.querySelectorAll('#adminPeriodFilter .btn');
            btns.forEach(b => b.classList.remove('active'));
            
            const activeBtn = document.querySelector(`#adminPeriodFilter [data-period="${period}"]`);
            if (activeBtn) activeBtn.classList.add('active');
            
            const labels = { day: 'Today', week: 'This Week', month: 'This Month', year: 'This Year', all: 'All Time' };
            const labelEl = document.getElementById('adminPeriodLabel');
            if (labelEl) labelEl.textContent = labels[period] || period;
            
            this.update();
        },

        filterByPeriod(orders, period) {
            if (!Array.isArray(orders)) return [];
            
            const now = new Date();
            const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
            let d;

            switch (period) {
                case 'day':
                    return orders.filter(o => o.orderDate && new Date(o.orderDate) >= startOfToday);
                case 'week':
                    d = new Date(startOfToday);
                    d.setDate(d.getDate() - d.getDay()); // Start of week (Sunday)
                    return orders.filter(o => o.orderDate && new Date(o.orderDate) >= d);
                case 'month':
                    d = new Date(now.getFullYear(), now.getMonth(), 1);
                    return orders.filter(o => o.orderDate && new Date(o.orderDate) >= d);
                case 'year':
                    d = new Date(now.getFullYear(), 0, 1);
                    return orders.filter(o => o.orderDate && new Date(o.orderDate) >= d);
                default: 
                    return orders;
            }
        },

        update() {
            const filtered = this.filterByPeriod(this.data, this.period);
            const revenueOrders = filtered.filter(o => this.isRevenueStatus(o.orderStatus));
            const revenue = revenueOrders.reduce((sum, o) => sum + (o.totalAmount || 0), 0);
            
            const completed = filtered.filter(o => o.orderStatus === 'Completed' || o.orderStatus === 'Delivered').length;
            const avg = revenueOrders.length > 0 ? revenue / revenueOrders.length : 0;

            const setText = (id, txt) => { const el = document.getElementById(id); if(el) el.textContent = txt; };
            
            setText('periodRevenue', Format.currency(revenue));
            setText('periodOrders', filtered.length);
            setText('periodCompleted', completed);
            setText('avgOrderValue', Format.currency(avg));
            setText('revenueInfo', `${revenueOrders.length} confirmed+ orders`);
            setText('ordersInfo', `${completed} completed`);
            
            const completionRate = filtered.length > 0 ? (completed / filtered.length * 100) : 0;
            setText('completedInfo', `${completionRate.toFixed(0)}% completion rate`);

            // Update chart - always use full dataset for monthly trends
            this.renderChart(this.data);
        },

        renderChart(orders) {
            try {
                const ctx = document.getElementById('salesChart');
                if (!ctx) return;
                
                // Safety check for Chart.js
                if (typeof Chart === 'undefined') {
                    console.error('Chart.js library not loaded');
                    ctx.parentElement.innerHTML = '<div class="alert alert-warning">Chart library not loaded</div>';
                    return;
                }
                
                // Monthly data aggregation for current year
                const monthlyData = new Array(12).fill(0);
                const currentYear = new Date().getFullYear();
                
                if (Array.isArray(orders)) {
                    orders.forEach(o => {
                        if (!o.orderDate) return;
                        if (!this.isRevenueStatus(o.orderStatus)) return;
                        const d = new Date(o.orderDate);
                        if (!isNaN(d.getTime()) && d.getFullYear() === currentYear) {
                            monthlyData[d.getMonth()] += (parseFloat(o.totalAmount) || 0);
                        }
                    });
                }
    
                if (this.chart) {
                    this.chart.destroy();
                }
    
                this.chart = new Chart(ctx.getContext('2d'), {
                    type: 'bar',
                    data: {
                        labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
                        datasets: [{
                            label: 'Sales',
                            data: monthlyData,
                            backgroundColor: '#008080',
                            borderRadius: 4
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: { legend: { display: false } },
                        scales: { 
                            y: { 
                                beginAtZero: true, 
                                ticks: { 
                                    callback: function(v) { return '₱' + (v / 1000).toFixed(0) + 'k'; } 
                                } 
                            } 
                        }
                    }
                });
            } catch (err) {
                console.error('Error rendering chart:', err);
            }
        },

        generatePdf() {
            const labels = { day: 'Today', week: 'This Week', month: 'This Month', year: 'This Year', all: 'All Time' };
            const periodLabel = labels[this.period] || 'All Time';
            const filtered = this.filterByPeriod(this.data, this.period);
            const revenueOrders = filtered.filter(o => this.isRevenueStatus(o.orderStatus));
            const totalRevenue = revenueOrders.reduce((s, o) => s + (o.totalAmount || 0), 0);
            const avgOrder = revenueOrders.length > 0 ? totalRevenue / revenueOrders.length : 0;
            const completedOrders = filtered.filter(o => o.orderStatus === 'Completed' || o.orderStatus === 'Delivered').length;
            const now = new Date();

            let rowsHtml = '';
            filtered.forEach(o => {
                let statusClass = 'bg-info';
                if (o.orderStatus === 'Completed' || o.orderStatus === 'Delivered') statusClass = 'bg-success';
                else if (o.orderStatus === 'Pending') statusClass = 'bg-warning';
                else if (o.orderStatus === 'Cancelled') statusClass = 'bg-danger';
                
                let payClass = 'bg-danger';
                if (o.paymentStatus === 'Paid') payClass = 'bg-success';
                else if (o.paymentStatus === 'Pending') payClass = 'bg-warning';

                rowsHtml += `<tr>
                    <td><strong>${o.orderNumber || ''}</strong></td>
                    <td>${o.customerName || 'Guest'}</td>
                    <td>${new Date(o.orderDate).toLocaleDateString('en-PH')}</td>
                    <td class="text-center">${o.itemCount || 0}</td>
                    <td class="text-end">${Format.currency(o.totalAmount)}</td>
                    <td class="text-center"><span class="badge ${statusClass}">${o.orderStatus || ''}</span></td>
                    <td class="text-center"><span class="badge ${payClass}">${o.paymentStatus || 'Pending'}</span></td>
                </tr>`;
            });

            const html = `<html><head><title>CompuGear Sales Report - ${periodLabel}</title>
                <style>
                *{margin:0;padding:0;box-sizing:border-box}
                body{font-family:"Segoe UI",Arial,sans-serif;margin:0;padding:40px;color:#333;font-size:12px}
                .header{display:flex;justify-content:space-between;align-items:center;border-bottom:3px solid #008080;padding-bottom:20px;margin-bottom:30px}
                .company h1{color:#008080;font-size:28px;margin-bottom:5px}
                .company p{color:#666}
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
                </style></head><body>
                <div class="header">
                <div class="company"><h1>CompuGear</h1><p>Computer & Gear Solutions</p></div>
                <div class="report-title"><h2>Sales Report</h2><p>Period: ${periodLabel}</p><p>Generated: ${now.toLocaleDateString('en-PH', { year: 'numeric', month: 'long', day: 'numeric' })}</p></div>
                </div>
                <div class="summary-cards">
                <div class="summary-card"><div class="value">${filtered.length}</div><div class="label">Total Orders</div></div>
                <div class="summary-card"><div class="value">${Format.currency(totalRevenue)}</div><div class="label">Total Revenue</div></div>
                <div class="summary-card"><div class="value">${Format.currency(avgOrder)}</div><div class="label">Avg Order Value</div></div>
                <div class="summary-card"><div class="value">${completedOrders}</div><div class="label">Completed Orders</div></div>
                </div>
                <h3 style="color:#008080;margin-bottom:15px">Order Details</h3>
                <table><thead><tr><th>Order #</th><th>Customer</th><th>Date</th><th class="text-center">Items</th><th class="text-end">Amount</th><th class="text-center">Status</th><th class="text-center">Payment</th></tr></thead>
                <tbody>${rowsHtml}
                <tr class="total-row"><td colspan="4" class="text-end"><strong>Grand Total:</strong></td><td class="text-end"><strong>${Format.currency(totalRevenue)}</strong></td><td colspan="2"></td></tr>
                </tbody></table>
                <div class="footer"><p>CompuGear Sales Report - Generated on ${now.toLocaleString('en-PH')} - Confidential</p></div>
                <script>window.onload=function(){window.print();}</script></body></html>`;

            const printWindow = window.open('', '_blank');
            printWindow.document.write(html);
            printWindow.document.close();
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
        },

        filter(search, status, priority) {
            let filtered = this.data;
            
            if (search) {
                filtered = filtered.filter(t => 
                    (t.ticketNumber || '').toLowerCase().includes(search) ||
                    (t.subject || '').toLowerCase().includes(search) ||
                    (t.customerName || '').toLowerCase().includes(search) ||
                    (t.contactEmail || '').toLowerCase().includes(search)
                );
            }
            
            if (status) {
                filtered = filtered.filter(t => t.status === status);
            }
            
            if (priority) {
                filtered = filtered.filter(t => t.priority === priority);
            }
            
            this.renderFiltered(filtered);
        },

        renderFiltered(data) {
            const tbody = document.getElementById('ticketsTableBody');
            if (!tbody) return;

            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No tickets match your search criteria.</td></tr>';
                document.getElementById('totalTickets')?.textContent && (document.getElementById('totalTickets').textContent = '0');
                return;
            }

            tbody.innerHTML = data.map(t => `
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

            document.getElementById('totalTickets')?.textContent && (document.getElementById('totalTickets').textContent = data.length);
        }
    }
};

// ===========================================
// USERS MODULE
// ===========================================
const Users = {
    data: [],
    allData: [],
    roles: [],
    currentId: null,

    async load() {
        try {
            const [users, roles] = await Promise.all([
                API.get('/users'),
                API.get('/roles')
            ]);
            this.allData = Array.isArray(users) ? users : [];
            this.data = this.allData.filter(u => u.isActive);
            this.roles = roles;
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
        const source = Array.isArray(this.allData) ? this.allData : this.data;
        const total = source.length;
        const active = source.filter(u => u.isActive).length;
        const inactive = total - active;
        const admins = source.filter(u => u.roleId === 1 || u.roleId === 2).length;

        const setEl = (id, val) => { const el = document.getElementById(id); if (el) el.textContent = val; };
        setEl('totalUsers', total);
        setEl('activeUsers', active);
        setEl('inactiveUsers', inactive);
        setEl('adminUsers', admins);
        setEl('userCount', total + ' users');
        setEl('paginationInfo', `Showing 1 to ${total} of ${total} entries`);
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
    },

    filter(search, role, status) {
        let filtered = this.data;
        
        if (search) {
            filtered = filtered.filter(u => 
                (u.fullName || '').toLowerCase().includes(search) ||
                (u.firstName || '').toLowerCase().includes(search) ||
                (u.lastName || '').toLowerCase().includes(search) ||
                (u.email || '').toLowerCase().includes(search) ||
                (u.username || '').toLowerCase().includes(search)
            );
        }
        
        if (role) {
            filtered = filtered.filter(u => u.roleId == role);
        }
        
        if (status === 'active') {
            filtered = filtered.filter(u => u.isActive);
        }
        
        this.renderFiltered(filtered);
    },

    renderFiltered(data) {
        const tbody = document.getElementById('usersTableBody');
        if (!tbody) return;

        if (data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">No users match your search criteria.</td></tr>';
            document.getElementById('userCount').textContent = '0 users';
            return;
        }

        tbody.innerHTML = data.map(u => `
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

        document.getElementById('userCount').textContent = data.length + ' users';
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
                const result = await API.get('/invoices');
                this.data = result.data || result;
                if (!Array.isArray(this.data)) this.data = [];
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
                    <td class="text-end ${i.balanceDue > 0 ? 'text-danger' : ''}">${Format.currency(i.balanceDue)}</td>
                    <td>${Format.statusBadge(i.status)}</td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Billing.invoices.view(${i.invoiceId})" title="View">
                                ${Icons.view}
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
            const outstanding = this.data.reduce((sum, i) => sum + (i.balanceDue || 0), 0);
            const overdue = this.data.filter(i => i.status === 'Overdue' || ((i.balanceDue || 0) > 0 && new Date(i.dueDate) < new Date())).length;

            const el = (id, val) => { const e = document.getElementById(id); if (e) e.textContent = val; };
            el('outstandingAmount', Format.currency(outstanding));
            el('paidAmount', Format.currency(paid));
            el('overdueAmount', Format.currency(this.data.filter(i => i.status === 'Overdue' || ((i.balanceDue || 0) > 0 && new Date(i.dueDate) < new Date())).reduce((s, i) => s + (i.balanceDue || 0), 0)));
            el('totalInvoices', this.data.length);
            el('invoiceCount', this.data.length + ' invoices');
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

        async saveAsDraft() {
            // Temporarily set status before save
            const invoiceId = document.getElementById('invoiceId').value;
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

            const subtotal = items.reduce((sum, item) => sum + item.totalPrice, 0);
            const taxAmount = subtotal * 0.12;
            const totalAmount = subtotal + taxAmount;

            const data = {
                customerId: parseInt(document.getElementById('invoiceCustomer').value) || null,
                invoiceDate: document.getElementById('invoiceDate').value,
                dueDate: document.getElementById('dueDate').value,
                paymentTerms: document.getElementById('paymentTerms')?.value || 'NET30',
                billingAddress: document.getElementById('billingAddress')?.value || '',
                notes: document.getElementById('invoiceNotes')?.value || '',
                subtotal, taxAmount, totalAmount,
                status: 'Draft',
                items: items
            };

            try {
                if (invoiceId) {
                    await API.put(`/invoices/${invoiceId}`, data);
                } else {
                    await API.post('/invoices', data);
                }
                Toast.success('Invoice saved as draft');
                Modal.hide('invoiceModal');
                this.load();
            } catch (error) {
                Toast.error(error.message || 'Failed to save draft');
            }
        },

        edit(id) {
            const invoice = this.data.find(i => i.invoiceId === id);
            if (!invoice) return;
            this.showModal(invoice);
        },

        async toggleStatus(id) {
            const invoice = this.data.find(i => i.invoiceId === id);
            if (!invoice) return;
            const newStatus = (invoice.status === 'Cancelled' || invoice.status === 'Void') ? 'Pending' : 'Void';
            try {
                await API.put(`/invoices/${id}/status`, { status: newStatus });
                Toast.success(`Invoice ${newStatus === 'Void' ? 'voided' : 'reactivated'}`);
                this.load();
            } catch (error) {
                Toast.error('Failed to update invoice status');
            }
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

        async view(id) {
            const i = this.data.find(inv => inv.invoiceId === id);
            if (!i) {
                Toast.error('Invoice not found');
                return;
            }
            
            this.currentId = id;
            if (typeof currentInvoiceId !== 'undefined') currentInvoiceId = id;

            // Populate modal fields
            const el = (elId, val) => { const e = document.getElementById(elId); if (e) e.textContent = val; };
            el('viewInvoiceNumber', '#' + i.invoiceNumber);
            el('viewCustomerName', i.customerName || '-');
            el('viewInvoiceDate', Format.date(i.invoiceDate));
            el('viewDueDate', Format.date(i.dueDate));
            
            const statusEl = document.getElementById('viewInvoiceStatus');
            if (statusEl) statusEl.innerHTML = Format.statusBadge(i.status);

            // Try to fetch full invoice data with items
            try {
                const fullResult = await API.get(`/invoices/${id}/pdf`);
                const full = fullResult.data || fullResult;
                // Render items
                const itemsTbody = document.getElementById('viewInvoiceItems');
                if (itemsTbody && full.items && full.items.length > 0) {
                    itemsTbody.innerHTML = full.items.map(item => `<tr>
                        <td>${item.description || item.productName || '-'}</td>
                        <td>${Format.currency(item.unitPrice)}</td>
                        <td>${item.quantity}</td>
                        <td class="text-end">${Format.currency(item.totalPrice || item.unitPrice * item.quantity)}</td>
                    </tr>`).join('');
                }

                el('viewCustomerAddress', full.billingAddress || '-');
                el('viewCustomerEmail', full.customerEmail || '-');
                el('viewCustomerPhone', full.customerPhone || '-');
                el('viewOrderNumber', full.orderNumber || '-');
                el('viewOrderDate', Format.date(full.orderDate));

                const orderStatusEl = document.getElementById('viewOrderStatus');
                if (orderStatusEl) orderStatusEl.innerHTML = full.orderStatus ? Format.statusBadge(full.orderStatus) : '-';

                const paymentStatusEl = document.getElementById('viewOrderPaymentStatus');
                if (paymentStatusEl) paymentStatusEl.innerHTML = full.orderPaymentStatus ? Format.statusBadge(full.orderPaymentStatus) : '-';

                el('viewOrderPaymentMethod', full.orderPaymentMethod || '-');
                el('viewOrderShippingMethod', full.orderShippingMethod || '-');
                el('viewOrderTrackingNumber', full.orderTrackingNumber || '-');
                el('viewOrderConfirmedAt', Format.date(full.orderConfirmedAt, true));

                // Render payment history
                const payHistEl = document.getElementById('paymentHistory');
                if (payHistEl && full.payments && full.payments.length > 0) {
                    payHistEl.innerHTML = `<table class="table table-sm mb-0">
                        <thead><tr><th>Date</th><th>Method</th><th>Reference</th><th class="text-end">Amount</th></tr></thead>
                        <tbody>${full.payments.map(p => `<tr>
                            <td>${Format.date(p.paymentDate)}</td>
                            <td>${p.paymentMethodType || '-'}</td>
                            <td>${p.referenceNumber || '-'}</td>
                            <td class="text-end">${Format.currency(p.amount)}</td>
                        </tr>`).join('')}</tbody>
                    </table>`;
                } else if (payHistEl) {
                    payHistEl.innerHTML = '<span class="text-muted">No payments recorded yet.</span>';
                }
            } catch (e) {
                console.warn('Could not fetch full invoice data:', e);
            }

            el('viewSubtotal', Format.currency(i.subtotal || (i.totalAmount / 1.12)));
            el('viewVAT', Format.currency(i.taxAmount || (i.totalAmount - i.totalAmount / 1.12)));
            el('viewTotal', Format.currency(i.totalAmount));

            Modal.show('viewInvoiceModal');
        },

        recordPayment(id) {
            const invoice = this.data.find(i => i.invoiceId === id);
            if (!invoice) return;

            this.currentId = id;
            if (typeof currentInvoiceId !== 'undefined') currentInvoiceId = id;

            const el = document.getElementById('paymentInvoiceNumber');
            if (el) el.textContent = '#' + invoice.invoiceNumber;
            const outEl = document.getElementById('outstandingAmount');
            // Don't overwrite the stat card - use the form text element
            const formText = document.querySelector('#paymentModal .form-text span');
            if (formText) formText.textContent = Format.currency(invoice.balanceDue);
            
            const amtInput = document.getElementById('paymentAmount');
            if (amtInput) { amtInput.value = invoice.balanceDue; amtInput.max = invoice.balanceDue; }
            const dateInput = document.getElementById('paymentDate');
            if (dateInput) dateInput.value = new Date().toISOString().split('T')[0];
            const methodSelect = document.getElementById('paymentMethod');
            if (methodSelect) methodSelect.value = '';
            const refInput = document.getElementById('paymentReference');
            if (refInput) refInput.value = '';
            const notesInput = document.getElementById('paymentNotes');
            if (notesInput) notesInput.value = '';

            Modal.show('paymentModal');
        },

        async confirmPayment() {
            const id = this.currentId;
            if (!id) return;
            
            const data = {
                invoiceId: id,
                amount: parseFloat(document.getElementById('paymentAmount').value) || 0,
                paymentMethodType: document.getElementById('paymentMethod').value,
                referenceNumber: document.getElementById('paymentReference').value,
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

        async savePayment() { await this.confirmPayment(); },

        async print(id) {
            try {
                const invResult = await API.get(`/invoices/${id}/pdf`);
                const inv = invResult.data || invResult;
                const items = inv.items || [];
                const payments = inv.payments || [];
                const subtotal = inv.subtotal || (inv.totalAmount / 1.12);
                const tax = inv.taxAmount || (inv.totalAmount - subtotal);

                const printWindow = window.open('', '_blank');
                printWindow.document.write(`
                    <html><head><title>Invoice ${inv.invoiceNumber}</title>
                    <style>
                        body { font-family: 'Segoe UI', Arial, sans-serif; margin: 40px; color: #333; }
                        .header { display: flex; justify-content: space-between; margin-bottom: 30px; border-bottom: 3px solid #008080; padding-bottom: 20px; }
                        .company h1 { color: #008080; margin: 0; font-size: 28px; }
                        .company p { margin: 2px 0; color: #666; }
                        .inv-badge { background: #008080; color: white; padding: 8px 20px; border-radius: 5px; font-size: 18px; }
                        .details { display: flex; justify-content: space-between; margin-bottom: 30px; }
                        .detail-block h3 { color: #008080; font-size: 14px; margin-bottom: 5px; }
                        .detail-block p { margin: 2px 0; }
                        table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
                        th { background: #008080; color: white; padding: 10px; text-align: left; }
                        td { padding: 10px; border-bottom: 1px solid #eee; }
                        .totals { text-align: right; }
                        .totals td { border: none; }
                        .totals .total-row td { font-weight: bold; font-size: 18px; color: #008080; border-top: 2px solid #008080; }
                        .payments { margin-top: 20px; }
                        .payments th { background: #6c757d; }
                        @media print { body { margin: 20px; } }
                    </style></head><body>
                    <div class="header">
                        <div class="company">
                            <h1>CompuGear</h1>
                            <p>Computer & Gear Solutions</p>
                        </div>
                        <div><span class="inv-badge">${inv.invoiceNumber}</span></div>
                    </div>
                    <div class="details">
                        <div class="detail-block">
                            <h3>BILL TO</h3>
                            <p><strong>${inv.customerName || '-'}</strong></p>
                            <p>${inv.billingAddress || ''}</p>
                            <p>${inv.customerEmail || ''}</p>
                        </div>
                        <div class="detail-block" style="text-align:right">
                            <h3>INVOICE DETAILS</h3>
                            <p>Date: ${Format.date(inv.invoiceDate)}</p>
                            <p>Due: ${Format.date(inv.dueDate)}</p>
                            <p>Status: ${inv.status}</p>
                        </div>
                    </div>
                    <table>
                        <thead><tr><th>Description</th><th style="text-align:center">Qty</th><th style="text-align:right">Unit Price</th><th style="text-align:right">Amount</th></tr></thead>
                        <tbody>
                            ${items.map(item => `<tr>
                                <td>${item.description || item.productName || '-'}</td>
                                <td style="text-align:center">${item.quantity}</td>
                                <td style="text-align:right">₱${(item.unitPrice || 0).toLocaleString('en-PH', {minimumFractionDigits: 2})}</td>
                                <td style="text-align:right">₱${((item.totalPrice || item.unitPrice * item.quantity) || 0).toLocaleString('en-PH', {minimumFractionDigits: 2})}</td>
                            </tr>`).join('')}
                        </tbody>
                    </table>
                    <table class="totals"><tbody>
                        <tr><td></td><td>Subtotal:</td><td>₱${subtotal.toLocaleString('en-PH', {minimumFractionDigits: 2})}</td></tr>
                        <tr><td></td><td>VAT (12%):</td><td>₱${tax.toLocaleString('en-PH', {minimumFractionDigits: 2})}</td></tr>
                        <tr class="total-row"><td></td><td>Total:</td><td>₱${inv.totalAmount.toLocaleString('en-PH', {minimumFractionDigits: 2})}</td></tr>
                    </tbody></table>
                    ${payments.length > 0 ? `
                    <div class="payments">
                        <h3 style="color:#008080">Payment History</h3>
                        <table>
                            <thead><tr><th>Date</th><th>Method</th><th>Reference</th><th style="text-align:right">Amount</th></tr></thead>
                            <tbody>${payments.map(p => `<tr>
                                <td>${Format.date(p.paymentDate)}</td>
                                <td>${p.paymentMethodType || '-'}</td>
                                <td>${p.referenceNumber || '-'}</td>
                                <td style="text-align:right">₱${(p.amount || 0).toLocaleString('en-PH', {minimumFractionDigits: 2})}</td>
                            </tr>`).join('')}</tbody>
                        </table>
                    </div>` : ''}
                    <script>window.onload = function() { window.print(); }<\/script>
                    </body></html>
                `);
                printWindow.document.close();
            } catch (error) {
                Toast.error('Failed to load invoice for printing');
            }
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
        },

        filter(search, status) {
            let filtered = this.data;
            
            if (search) {
                filtered = filtered.filter(i => 
                    (i.invoiceNumber || '').toLowerCase().includes(search) ||
                    (i.customerName || '').toLowerCase().includes(search)
                );
            }
            
            if (status) {
                filtered = filtered.filter(i => i.status === status);
            }
            
            this.renderFiltered(filtered);
        },

        renderFiltered(data) {
            const tbody = document.getElementById('invoicesTableBody');
            if (!tbody) return;

            if (data.length === 0) {
                tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4 text-muted">No invoices match your search criteria.</td></tr>';
                return;
            }

            tbody.innerHTML = data.map(i => `
                <tr>
                    <td>
                        <div class="fw-semibold">${i.invoiceNumber}</div>
                        <small class="text-muted">${Format.date(i.invoiceDate)}</small>
                    </td>
                    <td>${i.customerName || '-'}</td>
                    <td>${Format.date(i.dueDate)}</td>
                    <td class="text-end">${Format.currency(i.totalAmount)}</td>
                    <td class="text-end text-success">${Format.currency(i.paidAmount)}</td>
                    <td class="text-end ${i.balanceDue > 0 ? 'text-danger' : ''}">${Format.currency(i.balanceDue)}</td>
                    <td>${Format.statusBadge(i.status)}</td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Billing.invoices.view(${i.invoiceId})" title="View">
                                ${Icons.view}
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');
        }
    },

    payments: {
        data: [],

        async load() {
            try {
                const result = await API.get('/payments');
                this.data = result.data || result;
                if (!Array.isArray(this.data)) this.data = [];
                this.render();
            } catch (error) {
                Toast.error('Failed to load payments');
            }
        },

        render(data) {
            const list = data || this.data;
            const tbody = document.getElementById('paymentsTableBody');
            if (!tbody) return;

            const countEl = document.getElementById('paymentCount');
            if (countEl) countEl.textContent = list.length + ' payments';

            if (list.length === 0) {
                tbody.innerHTML = '<tr><td colspan="10" class="text-center py-4 text-muted">No payments found.</td></tr>';
                return;
            }

            tbody.innerHTML = list.map(p => {
                const subtotal = p.invoiceSubtotal || 0;
                const taxAmount = p.invoiceTaxAmount || 0;
                const taxLabel = taxAmount > 0 ? `VAT (12%)` : '-';
                return `
                <tr>
                    <td>
                        <div class="fw-semibold">${p.paymentNumber || p.transactionId || '-'}</div>
                        <small class="text-muted">${Format.date(p.paymentDate, true)}</small>
                    </td>
                    <td>${p.customerName || '-'}</td>
                    <td>${p.invoiceNumber || p.orderNumber || '-'}</td>
                    <td>${Format.date(p.paymentDate)}</td>
                    <td><span class="badge bg-info">${p.paymentMethodType || p.paymentMethod || '-'}</span></td>
                    <td class="text-end">${subtotal > 0 ? Format.currency(subtotal) : '-'}</td>
                    <td class="text-end">${taxAmount > 0 ? `<span class="text-warning">${Format.currency(taxAmount)}</span>` : '-'}</td>
                    <td class="text-end fw-semibold text-success">${Format.currency(p.amount)}</td>
                    <td>${Format.statusBadge(p.status)}</td>
                    <td class="text-center">
                        <div class="btn-group">
                            <button class="btn btn-sm btn-outline-primary" onclick="Billing.payments.view(${p.paymentId})" title="View Details">
                                ${Icons.view}
                            </button>
                        </div>
                    </td>
                </tr>
            `}).join('');
        },

        filter() {
            const search = (document.getElementById('searchPayments')?.value || '').toLowerCase();
            if (!search) {
                this.render();
                return;
            }
            const filtered = this.data.filter(p =>
                (p.paymentNumber || '').toLowerCase().includes(search) ||
                (p.customerName || '').toLowerCase().includes(search) ||
                (p.invoiceNumber || '').toLowerCase().includes(search) ||
                (p.transactionId || '').toLowerCase().includes(search) ||
                (p.paymentMethodType || '').toLowerCase().includes(search)
            );
            this.render(filtered);
        },

        async view(id) {
            try {
                const result = await API.get(`/payments/${id}`);
                const p = result.data || result;
                
                const subtotal = p.invoiceSubtotal || 0;
                const taxAmount = p.invoiceTaxAmount || 0;
                const discountAmount = p.invoiceDiscountAmount || 0;
                const shippingAmount = p.invoiceShippingAmount || 0;
                const invoiceTotal = p.invoiceTotalAmount || 0;
                
                let html = `
                    <div class="row g-3 mb-3">
                        <div class="col-md-6">
                            <strong>Payment Number:</strong> ${p.paymentNumber || '-'}<br>
                            <strong>Transaction ID:</strong> ${p.transactionId || '-'}<br>
                            <strong>Reference:</strong> ${p.referenceNumber || '-'}<br>
                            <strong>Date:</strong> ${Format.date(p.paymentDate)}<br>
                            <strong>Method:</strong> ${p.paymentMethodType || p.paymentMethod || '-'}<br>
                            <strong>Currency:</strong> ${p.currency || 'PHP'}
                        </div>
                        <div class="col-md-6">
                            <strong>Customer:</strong> ${p.customerName || '-'}<br>
                            <strong>Invoice:</strong> ${p.invoiceNumber || '-'}<br>
                            <strong>Order:</strong> ${p.orderNumber || '-'}<br>
                            <strong>Amount Paid:</strong> <span class="text-success fw-bold">${Format.currency(p.amount)}</span><br>
                            <strong>Status:</strong> ${Format.statusBadge(p.status)}<br>
                            <strong>Processed:</strong> ${p.processedAt ? Format.date(p.processedAt) : 'Pending'}
                        </div>
                    </div>`;
                
                // Tax / VAT breakdown
                if (p.invoiceId) {
                    html += `
                    <hr>
                    <h6>Invoice Breakdown</h6>
                    <table class="table table-sm table-bordered">
                        <tbody>
                            <tr><td>Subtotal</td><td class="text-end">${Format.currency(subtotal)}</td></tr>
                            ${taxAmount > 0 ? `<tr><td>VAT / Tax (12%)</td><td class="text-end text-warning">${Format.currency(taxAmount)}</td></tr>` : ''}
                            ${discountAmount > 0 ? `<tr><td>Discount</td><td class="text-end text-danger">-${Format.currency(discountAmount)}</td></tr>` : ''}
                            ${shippingAmount > 0 ? `<tr><td>Shipping</td><td class="text-end">${Format.currency(shippingAmount)}</td></tr>` : ''}
                            <tr class="fw-bold"><td>Invoice Total</td><td class="text-end">${Format.currency(invoiceTotal)}</td></tr>
                            <tr class="fw-bold text-success"><td>Amount Paid</td><td class="text-end">${Format.currency(p.amount)}</td></tr>
                        </tbody>
                    </table>`;
                }
                
                if (p.notes) {
                    html += `<div class="mt-2"><strong>Notes:</strong> ${p.notes}</div>`;
                }
                if (p.failureReason) {
                    html += `<div class="mt-2 text-danger"><strong>Failure Reason:</strong> ${p.failureReason}</div>`;
                }
                if (p.refunds && p.refunds.length > 0) {
                    html += `<hr><h6>Refunds</h6>
                        <table class="table table-sm"><thead><tr><th>Refund #</th><th>Amount</th><th>Method</th><th>Status</th><th>Date</th></tr></thead>
                        <tbody>${p.refunds.map(r => `<tr>
                            <td>${r.refundNumber || '-'}</td>
                            <td>${Format.currency(r.amount)}</td>
                            <td>${r.refundMethod || '-'}</td>
                            <td>${Format.statusBadge(r.status)}</td>
                            <td>${Format.date(r.requestedAt)}</td>
                        </tr>`).join('')}</tbody></table>`;
                }

                Modal.showDynamic('Payment Details', html);
            } catch (error) {
                Toast.error('Failed to load payment details');
            }
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

// Compatibility Functions for Admin Sales Orders
window.printOrder = function() { Sales.orders.print(); };
window.approveOrder = function(id) { Sales.orders.approve(id); };
window.rejectOrder = function(id) { Sales.orders.reject(id); };
window.updateStatus = function(id) { Sales.orders.updateStatus(id); };

// Compatibility Functions for Admin Sales Orders filters & payment link modal
window.filterOrders = function() {
    if (!window.Sales || !Sales.orders) return;
    const search = (document.getElementById('searchOrders')?.value || '').toLowerCase();
    const status = document.getElementById('filterStatus')?.value || '';
    Sales.orders.filter(search, status);
};

window.applyFilters = function() {
    if (!window.Sales || !Sales.orders) return;

    const search = (document.getElementById('searchOrders')?.value || '').toLowerCase();
    const status = document.getElementById('filterStatus')?.value || '';
    const dateFrom = document.getElementById('filterDateFrom')?.value;
    const dateTo = document.getElementById('filterDateTo')?.value;

    let data = Sales.orders.data || [];

    if (search) {
        data = data.filter(o =>
            (o.orderNumber || '').toLowerCase().includes(search) ||
            (o.customerName || '').toLowerCase().includes(search)
        );
    }

    if (status) {
        data = data.filter(o => o.orderStatus === status);
    }

    if (dateFrom) {
        const from = new Date(dateFrom);
        from.setHours(0, 0, 0, 0);
        data = data.filter(o => o.orderDate && new Date(o.orderDate) >= from);
    }

    if (dateTo) {
        const to = new Date(dateTo);
        to.setHours(23, 59, 59, 999);
        data = data.filter(o => o.orderDate && new Date(o.orderDate) <= to);
    }

    Sales.orders.renderFiltered(data);
};

window.resetFilters = function() {
    document.getElementById('searchOrders') && (document.getElementById('searchOrders').value = '');
    document.getElementById('filterStatus') && (document.getElementById('filterStatus').value = '');
    document.getElementById('filterDateFrom') && (document.getElementById('filterDateFrom').value = '');
    document.getElementById('filterDateTo') && (document.getElementById('filterDateTo').value = '');
    if (window.Sales && Sales.orders) Sales.orders.render();
};

window.copyPaymentLink = async function() {
    const input = document.getElementById('paymentLinkUrl');
    const url = input ? input.value : '';
    if (!url) { Toast.warning('No payment link to copy'); return; }

    try {
        await navigator.clipboard.writeText(url);
        Toast.success('Payment link copied');
    } catch (e) {
        try {
            if (input) {
                input.select();
                document.execCommand('copy');
                Toast.success('Payment link copied');
            }
        } catch (err) {
            Toast.error('Copy failed');
        }
    }
};

window.openPaymentLink = function() {
    const anchor = document.getElementById('paymentLinkAnchor');
    const input = document.getElementById('paymentLinkUrl');
    const url = (anchor && anchor.href) ? anchor.href : (input ? input.value : '');
    if (!url) { Toast.warning('No payment link to open'); return; }
    window.open(url, '_blank');
};

