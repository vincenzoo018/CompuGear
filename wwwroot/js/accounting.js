/**
 * CompuGear Accounting Module JavaScript
 * Handles Chart of Accounts, Journal Entries, and General Ledger
 * Now with system integration: pulls data from Invoices, Payments, Refunds, COGS, Tax
 * Depends on: Toast, API, Format, Modal (from admin.js or billing.js)
 */

// ====== RAW API helper (for responses with multiple data keys like GL) ======
const AccountingAPI = {
    async raw(endpoint, method = 'GET', data = null) {
        const options = { method, headers: { 'Content-Type': 'application/json' } };
        if (data && method !== 'GET') options.body = JSON.stringify(data);
        const response = await fetch(`/api${endpoint}`, options);
        const result = await response.json();
        if (!response.ok) throw new Error(result.message || 'An error occurred');
        return result;
    }
};

// ====== SOURCE BADGE HELPER ======
const SourceBadge = {
    colors: {
        'Manual': 'secondary',
        'Invoice': 'primary',
        'Payment': 'success',
        'Refund': 'danger',
        'COGS': 'warning',
        'Tax': 'info',
        'Sales Revenue': 'success',
        'Journal Entry': 'secondary',
        'System': 'dark'
    },
    icons: {
        'Manual': '&#9998;',
        'Invoice': '&#128196;',
        'Payment': '&#128176;',
        'Refund': '&#8617;',
        'COGS': '&#128230;',
        'Tax': '&#127963;',
        'Sales Revenue': '&#128181;',
        'Journal Entry': '&#128203;',
        'System': '&#9881;'
    },
    render(sourceType, small = false) {
        const color = this.colors[sourceType] || 'secondary';
        const icon = this.icons[sourceType] || '';
        const cls = small ? 'badge rounded-pill' : 'badge';
        return `<span class="${cls} bg-${color}">${icon} ${sourceType}</span>`;
    }
};

// ===========================================================================
// CHART OF ACCOUNTS MODULE
// ===========================================================================
const COA = {
    data: [],
    filtered: [],

    async load() {
        try {
            const data = await API.get('/chart-of-accounts');
            this.data = Array.isArray(data) ? data : [];
            this.filtered = [...this.data];
            this.render();
            this.updateStats();
            this.populateParentDropdown();
        } catch (error) {
            console.error('COA Load Error:', error);
            Toast.error('Failed to load chart of accounts');
        }
    },

    render() {
        const tbody = document.getElementById('coaTableBody');
        if (!tbody) return;

        const countEl = document.getElementById('coaCount');
        if (countEl) countEl.textContent = `${this.filtered.length} accounts`;

        if (this.filtered.length === 0) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4 text-muted">No accounts found</td></tr>';
            if (typeof initPagination === 'function') initPagination('coaTableBody', 'coaPagination', 10);
            return;
        }

        tbody.innerHTML = this.filtered.map(a => {
            const balance = a.displayBalance || 0;
            const balClass = balance > 0 ? 'text-success' : (balance < 0 ? 'text-danger' : 'text-muted');
            return `
            <tr>
                <td><strong>${this.esc(a.accountCode)}</strong></td>
                <td>${this.esc(a.accountName)}</td>
                <td><span class="badge bg-${this.typeBadge(a.accountType)}">${a.accountType}</span></td>
                <td>${a.normalBalance || '-'}</td>
                <td class="text-end">${a.totalDebit > 0 ? Format.currency(a.totalDebit) : '-'}</td>
                <td class="text-end">${a.totalCredit > 0 ? Format.currency(a.totalCredit) : '-'}</td>
                <td class="text-end fw-bold ${balClass}">${Format.currency(Math.abs(balance))} ${balance >= 0 ? 'Dr' : 'Cr'}</td>
                <td>${a.isActive ? '<span class="badge bg-success">Active</span>' : '<span class="badge bg-secondary">Inactive</span>'}</td>
                <td class="text-center">
                    <div class="btn-group btn-group-sm">
                        <button class="btn btn-outline-info btn-sm" onclick="COA.view(${a.accountId})" title="View">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                        </button>
                        <button class="btn btn-outline-primary btn-sm" onclick="COA.edit(${a.accountId})" title="Edit">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                        </button>
                        <button class="btn btn-outline-danger btn-sm" onclick="COA.archive(${a.accountId})" title="Archive">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                        </button>
                    </div>
                </td>
            </tr>`;
        }).join('');
        if (typeof initPagination === 'function') initPagination('coaTableBody', 'coaPagination', 10);
    },

    updateStats() {
        const el = (id, val) => { const e = document.getElementById(id); if (e) e.textContent = val; };
        el('totalAccounts', this.data.length);
        el('assetAccounts', this.data.filter(a => a.accountType === 'Asset').length);
        el('revenueAccounts', this.data.filter(a => a.accountType === 'Revenue').length);
        el('expenseAccounts', this.data.filter(a => a.accountType === 'Expense').length);
        // Total balance stat
        const totalBalance = this.data.reduce((sum, a) => sum + Math.abs(a.displayBalance || 0), 0);
        el('totalBalance', typeof Format !== 'undefined' ? Format.currency(totalBalance) : totalBalance.toFixed(2));
    },

    populateParentDropdown() {
        const sel = document.getElementById('coaParent');
        if (!sel) return;
        const current = sel.value;
        sel.innerHTML = '<option value="">None (Top Level)</option>';
        this.data.filter(a => a.isActive).forEach(a => {
            sel.innerHTML += `<option value="${a.accountId}">${a.accountCode} - ${this.esc(a.accountName)}</option>`;
        });
        if (current) sel.value = current;
    },

    applyFilters() {
        if (typeof resetPagination === 'function') resetPagination('coaPagination');
        const search = (document.getElementById('coaSearch')?.value || '').toLowerCase();
        const type = document.getElementById('coaTypeFilter')?.value || '';
        const status = document.getElementById('coaStatusFilter')?.value || '';

        this.filtered = this.data.filter(a => {
            if (search && !a.accountCode.toLowerCase().includes(search) && !a.accountName.toLowerCase().includes(search)) return false;
            if (type && a.accountType !== type) return false;
            if (status === 'active' && !a.isActive) return false;
            if (status === 'inactive' && a.isActive) return false;
            return true;
        });
        this.render();
    },

    openCreate() {
        Modal.reset('coaForm');
        document.getElementById('coaId').value = '0';
        document.getElementById('coaModalTitle').textContent = 'New Account';
        const activeEl = document.getElementById('coaActive');
        if (activeEl) activeEl.checked = true;
        this.populateParentDropdown();
        Modal.show('coaModal');
    },

    async edit(id) {
        try {
            const account = await API.get(`/chart-of-accounts/${id}`);
            Modal.reset('coaForm');
            document.getElementById('coaId').value = account.accountId;
            document.getElementById('coaModalTitle').textContent = 'Edit Account';
            document.getElementById('coaCode').value = account.accountCode || '';
            document.getElementById('coaName').value = account.accountName || '';
            document.getElementById('coaType').value = account.accountType || '';
            document.getElementById('coaNormalBalance').value = account.normalBalance || '';
            document.getElementById('coaDescription').value = account.description || '';
            const activeEl = document.getElementById('coaActive');
            if (activeEl) activeEl.checked = account.isActive;

            this.populateParentDropdown();
            const parentEl = document.getElementById('coaParent');
            if (parentEl) parentEl.value = account.parentAccountId || '';

            Modal.show('coaModal');
        } catch (error) {
            Toast.error('Failed to load account details');
        }
    },

    async save(e) {
        e.preventDefault();
        const id = parseInt(document.getElementById('coaId').value) || 0;
        const payload = {
            accountCode: document.getElementById('coaCode').value.trim(),
            accountName: document.getElementById('coaName').value.trim(),
            accountType: document.getElementById('coaType').value,
            normalBalance: document.getElementById('coaNormalBalance').value,
            parentAccountId: parseInt(document.getElementById('coaParent').value) || null,
            description: document.getElementById('coaDescription').value.trim(),
            isActive: document.getElementById('coaActive')?.checked ?? true
        };

        if (!payload.accountCode || !payload.accountName || !payload.accountType) {
            Toast.error('Please fill in all required fields');
            return;
        }

        try {
            if (id > 0) {
                await API.put(`/chart-of-accounts/${id}`, payload);
                Toast.success('Account updated successfully');
            } else {
                await API.post('/chart-of-accounts', payload);
                Toast.success('Account created successfully');
            }
            Modal.hide('coaModal');
            await this.load();
        } catch (error) {
            Toast.error(error.message || 'Failed to save account');
        }
    },

    async view(id) {
        const content = document.getElementById('viewCoaContent');
        if (!content) return;
        content.innerHTML = '<div class="text-center py-4"><div class="spinner-border text-primary"></div></div>';
        Modal.show('viewCoaModal');

        try {
            const a = await API.get(`/chart-of-accounts/${id}`);
            // Find balance info from loaded data
            const withBal = this.data.find(x => x.accountId === id);
            const balSection = withBal ? `
                    <div class="col-12"><hr><h6 class="text-primary">System Balance (Live)</h6></div>
                    <div class="col-md-4">
                        <label class="text-muted small">Total Debits</label>
                        <p class="fw-bold text-success mb-2">${Format.currency(withBal.totalDebit || 0)}</p>
                    </div>
                    <div class="col-md-4">
                        <label class="text-muted small">Total Credits</label>
                        <p class="fw-bold text-danger mb-2">${Format.currency(withBal.totalCredit || 0)}</p>
                    </div>
                    <div class="col-md-4">
                        <label class="text-muted small">Net Balance</label>
                        <p class="fw-bold mb-2">${Format.currency(Math.abs(withBal.displayBalance || 0))} ${(withBal.displayBalance || 0) >= 0 ? 'Dr' : 'Cr'}</p>
                    </div>` : '';

            content.innerHTML = `
                <div class="row g-3">
                    <div class="col-md-6">
                        <label class="text-muted small">Account Code</label>
                        <p class="fw-bold mb-2">${this.esc(a.accountCode)}</p>
                    </div>
                    <div class="col-md-6">
                        <label class="text-muted small">Account Name</label>
                        <p class="fw-bold mb-2">${this.esc(a.accountName)}</p>
                    </div>
                    <div class="col-md-4">
                        <label class="text-muted small">Account Type</label>
                        <p><span class="badge bg-${this.typeBadge(a.accountType)}">${a.accountType}</span></p>
                    </div>
                    <div class="col-md-4">
                        <label class="text-muted small">Normal Balance</label>
                        <p class="fw-bold mb-2">${a.normalBalance || '-'}</p>
                    </div>
                    <div class="col-md-4">
                        <label class="text-muted small">Status</label>
                        <p>${a.isActive ? '<span class="badge bg-success">Active</span>' : '<span class="badge bg-secondary">Inactive</span>'}</p>
                    </div>
                    <div class="col-12">
                        <label class="text-muted small">Description</label>
                        <p class="mb-2">${a.description || '<em class="text-muted">No description</em>'}</p>
                    </div>
                    ${balSection}
                    <div class="col-md-6">
                        <label class="text-muted small">Created</label>
                        <p class="mb-0">${Format.date(a.createdAt)}</p>
                    </div>
                    <div class="col-md-6">
                        <label class="text-muted small">Last Updated</label>
                        <p class="mb-0">${Format.date(a.updatedAt)}</p>
                    </div>
                </div>`;
        } catch (error) {
            content.innerHTML = '<div class="alert alert-danger">Failed to load account details</div>';
        }
    },

    async archive(id) {
        if (!confirm('Are you sure you want to archive this account?')) return;
        try {
            await API.put(`/chart-of-accounts/${id}/archive`);
            Toast.success('Account archived');
            await this.load();
        } catch (error) {
            Toast.error(error.message || 'Failed to archive account');
        }
    },

    typeBadge(type) {
        const map = { 'Asset': 'primary', 'Liability': 'warning', 'Equity': 'info', 'Revenue': 'success', 'Expense': 'danger' };
        return map[type] || 'secondary';
    },

    esc(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }
};


// ===========================================================================
// JOURNAL ENTRIES MODULE
// ===========================================================================
const JE = {
    data: [],
    filtered: [],
    accounts: [],
    lineCounter: 0,

    async load() {
        try {
            const [entries, accounts] = await Promise.all([
                API.get('/journal-entries'),
                API.get('/chart-of-accounts')
            ]);
            this.data = Array.isArray(entries) ? entries : [];
            this.accounts = Array.isArray(accounts) ? accounts.filter(a => a.isActive) : [];
            this.filtered = [...this.data];

            // Reset filters
            ['jeSearch', 'jeStatusFilter', 'jeDateFrom', 'jeDateTo', 'jeSourceFilter'].forEach(id => {
                const e = document.getElementById(id); if (e) e.value = '';
            });

            this.render();
            this.updateStats();
        } catch (error) {
            console.error('JE Load Error:', error);
            Toast.error('Failed to load journal entries');
        }
    },

    render() {
        const tbody = document.getElementById('jeTableBody');
        if (!tbody) return;

        const countEl = document.getElementById('jeCount');
        if (countEl) countEl.textContent = `${this.filtered.length} entries`;

        if (this.filtered.length === 0) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4 text-muted">No journal entries found</td></tr>';
            if (typeof initPagination === 'function') initPagination('jeTableBody', 'jePagination', 10);
            return;
        }

        tbody.innerHTML = this.filtered.map(e => {
            const isSystem = e.source === 'System';
            const sourceType = e.sourceType || 'Journal Entry';
            return `
            <tr class="${isSystem ? 'table-light' : ''}">
                <td><strong>${COA.esc(e.entryNumber)}</strong></td>
                <td>${Format.date(e.entryDate)}</td>
                <td>${COA.esc(e.description)}</td>
                <td>${e.reference || '-'}</td>
                <td>${SourceBadge.render(sourceType, true)}</td>
                <td class="text-end">${Format.currency(e.totalDebit)}</td>
                <td class="text-end">${Format.currency(e.totalCredit)}</td>
                <td>${Format.statusBadge(e.status)}</td>
                <td class="text-center">
                    <div class="btn-group btn-group-sm">
                        <button class="btn btn-outline-info btn-sm" onclick="JE.view(${e.entryId})" title="View">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                        </button>
                        ${!isSystem && e.status === 'Draft' ? `
                        <button class="btn btn-outline-primary btn-sm" onclick="JE.edit(${e.entryId})" title="Edit">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/><path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/></svg>
                        </button>
                        <button class="btn btn-outline-success btn-sm" onclick="JE.post(${e.entryId})" title="Post">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"/></svg>
                        </button>` : ''}
                        ${!isSystem && e.status === 'Posted' ? `
                        <button class="btn btn-outline-dark btn-sm" onclick="JE.void(${e.entryId})" title="Void">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="4.93" y1="4.93" x2="19.07" y2="19.07"/></svg>
                        </button>` : ''}
                        ${!isSystem && e.status !== 'Posted' ? `
                        <button class="btn btn-outline-danger btn-sm" onclick="JE.archive(${e.entryId})" title="Archive">
                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                        </button>` : ''}
                    </div>
                </td>
            </tr>`;
        }).join('');
        if (typeof initPagination === 'function') initPagination('jeTableBody', 'jePagination', 10);
    },

    updateStats() {
        const el = (id, val) => { const e = document.getElementById(id); if (e) e.textContent = val; };
        el('totalEntries', this.data.length);
        const manual = this.data.filter(e => e.source !== 'System');
        const system = this.data.filter(e => e.source === 'System');
        el('manualEntries', manual.length);
        el('systemEntries', system.length);
        el('draftEntries', manual.filter(e => e.status === 'Draft').length);
        el('postedEntries', this.data.filter(e => e.status === 'Posted').length);
        const totalDebits = this.data.filter(e => e.status === 'Posted' || e.source === 'System').reduce((sum, e) => sum + (e.totalDebit || 0), 0);
        el('totalDebits', Format.currency(totalDebits));
    },

    applyFilters() {
        if (typeof resetPagination === 'function') resetPagination('jePagination');
        const search = (document.getElementById('jeSearch')?.value || '').toLowerCase();
        const status = document.getElementById('jeStatusFilter')?.value || '';
        const source = document.getElementById('jeSourceFilter')?.value || '';
        const dateFrom = document.getElementById('jeDateFrom')?.value || '';
        const dateTo = document.getElementById('jeDateTo')?.value || '';

        this.filtered = this.data.filter(e => {
            if (search && !e.entryNumber?.toLowerCase().includes(search) && !e.description?.toLowerCase().includes(search) && !(e.reference || '').toLowerCase().includes(search)) return false;
            if (status && e.status !== status) return false;
            if (source === 'Manual' && e.source !== 'Manual') return false;
            if (source === 'System' && e.source !== 'System') return false;
            if (source && source !== 'Manual' && source !== 'System' && e.sourceType !== source) return false;
            if (dateFrom && new Date(e.entryDate) < new Date(dateFrom)) return false;
            if (dateTo && new Date(e.entryDate) > new Date(dateTo + 'T23:59:59')) return false;
            return true;
        });
        this.render();
    },

    openCreate() {
        Modal.reset('jeForm');
        document.getElementById('jeId').value = '0';
        document.getElementById('jeModalTitle').textContent = 'New Journal Entry';
        document.getElementById('jeDate').value = new Date().toISOString().split('T')[0];
        document.getElementById('jeLinesBody').innerHTML = '';
        this.lineCounter = 0;
        this.addLine();
        this.addLine();
        this.updateTotals();
        Modal.show('jeModal');
    },

    async edit(id) {
        try {
            const entry = await API.get(`/journal-entries/${id}`);
            // Cannot edit system entries
            if (entry.source === 'System') {
                Toast.error('System-generated entries cannot be edited');
                return;
            }
            Modal.reset('jeForm');
            document.getElementById('jeId').value = entry.entryId;
            document.getElementById('jeModalTitle').textContent = `Edit ${entry.entryNumber}`;
            document.getElementById('jeDate').value = entry.entryDate ? entry.entryDate.split('T')[0] : '';
            document.getElementById('jeDescription').value = entry.description || '';
            document.getElementById('jeReference').value = entry.reference || '';
            document.getElementById('jeNotes').value = entry.notes || '';

            document.getElementById('jeLinesBody').innerHTML = '';
            this.lineCounter = 0;

            if (entry.lines && entry.lines.length > 0) {
                entry.lines.forEach(line => {
                    this.addLine(line.accountId, line.description, line.debitAmount, line.creditAmount);
                });
            } else {
                this.addLine();
                this.addLine();
            }
            this.updateTotals();
            Modal.show('jeModal');
        } catch (error) {
            Toast.error('Failed to load journal entry');
        }
    },

    addLine(accountId = '', description = '', debit = '', credit = '') {
        this.lineCounter++;
        const idx = this.lineCounter;
        const tbody = document.getElementById('jeLinesBody');
        if (!tbody) return;

        const accountOptions = this.accounts.map(a =>
            `<option value="${a.accountId}" ${a.accountId == accountId ? 'selected' : ''}>${a.accountCode} - ${COA.esc(a.accountName)}</option>`
        ).join('');

        const row = document.createElement('tr');
        row.id = `jeLine_${idx}`;
        row.innerHTML = `
            <td>
                <select class="form-select form-select-sm je-account" required>
                    <option value="">Select Account...</option>
                    ${accountOptions}
                </select>
            </td>
            <td>
                <input type="text" class="form-control form-control-sm je-line-desc" value="${COA.esc(description || '')}" placeholder="Line description">
            </td>
            <td>
                <input type="number" class="form-control form-control-sm text-end je-debit" value="${debit || ''}" placeholder="0.00" step="0.01" min="0" oninput="JE.onDebitInput(this)">
            </td>
            <td>
                <input type="number" class="form-control form-control-sm text-end je-credit" value="${credit || ''}" placeholder="0.00" step="0.01" min="0" oninput="JE.onCreditInput(this)">
            </td>
            <td class="text-center">
                <button type="button" class="btn btn-outline-danger btn-sm" onclick="JE.removeLine(${idx})">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
                </button>
            </td>`;
        tbody.appendChild(row);
    },

    onDebitInput(input) {
        // If debit has value, clear credit
        const row = input.closest('tr');
        if (input.value && parseFloat(input.value) > 0) {
            row.querySelector('.je-credit').value = '';
        }
        this.updateTotals();
    },

    onCreditInput(input) {
        // If credit has value, clear debit
        const row = input.closest('tr');
        if (input.value && parseFloat(input.value) > 0) {
            row.querySelector('.je-debit').value = '';
        }
        this.updateTotals();
    },

    removeLine(idx) {
        const row = document.getElementById(`jeLine_${idx}`);
        if (row) row.remove();
        this.updateTotals();
    },

    updateTotals() {
        const rows = document.querySelectorAll('#jeLinesBody tr');
        let totalDebit = 0, totalCredit = 0;
        rows.forEach(row => {
            totalDebit += parseFloat(row.querySelector('.je-debit')?.value || 0);
            totalCredit += parseFloat(row.querySelector('.je-credit')?.value || 0);
        });

        const debitEl = document.getElementById('jeTotalDebit');
        const creditEl = document.getElementById('jeTotalCredit');
        const diffEl = document.getElementById('jeDifference');

        if (debitEl) debitEl.textContent = Format.currency(totalDebit);
        if (creditEl) creditEl.textContent = Format.currency(totalCredit);

        if (diffEl) {
            const diff = Math.abs(totalDebit - totalCredit);
            if (diff < 0.01) {
                diffEl.textContent = `${Format.currency(0)} (Balanced)`;
                diffEl.className = 'text-success';
            } else {
                diffEl.textContent = `${Format.currency(diff)} (Unbalanced)`;
                diffEl.className = 'text-danger';
            }
        }
    },

    async save(e) {
        e.preventDefault();
        const id = parseInt(document.getElementById('jeId').value) || 0;
        const rows = document.querySelectorAll('#jeLinesBody tr');
        const lines = [];

        rows.forEach(row => {
            const accountId = parseInt(row.querySelector('.je-account')?.value || 0);
            const desc = row.querySelector('.je-line-desc')?.value || '';
            const debit = parseFloat(row.querySelector('.je-debit')?.value || 0);
            const credit = parseFloat(row.querySelector('.je-credit')?.value || 0);

            if (accountId > 0 && (debit > 0 || credit > 0)) {
                lines.push({ accountId, description: desc, debitAmount: debit, creditAmount: credit });
            }
        });

        if (lines.length < 2) {
            Toast.error('Journal entry must have at least 2 line items');
            return;
        }

        const totalDebit = lines.reduce((s, l) => s + l.debitAmount, 0);
        const totalCredit = lines.reduce((s, l) => s + l.creditAmount, 0);

        if (Math.abs(totalDebit - totalCredit) >= 0.01) {
            Toast.error('Total debits must equal total credits');
            return;
        }

        const payload = {
            entryDate: document.getElementById('jeDate').value,
            description: document.getElementById('jeDescription').value.trim(),
            reference: document.getElementById('jeReference').value.trim(),
            notes: document.getElementById('jeNotes').value.trim(),
            status: 'Draft',
            lines: lines
        };

        if (!payload.entryDate || !payload.description) {
            Toast.error('Please fill in the entry date and description');
            return;
        }

        try {
            if (id > 0) {
                await API.put(`/journal-entries/${id}`, payload);
                Toast.success('Journal entry updated');
            } else {
                await API.post('/journal-entries', payload);
                Toast.success('Journal entry created');
            }
            Modal.hide('jeModal');
            await this.load();
        } catch (error) {
            Toast.error(error.message || 'Failed to save journal entry');
        }
    },

    async view(id) {
        const content = document.getElementById('viewJeContent');
        if (!content) return;
        content.innerHTML = '<div class="text-center py-4"><div class="spinner-border text-primary"></div></div>';
        Modal.show('viewJeModal');

        try {
            const e = await API.get(`/journal-entries/${id}`);
            const isSystem = e.source === 'System';
            const sourceType = e.sourceType || 'Journal Entry';
            const linesHtml = (e.lines || []).map(l => `
                <tr>
                    <td><strong>${COA.esc(l.accountCode)}</strong> - ${COA.esc(l.accountName)}</td>
                    <td>${COA.esc(l.description || '')}</td>
                    <td class="text-end">${l.debitAmount > 0 ? Format.currency(l.debitAmount) : '-'}</td>
                    <td class="text-end">${l.creditAmount > 0 ? Format.currency(l.creditAmount) : '-'}</td>
                </tr>`).join('');

            content.innerHTML = `
                <div class="row g-3 mb-4">
                    <div class="col-md-3">
                        <label class="text-muted small">Entry Number</label>
                        <p class="fw-bold mb-2">${COA.esc(e.entryNumber)}</p>
                    </div>
                    <div class="col-md-3">
                        <label class="text-muted small">Entry Date</label>
                        <p class="fw-bold mb-2">${Format.date(e.entryDate)}</p>
                    </div>
                    <div class="col-md-3">
                        <label class="text-muted small">Status</label>
                        <p>${Format.statusBadge(e.status)}</p>
                    </div>
                    <div class="col-md-3">
                        <label class="text-muted small">Source</label>
                        <p>${SourceBadge.render(sourceType)} ${isSystem ? '<span class="badge bg-dark">Auto</span>' : ''}</p>
                    </div>
                    <div class="col-md-4">
                        <label class="text-muted small">Reference</label>
                        <p class="fw-bold mb-2">${e.reference || '-'}</p>
                    </div>
                    <div class="col-md-8">
                        <label class="text-muted small">Description</label>
                        <p class="mb-2">${COA.esc(e.description)}</p>
                    </div>
                    <div class="col-12">
                        <label class="text-muted small">Notes</label>
                        <p class="mb-2">${e.notes || '<em class="text-muted">No notes</em>'}</p>
                    </div>
                    ${e.postedAt ? `<div class="col-md-6">
                        <label class="text-muted small">Posted On</label>
                        <p class="mb-2">${Format.date(e.postedAt)}</p>
                    </div>` : ''}
                </div>
                <h6>Line Items</h6>
                <div class="table-responsive">
                    <table class="table table-sm table-bordered">
                        <thead class="table-light">
                            <tr><th>Account</th><th>Description</th><th class="text-end">Debit</th><th class="text-end">Credit</th></tr>
                        </thead>
                        <tbody>${linesHtml}</tbody>
                        <tfoot class="table-primary">
                            <tr>
                                <td colspan="2" class="text-end"><strong>Totals:</strong></td>
                                <td class="text-end"><strong>${Format.currency(e.totalDebit)}</strong></td>
                                <td class="text-end"><strong>${Format.currency(e.totalCredit)}</strong></td>
                            </tr>
                        </tfoot>
                    </table>
                </div>`;
        } catch (error) {
            content.innerHTML = '<div class="alert alert-danger">Failed to load journal entry details</div>';
        }
    },

    async post(id) {
        if (!confirm('Post this journal entry to the General Ledger? This action records all line items as ledger transactions.')) return;
        try {
            await API.put(`/journal-entries/${id}/post`);
            Toast.success('Journal entry posted to General Ledger');
            await this.load();
        } catch (error) {
            Toast.error(error.message || 'Failed to post journal entry');
        }
    },

    async void(id) {
        if (!confirm('Void this journal entry? This will remove its transactions from the General Ledger.')) return;
        try {
            await API.put(`/journal-entries/${id}/void`);
            Toast.success('Journal entry voided');
            await this.load();
        } catch (error) {
            Toast.error(error.message || 'Failed to void journal entry');
        }
    },

    async archive(id) {
        if (!confirm('Are you sure you want to archive this journal entry?')) return;
        try {
            await API.put(`/journal-entries/${id}/archive`);
            Toast.success('Journal entry archived');
            await this.load();
        } catch (error) {
            Toast.error(error.message || 'Failed to archive journal entry');
        }
    }
};


// ===========================================================================
// GENERAL LEDGER MODULE
// ===========================================================================
const GL = {
    data: [],
    filtered: [],
    summary: [],

    async load() {
        try {
            const result = await AccountingAPI.raw('/general-ledger');
            this.data = Array.isArray(result.data) ? result.data : [];
            this.summary = Array.isArray(result.summary) ? result.summary : [];
            this.filtered = [...this.data];

            // Reset filters
            ['glDateFrom', 'glDateTo', 'glSearch'].forEach(id => {
                const e = document.getElementById(id); if (e) e.value = '';
            });
            const acctFilter = document.getElementById('glAccountFilter');
            if (acctFilter) acctFilter.value = '';
            const srcFilter = document.getElementById('glSourceFilter');
            if (srcFilter) srcFilter.value = '';

            this.populateAccountFilter();
            this.render();
            this.renderSummary();
            this.updateStats();
        } catch (error) {
            console.error('GL Load Error:', error);
            Toast.error('Failed to load general ledger');
        }
    },

    populateAccountFilter() {
        const sel = document.getElementById('glAccountFilter');
        if (!sel) return;
        const current = sel.value;
        sel.innerHTML = '<option value="">All Accounts</option>';
        // Build unique accounts from summary
        this.summary.forEach(a => {
            sel.innerHTML += `<option value="${a.accountId}">${a.accountCode} - ${COA.esc(a.accountName)}</option>`;
        });
        if (current) sel.value = current;
    },

    render() {
        const tbody = document.getElementById('glTableBody');
        if (!tbody) return;

        const countEl = document.getElementById('glCount');
        if (countEl) countEl.textContent = `${this.filtered.length} transactions`;

        if (this.filtered.length === 0) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center py-4 text-muted">No ledger transactions found</td></tr>';
            if (typeof initPagination === 'function') initPagination('glTableBody', 'glPagination', 10);
            return;
        }

        tbody.innerHTML = this.filtered.map(t => {
            const isSystem = t.source === 'System';
            const sourceType = t.sourceType || 'Manual';
            return `
            <tr class="${isSystem ? 'table-light' : ''}">
                <td>${Format.date(t.transactionDate)}</td>
                <td><strong>${COA.esc(t.accountCode)}</strong> - ${COA.esc(t.accountName)}</td>
                <td>${COA.esc(t.description || '')}</td>
                <td>${t.entryNumber || t.reference || '-'}</td>
                <td>${SourceBadge.render(sourceType, true)}</td>
                <td class="text-end">${t.debitAmount > 0 ? Format.currency(t.debitAmount) : '-'}</td>
                <td class="text-end">${t.creditAmount > 0 ? Format.currency(t.creditAmount) : '-'}</td>
                <td class="text-end">${t.runningBalance != null && t.runningBalance !== 0 ? Format.currency(t.runningBalance) : '-'}</td>
                <td class="text-center">
                    ${!isSystem ? `
                    <button class="btn btn-outline-danger btn-sm" onclick="GL.archive(${t.ledgerId})" title="Archive">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                    </button>` : '<span class="badge bg-light text-muted">Auto</span>'}
                </td>
            </tr>`;
        }).join('');
        if (typeof initPagination === 'function') initPagination('glTableBody', 'glPagination', 10);
    },

    renderSummary() {
        const tbody = document.getElementById('glSummaryBody');
        const tfoot = document.getElementById('glSummaryFooter');
        if (!tbody) return;

        if (this.summary.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center py-4 text-muted">No account data available</td></tr>';
            if (tfoot) tfoot.innerHTML = '';
            return;
        }

        let grandDebit = 0, grandCredit = 0;

        tbody.innerHTML = this.summary.map(a => {
            grandDebit += a.totalDebit || 0;
            grandCredit += a.totalCredit || 0;
            const balance = (a.totalDebit || 0) - (a.totalCredit || 0);
            return `
                <tr>
                    <td><strong>${COA.esc(a.accountCode)}</strong></td>
                    <td>${COA.esc(a.accountName)}</td>
                    <td><span class="badge bg-${COA.typeBadge(a.accountType)}">${a.accountType}</span></td>
                    <td class="text-end">${Format.currency(a.totalDebit)}</td>
                    <td class="text-end">${Format.currency(a.totalCredit)}</td>
                    <td class="text-end fw-bold ${balance >= 0 ? 'text-success' : 'text-danger'}">${Format.currency(Math.abs(balance))} ${balance >= 0 ? 'Dr' : 'Cr'}</td>
                </tr>`;
        }).join('');

        if (tfoot) {
            const diff = grandDebit - grandCredit;
            tfoot.innerHTML = `
                <tr class="table-primary">
                    <td colspan="3" class="text-end"><strong>Grand Totals:</strong></td>
                    <td class="text-end"><strong>${Format.currency(grandDebit)}</strong></td>
                    <td class="text-end"><strong>${Format.currency(grandCredit)}</strong></td>
                    <td class="text-end"><strong class="${Math.abs(diff) < 0.01 ? 'text-success' : 'text-danger'}">${Math.abs(diff) < 0.01 ? 'Balanced' : Format.currency(Math.abs(diff)) + ' difference'}</strong></td>
                </tr>`;
        }
    },

    updateStats() {
        const el = (id, val) => { const e = document.getElementById(id); if (e) e.textContent = val; };
        el('glTransactions', this.data.length);
        const manualCount = this.data.filter(t => t.source !== 'System').length;
        const systemCount = this.data.filter(t => t.source === 'System').length;
        el('glManualTx', manualCount);
        el('glSystemTx', systemCount);
        const totalDebit = this.data.reduce((s, t) => s + (t.debitAmount || 0), 0);
        const totalCredit = this.data.reduce((s, t) => s + (t.creditAmount || 0), 0);
        el('glTotalDebit', Format.currency(totalDebit));
        el('glTotalCredit', Format.currency(totalCredit));
        el('glAccounts', this.summary.length);
    },

    async applyFilters() {
        if (typeof resetPagination === 'function') resetPagination('glPagination');
        const accountId = document.getElementById('glAccountFilter')?.value || '';
        const dateFrom = document.getElementById('glDateFrom')?.value || '';
        const dateTo = document.getElementById('glDateTo')?.value || '';
        const search = (document.getElementById('glSearch')?.value || '').toLowerCase();
        const source = document.getElementById('glSourceFilter')?.value || '';

        try {
            const params = new URLSearchParams();
            if (accountId) params.append('accountId', accountId);
            if (dateFrom) params.append('dateFrom', dateFrom);
            if (dateTo) params.append('dateTo', dateTo);
            if (source) params.append('source', source);

            const qs = params.toString();
            const result = await AccountingAPI.raw(`/general-ledger${qs ? '?' + qs : ''}`);
            this.data = Array.isArray(result.data) ? result.data : [];
            this.summary = Array.isArray(result.summary) ? result.summary : [];

            // Client-side search + source filter
            this.filtered = this.data.filter(t => {
                if (search && !(t.description || '').toLowerCase().includes(search)
                    && !(t.accountName || '').toLowerCase().includes(search)
                    && !(t.accountCode || '').toLowerCase().includes(search)
                    && !(t.reference || '').toLowerCase().includes(search)
                    && !(t.entryNumber || '').toLowerCase().includes(search)) return false;

                if (source === 'Manual' && t.source !== 'Manual') return false;
                if (source === 'System' && t.source !== 'System') return false;
                if (source && source !== 'Manual' && source !== 'System' && t.sourceType !== source) return false;

                return true;
            });

            this.populateAccountFilter();
            this.render();
            this.renderSummary();
            this.updateStats();
        } catch (error) {
            Toast.error('Failed to filter general ledger');
        }
    },

    async archive(id) {
        if (id < 0) {
            Toast.error('System-generated entries cannot be archived');
            return;
        }
        if (!confirm('Are you sure you want to archive this ledger entry?')) return;
        try {
            await API.put(`/general-ledger/${id}/archive`);
            Toast.success('Ledger entry archived');
            await this.applyFilters();
        } catch (error) {
            Toast.error(error.message || 'Failed to archive ledger entry');
        }
    }
};
