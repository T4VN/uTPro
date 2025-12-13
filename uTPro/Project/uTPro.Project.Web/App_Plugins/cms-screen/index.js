// Add stylesheet
const styleLink = document.createElement('link');
styleLink.rel = 'stylesheet';
styleLink.href = '/App_Plugins/cms-screen/style.css';
document.head.appendChild(styleLink);

// Add icon
const iconLink = document.createElement('link');
iconLink.rel = 'icon';
iconLink.type = 'image/svg+xml';
iconLink.href = '/assets/img/logo.svg';
document.head.appendChild(iconLink);
