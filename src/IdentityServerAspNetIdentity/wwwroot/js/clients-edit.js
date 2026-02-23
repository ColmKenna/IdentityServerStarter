(() => {
  function initEditArray(elementId) {
    const el = document.getElementById(elementId);
    if (!el) return;
    try {
      const raw = el.getAttribute('data-initial-json');
      if (!raw) return;
      let data;
      try {
        data = JSON.parse(raw);
      } catch (err) {
        console.error(`clients-edit: invalid data-initial-json for ${elementId}`, err);
        return;
      }
      el.data = Array.isArray(data) ? data : [];
    } catch (e) {
      console.error(`clients-edit: failed to initialize ${elementId}`, e);
    }
  }

  function ready(fn) {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', fn, { once: true });
    } else {
      fn();
    }
  }

  ready(async () => {
    try {
      if (window.customElements && customElements.whenDefined) {
        await customElements.whenDefined('ck-edit-array');
      }
    } catch (_) {
      // ignore; proceed anyway
    }
    initEditArray('redirectUrisEditor');
    initEditArray('postLogoutRedirectUrisEditor');
  });
})();

