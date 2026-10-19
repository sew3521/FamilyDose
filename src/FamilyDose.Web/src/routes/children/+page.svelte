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

                            <div>
                                <strong>{child.name}</strong>
                                <small>
                                    Born {displayBirthday(child.dateOfBirth)}
                                </small>
                            </div>
                        </div>
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
</style>
