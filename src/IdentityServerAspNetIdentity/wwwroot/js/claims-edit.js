import { confirmAction } from '/js/confirmModal.js';

function submitWithButton(form, button) {
    if (typeof form.requestSubmit === 'function') {
        form.requestSubmit(button);
        return;
    }

    form.submit();
}

function buildRemoveMessage(button) {
    const userName = (button.dataset.userName ?? 'this user').trim();
    const claimType = (button.dataset.claimType ?? 'this claim').trim();
    const claimValue = (button.dataset.claimValue ?? '').trim();
    const isLastUser = (button.dataset.lastUser ?? 'false') === 'true';

    let message = `Remove claim '${claimType}' from ${userName}?`;
    if (claimValue) {
        message += `\nClaim value: ${claimValue}`;
    }

    if (isLastUser) {
        message += '\nWARNING: This is the last user with this claim. Removing it will remove the claim from the system. Assign it to another user with a different value to keep this claim.';
    }

    return message;
}

document.querySelectorAll('.remove-user-claim-btn').forEach((button) => {
    const form = button.closest('form');
    if (!form) {
        return;
    }

    button.addEventListener('click', (event) => {
        event.preventDefault();

        const message = buildRemoveMessage(button);
        confirmAction('Remove Claim Assignment', message, () => submitWithButton(form, button));
    });
});
