class ArroSimpleFormDashboardElement extends HTMLElement {
    constructor() {
        super();
        this.stats = {
            totalForms: 0,
            totalSubmissions: 0
        };
    }

    async fetchStatistics() {
        try {
            const response = await fetch('/umbraco/Simpleform/api/GetStatistics');
            if (!response.ok) {
                throw new Error('Failed to fetch statistics');
            }
            const data = await response.json();
            this.stats.totalForms = data.totalForms;
            this.stats.totalSubmissions = data.totalFormCollections;
            this.updateStatistics();
        } catch (error) {
            console.error('Error fetching statistics:', error);
        }
    }

    updateStatistics() {
        const totalFormsElement = this.querySelector('#totalForms');
        const totalSubmissionsElement = this.querySelector('#totalSubmissions');
        
        if (totalFormsElement) {
            totalFormsElement.textContent = this.stats.totalForms;
        }
        if (totalSubmissionsElement) {
            totalSubmissionsElement.textContent = this.stats.totalSubmissions;
        }
    }

 

    connectedCallback() {
        this.innerHTML = `
            <div class="umb-dashboard">
                <div class="umb-panel-header">
                    <div class="umb-panel-header-content">
                        <div class="umb-panel-header-icon">
                            <umb-icon name="icon-dashboard" class="color-blue"></umb-icon>
                        </div>
                        <div>
                            <h1 class="umb-panel-header-name">Simple Form Dashboard</h1>
                            <p class="umb-panel-header-description">Overview of forms and submissions</p>
                        </div>
                    </div>
                </div>
                
                <div class="umb-panel-body">
                    <div class="stats-container">
                        <h2 class="stats-header">Statistics Overview</h2>
                        <div class="stats-grid">
                        
                            <a href="/umbraco/section/ArroSimpleForm/view/arroforms" class="stat-box clickable" id="totalFormsCard">
                                <div class="stat-icon">
                                    <umb-icon name="icon-document" class="color-blue"></umb-icon>
                                </div>
                                <div class="stat-number" id="totalForms">0</div>
                                <div class="stat-label">Total Forms</div>
                            </a>
                            <a href="/umbraco/section/ArroSimpleForm/view/reports" class="stat-box clickable" id="totalSubmissionsCard">
                                <div class="stat-icon">
                                    <umb-icon name="icon-message" class="color-green"></umb-icon>
                                </div>
                                <div class="stat-number" id="totalSubmissions">0</div>
                                <div class="stat-label">Total Submissions</div>
                            </a>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // No event listeners are needed as we're using anchor tags with href

        // Fetch statistics when the component is mounted
        this.fetchStatistics();

        // Add styles
        const style = document.createElement('style');
        style.textContent = `
            .umb-dashboard {
                padding: 0;
                background: #f6f6f7;
                min-height: 100vh;
            }

            .umb-panel-header {
                background: white;
                padding: 20px 30px;
                margin-bottom: 30px;
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

            .stats-container {
                background: white;
                border-radius: 6px;
                padding: 25px;
                box-shadow: 0 1px 3px rgba(0,0,0,0.1);
            }

            .stats-header {
                margin: 0 0 20px;
                font-size: 18px;
                font-weight: 600;
                color: #1b264f;
            }

            .stats-grid {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
                gap: 25px;
            }

            .stat-box {
                background: white;
                border-radius: 5px;
                padding: 20px;
                box-shadow: 0 1px 3px rgba(0,0,0,0.12);
                text-align: center;
                transition: all 0.2s ease;
                cursor: pointer;
            }
            
            a.stat-box {
                display: block;
                text-decoration: none;
                color: inherit;
            }
            
            .stat-box:hover {
                box-shadow: 0 3px 6px rgba(0,0,0,0.16);
                transform: translateY(-2px);
            }

            .stat-icon {
                font-size: 28px;
                margin-bottom: 15px;
                display: flex;
                justify-content: center;
                align-items: center;
            }
            
            .stat-icon umb-icon {
                font-size: 32px;
            }

            .stat-number {
                font-size: 36px;
                font-weight: 700;
                color: #1b264f;
                margin-bottom: 8px;
            }

            .stat-label {
                font-size: 14px;
                color: #666;
                font-weight: 500;
            }

            .color-blue {
                color: #2152A3;
            }

            .color-green {
                color: #2bc37c;
            }

            .icon {
                display: inline-block;
                text-align: center;
                line-height: 1;
            }
            
            .icon::before {
                font-family: 'icomoon';
                font-style: normal;
                font-weight: normal;
                speak: none;
                display: inline-block;
                text-decoration: inherit;
                width: 1em;
                text-align: center;
                font-variant: normal;
                text-transform: none;
                line-height: 1;
            }
            
            .icon-dashboard::before {
                content: "\\e1e5";
            }
            
            .icon-document::before {
                content: "\\e160";
            }
            
            .icon-message::before {
                content: "\\e1e0";
            }
            
            .icon-list::before {
                content: "\\e116";
            }
            
            .icon-inbox::before {
                content: "\\e028";
            }
        `;
        this.appendChild(style);
        
        // Register custom elements
        if (!customElements.get('umb-icon')) {
            customElements.define('umb-icon', class extends HTMLElement {
                constructor() {
                    super();
                    this.attachShadow({ mode: 'open' });
                }
                
                connectedCallback() {
                    const icon = this.getAttribute('name');
                    this.shadowRoot.innerHTML = `
                        <style>
                            :host {
                                display: inline-block;
                            }
                            .icon {
                                font-family: 'icomoon';
                                font-style: normal;
                                font-weight: normal;
                                font-size: inherit;
                                text-rendering: auto;
                                -webkit-font-smoothing: antialiased;
                                -moz-osx-font-smoothing: grayscale;
                            }
                            .color-blue {
                                color: #2152A3;
                            }
                            .color-green {
                                color: #2bc37c;
                            }
                        </style>
                        <span class="icon ${icon} ${this.getAttribute('class') || ''}"></span>
                    `;
                }
            });
        }
    }
}

if (!customElements.get('arro-simple-form-dashboard')) {
    customElements.define('arro-simple-form-dashboard', ArroSimpleFormDashboardElement);
}

export default ArroSimpleFormDashboardElement;