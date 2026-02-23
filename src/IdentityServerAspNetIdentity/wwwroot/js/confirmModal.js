/**
 * confirmModal.js
 *
 * Provides a single helper function to open the shared confirmation dialog.
 *
 * Usage:
 *   import { confirmAction } from '/js/confirmModal.js';
 *
 *   // Pass a <form> element — it will be submitted on confirm:
 *   confirmAction('Delete user', 'This action cannot be undone.', myForm);
 *
 *   // Or pass a callback:
 *   confirmAction('Revoke grant', 'Are you sure?', () => doRevoke(id));
 */

const modal   = /** @type {HTMLDialogElement} */ (document.getElementById('confirm-modal'));
const titleEl = document.getElementById('confirm-modal-title');
const msgEl   = document.getElementById('confirm-modal-message');
const cancelBtn  = document.getElementById('confirm-modal-cancel');
const confirmBtn = document.getElementById('confirm-modal-confirm');

let _pendingAction = null;

function closeModal() {
    modal.close();
    _pendingAction = null;
}

cancelBtn.addEventListener('click', closeModal);

// Close on backdrop click (click outside the dialog content)
modal.addEventListener('click', (e) => {
    if (e.target === modal) closeModal();
});

// Close on Escape (native <dialog> behaviour already handles this,
// but we also clear the pending action)
modal.addEventListener('cancel', () => {
    _pendingAction = null;
});

confirmBtn.addEventListener('click', () => {
    modal.close();
    if (_pendingAction instanceof HTMLFormElement) {
        _pendingAction.submit();
    } else if (typeof _pendingAction === 'function') {
        _pendingAction();
    }
    _pendingAction = null;
});

/**
 * Open the confirmation dialog.
 *
 * @param {string} title   - Short heading shown in the dialog.
 * @param {string} message - Longer description / warning text.
 * @param {HTMLFormElement|Function} action
 *   - If a <form> element: the form is submitted when the user confirms.
 *   - If a function:       the function is called when the user confirms.
 */
export function confirmAction(title, message, action) {
    titleEl.textContent = title;
    msgEl.textContent   = message;
    _pendingAction = action;
    modal.showModal();
}
