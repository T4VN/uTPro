class ArroReportsElement extends HTMLElement {
    constructor() {
        super();
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalPages = 1;
        this.totalItems = 0;
    }

    connectedCallback() {
        const gridHtml = `
            <div class="umb-dashboard">
                <div class="form-list-container">
                    <div class="umb-panel-header">
                        <div class="umb-panel-header-content">
                            <div class="umb-panel-header-icon">
                                <umb-icon name="icon-chart-curve" class="color-blue"></umb-icon>
                            </div>
                            <div>
                                <h1 class="umb-panel-header-name">Form Submissions Report</h1>
                                <p class="umb-panel-header-description">View and manage form submissions</p>
                            </div>
                        </div>
                    </div>
                    
                    <div class="umb-panel-body">
                        <div class="umb-sub-header">
                            <div class="umb-sub-header__content">
                                <div class="flex items-center">
                                    <button class="umb-button umb-button--secondary" id="refreshBtn">
                                        <span class="umb-button__content">
                                            <umb-icon name="icon-refresh"></umb-icon>
                                            Refresh
                                        </span>
                                    </button>
                                </div>
                                <div class="umb-pagination">
                                    <span class="umb-pagination__text">Total: ${this.totalItems}</span>
                                    <button class="umb-button umb-button--secondary" id="prevBtn" ${this.currentPage === 1 ? 'disabled' : ''}>
                                        <span class="umb-button__content">
                                            <umb-icon name="icon-previous"></umb-icon>
                                            Previous
                                        </span>
                                    </button>
                                    <button class="umb-button umb-button--secondary" id="nextBtn" ${this.currentPage >= this.totalPages ? 'disabled' : ''}>
                                        <span class="umb-button__content">
                                            <umb-icon name="icon-next"></umb-icon>
                                            Next
                                        </span>
                                    </button>
                                </div>
                            </div>
                        </div>

                        <div id="loading" class="umb-empty-state" style="display: none;">
                            Loading...
                        </div>

                        <div class="umb-box">
                            <div class="umb-table">
                                <div class="umb-table-head">
                                    <div class="umb-table-row">
                                        <div class="umb-table-cell">
                                            <i class="icon icon-document color-blue mr1"></i>Form Name
                                        </div>
                                        <div class="umb-table-cell">
                                            <i class="icon icon-link color-blue mr1"></i>Website URL
                                        </div>
                                        <div class="umb-table-cell">
                                            <i class="icon icon-link-alt color-blue mr1"></i>Page URL
                                        </div>
                                        <div class="umb-table-cell">
                                            <i class="icon icon-globe color-blue mr1"></i>IP Address
                                        </div>
                                        <div class="umb-table-cell">
                                            <i class="icon icon-calendar color-blue mr1"></i>Created Date
                                        </div>
                                        <div class="umb-table-cell">Actions</div>
                                    </div>
                                </div>
                                <div class="umb-table-body" id="reportsTableBody">
                                    <!-- Reports will be loaded here -->
                                </div>
                            </div>

                            <div class="umb-table-footer">
                                <div class="umb-table-pagination">
                                    <button class="umb-button umb-button--secondary" id="prevBtnBottom" ${this.currentPage === 1 ? 'disabled' : ''}>
                                        <span class="umb-button__content">
                                            <umb-icon name="icon-previous"></umb-icon>
                                            Previous
                                        </span>
                                    </button>
                                    <span class="umb-table-pagination__text">Page ${this.currentPage} of ${this.totalPages}</span>
                                    <button class="umb-button umb-button--secondary" id="nextBtnBottom" ${this.currentPage >= this.totalPages ? 'disabled' : ''}>
                                        <span class="umb-button__content">
                                            Next
                                            <umb-icon name="icon-next"></umb-icon>
                                        </span>
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Modal Dialog for Form Data -->
            <div id="formDataModal" class="umb-modal" style="display: none;">
                <div class="umb-modal-content">
                    <div class="umb-panel-header modal-header">
                        <div class="umb-panel-header-content">
                            <div class="umb-panel-header-icon">
                                <umb-icon name="icon-info" class="color-blue"></umb-icon>
                            </div>
                            <div>
                                <h2 id="modalTitle" class="umb-panel-header-name">Form Submission Details</h2>
                                <p class="umb-panel-header-description">View the details of this form submission</p>
                            </div>
                        </div>
                        <button class="umb-button umb-button--xs umb-button--secondary close-modal-btn" id="closeModalBtnTop">
                            <span class="umb-button__content">
                                <umb-icon name="icon-delete"></umb-icon>
                            </span>
                        </button>
                    </div>
                    <div class="umb-panel-body modal-body">
                        <div id="formDataContent"></div>
                    </div>
                    <div class="umb-panel-footer">
                        <button class="umb-button umb-button--secondary" id="closeModalBtn">
                            <span class="umb-button__content">Close</span>
                        </button>
                    </div>
                </div>
            </div>
        `;

        this.innerHTML = gridHtml;

        // Add styles
        const style = document.createElement('style');
        style.textContent = `
            .umb-dashboard {
                background: #f6f6f7;
                min-height: 100vh;
            }

            .form-list-container {
                
                border-radius: 3px;
              
            }

            .umb-panel-header {
                padding: 20px 30px;
                border-bottom: 1px solid #e9e9eb;
                background: white;
                margin-bottom: 20px;
            }

            .umb-panel-header-content {
                display: flex;
                align-items: center;
                gap: 20px;
            }

            .umb-panel-header-icon {
                font-size: 40px;
            }

            .umb-panel-header-icon .icon {
                color: #2152A3;
            }

            .umb-panel-header-name {
                margin: 0;
                font-size: 24px;
                font-weight: 700;
                color: #1b264f;
            }

            .umb-panel-header-description {
                margin: 5px 0 0;
                color: #666;
                font-size: 15px;
            }

            .umb-panel-body {
                padding: 0 30px 30px;
            }

            .umb-sub-header {
                margin-bottom: 20px;
            }

            .umb-sub-header__content {
                display: flex;
                justify-content: space-between;
                align-items: center;
            }

            .flex {
                display: flex;
            }

            .items-center {
                align-items: center;
            }

            .mr1 {
                margin-right: 8px;
            }

            .umb-pagination {
                display: flex;
                align-items: center;
                gap: 10px;
            }

            .umb-pagination__text {
                color: #666;
                font-size: 14px;
            }

            .umb-box {
                background: white;
                border-radius: 3px;
                box-shadow: 0 1px 3px rgba(0,0,0,0.1);
            }

            .umb-table {
                width: 100%;
            }

            .umb-table-head {
                background: #f8f9fa;
                border-bottom: 1px solid #e9e9eb;
            }

            .umb-table-row {
                display: grid;
                grid-template-columns: 1fr 1fr 1fr 0.7fr 0.7fr 0.5fr;
                align-items: center;
                border-bottom: 1px solid #e9e9eb;
            }

            .umb-table-body .umb-table-row:last-child {
                border-bottom: none;
            }

            .umb-table-body .umb-table-row:hover {
                background-color: #f8f9fa;
            }

            .umb-table-cell {
                padding: 12px 20px;
                font-size: 14px;
                color: #1b264f;
                white-space: nowrap;
                overflow: hidden;
                text-overflow: ellipsis;
            }

            .umb-table-head .umb-table-cell {
                font-weight: 700;
                color: #1b264f;
            }

            .cell-inner {
                display: flex;
                align-items: center;
            }

            .cell-name {
                font-weight: 500;
                white-space: nowrap;
                overflow: hidden;
                text-overflow: ellipsis;
            }

            .cell-date {
                color: #666;
            }

            .cell-actions {
                display: flex;
                justify-content: flex-end;
            }

            .umb-empty-state {
                display: flex;
                justify-content: center;
                align-items: center;
                padding: 30px 0;
            }

            .umb-empty-state__content {
                text-align: center;
                max-width: 500px;
                padding: 0 20px;
            }

            .umb-empty-state__icon {
                font-size: 40px;
                color: #d8d7d9;
                margin-bottom: 20px;
            }

            .umb-empty-state__title {
                font-size: 18px;
                font-weight: 600;
                margin-bottom: 10px;
                color: #1b264f;
            }

            .umb-empty-state__text {
                font-size: 14px;
                color: #68676b;
                margin-bottom: 0;
            }

            .umb-load-indicator {
                display: flex;
                justify-content: center;
            }

            .umb-load-indicator__spinner {
                border: 2px solid #f3f3f3;
                border-top: 2px solid #2152a3;
                border-radius: 50%;
                width: 30px;
                height: 30px;
                animation: spin 1s linear infinite;
            }

            @keyframes spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }

            .umb-table-footer {
                padding: 15px;
                background: #f6f6f7;
                border-top: 1px solid #e9e9eb;
            }

            .umb-table-pagination {
                display: flex;
                align-items: center;
                justify-content: center;
                gap: 15px;
            }

            .umb-table-pagination__text {
                color: #666;
                font-size: 14px;
            }

            .umb-button {
                display: inline-flex;
                align-items: center;
                padding: 6px 14px;
                border-radius: 3px;
                border: none;
                font-size: 14px;
                font-weight: 600;
                cursor: pointer;
                transition: all 0.2s ease;
            }

            .umb-button--primary {
                background: #2152A3;
                color: white;
            }

            .umb-button--primary:hover {
                background: #1a4182;
            }

            .umb-button--secondary {
                background: #f8f9fa;
                color: #1b264f;
                border: 1px solid #e9e9eb;
            }

            .umb-button--secondary:hover {
                background: #e9ecef;
            }

            .umb-button--xs {
                padding: 4px 8px;
                font-size: 12px;
            }

            .umb-button:disabled {
                opacity: 0.6;
                cursor: not-allowed;
            }

            .umb-button__content {
                display: flex;
                align-items: center;
                gap: 6px;
            }

            .umb-modal {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background-color: rgba(0, 0, 0, 0.5);
                z-index: 1000;
                display: flex;
                justify-content: center;
                align-items: center;
            }

            .umb-modal-content {
                background: white;
                border-radius: 4px;
                box-shadow: 0 5px 15px rgba(0, 0, 0, 0.2);
                width: 100%;
                max-width: 800px;
                position: relative;
                margin: 0 20px;
                overflow: hidden;
                display: flex;
                flex-direction: column;
                max-height: 90vh;
            }

            .modal-header {
                padding: 15px 20px;
                margin-bottom: 0;
                border-bottom: 1px solid #e9e9eb;
                display: flex;
                justify-content: space-between;
                align-items: flex-start;
            }

            .modal-header .umb-panel-header-content {
                gap: 15px;
            }

            .modal-header .umb-panel-header-icon {
                font-size: 24px;
            }

            .close-modal-btn {
                background: transparent;
                border: none;
                padding: 5px;
                margin-left: auto;
                cursor: pointer;
                transition: all 0.2s;
            }

            .close-modal-btn:hover {
                background-color: #f0f0f0;
            }

            .modal-body {
                padding: 0;
                overflow-y: auto;
                max-height: calc(90vh - 150px);
                scrollbar-width: thin;
                scrollbar-color: #d8d7d9 #f6f6f7;
            }

            .modal-body::-webkit-scrollbar {
                width: 8px;
            }

            .modal-body::-webkit-scrollbar-track {
                background: #f6f6f7;
                border-radius: 4px;
            }

            .modal-body::-webkit-scrollbar-thumb {
                background-color: #d8d7d9;
                border-radius: 4px;
                border: 2px solid #f6f6f7;
            }

            .modal-body::-webkit-scrollbar-thumb:hover {
                background-color: #c0c0c0;
            }

            .umb-panel-header-name {
                margin: 0;
                font-size: 18px;
                font-weight: 600;
            }

            .umb-panel-footer {
                display: flex;
                justify-content: flex-end;
                gap: 10px;
                margin-top: 0;
                padding: 15px 20px;
                border-top: 1px solid #e9e9eb;
                background-color: #f8f9fa;
            }

            .form-data-table {
                width: 100%;
                border-collapse: separate;
                border-spacing: 0;
                background: white;
                border: none;
                border-radius: 0;
                margin-bottom: 0;
                table-layout: fixed;
            }

            .form-data-table thead {
                position: sticky;
                top: 0;
                z-index: 10;
            }

            .form-data-table th,
            .form-data-table td {
                padding: 12px 15px;
                text-align: left;
                border-bottom: 1px solid #e9e9eb;
                font-size: 14px;
                word-wrap: break-word;
                overflow-wrap: break-word;
            }

            .form-data-table th {
                background: #f6f6f7;
                font-weight: 600;
                color: #1b264f;
                box-shadow: 0 1px 0 rgba(0,0,0,0.1);
            }

            .form-data-table th:first-child {
                width: 30%;
                border-right: 1px solid #e9e9eb;
            }

            .form-data-table th:last-child {
                width: 70%;
            }

            .form-data-table td:first-child {
                background-color: #fafbfc;
                font-weight: 500;
                border-right: 1px solid #e9e9eb;
                position: sticky;
                left: 0;
                z-index: 5;
            }

            .form-data-table tr:hover td {
                background-color: #f8f9fa;
            }

            .form-data-table tr:hover td:first-child {
                background-color: #f3f4f5;
            }

            .form-data-table tr:last-child td {
                border-bottom: none;
            }

            .expandable-text {
                position: relative;
            }

            .expand-text-btn {
                margin-top: 8px;
                font-size: 12px;
                padding: 3px 8px;
            }

            .field-link {
                color: #2152a3;
                text-decoration: none;
                word-break: break-all;
            }

            .field-link:hover {
                text-decoration: underline;
            }

            .table-container {
                overflow-x: auto;
                width: 100%;
                border: 1px solid #e9e9eb;
                border-radius: 3px;
                margin: 0;
                background: white;
            }
        `;
        this.appendChild(style);

        this.loadReports();
        this.setupEventListeners();
    }

    setupEventListeners() {
        const prevBtn = this.querySelector('#prevBtn');
        const nextBtn = this.querySelector('#nextBtn');
        const prevBtnBottom = this.querySelector('#prevBtnBottom');
        const nextBtnBottom = this.querySelector('#nextBtnBottom');
        const modal = this.querySelector('#formDataModal');
        const closeBtn = this.querySelector('#closeModalBtn');
        const closeBtnTop = this.querySelector('#closeModalBtnTop');
        const refreshBtn = this.querySelector('#refreshBtn');

        prevBtn.addEventListener('click', () => {
            if (this.currentPage > 1) {
                this.currentPage--;
                this.loadReports();
            }
        });

        nextBtn.addEventListener('click', () => {
            if (this.currentPage < this.totalPages) {
                this.currentPage++;
                this.loadReports();
            }
        });

        // Bottom pagination buttons
        prevBtnBottom.addEventListener('click', () => {
            if (this.currentPage > 1) {
                this.currentPage--;
                this.loadReports();
            }
        });

        nextBtnBottom.addEventListener('click', () => {
            if (this.currentPage < this.totalPages) {
                this.currentPage++;
                this.loadReports();
            }
        });

        // Close modal when clicking the close button
        closeBtn.addEventListener('click', () => {
            modal.style.display = 'none';
        });

        closeBtnTop.addEventListener('click', () => {
            modal.style.display = 'none';
        });

        // Close modal when clicking outside
        window.addEventListener('click', (event) => {
            if (event.target === modal) {
                modal.style.display = 'none';
            }
        });

        refreshBtn.addEventListener('click', () => {
            this.loadReports();
        });

        // Add event delegation for view data buttons
        this.addEventListener('click', (e) => {
            const viewBtn = e.target.closest('.view-data-btn');
            if (viewBtn) {
                const id = viewBtn.dataset.id;
                const formName = viewBtn.dataset.formName;
                this.viewFormData(id, formName);
            }
        });
    }

    async loadReports() {
        try {
            this.showLoading(true);
            const response = await fetch(`/umbraco/Simpleform/api/GetFormCollections?page=${this.currentPage}&pageSize=${this.pageSize}`);
            const data = await response.json();

            this.renderReports(data.items);
            this.updatePagination(data);
        } catch (error) {
            console.error('Error loading reports:', error);
        } finally {
            this.showLoading(false);
        }
    }

    async viewFormData(id, formName) {
        try {
            // Show loading state in modal
            const formDataContent = this.querySelector('#formDataContent');
            formDataContent.innerHTML = '<div class="umb-load-indicator" style="margin: 30px auto;"><div class="umb-load-indicator__spinner"></div></div>';
            
            // Show modal first so user sees the loading indicator
            const modal = this.querySelector('#formDataModal');
            modal.style.display = 'flex';
            
            // Update modal title
            const modalTitle = this.querySelector('#modalTitle');
            modalTitle.textContent = formName || 'Form Submission Details';
            
            // Fetch data
            const response = await fetch(`/umbraco/Simpleform/api/GetFormCollectionData/${id}`);
            const data = await response.json();

            if (data && data.length > 0) {
                let tableHtml = `
                    <div class="table-container">
                        <table class="form-data-table">
                            <thead>
                                <tr>
                                    <th>Field Name</th>
                                    <th>Value</th>
                                </tr>
                            </thead>
                            <tbody>
                `;

                // Process and organize data
                const processedData = [];
                
                // Add submission date if available
                const dateItem = data.find(item => item.collectionName.toLowerCase() === 'date' || item.collectionName.toLowerCase() === 'created date');
                if (dateItem) {
                    processedData.push({
                        name: 'Submission Date',
                        value: this.formatDate(dateItem.collectionValue)
                    });
                    // Remove from original array to avoid duplication
                    data = data.filter(item => item !== dateItem);
                }
                
                // Add all other fields
                data.forEach(item => {
                    // Skip ID field as we already show form name in the title
                    if (item.collectionName.toLowerCase() !== 'id') {
                        processedData.push({
                            name: item.collectionName,
                            value: item.collectionValue || '-',
                            fieldType: item.type,
                            fileName: item.fileName || null
                        });
                    }
                });
                
                // Generate table rows
                processedData.forEach(item => {
                    tableHtml += `
                        <tr>
                            <td>${this.escapeHtml(item.name)}</td>
                            <td>${this.formatFieldValue(item.value, item.fieldType, item.fileName)}</td>
                        </tr>
                    `;
                });

                tableHtml += `
                            </tbody>
                        </table>
                    </div>
                `;
                
                formDataContent.innerHTML = tableHtml;
                
                // Add event listeners for "Show More" buttons
                const expandButtons = formDataContent.querySelectorAll('.expand-text-btn');
                expandButtons.forEach(button => {
                    button.addEventListener('click', (e) => {
                        const container = e.target.closest('.expandable-text');
                        const truncatedText = container.querySelector('.truncated-text');
                        const fullText = container.querySelector('.full-text');
                        const buttonContent = button.querySelector('.umb-button__content');
                        
                        if (fullText.style.display === 'none') {
                            truncatedText.style.display = 'none';
                            fullText.style.display = 'block';
                            buttonContent.textContent = 'Show Less';
                        } else {
                            truncatedText.style.display = 'block';
                            fullText.style.display = 'none';
                            buttonContent.textContent = 'Show More';
                        }
                    });
                });
            } else {
                formDataContent.innerHTML = `
                    <div class="umb-empty-state">
                        <div class="umb-empty-state__content">
                            <div class="umb-empty-state__icon">
                             <umb-icon name="icon-info" class="color-red"></umb-icon>
                              
                            </div>
                            <h3 class="umb-empty-state__title">No data available</h3>
                            <p class="umb-empty-state__text">This form submission doesn't contain any data.</p>
                        </div>
                    </div>
                `;
            }
        } catch (error) {
            console.error('Error viewing form data:', error);
            const formDataContent = this.querySelector('#formDataContent');
            formDataContent.innerHTML = `
                <div class="umb-empty-state">
                    <div class="umb-empty-state__content">
                        <div class="umb-empty-state__icon">
                            <umb-icon name="icon-wrong" class="color-red"></umb-icon>
                        </div>
                        <h3 class="umb-empty-state__title">Error loading data</h3>
                        <p class="umb-empty-state__text">There was a problem loading the form submission data.</p>
                    </div>
                </div>
            `;
        }
    }

    renderReports(items) {
        const tbody = this.querySelector('#reportsTableBody');
        tbody.innerHTML = '';

        if (!items || items.length === 0) {
            tbody.innerHTML = `
                <div class="umb-table-row">
                    <div class="umb-table-cell" style="grid-column: 1 / -1; text-align: center;">
                        No records found
                    </div>
                </div>`;
            return;
        }

        items.forEach(item => {
            const row = document.createElement('div');
            row.className = 'umb-table-row';
            row.innerHTML = `
                <div class="umb-table-cell">
                    <div class="cell-inner">
                        <umb-icon name="icon-document" class="color-blue mr1"></umb-icon>
                        <span class="cell-name">${this.escapeHtml(item.formName || '-')}</span>
                    </div>
                </div>
                <div class="umb-table-cell">${this.escapeHtml(item.websiteurl || '-')}</div>
                <div class="umb-table-cell">${this.escapeHtml(item.formPageUrl || '-')}</div>
                <div class="umb-table-cell">${this.escapeHtml(item.collectionIp || '-')}</div>
                <div class="umb-table-cell">
                    <span class="cell-date">${this.formatDate(item.createdDate)}</span>
                </div>
                <div class="umb-table-cell">
                    <div class="cell-actions">
                        <button class="umb-button umb-button--xs umb-button--primary view-data-btn" 
                            data-id="${item.id}" 
                            data-form-name="${this.escapeHtml(item.formName || '')}">
                            <span class="umb-button__content">
                                <i class="icon icon-eye"></i>
                                View
                            </span>
                        </button>
                    </div>
                </div>
            `;
            tbody.appendChild(row);
        });
    }

    updatePagination(data) {
        this.totalPages = data.totalPages;
        this.totalItems = data.totalItems;
        
        // Update top pagination
        const prevBtn = this.querySelector('#prevBtn');
        const nextBtn = this.querySelector('#nextBtn');
        const totalItemsSpan = this.querySelector('.umb-pagination__text');
        
        // Update bottom pagination
        const prevBtnBottom = this.querySelector('#prevBtnBottom');
        const nextBtnBottom = this.querySelector('#nextBtnBottom');
        const paginationText = this.querySelector('.umb-table-pagination__text');
        
        // Update text and button states
        totalItemsSpan.textContent = `Total: ${this.totalItems}`;
        paginationText.textContent = `Page ${this.currentPage} of ${this.totalPages}`;
        
        // Update button states
        const disablePrev = this.currentPage <= 1;
        const disableNext = this.currentPage >= this.totalPages;
        
        prevBtn.disabled = disablePrev;
        nextBtn.disabled = disableNext;
        prevBtnBottom.disabled = disablePrev;
        nextBtnBottom.disabled = disableNext;
    }

    showLoading(show) {
        const loading = this.querySelector('#loading');
        loading.style.display = show ? 'block' : 'none';
    }

    formatDate(dateString) {
        if (!dateString) return '-';
        const date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
    }

    escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    formatFieldValue(value, fieldType, fileName) {
        if (!value) return '-';
        
        // Check if it's a file type based on field type or try to detect base64 file data
        if (fieldType === 'File' || this.isFileData(value)) {
            try {
                // If we have a fileName from the API, use it directly with the base64 data
                if (fileName) {
                    return `<a href="data:application/octet-stream;base64,${value}" 
                        download="${this.escapeHtml(fileName)}" 
                        class="field-link">Download ${this.escapeHtml(fileName)}</a>`;
                }
                
                // Try to parse as JSON to extract file data
                let fileData;
                try {
                    fileData = JSON.parse(value);
                } catch (e) {
                    // If not valid JSON, check if it's a base64 string directly
                    if (this.isBase64(value)) {
                        // Create a generic download link if it's just a base64 string without filename
                        return `<a href="data:application/octet-stream;base64,${value}" 
                            download="file" class="field-link">Download File</a>`;
                    }
                    // Not parseable as JSON and not a base64 string
                    return this.escapeHtml(value);
                }
                
                // If we have parsed JSON with base64 data and filename
                if (fileData && fileData.base64Data && fileData.fileName) {
                    return `<a href="data:application/octet-stream;base64,${fileData.base64Data}" 
                        download="${this.escapeHtml(fileData.fileName)}" 
                        class="field-link">Download ${this.escapeHtml(fileData.fileName)}</a>`;
                } else if (fileData && typeof fileData === 'object') {
                    // If it's an object but doesn't have the expected properties
                    return `<a href="#" class="field-link">File Attachment</a>`;
                }
                
                // If JSON parsing succeeded but it's not a file data object
                return this.escapeHtml(value);
            } catch (e) {
                console.error('Error processing file data:', e);
                return this.escapeHtml(value);
            }
        }
        
        // Check if it's a URL
        if (value.match(/^(http|https):\/\/[^\s]+$/)) {
            return `<a href="${this.escapeHtml(value)}" target="_blank" class="field-link">${this.escapeHtml(value)}</a>`;
        }
        
        // Check if it's an email
        if (value.match(/^[^\s@]+@[^\s@]+\.[^\s@]+$/)) {
            return `<a href="mailto:${this.escapeHtml(value)}" class="field-link">${this.escapeHtml(value)}</a>`;
        }
        
        // Check if it's a long text (more than 100 characters)
        if (value.length > 100) {
            const truncated = value.substring(0, 100);
            return `
                <div class="expandable-text">
                    <div class="truncated-text">${this.escapeHtml(truncated)}...</div>
                    <div class="full-text" style="display: none;">${this.escapeHtml(value)}</div>
                    <button class="expand-text-btn umb-button umb-button--xs">
                        <span class="umb-button__content">Show More</span>
                    </button>
                </div>
            `;
        }
        
        return this.escapeHtml(value);
    }

    // Helper method to check if a string is likely file data
    isFileData(str) {
        // Check if it's a JSON string that might contain file data
        try {
            const obj = JSON.parse(str);
            return obj && obj.base64Data && obj.fileName;
        } catch (e) {
            // Check if it's a base64 string (common for file data)
            return this.isBase64(str);
        }
    }

    // Helper method to check if a string is base64 encoded
    isBase64(str) {
        if (typeof str !== 'string') return false;
        
        // Simple check for base64 pattern
        const base64Regex = /^[A-Za-z0-9+/=]+$/;
        // Base64 strings are typically long
        return str.length > 20 && base64Regex.test(str);
    }
}

if (!customElements.get('arro-reports')) {
    customElements.define('arro-reports', ArroReportsElement);
}

export default ArroReportsElement;
