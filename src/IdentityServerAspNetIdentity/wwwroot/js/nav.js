// nav.js: moved from _Nav.cshtml

const userMenuButton = document.getElementById('menu-button');
const userMenu = document.getElementById('menu');
const themeMenuButton = document.getElementById('theme-button');
const themeMenu = document.getElementById('theme-menu-list');

function closeAllMenus() {
    userMenuButton.setAttribute('aria-expanded', 'false');
    userMenuButton.classList.remove('open');
    themeMenuButton.setAttribute('aria-expanded', 'false');
    themeMenuButton.classList.remove('open');
}

if (userMenuButton) {
    userMenuButton.addEventListener('click', (e) => {
        e.stopPropagation();
        const isExpanded = userMenuButton.getAttribute('aria-expanded') === 'true';
        if (isExpanded) {
            closeAllMenus();
        } else {
            closeAllMenus();
            userMenuButton.setAttribute('aria-expanded', 'true');
            userMenuButton.classList.add('open');
        }
    });
}

if (themeMenuButton) {
    themeMenuButton.addEventListener('click', (e) => {
        e.stopPropagation();
        const isExpanded = themeMenuButton.getAttribute('aria-expanded') === 'true';
        if (isExpanded) {
            closeAllMenus();
        } else {
            closeAllMenus();
            themeMenuButton.setAttribute('aria-expanded', 'true');
            themeMenuButton.classList.add('open');
        }
    });
}

if (themeMenu) {
    const themeLinks = themeMenu.querySelectorAll('a');

    function applyTheme(theme) {
        if (theme === 'light') {
            document.documentElement.removeAttribute('data-theme');
        } else {
            document.documentElement.setAttribute('data-theme', theme);
        }
        localStorage.setItem('theme', theme);
    }

    themeLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const theme = e.target.dataset.theme;
            applyTheme(theme);
            closeAllMenus();
        });
    });

    document.addEventListener('DOMContentLoaded', () => {
        const savedTheme = localStorage.getItem('theme') || 'light';
        applyTheme(savedTheme);
    });
}

document.addEventListener('click', (e) => {
    if (!userMenu.contains(e.target) && !userMenuButton.contains(e.target) &&
        !themeMenu.contains(e.target) && !themeMenuButton.contains(e.target)) {
        closeAllMenus();
    }
});
