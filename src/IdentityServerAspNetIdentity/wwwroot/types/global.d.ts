// Global type declarations for ck-tabs web components

import type { CKTab, CKTabs } from '../lib/@colmkenna/ck-tabs/dist/components/ck-tabs/ck-tabs';

declare global {
  // Add the custom elements to the HTMLElementTagNameMap for proper IntelliSense
  interface HTMLElementTagNameMap {
    'ck-tab': CKTab;
    'ck-tabs': CKTabs;
  }

  // For HTML element creation via document.createElement
  interface HTMLElementEventMap {
    // Add custom events here if needed
  }

  // For JSX/TSX support (if you use React/similar)
  namespace JSX {
    interface IntrinsicElements {
      'ck-tab': React.DetailedHTMLProps<React.HTMLAttributes<CKTab>, CKTab> & {
        label?: string;
        active?: boolean;
      };
      'ck-tabs': React.DetailedHTMLProps<React.HTMLAttributes<CKTabs>, CKTabs>;
    }
  }
}

// Make this file a module
export {};