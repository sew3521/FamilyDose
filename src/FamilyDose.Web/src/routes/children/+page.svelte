<script lang="ts">
    import { onMount } from 'svelte';

    // These fields match the JSON returned by our child API.
    type Child = {
        id: string;
        name: string;
        dateOfBirth: string;
        isActive: boolean;
    };

    let children = $state<Child[]>([]);
    let name = $state('');
    let birthday = $state('');
    let loading = $state(true);
    let saving = $state(false);
    let error = $state('');
    let message = $state('');

    // Editing uses separate fields so Cancel never changes saved data.
    let editingId = $state<string | null>(null);
    let editName = $state('');
    let editBirthday = $state('');
    let editingBusy = $state(false);
    let confirmDelete = $state(false);
    let deleteConfirmation = $state('');
    let editError = $state('');


    // Format eight typed digits as MM/DD/YYYY.
    function formatBirthday(value: string): string {
        const digits = value.replace(/\D/g, '').slice(0, 8);

        if (digits.length <= 2) return digits;
        if (digits.length <= 4) {
            return `${digits.slice(0, 2)}/${digits.slice(2)}`;
        }

        return `${digits.slice(0, 2)}/${digits.slice(2, 4)}/${digits.slice(4)}`;
    }

    // Convert the display format into the ISO date expected by our API.
    // Reject impossible calendar dates rather than letting JavaScript
    // silently normalize them into a different date.
    function birthdayToIso(value: string): string | null {
        const match = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(value);
        if (!match) return null;

        const month = Number(match[1]);
        const day = Number(match[2]);
        const year = Number(match[3]);

        const date = new Date(Date.UTC(year, month - 1, day));

        if (
            year < 1900 ||
            date.getUTCFullYear() !== year ||
            date.getUTCMonth() !== month - 1 ||
            date.getUTCDate() !== day
        ) {
            return null;
        }

        return `${match[3]}-${match[1]}-${match[2]}`;
    }


    // Convert the ISO date returned by the API into MM/DD/YYYY.
    // Keep dates as strings to avoid timezone-related date shifts.
    function displayBirthday(isoDate: string): string {
        const parts = isoDate.split('-');

        if (parts.length !== 3) {
            return isoDate;
        }

        return `${parts[1]}/${parts[2]}/${parts[0]}`;
    }

    // Reload the list after saving so the server remains
    // the source of truth for child profile information.
    async function loadChildren() {
        loading = true;
        error = '';

        try {
            const response = await fetch('/api/children/');

            if (!response.ok) {
                throw new Error('Unable to load child profiles.');
            }

            children = await response.json();
        } catch (err) {
            error = err instanceof Error
                ? err.message
                : 'An unexpected error occurred.';
        } finally {
            loading = false;
        }
    }

    async function addChild(event: SubmitEvent) {
        event.preventDefault();

        if (saving) return;

        saving = true;
        error = '';
        message = '';

        const isoBirthday = birthdayToIso(birthday);

        if (!isoBirthday) {
            error = 'Enter a valid birthday in MM/DD/YYYY format.';
            saving = false;
            return;
        }

        try {
            const response = await fetch('/api/children/', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    name: name.trim(),
                    dateOfBirth: isoBirthday
                })
            });

            if (!response.ok) {
                const result = await response.json();
                throw new Error(
                    result.error ?? 'Unable to save child profile.'
                );
            }

            name = '';
            birthday = '';

            await loadChildren();

            if (!error) {
                message = 'Child profile created successfully.';
            }
        } catch (err) {
            error = err instanceof Error
                ? err.message
                : 'An unexpected error occurred.';
        } finally {
            saving = false;
        }
    }

    function startEditing(child: Child) {
        editingId = child.id;
        editName = child.name;
        editBirthday = displayBirthday(child.dateOfBirth);
        confirmDelete = false;
        deleteConfirmation = '';
        editError = '';
        error = '';
        message = '';
    }

    function cancelEditing() {
        editingId = null;
        confirmDelete = false;
        deleteConfirmation = '';
        editError = '';
        error = '';
    }

    async function saveEditing(event: SubmitEvent) {
        event.preventDefault();

        if (!editingId || editingBusy) return;

        const isoBirthday = birthdayToIso(editBirthday);

        if (!isoBirthday) {
            error = 'Enter a valid birthday in MM/DD/YYYY format.';
            return;
        }

        editingBusy = true;
        error = '';
        message = '';

        try {
            const response = await fetch(`/api/children/${editingId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    name: editName.trim(),
                    dateOfBirth: isoBirthday
                })
            });

            if (!response.ok) {
                const result = await response.json();
                throw new Error(result.error ?? 'Unable to update profile.');
            }

            await loadChildren();

            if (error) return;

            editingId = null;
            message = 'Child profile updated successfully.';
        } catch (err) {
            error = err instanceof Error
                ? err.message
                : 'Unable to update profile.';
        } finally {
            editingBusy = false;
        }
    }

    async function changeArchiveStatus(child: Child) {
        if (editingBusy) return;

        editingBusy = true;
        error = '';
        message = '';

        const action = child.isActive ? 'archive' : 'restore';

        try {
            const response = await fetch(
                `/api/children/${child.id}/${action}`,
                { method: 'POST' }
            );

            if (!response.ok) {
                throw new Error(`Unable to ${action} profile.`);
            }

            await loadChildren();

            if (error) return;

            editingId = null;
            message = child.isActive
                ? 'Child profile archived.'
                : 'Child profile restored.';
        } catch (err) {
            error = err instanceof Error
                ? err.message
                : 'Unable to update profile status.';
        } finally {
            editingBusy = false;
        }
    }

    // Deletion is deliberately separate from editing.
    // The parent must explicitly confirm the selected profile.
    async function deleteChild(child: Child) {
        if (editingBusy || deleteConfirmation !== child.name) return;

        editingBusy = true;
        editError = '';
        message = '';

        try {
            const response = await fetch(
                `/api/children/${child.id}`,
                { method: 'DELETE' }
            );

            if (!response.ok) {
                if (response.status === 409) {
                    throw new Error(
                        'This profile has associated records and cannot be deleted.'
                    );
                }

                throw new Error('Unable to delete child profile.');
            }

            // Do not report success until the server confirms deletion.
            editingId = null;
            confirmDelete = false;
            deleteConfirmation = '';

            await loadChildren();

            if (!error) {
                message = 'Child profile permanently deleted.';
            }
        } catch (err) {
            editError = err instanceof Error
                ? err.message
                : 'Unable to delete child profile.';
        } finally {
            editingBusy = false;
        }
    }

    onMount(loadChildren);
</script>

<svelte:head>
    <title>Children | FamilyDose</title>
    <meta
        name="description"
        content="Manage family medication profiles."
    />
</svelte:head>

<main>
    <div class="container">
        <header>
            <a href="/" class="back">FamilyDose</a>
            <h1>Children</h1>
            <p>Manage your family's medication profiles.</p>
        </header>

        <section class="card">
            <h2>Child profiles</h2>

            {#if loading}
                <p>Loading profiles...</p>
            {:else if children.length === 0}
                <p>No child profiles have been added yet.</p>
            {:else}
                <div class="children">
                    {#each children as child (child.id)}
                        <div class="child">
                            <div class="avatar">
                                {child.name.charAt(0).toUpperCase()}
                            </div>

                            <div class="child-details">
                                <strong>{child.name}</strong>
                                <small>
                                    Born {displayBirthday(child.dateOfBirth)}
                                </small>
                                {#if !child.isActive}
                                    <span class="archived">Archived</span>
                                {/if}
                            </div>

                            <button
                                type="button"
                                class="edit-button"
                                onclick={() => startEditing(child)}
                                disabled={editingBusy}
                            >
                                Edit
                            </button>
                        </div>

                        {#if editingId === child.id}
                            <div class="edit-panel">
                                <h3>Edit {child.name}</h3>

                                <form onsubmit={saveEditing}>
                                    <label for="edit-name">Name</label>
                                    <input
                                        id="edit-name"
                                        type="text"
                                        maxlength="100"
                                        bind:value={editName}
                                        required
                                    />

                                    <label for="edit-birthday">
                                        Date of birth
                                    </label>
                                    <input
                                        id="edit-birthday"
                                        type="text"
                                        inputmode="numeric"
                                        placeholder="MM/DD/YYYY"
                                        maxlength="10"
                                        value={editBirthday}
                                        oninput={(event) => {
                                            editBirthday = formatBirthday(
                                                event.currentTarget.value
                                            );
                                        }}
                                        required
                                    />

                                    <button type="submit" disabled={editingBusy}>
                                        {editingBusy ? 'Saving...' : 'Save changes'}
                                    </button>
                                </form>

                                {#if editError}
                                    <div class="alert error" role="alert">
                                        {editError}
                                    </div>
                                {/if}

                                <div class="edit-actions">
                                    <button
                                        type="button"
                                        class="secondary-button"
                                        onclick={cancelEditing}
                                        disabled={editingBusy}
                                    >
                                        Cancel
                                    </button>

                                    <button
                                        type="button"
                                        class="archive-button"
                                        onclick={() => changeArchiveStatus(child)}
                                        disabled={editingBusy}
                                    >
                                        {child.isActive ? 'Archive child' : 'Restore child'}
                                    </button>
                                </div>

                                <div class="danger-zone">
                                    {#if !confirmDelete}
                                        <button
                                            type="button"
                                            class="delete-link"
                                            onclick={() => {
                                                confirmDelete = true;
                                                deleteConfirmation = '';
                                                editError = '';
                                            }}
                                            disabled={editingBusy}
                                        >
                                            Permanently delete profile
                                        </button>
                                    {:else}
                                        <h4>Delete {child.name}?</h4>
                                        <p>
                                            This permanently removes the child
                                            profile. This action cannot be undone.
                                        </p>

                                        <label for="delete-confirmation">
                                            Type {child.name} to confirm
                                        </label>

                                        <input
                                            id="delete-confirmation"
                                            type="text"
                                            autocomplete="off"
                                            bind:value={deleteConfirmation}
                                        />

                                        <div class="edit-actions">
                                            <button
                                                type="button"
                                                class="secondary-button"
                                                onclick={() => {
                                                    confirmDelete = false;
                                                    deleteConfirmation = '';
                                                }}
                                                disabled={editingBusy}
                                            >
                                                Cancel deletion
                                            </button>

                                            <button
                                                type="button"
                                                class="delete-button"
                                                onclick={() => deleteChild(child)}
                                                disabled={editingBusy ||
                                                    deleteConfirmation !== child.name}
                                            >
                                                {editingBusy
                                                    ? 'Deleting...'
                                                    : 'Delete permanently'}
                                            </button>
                                        </div>
                                    {/if}
                                </div>
                            </div>
                        {/if}
                    {/each}
                </div>
            {/if}
        </section>

        <section class="card">
            <h2>Add a child</h2>

            <form onsubmit={addChild}>
                <label for="child-name">Name</label>
                <input
                    id="child-name"
                    type="text"
                    maxlength="100"
                    placeholder="Child's name"
                    bind:value={name}
                    required
                />

                <label for="birthday">Date of birth</label>
                <input
                    id="birthday"
                    type="text"
                    inputmode="numeric"
                    autocomplete="bday"
                    placeholder="MM/DD/YYYY"
                    maxlength="10"
                    value={birthday}
                    oninput={(event) => {
                        birthday = formatBirthday(event.currentTarget.value);
                    }}
                    required
                />

                <button type="submit" disabled={saving}>
                    {saving ? 'Saving...' : 'Add child'}
                </button>
            </form>
        </section>

        {#if error}
            <div class="alert error" role="alert">{error}</div>
        {/if}

        {#if message}
            <div class="alert success" role="status">{message}</div>
        {/if}
    </div>
</main>

<style>
    :global(body) {
        margin: 0;
        background: #f3f6fa;
        color: #253247;
        font-family: system-ui, sans-serif;
    }

    :global(*) {
        box-sizing: border-box;
    }

    main {
        min-height: 100vh;
        padding: 24px 16px;
    }

    .container {
        max-width: 680px;
        margin: 0 auto;
    }

    header {
        margin-bottom: 28px;
    }

    .back {
        color: #2878d0;
        text-decoration: none;
        font-weight: 600;
    }

    h1 {
        font-size: 32px;
        margin: 16px 0 8px;
    }

    header p {
        color: #69788b;
        margin: 0;
    }

    .card {
        background: white;
        border-radius: 16px;
        padding: 24px;
        margin-bottom: 20px;
        box-shadow: 0 4px 20px #00000008;
    }

    h2 {
        font-size: 20px;
        margin: 0 0 20px;
    }

    .children {
        display: grid;
        gap: 12px;
    }

    .child {
        display: flex;
        align-items: center;
        gap: 14px;
        padding: 12px;
        background: #f3f6fa;
        border-radius: 12px;
    }

    .avatar {
        width: 46px;
        height: 46px;
        display: grid;
        place-items: center;
        border-radius: 50%;
        background: #dcecff;
        color: #2878d0;
        font-size: 20px;
        font-weight: 700;
    }

    .child small {
        display: block;
        color: #69788b;
        margin-top: 4px;
    }

    form {
        display: grid;
        gap: 12px;
    }

    label {
        font-weight: 600;
        font-size: 14px;
    }

    input {
        width: 100%;
        padding: 13px;
        border: 1px solid #cbd5e1;
        border-radius: 10px;
        font: inherit;
        font-size: 16px;
    }

    input:focus {
        outline: 2px solid #2878d0;
        outline-offset: 1px;
    }

    button {
        margin-top: 12px;
        padding: 14px;
        border: 0;
        border-radius: 10px;
        background: #2878d0;
        color: white;
        font: inherit;
        font-weight: 600;
        cursor: pointer;
    }

    button:disabled {
        opacity: 0.6;
        cursor: not-allowed;
    }

    .alert {
        padding: 16px;
        border-radius: 10px;
        margin-bottom: 16px;
    }

    .error {
        background: #fee2e2;
        color: #991b1b;
    }

    .success {
        background: #dcfce7;
        color: #166534;
    }

    .child-details {
        flex: 1;
        min-width: 0;
    }

    .edit-button {
        margin: 0;
        padding: 10px 16px;
        background: white;
        color: #2878d0;
        border: 1px solid #2878d0;
    }

    .archived {
        display: inline-block;
        margin-top: 6px;
        padding: 3px 8px;
        border-radius: 6px;
        background: #e2e8f0;
        color: #475569;
        font-size: 12px;
    }

    .edit-panel {
        margin: 12px 0 20px;
        padding: 20px;
        border: 1px solid #cbd5e1;
        border-radius: 12px;
        background: #fafcff;
    }

    .edit-panel h3 {
        margin-top: 0;
    }

    .edit-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        margin-top: 16px;
    }

    .edit-actions button {
        flex: 1;
        margin: 0;
    }

    .secondary-button {
        background: #e2e8f0;
        color: #334155;
    }

    .archive-button {
        background: #64748b;
    }

    .danger-zone {
        margin-top: 24px;
        padding-top: 20px;
        border-top: 1px solid #fecaca;
    }

    .danger-zone h4 {
        margin: 0 0 8px;
        color: #991b1b;
    }

    .danger-zone p {
        color: #64748b;
        line-height: 1.5;
    }

    .delete-link {
        width: 100%;
        margin: 0;
        background: transparent;
        color: #b91c1c;
        border: 1px solid #fecaca;
    }

    .delete-button {
        background: #b91c1c;
    }

    .delete-button:hover:not(:disabled) {
        background: #991b1b;
    }
</style>
