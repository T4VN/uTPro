class ArroFormsElement extends HTMLElement {
    constructor() {
        super();
        this.forms = [];
        this.currentPage = 1;
        this.pageSize = 10;
        this.totalPages = 1;
        this.totalItems = 0;
        this.currentFormId = null;
    }

    async loadForms() {
        try {
            // Show loading indicator
            this.showLoading(true);
            
            const response = await fetch(`/umbraco/Simpleform/api/getallforms?page=${this.currentPage}&pageSize=${this.pageSize}`, {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin' // This is important for Umbraco backoffice authentication
            });
            
            // Hide loading indicator
            this.showLoading(false);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            this.forms = data.items;
            this.totalItems = data.totalItems;
            this.totalPages = data.totalPages;
            
            this.renderGrid();
            this.updatePagination();
        } catch (error) {
            // Hide loading indicator on error
            this.showLoading(false);
            
            // Set default values for empty state
            this.forms = [];
            this.totalItems = 0;
            this.totalPages = 1;
            
            // Show error message but still render the grid with empty state
            console.error('Error loading forms:', error);
            
            // Display a notification about the error
            if (typeof notificationService !== 'undefined') {
                notificationService.error('Error', 'Failed to load forms. Please try again later.');
            }
            
            // Still render the grid to show empty state and allow adding new forms
            this.renderGrid();
            this.updatePagination();
        }
    }

    showError(message) {
        this.innerHTML = `
            <div class="error-message">${message}</div>
            <style>
                .error-message {
                    color: #dc3545;
                    padding: 10px;
                    margin: 10px 0;
                    border: 1px solid #dc3545;
                    border-radius: 4px;
                    background-color: #f8d7da;
                }
            </style>
        `;
    }

    renderGrid() {
        const gridHtml = `
            <div class="umb-dashboard">
                <div class="form-list-container">
                    <div class="umb-panel-header">
                        <div class="umb-panel-header-content">
                            <div class="umb-panel-header-icon">
                             <umb-icon name="icon-list" class="color-blue"></umb-icon>
                            </div>
                            <div>
                                <h1 class="umb-panel-header-name">Forms List</h1>
                                <p class="umb-panel-header-description">Manage and organize your forms</p>
                            </div>
                        </div>
                    </div>
                    
                    <div class="umb-panel-body">
                        <div class="umb-sub-header">
                            <div class="umb-sub-header__content">
                                <div class="flex items-center">
                                    <button class="umb-button umb-button--primary" id="createBtn">
                                        <span class="umb-button__content">
                                            <umb-icon name="icon-add"></umb-icon>
                                            Create Form
                                        </span>
                                    </button>
                                    <button class="umb-button umb-button--secondary ml2" id="refreshBtn">
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
                                    <button class="umb-button umb-button--secondary" id="nextBtn" ${(this.currentPage >= this.totalPages) ? 'disabled' : ''}>
                                        <span class="umb-button__content">
                                            <umb-icon name="icon-next"></umb-icon>
                                            Next
                                        </span>
                                    </button>
                                </div>
                            </div>
                        </div>

                        <div class="umb-box">
                            ${this.forms.length === 0 ?
                            '<div class="umb-empty-state">No forms found. Click "Create Form" to add a new form.</div>' :
                            `<div class="umb-table">
                                <div class="umb-table-head">
                                    <div class="umb-table-row">
                                        <div class="umb-table-cell">
                                            Name
                                        </div>
                                        <div class="umb-table-cell">
                                            Created Date
                                        </div>
                                        <div class="umb-table-cell">Actions</div>
                                    </div>
                                </div>
                                <div class="umb-table-body">
                                    ${this.forms.map(form => `
                                        <div class="umb-table-row">
                                            <div class="umb-table-cell">
                                                <div class="cell-inner">
                                                    <umb-icon name="icon-document" class="color-blue mr1"></umb-icon>
                                                    <span class="cell-name">${form.name || '-'}</span>
                                                </div>
                                            </div>
                                            <div class="umb-table-cell">
                                                <span class="cell-date">${new Date(form.createdDate).toLocaleDateString()}</span>
                                            </div>
                                            <div class="umb-table-cell">
                                                <div class="cell-actions">
                                                    <button class="umb-button umb-button--xs umb-button--secondary edit-form-btn" 
                                                        data-id="${form.id}"
                                                        data-name="${form.name}">
                                                        <span class="umb-button__content">
                                                            <i class="icon icon-edit"></i>
                                                            Edit
                                                        </span>
                                                    </button>
                                                </div>
                                            </div>
                                        </div>
                                    `).join('')}
                                </div>
                            </div>`}
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
                                            <umb-icon name="icon-next"></umb-icon>
                                            Next
                                        </span>
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="form-editor-container" style="display: none;">
                    <div class="umb-panel-header">
                        <div class="umb-panel-header-content">
                            <div class="umb-panel-header-icon">
                                    <umb-icon name="icon-edit" class="color-blue"></umb-icon>
                            </div>
                            <div class="umb-panel-header-title">
                                <h1 class="umb-panel-header-name"></h1>
                                <p class="umb-panel-header-description">Edit form settings and fields</p>
                            </div>
                            <div class="umb-panel-header-actions">
                                <button class="umb-button umb-button--secondary" id="backToList">
                                    <span class="umb-button__content">
                                        <umb-icon name="icon-arrow-left"></umb-icon>
                                        Back to List
                                    </span>
                                </button>
                            </div>
                        </div>
                    </div>
                    <div class="form-editor-frame">
                        <iframe class="form-editor-iframe" frameborder="0"></iframe>
                    </div>
                </div>

                <div id="createFormModal" class="umb-modal" style="display: none;">
                    <div class="umb-modal-content">
                        <div class="umb-panel-header">
                            <h2>Create New Form</h2>
                        </div>
                        <div class="umb-panel-body">
                            <div class="form-group">
                                <label for="formName">Form Name</label>
                                <input type="text" id="formName" class="form-control" pattern="[A-Za-z0-9 ]+" title="Only letters, numbers and spaces are allowed" placeholder="Enter form name">
                                <small class="error-message" style="display: none; color: #dc3545; margin-top: 8px; padding: 8px; background-color: #fff; border: 1px solid #dc3545; border-radius: 3px;"></small>
                            </div>
                        </div>
                        <div class="umb-panel-footer">
                            <button class="umb-button umb-button--primary" id="createFormBtn">
                                <span class="umb-button__content">Create</span>
                            </button>
                            <button class="umb-button umb-button--secondary" id="cancelFormBtn">
                                <span class="umb-button__content">Cancel</span>
                            </button>
                        </div>
                    </div>
                </div>

                <div class="loading-indicator" style="display: none;">
                    <div class="umb-loader"></div>
                    <p>Loading forms...</p>
                </div>

                <style>
                    .umb-dashboard {
                        background: #f6f6f7;
                        min-height: 100vh;
                    }

                    .umb-panel-header {
                        background: white;
                        padding: 20px 30px;
                        margin-bottom: 20px;
                        border-bottom: 1px solid #e9e9eb;
                    }

                    .umb-panel-header-content {
                        display: flex;
                        align-items: center;
                        gap: 20px;
                    }

                    .umb-panel-header-icon {
                        font-size: 40px;
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

                    .ml2 {
                        margin-right: 10px;
                    }

                    .mr1 {
                        margin-right: 8px;
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
                        margin-left: 10px;
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
                        grid-template-columns: minmax(300px, 2fr) minmax(150px, 1fr) 120px;
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
                        text-align: center;
                        padding: 40px;
                        color: #666;
                        font-size: 15px;
                        background: #f8f9fa;
                        border-radius: 3px;
                    }

                    .form-editor-container {
                        background: #f6f6f7;
                        min-height: 100vh;
                    }

                    .umb-panel-header-actions {
                        margin-left: auto;
                    }

                    .form-editor-frame {
                        
                        height: calc(100vh - 100px);
                    }

                    .form-editor-iframe {
                        width: 100%;
                        height: 100%;
                        background: white;
                        border-radius: 3px;
                        box-shadow: 0 1px 3px rgba(0,0,0,0.1);
                    }

                    .umb-modal {
                        display: none;
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
                        padding: 20px;
                        border-radius: 4px;
                        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
                        width: 100%;
                        max-width: 500px;
                        position: relative;
                        margin: 0 20px;
                    }

                    .umb-panel-header h2 {
                        margin: 0 0 20px 0;
                        font-size: 20px;
                        color: #1b264f;
                    }

                    .form-group {
                        margin-bottom: 20px;
                    }

                    .form-group label {
                        display: block;
                        margin-bottom: 8px;
                        font-weight: 600;
                        color: #1b264f;
                    }

                    .form-control {
                        width: 100%;
                        padding: 8px 12px;
                        border: 1px solid #e9e9eb;
                        border-radius: 3px;
                        font-size: 14px;
                    }

                    .umb-panel-footer {
                        display: flex;
                        justify-content: flex-end;
                        gap: 10px;
                        margin-top: 20px;
                        padding-top: 20px;
                        border-top: 1px solid #e9e9eb;
                    }

                    .server-error-message {
                        display: none;
                        color: #dc3545;
                        font-size: 12px;
                        margin-top: 8px;
                        padding: 8px;
                        background-color: #fff;
                        border: 1px solid #dc3545;
                        border-radius: 3px;
                        transition: all 0.3s ease;
                    }

                    .form-control.has-error {
                        border-color: #dc3545;
                    }

                    .umb-table-body .umb-table-row:hover {
                        background-color: #f8f9fa;
                    }

                    .umb-table-cell {
                        padding: 12px 20px;
                        font-size: 14px;
                        color: #1b264f;
                    }

                    .umb-table-head .umb-table-cell {
                        font-weight: 700;
                        color: #1b264f;
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

                    .loading-indicator {
                        position: absolute;
                        top: 50%;
                        left: 50%;
                        transform: translate(-50%, -50%);
                        text-align: center;
                    }

                    .umb-loader {
                        border: 8px solid #f3f3f3;
                        border-top: 8px solid #3498db;
                        border-radius: 50%;
                        width: 60px;
                        height: 60px;
                        animation: spin 2s linear infinite;
                    }

                    @keyframes spin {
                        0% {
                            transform: rotate(0deg);
                        }
                        100% {
                            transform: rotate(360deg);
                        }
                    }
                </style>
            `;

        this.innerHTML = gridHtml;
    }

    updatePagination() {
        // Update top pagination
        const prevBtn = this.querySelector('#prevBtn');
        const nextBtn = this.querySelector('#nextBtn');
        const totalItemsSpan = this.querySelector('.umb-pagination__text');
        
        // Update bottom pagination
        const prevBtnBottom = this.querySelector('#prevBtnBottom');
        const nextBtnBottom = this.querySelector('#nextBtnBottom');
        const paginationText = this.querySelector('.umb-table-pagination__text');
        
        if (!prevBtn || !nextBtn || !totalItemsSpan) {
            return;
        }
        
        if (!prevBtnBottom || !nextBtnBottom || !paginationText) {
            return;
        }
        
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

    async connectedCallback() {
        await this.loadForms();
        this.attachEventListeners();
        // Show create form modal on page load
       
    }

    attachEventListeners() {
        this.addEventListener('click', (event) => {
            const target = event.target.closest('button');
            if (!target) return;

            // Handle pagination buttons
            if (target.id === 'prevBtn' || target.id === 'prevBtnBottom') {
                if (!target.disabled && this.currentPage > 1) {
                    this.currentPage--;
                    this.loadForms();
                }
                return;
            }
            
            if (target.id === 'nextBtn' || target.id === 'nextBtnBottom') {
                if (!target.disabled && this.currentPage < this.totalPages) {
                    this.currentPage++;
                    this.loadForms();
                }
                return;
            }

            switch (target.id) {
                case 'createBtn':
                    this.showCreateFormModal();
                    break;
                case 'refreshBtn':
                    this.loadForms();
                    break;
                case 'backToList':
                    this.hideFormEditor();
                    break;
                case 'createFormBtn':
                    this.saveNewForm();
                    break;
                case 'cancelFormBtn':
                    this.hideCreateFormModal();
                    break;
            }

            if (target.classList.contains('edit-form-btn')) {
                const formId = target.getAttribute('data-id');
                const formRow = target.closest('.umb-table-row');
                const formName = formRow.querySelector('.cell-name').textContent;
                this.showFormEditor(formId, formName);
            }
        });
    }

    async showFormEditor(formId, formName) {
        this.currentFormId = formId;
        
        const formList = this.querySelector('.form-list-container');
        const formEditor = this.querySelector('.form-editor-container');
        const iframe = this.querySelector('.form-editor-iframe');
        
        // Update the editor header with form name
        const editorHeader = formEditor.querySelector('.umb-panel-header-name');
        editorHeader.textContent = `Editing: ${formName}`;
        
        formList.style.display = 'none';
        formEditor.style.display = 'block';
        iframe.src = `/App_Plugins/ArroSimpleForm/formbuilder.html?id=${formId}`;
    }

    hideFormEditor() {
        this.currentFormId = null;
        const formList = this.querySelector('.form-list-container');
        const formEditor = this.querySelector('.form-editor-container');
        const iframe = this.querySelector('.form-editor-iframe');
        
        formList.style.display = 'block';
        formEditor.style.display = 'none';
        iframe.src = '';
    }

    showCreateFormModal() {
        const modal = this.querySelector('#createFormModal');
        if (modal) {
            modal.style.display = 'flex';
            const formNameInput = modal.querySelector('#formName');
            const errorMessage = modal.querySelector('.error-message');
            
            if (formNameInput) {
                formNameInput.value = '';
                errorMessage.style.display = 'none';
                formNameInput.focus();
                
                formNameInput.addEventListener('input', (e) => {
                    const pattern = /^[A-Za-z0-9 ]+$/;
                    
                    if (!pattern.test(formNameInput.value) && formNameInput.value !== '') {
                        errorMessage.textContent = 'Special characters are not allowed. Use only letters, numbers and spaces.';
                        errorMessage.style.display = 'block';
                        formNameInput.classList.add('invalid');
                    } else {
                        errorMessage.style.display = 'none';
                        formNameInput.classList.remove('invalid');
                    }
                });
            }
        }
    }

    hideCreateFormModal() {
        const modal = this.querySelector('#createFormModal');
        if (modal) {
            modal.style.display = 'none';
            const formNameInput = modal.querySelector('#formName');
            if (formNameInput) {
                formNameInput.value = '';
            }
        }
    }

    async saveNewForm() {
        const formNameInput = this.querySelector('#formName');
        const formName = formNameInput ? formNameInput.value.trim() : '';
        const errorMessage = this.querySelector('.error-message');
        
        if (errorMessage) {
            errorMessage.style.display = 'none';
            errorMessage.textContent = '';
        }

        if (!formName) {
            if (errorMessage) {
                errorMessage.textContent = 'Please enter a form name';
                errorMessage.style.display = 'block';
                formNameInput.classList.add('invalid');
            }
            return;
        }

        const pattern = /^[A-Za-z0-9 ]+$/;
        if (!pattern.test(formName)) {
            if (errorMessage) {
                errorMessage.textContent = 'Form name cannot contain special characters. Use only letters, numbers and spaces.';
                errorMessage.style.display = 'block';
                formNameInput.classList.add('invalid');
            }
            return;
        }

        try {
            const userData = {
                name: formName,
                userId: window.Umbraco?.currentUser?.id || '-1'
            };

            const response = await fetch('/umbraco/Simpleform/api/createform', {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin', // This is important for Umbraco backoffice authentication
                body: JSON.stringify(userData)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            if (result.iscreated) {
                this.hideCreateFormModal();
                await this.loadForms(); 
                await this.showFormEditor(result.formid, formName);
            }
          else {
                if (errorMessage) {
                    errorMessage.textContent = result.msg;
                    errorMessage.style.display = 'block';
                    formNameInput.classList.add('invalid');
                }
                return;
            }

            // If successful, hide the modal and refresh the list
           
            
        } catch (error) {
            if (errorMessage) {
                errorMessage.textContent = 'Failed to create form: ' + error.message;
                errorMessage.style.display = 'block';
                formNameInput.classList.add('invalid');
            }
        }
    }

    showLoading(isLoading) {
        const loadingIndicator = this.querySelector('.loading-indicator');
        if (loadingIndicator) {
            if (isLoading) {
                loadingIndicator.style.display = 'block';
            } else {
                loadingIndicator.style.display = 'none';
            }
        }
    }
}

if (!customElements.get('arro-forms')) {
    customElements.define('arro-forms', ArroFormsElement);
}

export const element = ArroFormsElement;