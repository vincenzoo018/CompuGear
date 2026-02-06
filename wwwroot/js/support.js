/**
 * CompuGear Support Staff JavaScript
 * Handles Customer Support module functionality
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
    resolve: '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>'
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
    date(dateString, includeTime = false) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        const options = { year: 'numeric', month: 'short', day: 'numeric' };
        if (includeTime) { options.hour = '2-digit'; options.minute = '2-digit'; }
        return date.toLocaleDateString(CONFIG.dateFormat, options);
    },
    statusBadge(status) {
        const colors = {
            'Open': 'warning', 'In Progress': 'info', 'Pending Customer': 'secondary',
            'Resolved': 'success', 'Closed': 'dark', 'Active': 'success'
        };
        return `<span class="badge bg-${colors[status] || 'secondary'}">${status}</span>`;
    },
    priorityBadge(priority) {
        const colors = { 'Low': 'info', 'Medium': 'warning', 'High': 'danger', 'Critical': 'dark' };
        return `<span class="badge bg-${colors[priority] || 'secondary'}">${priority}</span>`;
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
// SUPPORT TICKETS MODULE
// ===========================================
const SupportTickets = {
    data: [],
    currentId: null,

    async load() {
        try {
            this.data = await API.get('/support/tickets');
            this.render();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to load tickets');
        }
    },

    render() {
        const tbody = document.getElementById('ticketsTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4">No tickets found</td></tr>';
            return;
        }

        tbody.innerHTML = this.data.map(t => `
            <tr>
                <td><strong>${t.ticketNumber}</strong></td>
                <td>
                    <div class="fw-semibold text-truncate" style="max-width: 200px;">${t.subject}</div>
                </td>
                <td>${t.customerName || t.contactName || 'N/A'}</td>
                <td>${Format.priorityBadge(t.priority)}</td>
                <td>${Format.statusBadge(t.status)}</td>
                <td>${t.assignedToName || 'Unassigned'}</td>
                <td>${Format.date(t.createdAt)}</td>
                <td class="text-center">
                    <div class="btn-group">
                        <button class="btn btn-sm btn-outline-primary" onclick="SupportTickets.view(${t.ticketId})">${Icons.view}</button>
                        <button class="btn btn-sm btn-outline-warning" onclick="SupportTickets.edit(${t.ticketId})">${Icons.edit}</button>
                        ${t.status !== 'Resolved' && t.status !== 'Closed' ? `<button class="btn btn-sm btn-outline-success" onclick="SupportTickets.resolve(${t.ticketId})">${Icons.resolve}</button>` : ''}
                    </div>
                </td>
            </tr>
        `).join('');
    },

    updateStats() {
        const open = this.data.filter(t => t.status === 'Open').length;
        const inProgress = this.data.filter(t => t.status === 'In Progress').length;
        const resolved = this.data.filter(t => t.status === 'Resolved').length;
        const high = this.data.filter(t => t.priority === 'High' || t.priority === 'Critical').length;

        if (document.getElementById('openTickets')) document.getElementById('openTickets').textContent = open;
        if (document.getElementById('inProgressTickets')) document.getElementById('inProgressTickets').textContent = inProgress;
        if (document.getElementById('resolvedTickets')) document.getElementById('resolvedTickets').textContent = resolved;
        if (document.getElementById('highPriorityTickets')) document.getElementById('highPriorityTickets').textContent = high;
    },

    view(id) {
        window.location.href = `/SupportStaff/TicketDetails/${id}`;
    },

    edit(id) {
        this.currentId = id;
        const ticket = this.data.find(t => t.ticketId === id);
        if (!ticket) return;
        // Populate modal
        document.getElementById('ticketId').value = id;
        document.getElementById('ticketStatus').value = ticket.status;
        document.getElementById('ticketPriority').value = ticket.priority;
        Modal.show('ticketModal');
    },

    async resolve(id) {
        if (!confirm('Mark this ticket as resolved?')) return;
        try {
            await API.put(`/support/tickets/${id}/resolve`, {});
            Toast.success('Ticket resolved successfully');
            this.load();
        } catch (error) {
            Toast.error('Failed to resolve ticket');
        }
    },

    async save() {
        const data = {
            ticketId: document.getElementById('ticketId').value,
            status: document.getElementById('ticketStatus').value,
            priority: document.getElementById('ticketPriority').value,
            resolution: document.getElementById('ticketResolution')?.value
        };

        try {
            await API.put(`/support/tickets/${data.ticketId}`, data);
            Toast.success('Ticket updated successfully');
            Modal.hide('ticketModal');
            this.load();
        } catch (error) {
            Toast.error('Failed to update ticket');
        }
    },

    async addMessage(id) {
        const message = document.getElementById('newMessage')?.value;
        if (!message) return;

        try {
            await API.post(`/support/tickets/${id}/messages`, { message });
            Toast.success('Message added');
            document.getElementById('newMessage').value = '';
            // Reload ticket details
            this.loadMessages(id);
        } catch (error) {
            Toast.error('Failed to add message');
        }
    },

    async loadMessages(id) {
        try {
            const messages = await API.get(`/support/tickets/${id}/messages`);
            const container = document.getElementById('ticketMessages');
            if (!container) return;

            container.innerHTML = messages.map(m => `
                <div class="message-item ${m.senderType === 'Customer' ? 'customer' : 'agent'}">
                    <div class="message-header">
                        <strong>${m.senderType === 'Customer' ? m.senderName : 'Support Agent'}</strong>
                        <small>${Format.date(m.createdAt, true)}</small>
                    </div>
                    <div class="message-body">${m.message}</div>
                </div>
            `).join('');
        } catch (error) {
            console.error('Failed to load messages:', error);
        }
    }
};

// ===========================================
// CUSTOMER PROFILES MODULE (Read-Only for sensitive data)
// ===========================================
const SupportCustomers = {
    data: [],

    async load() {
        try {
            this.data = await API.get('/customers');
            this.render();
        } catch (error) {
            Toast.error('Failed to load customers');
        }
    },

    render() {
        const tbody = document.getElementById('customersTableBody');
        if (!tbody) return;

        if (this.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4">No customers found</td></tr>';
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
                <td>${c.totalOrders || 0} orders</td>
                <td>${Format.statusBadge(c.status)}</td>
                <td class="text-center">
                    <button class="btn btn-sm btn-outline-primary" onclick="SupportCustomers.view(${c.customerId})">${Icons.view}</button>
                </td>
            </tr>
        `).join('');
    },

    view(id) {
        window.location.href = `/SupportStaff/CustomerProfile/${id}`;
    }
};

// ===========================================
// KNOWLEDGE BASE MODULE
// ===========================================
const KnowledgeBase = {
    articles: [],
    categories: [],

    async load() {
        try {
            this.articles = await API.get('/knowledge/articles');
            this.categories = await API.get('/knowledge/categories');
            this.render();
        } catch (error) {
            Toast.error('Failed to load knowledge base');
        }
    },

    render() {
        const container = document.getElementById('articlesContainer');
        if (!container) return;

        if (this.articles.length === 0) {
            container.innerHTML = '<div class="text-center py-4">No articles found</div>';
            return;
        }

        container.innerHTML = this.articles.map(a => `
            <div class="card mb-3">
                <div class="card-body">
                    <h5 class="card-title">${a.title}</h5>
                    <p class="card-text text-muted">${a.excerpt || ''}</p>
                    <div class="d-flex justify-content-between align-items-center">
                        <span class="badge bg-info">${a.categoryName || 'General'}</span>
                        <button class="btn btn-sm btn-outline-primary" onclick="KnowledgeBase.view(${a.articleId})">Read More</button>
                    </div>
                </div>
            </div>
        `).join('');
    },

    view(id) {
        window.location.href = `/SupportStaff/Article/${id}`;
    },

    search(query) {
        if (!query) {
            this.render();
            return;
        }
        const filtered = this.articles.filter(a => 
            a.title.toLowerCase().includes(query.toLowerCase()) ||
            (a.content && a.content.toLowerCase().includes(query.toLowerCase()))
        );
        // Re-render with filtered data
        const container = document.getElementById('articlesContainer');
        if (!container) return;

        if (filtered.length === 0) {
            container.innerHTML = '<div class="text-center py-4">No articles match your search</div>';
            return;
        }

        container.innerHTML = filtered.map(a => `
            <div class="card mb-3">
                <div class="card-body">
                    <h5 class="card-title">${a.title}</h5>
                    <p class="card-text text-muted">${a.excerpt || ''}</p>
                    <button class="btn btn-sm btn-outline-primary" onclick="KnowledgeBase.view(${a.articleId})">Read More</button>
                </div>
            </div>
        `).join('');
    }
};

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    const path = window.location.pathname.toLowerCase();
    
    if (path.includes('/supportstaff/tickets') || path.endsWith('/tickets')) {
        SupportTickets.load();
    } else if (path.includes('/supportstaff/customers') || path.endsWith('/customers')) {
        SupportCustomers.load();
    } else if (path.includes('/supportstaff/knowledge') || path.endsWith('/knowledge')) {
        KnowledgeBase.load();
    } else if (path === '/supportstaff' || path === '/supportstaff/') {
        // Dashboard - load stats
        Promise.all([
            API.get('/support/tickets').catch(() => []),
            API.get('/customers').catch(() => [])
        ]).then(([tickets, customers]) => {
            const open = tickets.filter(t => t.status === 'Open').length;
            const inProgress = tickets.filter(t => t.status === 'In Progress').length;
            const resolved = tickets.filter(t => t.status === 'Resolved').length;
            
            if (document.getElementById('openTickets')) document.getElementById('openTickets').textContent = open;
            if (document.getElementById('inProgressTickets')) document.getElementById('inProgressTickets').textContent = inProgress;
            if (document.getElementById('resolvedTickets')) document.getElementById('resolvedTickets').textContent = resolved;
            if (document.getElementById('totalCustomers')) document.getElementById('totalCustomers').textContent = customers.length;
        });
    }
});
