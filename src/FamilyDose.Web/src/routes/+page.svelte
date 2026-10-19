<script lang="ts">
    import { onMount } from 'svelte';

    let status = $state('Connecting...');
    let connected = $state(false);
    let taps = $state(0);
    let dedication = $state(false);

    onMount(async () => {
        try {
            const response = await fetch('/api/health');
            if (!response.ok) throw new Error('API unavailable');

            const data = await response.json();
            connected = data.status === 'healthy';
            status = connected ? 'API connected' : 'Unexpected API response';
        } catch {
            status = 'API unavailable';
        }
    });

    function showEasterEgg() {
        taps++;
        if (taps >= 5) {
            dedication = !dedication;
            taps = 0;
        }
    }
</script>

<svelte:head>
    <title>FamilyDose</title>
    <meta name="description" content="Family medication tracking made simple." />
</svelte:head>

<main>
    <article>
        <div class="symbol">+</div>
        <h1>FamilyDose</h1>
        <p class="subtitle">Medication tracking made simple.</p>

        <div class="status" class:connected>
            <span class="dot"></span>
            {status}
        </div>

        <section>
            <h2>Welcome home</h2>
            <p>
                Your family medication tracker is taking shape.
                Soon you'll be able to record doses, view medication
                history, and see when another dose is eligible.
            </p>
        </section>


        <nav class="navigation" aria-label="Main navigation">
            <a href="/children">Manage children</a>
        </nav>

        <footer>
            <button onclick={showEasterEgg}>FamilyDose v0.1.0</button>
        </footer>

        {#if dedication}
            <aside>
                <div class="heart">&#9829;</div>
                <h2>For Callen &amp; Graham Wesche</h2>
                <p>The two little reasons this app exists.</p>
                <p>
                    Built with love, a little sleep deprivation,
                    and the hope that no parent has to wonder
                    when the last dose was given.
                </p>
                <strong>With love, Dad.</strong>
            </aside>
        {/if}
    </article>
</main>

<style>
    :global(*) { box-sizing: border-box; }
    :global(body) {
        margin: 0;
        background: #f3f6fa;
        color: #253247;
        font-family: system-ui, sans-serif;
    }
    main {
        min-height: 100vh;
        display: grid;
        place-items: center;
        padding: 20px;
    }
    article {
        width: 100%;
        max-width: 440px;
        padding: 32px 24px;
        background: white;
        border-radius: 20px;
        box-shadow: 0 8px 32px #00000010;
        text-align: center;
    }
    .symbol {
        width: 64px;
        height: 64px;
        margin: 0 auto 16px;
        border-radius: 18px;
        background: #2878d0;
        color: white;
        font-size: 46px;
        line-height: 60px;
    }
    h1 { font-size: 32px; margin: 0; }
    .subtitle { color: #69788b; margin: 8px 0 28px; }
    .status {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 10px;
        padding: 14px;
        border-radius: 10px;
        background: #fff2e5;
        color: #965300;
    }
    .status.connected {
        background: #e6f6ed;
        color: #187347;
    }
    .dot {
        width: 9px;
        height: 9px;
        border-radius: 50%;
        background: currentColor;
    }
    section { margin: 32px 0; text-align: left; }
    section p { color: #69788b; line-height: 1.6; }
    footer { border-top: 1px solid #e8edf2; padding-top: 20px; }
    footer button {
        border: 0;
        background: transparent;
        color: #8a96a6;
        cursor: pointer;
        padding: 10px;
    }
    aside {
        margin-top: 24px;
        padding: 20px;
        background: #fff3f5;
        border-radius: 14px;
        line-height: 1.6;
    }
    aside p { color: #695c65; }
    .heart { color: #d94c68; font-size: 32px; }

    .navigation {
        margin: 24px 0;
    }

    .navigation a {
        display: block;
        padding: 14px;
        border-radius: 10px;
        background: #2878d0;
        color: white;
        font-weight: 600;
        text-decoration: none;
        text-align: center;
    }

    .navigation a:hover {
        background: #1e63af;
    }

</style>
