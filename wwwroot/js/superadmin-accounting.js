/**
 * SuperAdmin Platform Accounting Module
 * Handles Chart of Accounts, Journal Entries, General Ledger
 * Uses platform-level data (CompanyId = null)
 */

const SA_API = '/api/superadmin';
const PAGE_SIZE = 10;

// ── Utility Helpers ──────────────────────────────────────────────────
function formatCurrency(val) {
    const n = parseFloat(val) || 0;
    return '₱' + n.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatDate(d) {
    if (!d) return '—';
    const dt = new Date(d);
    return dt.toLocaleDateString('en-PH', { year: 'numeric', month: 'short', day: 'numeric' });
}

function formatDateTime(d) {
    if (!d) return '—';
    const dt = new Date(d);
    return dt.toLocaleDateString('en-PH', { year: 'numeric', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

function sourceBadge(source, type) {
    if (source === 'System') {
        const colors = { Subscription: '#008080', Tax: '#e67e22', Revenue: '#27ae60' };
        const c = colors[type] || '#6c757d';
        return `<span class="badge" style="background:${c};color:#fff;font-size:11px;padding:3px 8px;border-radius:4px;">${type || 'System'}</span>`;
    }
    return '<span class="badge" style="background:#6c757d;color:#fff;font-size:11px;padding:3px 8px;border-radius:4px;">Manual</span>';
}

function statusBadge(status) {
    const map = { Posted: 'success', Draft: 'warning', Void: 'danger', Archived: 'secondary' };
    const cls = map[status] || 'secondary';
    return `<span class="badge bg-${cls}">${status}</span>`;
}

function typeBadge(type) {
    const map = { Asset: '#3498db', Liability: '#e74c3c', Equity: '#9b59b6', Revenue: '#27ae60', Expense: '#e67e22', 'Cost of Goods Sold': '#e67e22' };
    const c = map[type] || '#6c757d';
    return `<span class="badge" style="background:${c};color:#fff;font-size:11px;padding:3px 8px;border-radius:4px;">${type}</span>`;
}

async function saFetch(url, options = {}) {
    const res = await fetch(SA_API + url, {
        ...options,
        headers: { 'Content-Type': 'application/json', ...(options.headers || {}) }
    });
    return res;
}

// ── Pagination Helper ────────────────────────────────────────────────
function renderPagination(containerId, currentPage, totalItems, onPageChange) {
    const totalPages = Math.ceil(totalItems / PAGE_SIZE);
    const start = totalItems === 0 ? 0 : (currentPage - 1) * PAGE_SIZE + 1;
    const end = Math.min(currentPage * PAGE_SIZE, totalItems);
    const container = document.getElementById(containerId);
    if (!container) return;

    container.innerHTML = `
        <span class="text-muted small">Showing ${start}–${end} of ${totalItems}</span>
        <div class="d-flex gap-1 align-items-center">
            <button class="btn btn-sm btn-outline-secondary" ${currentPage <= 1 ? 'disabled' : ''} id="${containerId}_prev">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"/></svg> Prev
            </button>
            <span class="small mx-2">Page ${currentPage} of ${totalPages || 1}</span>
            <button class="btn btn-sm btn-outline-secondary" ${currentPage >= totalPages ? 'disabled' : ''} id="${containerId}_next">
                Next <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="9 18 15 12 9 6"/></svg>
            </button>
        </div>
    `;

    document.getElementById(`${containerId}_prev`)?.addEventListener('click', () => onPageChange(currentPage - 1));
    document.getElementById(`${containerId}_next`)?.addEventListener('click', () => onPageChange(currentPage + 1));
}

// ══════════════════════════════════════════════════════════════════════
// CHART OF ACCOUNTS MODULE
// ══════════════════════════════════════════════════════════════════════
const COAModule = {
    data: [],
    currentPage: 1,

    async load() {
        try {
            const res = await saFetch('/chart-of-accounts');
            this.data = await res.json();
            this.currentPage = 1;
            this.updateStats();
            this.render();
        } catch (e) {
            console.error('Failed to load COA:', e);
            showError('Failed to load Chart of Accounts');
        }
    },

    updateStats() {
        const active = this.data.filter(a => a.isActive);
        const totalDr = this.data.reduce((s, a) => s + (a.totalDebit || 0), 0);
        const totalCr = this.data.reduce((s, a) => s + (a.totalCredit || 0), 0);

        document.getElementById('totalAccounts').textContent = this.data.length;
        document.getElementById('activeAccounts').textContent = active.length;
        document.getElementById('totalDebits').textContent = formatCurrency(totalDr);
        document.getElementById('totalCredits').textContent = formatCurrency(totalCr);
    },

    getFiltered() {
        const search = (document.getElementById('coaSearch')?.value || '').toLowerCase();
        const typeFilter = document.getElementById('coaTypeFilter')?.value || '';
        let filtered = this.data;
        if (search) filtered = filtered.filter(a => a.accountCode.toLowerCase().includes(search) || a.accountName.toLowerCase().includes(search));
        if (typeFilter) filtered = filtered.filter(a => a.accountType === typeFilter);
        return filtered;
    },

    render() {
        const filtered = this.getFiltered();
        const tbody = document.getElementById('coaTableBody');

        if (!filtered.length) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center py-4">No accounts found</td></tr>';
            renderPagination('coaPagination', 1, 0, () => {});
            return;
        }

        const totalPages = Math.ceil(filtered.length / PAGE_SIZE);
        if (this.currentPage > totalPages) this.currentPage = totalPages;
        const start = (this.currentPage - 1) * PAGE_SIZE;
        const pageData = filtered.slice(start, start + PAGE_SIZE);

        tbody.innerHTML = pageData.map(a => `
            <tr>
                <td><strong>${a.accountCode}</strong></td>
                <td>${a.accountName}</td>
                <td>${typeBadge(a.accountType)}</td>
                <td class="text-center">${a.normalBalance}</td>
                <td class="text-end">${formatCurrency(a.totalDebit)}</td>
                <td class="text-end">${formatCurrency(a.totalCredit)}</td>
                <td class="text-end"><strong>${formatCurrency(a.displayBalance)}</strong></td>
                <td class="text-center">
                    <div class="d-flex gap-1 justify-content-center">
                        <button class="btn btn-sm btn-outline-primary" onclick="COAModule.edit(${a.accountId})" title="Edit">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="COAModule.archive(${a.accountId}, '${a.accountName.replace(/'/g, "\\'")}')" title="Archive">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                        </button>
                    </div>
                </td>
            </tr>
        `).join('');

        renderPagination('coaPagination', this.currentPage, filtered.length, (page) => { this.currentPage = page; this.render(); });
    },

    openCreate() {
        document.getElementById('coaModalTitle').innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="me-2"><line x1="12" y1="1" x2="12" y2="23"/><path d="M17 5H9.5a3.5 3.5 0 0 0 0 7h5a3.5 3.5 0 0 1 0 7H6"/></svg> Add Account';
        document.getElementById('coaId').value = '';
        document.getElementById('coaCode').value = '';
        document.getElementById('coaName').value = '';
        document.getElementById('coaType').value = 'Asset';
        document.getElementById('coaNormal').value = 'Debit';
        document.getElementById('coaParent').value = '';
        document.getElementById('coaDesc').value = '';
        document.getElementById('coaActive').checked = true;
        new bootstrap.Modal(document.getElementById('coaModal')).show();
    },

    edit(id) {
        const a = this.data.find(x => x.accountId === id);
        if (!a) return;
        document.getElementById('coaModalTitle').innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="me-2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg> Edit Account';
        document.getElementById('coaId').value = a.accountId;
        document.getElementById('coaCode').value = a.accountCode;
        document.getElementById('coaName').value = a.accountName;
        document.getElementById('coaType').value = a.accountType;
        document.getElementById('coaNormal').value = a.normalBalance;
        document.getElementById('coaParent').value = a.parentAccountId || '';
        document.getElementById('coaDesc').value = a.description || '';
        document.getElementById('coaActive').checked = a.isActive;
        new bootstrap.Modal(document.getElementById('coaModal')).show();
    },

    async save() {
        const id = document.getElementById('coaId').value;
        const payload = {
            accountCode: document.getElementById('coaCode').value.trim(),
            accountName: document.getElementById('coaName').value.trim(),
            accountType: document.getElementById('coaType').value,
            normalBalance: document.getElementById('coaNormal').value,
            parentAccountId: document.getElementById('coaParent').value ? parseInt(document.getElementById('coaParent').value) : null,
            description: document.getElementById('coaDesc').value.trim(),
            isActive: document.getElementById('coaActive').checked
        };

        if (!payload.accountCode || !payload.accountName) return showError('Account code and name are required');

        try {
            const url = id ? `/chart-of-accounts/${id}` : '/chart-of-accounts';
            const method = id ? 'PUT' : 'POST';
            const res = await saFetch(url, { method, body: JSON.stringify(payload) });
            const data = await res.json();
            if (!res.ok) return showError(data.message || 'Failed to save');

            bootstrap.Modal.getInstance(document.getElementById('coaModal'))?.hide();
            showSuccess(data.message || 'Account saved');
            await this.load();
        } catch (e) { showError('Failed to save account'); }
    },

    async archive(id, name) {
        if (!confirm(`Archive account "${name}"?`)) return;
        try {
            const res = await saFetch(`/chart-of-accounts/${id}/archive`, { method: 'PUT' });
            const data = await res.json();
            if (!res.ok) return showError(data.message);
            showSuccess('Account archived');
            await this.load();
        } catch (e) { showError('Failed to archive'); }
    }
};

// ══════════════════════════════════════════════════════════════════════
// JOURNAL ENTRIES MODULE
// ══════════════════════════════════════════════════════════════════════
const JEModule = {
    data: [],
    accounts: [],
    lineCounter: 0,
    currentPage: 1,

    async load() {
        try {
            const [jeRes, coaRes] = await Promise.all([
                saFetch('/journal-entries'),
                saFetch('/chart-of-accounts')
            ]);
            this.data = await jeRes.json();
            this.accounts = await coaRes.json();
            this.currentPage = 1;
            this.updateStats();
            this.render();
        } catch (e) {
            console.error('JE load error:', e);
            showError('Failed to load journal entries');
        }
    },

    updateStats() {
        const posted = this.data.filter(e => e.status === 'Posted');
        const drafts = this.data.filter(e => e.status === 'Draft');
        const system = this.data.filter(e => e.source === 'System');
        const totalDr = this.data.reduce((s, e) => s + (e.totalDebit || 0), 0);

        document.getElementById('totalEntries').textContent = this.data.length;
        document.getElementById('postedEntries').textContent = posted.length;
        document.getElementById('draftEntries').textContent = drafts.length;
        document.getElementById('systemEntries').textContent = system.length;
    },

    getFiltered() {
        const search = (document.getElementById('jeSearch')?.value || '').toLowerCase();
        const statusFilter = document.getElementById('jeStatusFilter')?.value || '';
        const sourceFilter = document.getElementById('jeSourceFilter')?.value || '';
        let filtered = this.data;
        if (search) filtered = filtered.filter(e => (e.entryNumber || '').toLowerCase().includes(search) || (e.description || '').toLowerCase().includes(search));
        if (statusFilter) filtered = filtered.filter(e => e.status === statusFilter);
        if (sourceFilter) filtered = filtered.filter(e => e.source === sourceFilter);
        return filtered;
    },

    render() {
        const filtered = this.getFiltered();
        const tbody = document.getElementById('jeTableBody');

        if (!filtered.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4">No journal entries found</td></tr>';
            renderPagination('jePagination', 1, 0, () => {});
            return;
        }

        const totalPages = Math.ceil(filtered.length / PAGE_SIZE);
        if (this.currentPage > totalPages) this.currentPage = totalPages;
        const start = (this.currentPage - 1) * PAGE_SIZE;
        const pageData = filtered.slice(start, start + PAGE_SIZE);

        tbody.innerHTML = pageData.map(e => `
            <tr>
                <td><strong>${e.entryNumber || ''}</strong></td>
                <td>${formatDate(e.entryDate)}</td>
                <td style="max-width:200px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;" title="${(e.description || '').replace(/"/g, '&quot;')}">${e.description || ''}</td>
                <td>${e.reference || '—'}</td>
                <td>${sourceBadge(e.source, e.sourceType)}</td>
                <td class="text-end">${formatCurrency(e.totalDebit)}</td>
                <td class="text-end">${formatCurrency(e.totalCredit)}</td>
                <td class="text-center">${statusBadge(e.status)}</td>
                <td class="text-center">
                    <div class="d-flex gap-1 justify-content-center">
                        <button class="btn btn-sm btn-outline-info" onclick="JEModule.view(${e.entryId})" title="View">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                        </button>
                        ${e.source === 'Manual' && e.status === 'Draft' ? `
                        <button class="btn btn-sm btn-outline-primary" onclick="JEModule.edit(${e.entryId})" title="Edit">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                        </button>
                        <button class="btn btn-sm btn-outline-success" onclick="JEModule.post(${e.entryId})" title="Post">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>
                        </button>` : ''}
                        ${e.source === 'Manual' && e.status === 'Posted' ? `
                        <button class="btn btn-sm btn-outline-warning" onclick="JEModule.voidEntry(${e.entryId})" title="Void">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="4.93" y1="4.93" x2="19.07" y2="19.07"/></svg>
                        </button>` : ''}
                        ${e.source === 'Manual' ? `
                        <button class="btn btn-sm btn-outline-danger" onclick="JEModule.archive(${e.entryId})" title="Archive">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                        </button>` : ''}
                    </div>
                </td>
            </tr>
        `).join('');

        renderPagination('jePagination', this.currentPage, filtered.length, (page) => { this.currentPage = page; this.render(); });
    },

    async view(id) {
        try {
            const res = await saFetch(`/journal-entries/${id}`);
            if (!res.ok) return showError('Entry not found');
            const e = await res.json();

            document.getElementById('viewJeNumber').textContent = e.entryNumber;
            document.getElementById('viewJeDate').textContent = formatDate(e.entryDate);
            document.getElementById('viewJeDesc').textContent = e.description;
            document.getElementById('viewJeRef').textContent = e.reference || '—';
            document.getElementById('viewJeStatus').innerHTML = statusBadge(e.status);
            document.getElementById('viewJeSource').innerHTML = sourceBadge(e.source, e.sourceType);
            document.getElementById('viewJeNotes').textContent = e.notes || '—';
            document.getElementById('viewJePosted').textContent = e.postedAt ? formatDateTime(e.postedAt) : '—';

            const lines = e.lines || [];
            document.getElementById('viewJeLines').innerHTML = lines.map(l => `
                <tr>
                    <td>${l.accountCode}</td>
                    <td>${l.accountName}</td>
                    <td>${l.description || '—'}</td>
                    <td class="text-end">${l.debitAmount > 0 ? formatCurrency(l.debitAmount) : '—'}</td>
                    <td class="text-end">${l.creditAmount > 0 ? formatCurrency(l.creditAmount) : '—'}</td>
                </tr>
            `).join('') + `
                <tr style="font-weight:700;border-top:2px solid #333;">
                    <td colspan="3" class="text-end">Total</td>
                    <td class="text-end">${formatCurrency(e.totalDebit)}</td>
                    <td class="text-end">${formatCurrency(e.totalCredit)}</td>
                </tr>`;

            new bootstrap.Modal(document.getElementById('viewJeModal')).show();
        } catch (e) { showError('Failed to load entry'); }
    },

    openCreate() {
        this.lineCounter = 0;
        document.getElementById('jeModalTitle').innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="me-2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg> Create Journal Entry';
        document.getElementById('jeId').value = '';
        document.getElementById('jeDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('jeDesc').value = '';
        document.getElementById('jeRef').value = '';
        document.getElementById('jeNotes').value = '';
        document.getElementById('jeLinesBody').innerHTML = '';
        this.addLine();
        this.addLine();
        this.updateTotals();
        new bootstrap.Modal(document.getElementById('jeModal')).show();
    },

    async edit(id) {
        try {
            const res = await saFetch(`/journal-entries/${id}`);
            if (!res.ok) return showError('Entry not found');
            const e = await res.json();
            if (e.source === 'System') return showError('System entries cannot be edited');

            this.lineCounter = 0;
            document.getElementById('jeModalTitle').innerHTML = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="me-2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg> Edit Journal Entry';
            document.getElementById('jeId').value = e.entryId;
            document.getElementById('jeDate').value = e.entryDate?.split('T')[0] || '';
            document.getElementById('jeDesc').value = e.description || '';
            document.getElementById('jeRef').value = e.reference || '';
            document.getElementById('jeNotes').value = e.notes || '';
            document.getElementById('jeLinesBody').innerHTML = '';

            (e.lines || []).forEach(l => {
                this.addLine();
                const row = document.getElementById('jeLinesBody').lastElementChild;
                row.querySelector('.line-account').value = l.accountId;
                row.querySelector('.line-desc').value = l.description || '';
                row.querySelector('.line-debit').value = l.debitAmount > 0 ? l.debitAmount : '';
                row.querySelector('.line-credit').value = l.creditAmount > 0 ? l.creditAmount : '';
            });
            this.updateTotals();
            new bootstrap.Modal(document.getElementById('jeModal')).show();
        } catch (e) { showError('Failed to load entry'); }
    },

    addLine() {
        this.lineCounter++;
        const opts = this.accounts.map(a => `<option value="${a.accountId}">${a.accountCode} - ${a.accountName}</option>`).join('');
        const row = document.createElement('tr');
        row.id = `jeLine_${this.lineCounter}`;
        row.innerHTML = `
            <td><select class="form-control form-control-sm line-account"><option value="">Select Account</option>${opts}</select></td>
            <td><input type="text" class="form-control form-control-sm line-desc" placeholder="Description"></td>
            <td><input type="number" class="form-control form-control-sm line-debit text-end" step="0.01" min="0" placeholder="0.00" oninput="JEModule.onDebitInput(this)"></td>
            <td><input type="number" class="form-control form-control-sm line-credit text-end" step="0.01" min="0" placeholder="0.00" oninput="JEModule.onCreditInput(this)"></td>
            <td class="text-center"><button class="btn btn-sm btn-outline-danger" onclick="JEModule.removeLine(this)"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg></button></td>
        `;
        document.getElementById('jeLinesBody').appendChild(row);
    },

    removeLine(btn) {
        const rows = document.getElementById('jeLinesBody').querySelectorAll('tr');
        if (rows.length <= 2) return showError('Minimum 2 lines required');
        btn.closest('tr').remove();
        this.updateTotals();
    },

    onDebitInput(el) {
        if (parseFloat(el.value) > 0) el.closest('tr').querySelector('.line-credit').value = '';
        this.updateTotals();
    },

    onCreditInput(el) {
        if (parseFloat(el.value) > 0) el.closest('tr').querySelector('.line-debit').value = '';
        this.updateTotals();
    },

    updateTotals() {
        let dr = 0, cr = 0;
        document.querySelectorAll('#jeLinesBody tr').forEach(row => {
            dr += parseFloat(row.querySelector('.line-debit')?.value || 0);
            cr += parseFloat(row.querySelector('.line-credit')?.value || 0);
        });
        document.getElementById('jeTotalDebit').textContent = formatCurrency(dr);
        document.getElementById('jeTotalCredit').textContent = formatCurrency(cr);
        const diff = document.getElementById('jeDifference');
        const d = Math.abs(dr - cr);
        diff.textContent = formatCurrency(d);
        diff.style.color = d < 0.01 ? '#27ae60' : '#e74c3c';
    },

    async save() {
        const id = document.getElementById('jeId').value;
        const lines = [];
        let valid = true;

        document.querySelectorAll('#jeLinesBody tr').forEach(row => {
            const accountId = parseInt(row.querySelector('.line-account').value);
            const dr = parseFloat(row.querySelector('.line-debit').value) || 0;
            const cr = parseFloat(row.querySelector('.line-credit').value) || 0;
            if (!accountId) { valid = false; return; }
            if (dr === 0 && cr === 0) { valid = false; return; }
            lines.push({ accountId, description: row.querySelector('.line-desc').value, debitAmount: dr, creditAmount: cr });
        });

        if (!valid || lines.length < 2) return showError('Ensure all lines have an account and amount');

        const totalDr = lines.reduce((s, l) => s + l.debitAmount, 0);
        const totalCr = lines.reduce((s, l) => s + l.creditAmount, 0);
        if (Math.abs(totalDr - totalCr) >= 0.01) return showError('Debits must equal credits');

        const payload = {
            entryDate: document.getElementById('jeDate').value,
            description: document.getElementById('jeDesc').value.trim(),
            reference: document.getElementById('jeRef').value.trim(),
            notes: document.getElementById('jeNotes').value.trim(),
            lines
        };

        if (!payload.description) return showError('Description is required');

        try {
            const url = id ? `/journal-entries/${id}` : '/journal-entries';
            const method = id ? 'PUT' : 'POST';
            const res = await saFetch(url, { method, body: JSON.stringify(payload) });
            const data = await res.json();
            if (!res.ok) return showError(data.message || 'Failed to save');
            bootstrap.Modal.getInstance(document.getElementById('jeModal'))?.hide();
            showSuccess(data.message || 'Entry saved');
            await this.load();
        } catch (e) { showError('Failed to save entry'); }
    },

    async post(id) {
        if (!confirm('Post this journal entry to the General Ledger?')) return;
        try {
            const res = await saFetch(`/journal-entries/${id}/post`, { method: 'PUT' });
            const data = await res.json();
            if (!res.ok) return showError(data.message);
            showSuccess('Entry posted');
            await this.load();
        } catch (e) { showError('Failed to post'); }
    },

    async voidEntry(id) {
        if (!confirm('Void this journal entry? GL entries will be removed.')) return;
        try {
            const res = await saFetch(`/journal-entries/${id}/void`, { method: 'PUT' });
            const data = await res.json();
            if (!res.ok) return showError(data.message);
            showSuccess('Entry voided');
            await this.load();
        } catch (e) { showError('Failed to void'); }
    },

    async archive(id) {
        if (!confirm('Archive this journal entry?')) return;
        try {
            const res = await saFetch(`/journal-entries/${id}/archive`, { method: 'PUT' });
            const data = await res.json();
            if (!res.ok) return showError(data.message);
            showSuccess('Entry archived');
            await this.load();
        } catch (e) { showError('Failed to archive'); }
    }
};

// ══════════════════════════════════════════════════════════════════════
// GENERAL LEDGER MODULE
// ══════════════════════════════════════════════════════════════════════
const GLModule = {
    data: [],
    summary: [],
    accounts: [],
    currentPage: 1,

    async load() {
        try {
            const params = new URLSearchParams();
            const acct = document.getElementById('glAccountFilter')?.value;
            const from = document.getElementById('glDateFrom')?.value;
            const to = document.getElementById('glDateTo')?.value;
            if (acct) params.set('accountId', acct);
            if (from) params.set('dateFrom', from);
            if (to) params.set('dateTo', to);

            const [glRes, coaRes] = await Promise.all([
                saFetch(`/general-ledger?${params}`),
                saFetch('/chart-of-accounts')
            ]);
            const glData = await glRes.json();
            this.data = glData.data || [];
            this.summary = glData.summary || [];
            this.accounts = await coaRes.json();
            this.currentPage = 1;
            this.populateAccountFilter();
            this.updateStats();
            this.render();
            this.renderSummary();
        } catch (e) {
            console.error('GL load error:', e);
            showError('Failed to load general ledger');
        }
    },

    populateAccountFilter() {
        const sel = document.getElementById('glAccountFilter');
        if (!sel || sel.options.length > 1) return;
        this.accounts.forEach(a => {
            const opt = document.createElement('option');
            opt.value = a.accountId;
            opt.textContent = `${a.accountCode} - ${a.accountName}`;
            sel.appendChild(opt);
        });
    },

    updateStats() {
        const totalDr = this.data.reduce((s, g) => s + (g.debitAmount || 0), 0);
        const totalCr = this.data.reduce((s, g) => s + (g.creditAmount || 0), 0);
        const manualCount = this.data.filter(g => g.source === 'Manual').length;
        const systemCount = this.data.filter(g => g.source === 'System').length;

        document.getElementById('glTotalEntries').textContent = this.data.length;
        document.getElementById('glTotalDebits').textContent = formatCurrency(totalDr);
        document.getElementById('glTotalCredits').textContent = formatCurrency(totalCr);
        document.getElementById('glSystemEntries').textContent = systemCount;
    },

    render() {
        const search = (document.getElementById('glSearch')?.value || '').toLowerCase();
        const sourceFilter = document.getElementById('glSourceFilter')?.value || '';

        let filtered = this.data;
        if (search) filtered = filtered.filter(g => (g.accountCode || '').toLowerCase().includes(search) || (g.accountName || '').toLowerCase().includes(search) || (g.description || '').toLowerCase().includes(search));
        if (sourceFilter) filtered = filtered.filter(g => g.source === sourceFilter);

        const tbody = document.getElementById('glTableBody');
        if (!filtered.length) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4">No ledger entries found</td></tr>';
            renderPagination('glPagination', 1, 0, () => {});
            return;
        }

        const totalPages = Math.ceil(filtered.length / PAGE_SIZE);
        if (this.currentPage > totalPages) this.currentPage = totalPages;
        const start = (this.currentPage - 1) * PAGE_SIZE;
        const pageData = filtered.slice(start, start + PAGE_SIZE);

        tbody.innerHTML = pageData.map(g => `
            <tr>
                <td>${formatDate(g.transactionDate)}</td>
                <td><strong>${g.accountCode}</strong></td>
                <td>${g.accountName}</td>
                <td style="max-width:180px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;" title="${(g.description || '').replace(/"/g, '&quot;')}">${g.description || '—'}</td>
                <td>${sourceBadge(g.source, g.sourceType)}</td>
                <td class="text-end">${g.debitAmount > 0 ? formatCurrency(g.debitAmount) : '—'}</td>
                <td class="text-end">${g.creditAmount > 0 ? formatCurrency(g.creditAmount) : '—'}</td>
                <td>${g.entryNumber || g.reference || '—'}</td>
                <td class="text-center">
                    ${g.ledgerId > 0 ? `<button class="btn btn-sm btn-outline-danger" onclick="GLModule.archive(${g.ledgerId})" title="Archive"><svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg></button>` : '<span class="text-muted">—</span>'}
                </td>
            </tr>
        `).join('');
        renderPagination('glPagination', this.currentPage, filtered.length, (p) => { this.currentPage = p; this.render(); });
    },

    renderSummary() {
        const tbody = document.getElementById('glSummaryBody');
        if (!tbody) return;
        if (!this.summary.length) {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center py-3">No account activity</td></tr>';
            return;
        }

        let tdr = 0, tcr = 0;
        tbody.innerHTML = this.summary.map(s => {
            tdr += s.totalDebit;
            tcr += s.totalCredit;
            return `
                <tr>
                    <td><strong>${s.accountCode}</strong></td>
                    <td>${s.accountName}</td>
                    <td>${typeBadge(s.accountType)}</td>
                    <td class="text-end">${formatCurrency(s.totalDebit)}</td>
                    <td class="text-end">${formatCurrency(s.totalCredit)}</td>
                </tr>`;
        }).join('') + `
            <tr style="font-weight:700;border-top:2px solid #333;">
                <td colspan="3" class="text-end">Total</td>
                <td class="text-end">${formatCurrency(tdr)}</td>
                <td class="text-end">${formatCurrency(tcr)}</td>
            </tr>`;
    },

    async archive(id) {
        if (!confirm('Archive this ledger entry?')) return;
        try {
            const res = await saFetch(`/general-ledger/${id}/archive`, { method: 'PUT' });
            const data = await res.json();
            if (!res.ok) return showError(data.message);
            showSuccess('Entry archived');
            await this.load();
        } catch (e) { showError('Failed to archive'); }
    }
};
