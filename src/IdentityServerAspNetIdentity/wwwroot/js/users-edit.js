import { confirmAction } from '/js/confirmModal.js';

function submitWithButton(form, button) {
    if (typeof form.requestSubmit === 'function') {
        form.requestSubmit(button);
        return;
    }

    form.submit();
}

function setupConfirm(buttonId, title, message) {
    const btn = document.getElementById(buttonId);
    if (!btn) {
        return;
    }

    const form = btn.closest('form');
    if (!form) {
        return;
    }

    btn.addEventListener('click', (event) => {
        event.preventDefault();
        confirmAction(title, message, () => submitWithButton(form, btn));
    });
}

const selectAllClaims = document.getElementById('select-all-claims');
if (selectAllClaims) {
    selectAllClaims.addEventListener('change', () => {
        document.querySelectorAll('.claim-checkbox').forEach((cb) => {
            cb.checked = selectAllClaims.checked;
        });
    });
}

const removeClaimsBtn = document.getElementById('remove-selected-btn');
if (removeClaimsBtn) {
    const removeClaimsForm = removeClaimsBtn.closest('form');
    if (removeClaimsForm) {
        removeClaimsBtn.addEventListener('click', (event) => {
            const count = document.querySelectorAll('.claim-checkbox:checked').length;
            if (count === 0) {
                event.preventDefault();
                return;
            }

            event.preventDefault();
            confirmAction('Remove Claims', `Remove ${count} claim(s) from this user?`, () => submitWithButton(removeClaimsForm, removeClaimsBtn));
        });
    }
}

const addClaimModal = document.getElementById('add-claim-modal');
const openAddClaimBtn = document.getElementById('open-add-claim-modal-btn');
const cancelAddClaimBtn = document.getElementById('cancel-add-claim-btn');
const closeAddClaimBtn = document.getElementById('close-add-claim-btn');

if (openAddClaimBtn && addClaimModal) {
    openAddClaimBtn.addEventListener('click', () => addClaimModal.showModal());
    cancelAddClaimBtn.addEventListener('click', () => addClaimModal.close());
    if (closeAddClaimBtn) {
        closeAddClaimBtn.addEventListener('click', () => addClaimModal.close());
    }
    addClaimModal.addEventListener('click', (e) => { if (e.target === addClaimModal) addClaimModal.close(); });
}

const editClaimModal = document.getElementById('edit-claim-modal');
const cancelReplaceBtn = document.getElementById('cancel-replace-btn');
const closeEditClaimBtn = document.getElementById('close-edit-claim-btn');

document.querySelectorAll('.edit-claim-btn').forEach((btn) => {
    btn.addEventListener('click', () => {
        if (!editClaimModal) return;
        document.getElementById('OldClaimType').value = btn.dataset.claimType;
        document.getElementById('OldClaimValue').value = btn.dataset.claimValue;
        document.getElementById('ReplacementClaimType').value = btn.dataset.claimType;
        document.getElementById('ReplacementClaimValue').value = btn.dataset.claimValue;
        editClaimModal.showModal();
    });
});

if (cancelReplaceBtn && editClaimModal) {
    cancelReplaceBtn.addEventListener('click', () => editClaimModal.close());
    if (closeEditClaimBtn) {
        closeEditClaimBtn.addEventListener('click', () => editClaimModal.close());
    }
    editClaimModal.addEventListener('click', (e) => { if (e.target === editClaimModal) editClaimModal.close(); });
}

// Auto-open edit modal if server returned with pre-filled replacement values
if (editClaimModal) {
    const oldType = document.getElementById('OldClaimType');
    if (oldType && oldType.value) {
        editClaimModal.showModal();
    }
}

const selectAllAssignedRoles = document.getElementById('select-all-assigned-roles');
if (selectAllAssignedRoles) {
    selectAllAssignedRoles.addEventListener('change', () => {
        document.querySelectorAll('.role-checkbox').forEach((cb) => {
            cb.checked = selectAllAssignedRoles.checked;
        });
    });
}

const selectAllAvailableRoles = document.getElementById('select-all-available-roles');
if (selectAllAvailableRoles) {
    selectAllAvailableRoles.addEventListener('change', () => {
        document.querySelectorAll('#available-roles-list .option-input:not(#select-all-available-roles)').forEach((cb) => {
            cb.checked = selectAllAvailableRoles.checked;
        });
    });
}

const removeRolesBtn = document.getElementById('remove-roles-btn');
if (removeRolesBtn) {
    const removeRolesForm = removeRolesBtn.closest('form');
    if (removeRolesForm) {
        removeRolesBtn.addEventListener('click', (event) => {
            const count = document.querySelectorAll('.role-checkbox:checked').length;
            if (count === 0) {
                event.preventDefault();
                return;
            }

            event.preventDefault();
            confirmAction('Remove Roles', `Remove this user from ${count} role(s)?`, () => submitWithButton(removeRolesForm, removeRolesBtn));
        });
    }
}

setupConfirm('delete-user-button', 'Delete user', 'This action cannot be undone.');
setupConfirm('reset-password-btn', 'Reset Password', 'This will replace the user\'s current password. Continue?');
setupConfirm('disable-account-btn', 'Disable Account', 'This will disable the user account. Continue?');
setupConfirm('force-signout-btn', 'Force Sign-Out', 'This will invalidate all active sessions for this user, forcing them to sign in again. Continue?');
setupConfirm('reset-authenticator-btn', 'Reset Authenticator', 'This will invalidate the user\'s current authenticator app. They will need to re-enrol. Continue?');
