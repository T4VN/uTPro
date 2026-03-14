class ArroNewFormElement extends HTMLElement {
    constructor() {
        super();
        this.formId = null;
    }

    connectedCallback() {
        // Get the form ID from the URL if it exists
        const urlParams = new URLSearchParams(window.location.search);
        this.formId = urlParams.get('id');
        
        const formBuilderUrl = this.formId 
            ? `/App_Plugins/ArroSimpleForm/formbuilder.html?id=${this.formId}`
            : '/App_Plugins/ArroSimpleForm/formbuilder.html';

        this.innerHTML = `
            <div class="content">
                <iframe src="${formBuilderUrl}"
                        frameborder="0" 
                        style="width: 100%; height: 800px; border: none;">
                </iframe>
            </div>
            <style>
                .content {
                    padding: 20px;
                    background: white;
                    border-radius: 4px;
                    box-shadow: 0 1px 3px rgba(0,0,0,0.12);
                }
            </style>
        `;
    }
}

if (!customElements.get('arro-new-form')) {
    customElements.define('arro-new-form', ArroNewFormElement);
}

export default ArroNewFormElement;