// Navbar Theme Switcher JavaScript
(function() {
    'use strict';

    // Theme configuration with icons and labels
    const themes = {
        'light': { label: 'Light', icon: '☀️' },
        'dark': { label: 'Dark', icon: '🌙' },
        'blue': { label: 'Blue', icon: '💙' },
        'green': { label: 'Green', icon: '💚' },
        'purple': { label: 'Purple', icon: '💜' }
    };

    // Get current theme from localStorage or default to 'light'
    function getCurrentTheme() {
        return localStorage.getItem('theme') || 'light';
    }

    // Set theme
    function setTheme(themeName) {
        if (themeName === 'light') {
            document.documentElement.removeAttribute('data-theme');
        } else {
            document.documentElement.setAttribute('data-theme', themeName);
        }
        localStorage.setItem('theme', themeName);
        updateActiveThemeButton(themeName);
        updateCurrentThemeDisplay(themeName);
    }

    // Update active theme button
    function updateActiveThemeButton(currentTheme) {
        const themeButtons = document.querySelectorAll('.theme-option');
        themeButtons.forEach(button => {
            if (button.dataset.theme === currentTheme) {
                button.classList.add('active');
            } else {
                button.classList.remove('active');
            }
        });
    }

    // Update current theme display in navbar
    function updateCurrentThemeDisplay(themeName) {
        const currentThemeElement = document.getElementById('current-theme-name');
        if (currentThemeElement && themes[themeName]) {
            currentThemeElement.textContent = themes[themeName].label;
        }
    }

    // Initialize navbar theme dropdown
    function initNavbarThemeDropdown() {
        const themeOptions = document.querySelectorAll('.theme-option');
        
        // Add click handlers to theme options
        themeOptions.forEach(option => {
            option.addEventListener('click', function(e) {
                e.preventDefault();
                const themeName = this.dataset.theme;
                setTheme(themeName);
                
                // Close dropdown after selection
                const dropdown = document.querySelector('.theme-dropdown');
                if (dropdown) {
                    dropdown.classList.remove('show');
                }
            });
        });

        // Handle dropdown toggle
        const themeToggle = document.getElementById('theme-toggle');
        if (themeToggle) {
            themeToggle.addEventListener('click', function(e) {
                e.preventDefault();
                const dropdown = document.getElementById('theme-dropdown');
                if (dropdown) {
                    // Close other dropdowns first
                    document.querySelectorAll('.dropdown-content').forEach(dd => {
                        if (dd !== dropdown) {
                            dd.parentElement.classList.remove('show');
                        }
                    });
                    
                    // Toggle this dropdown
                    dropdown.parentElement.classList.toggle('show');
                }
            });
        }
    }

    // Close dropdowns when clicking outside
    function handleOutsideClick(event) {
        const dropdowns = document.querySelectorAll('.nav-dropdown');
        dropdowns.forEach(dropdown => {
            if (!dropdown.contains(event.target)) {
                dropdown.classList.remove('show');
            }
        });
    }

    // Initialize theme system
    function initThemeSystem() {
        // Apply saved theme
        const currentTheme = getCurrentTheme();
        setTheme(currentTheme);

        // Initialize navbar dropdown
        initNavbarThemeDropdown();

        // Update active button and display
        updateActiveThemeButton(currentTheme);
        updateCurrentThemeDisplay(currentTheme);

        // Add outside click listener
        document.addEventListener('click', handleOutsideClick);

        // Add keyboard support
        document.addEventListener('keydown', function(event) {
            if (event.key === 'Escape') {
                document.querySelectorAll('.nav-dropdown').forEach(dropdown => {
                    dropdown.classList.remove('show');
                });
            }
        });
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initThemeSystem);
    } else {
        initThemeSystem();
    }

    // Expose theme functions globally for potential external use
    window.ThemeSwitcher = {
        setTheme: setTheme,
        getCurrentTheme: getCurrentTheme,
        themes: themes
    };
})();