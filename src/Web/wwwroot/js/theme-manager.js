/**
 * Theme Manager for Tailwind CSS Color Themes
 * Handles switching between 8 theme combinations:
 * 4 colors (Red, Blue, Green, Yellow) × 2 brightness (Light/Pastel, Dark/Rich)
 */
class ThemeManager {
  static THEMES = {
    RED_LIGHT: 'theme-red-light',
    RED_DARK: 'theme-red-dark',
    BLUE_LIGHT: 'theme-blue-light',
    BLUE_DARK: 'theme-blue-dark',
    GREEN_LIGHT: 'theme-green-light',
    GREEN_DARK: 'theme-green-dark',
    YELLOW_LIGHT: 'theme-yellow-light',
    YELLOW_DARK: 'theme-yellow-dark'
  };
  
  static COLOR_FAMILIES = {
    RED: ['theme-red-light', 'theme-red-dark'],
    BLUE: ['theme-blue-light', 'theme-blue-dark'],
    GREEN: ['theme-green-light', 'theme-green-dark'],
    YELLOW: ['theme-yellow-light', 'theme-yellow-dark']
  };
  
  static BRIGHTNESS_OPTIONS = {
    LIGHT: 'light',
    DARK: 'dark'
  };

  static STORAGE_KEY = 'tailwind-color-theme';
  static DEFAULT_THEME = this.THEMES.BLUE_LIGHT;

  /**
   * Initialize theme manager and apply saved or default theme
   */
  static initialize() {
    // Theme classes are already applied in App.razor to prevent FOUC,
    // but we re-apply here to ensure the ThemeManager state is consistent
    // and to handle any edge cases.
    const savedTheme = localStorage.getItem(this.STORAGE_KEY) || this.DEFAULT_THEME;
    this.setTheme(savedTheme);
  }

  /**
   * Set the active theme by adding class to <html> element
   * @param {string} themeName - Theme name (must be in THEMES object values)
   */
  static setTheme(themeName) {
    if (!Object.values(this.THEMES).includes(themeName)) {
        themeName = this.DEFAULT_THEME;
    }

    // Remove all theme classes
    Object.values(this.THEMES).forEach(theme => {
      document.documentElement.classList.remove(theme);
    });

    // Add the selected theme class
    document.documentElement.classList.add(themeName);
    localStorage.setItem(this.STORAGE_KEY, themeName);
    
    // Also sync the 'theme' key used by ThemeToggle.razor
    const isDark = themeName.includes('dark');
    const brightness = isDark ? 'dark' : 'light';
    localStorage.setItem('theme', brightness);
    
    if (isDark) {
        document.documentElement.classList.add('dark');
        document.documentElement.classList.remove('light');
    } else {
        document.documentElement.classList.remove('dark');
        document.documentElement.classList.add('light');
    }
  }

  /**
   * Set brightness for a specific color family
   * @param {string} colorFamily - RED, BLUE, GREEN, or YELLOW
   * @param {string} brightness - 'light' or 'dark'
   */
  static setBrightness(colorFamily, brightness) {
    const themes = this.COLOR_FAMILIES[colorFamily];
    if (!themes) return;
    
    const themeName = brightness === this.BRIGHTNESS_OPTIONS.DARK 
      ? themes[1] 
      : themes[0];
    
    this.setTheme(themeName);
  }

  /**
   * Set color while preserving current brightness
   * @param {string} colorFamily - RED, BLUE, GREEN, or YELLOW
   */
  static setColor(colorFamily) {
    const currentBrightness = this.getCurrentBrightness();
    this.setBrightness(colorFamily, currentBrightness);
  }

  /**
   * Get the currently active theme
   * @returns {string} Current theme name
   */
  static getCurrentTheme() {
    return Object.values(this.THEMES).find(theme =>
      document.documentElement.classList.contains(theme)
    ) || this.DEFAULT_THEME;
  }

  /**
   * Get current color (without brightness)
   * @returns {string} Color family name
   */
  static getCurrentColor() {
    const current = this.getCurrentTheme();
    for (const [family, themes] of Object.entries(this.COLOR_FAMILIES)) {
      if (themes.includes(current)) return family;
    }
    return 'BLUE';
  }

  /**
   * Get current brightness
   * @returns {string} 'light' or 'dark'
   */
  static getCurrentBrightness() {
    const current = this.getCurrentTheme();
    return current.includes('light') ? this.BRIGHTNESS_OPTIONS.LIGHT : this.BRIGHTNESS_OPTIONS.DARK;
  }

  /**
   * Get all available themes
   * @returns {Object} Themes object
   */
  static getAvailableThemes() {
    return this.THEMES;
  }

  /**
   * Get themes for a specific color family
   * @param {string} colorFamily - RED, BLUE, GREEN, or YELLOW
   * @returns {Array} Array of light and dark theme names
   */
  static getColorFamilyThemes(colorFamily) {
    return this.COLOR_FAMILIES[colorFamily] || [];
  }
  
  /**
   * Sync UI with current theme - updates button states and display
   */
  static syncUI() {
    const currentTheme = this.getCurrentTheme();
    const color = this.getCurrentColor();
    const brightness = this.getCurrentBrightness();
    
    // Update color buttons
    document.querySelectorAll('.color-btn').forEach(btn => {
      btn.classList.toggle('active', btn.textContent.toUpperCase() === color);
    });
    
    // Update brightness buttons
    document.querySelectorAll('.brightness-btn').forEach(btn => {
      btn.classList.toggle('active', btn.textContent.toLowerCase().includes(brightness));
    });
    
    // Update current theme display
    const themeDisplay = document.getElementById('current-theme');
    if (themeDisplay) {
      themeDisplay.textContent = color + ' ' + brightness.charAt(0).toUpperCase() + brightness.slice(1);
    }
  }
  
  /**
   * Select a color and update UI
   * @param {string} color - Color family name (RED, BLUE, GREEN, or YELLOW)
   */
  static selectColorAndUpdateUI(color) {
    this.setColor(color);
    this.syncUI();
  }
  
  /**
   * Select brightness and update UI
   * @param {string} brightness - 'light' or 'dark'
   */
  static selectBrightnessAndUpdateUI(brightness) {
    const color = this.getCurrentColor();
    this.setBrightness(color, brightness);
    this.syncUI();
  }
}

// Initialize theme on page load
document.addEventListener('DOMContentLoaded', () => {
  ThemeManager.initialize();
});

window.ThemeManager = ThemeManager;