(() => {
    let visibilityHandler = null;
    let formationDotnetReference = null;
    let formationDrag = null;
    let idleDotnetReference = null;
    let idleTimeoutId = null;
    let idleTimeoutMilliseconds = 0;

    const idleEvents = ["pointerdown", "keydown", "touchstart"];
    const clearIdleTimeout = () => {
        if (idleTimeoutId !== null) window.clearTimeout(idleTimeoutId);
        idleTimeoutId = null;
    };
    const scheduleIdleTimeout = () => {
        clearIdleTimeout();
        if (!idleDotnetReference || idleTimeoutMilliseconds <= 0 || document.visibilityState !== "visible") return;
        idleTimeoutId = window.setTimeout(() => {
            idleTimeoutId = null;
            idleDotnetReference?.invokeMethodAsync("EnterPowerSavingFromIdleAsync").catch(() => {});
        }, idleTimeoutMilliseconds);
    };
    const onIdleActivity = () => scheduleIdleTimeout();

    const formationCards = container => Array.from(container.querySelectorAll(":scope > .mi-formation-unit"));

    const syncFormationSlots = container => {
        const cards = formationCards(container);
        cards.forEach((card, index) => {
            card.className = card.className.replace(/\bslot-\d+\b/g, "").trim();
            card.classList.add(`slot-${index}`);
            const number = card.querySelector(":scope > header > span");
            if (number) number.textContent = String(index + 1);
            const buttons = card.querySelectorAll(".mi-formation-unit-actions button");
            if (buttons[0]) buttons[0].disabled = index === 0;
            if (buttons[1]) buttons[1].disabled = index === cards.length - 1;
        });
    };

    const clearFormationDragClasses = container => {
        container.querySelectorAll(".is-dragging,.is-drop-target").forEach(card =>
            card.classList.remove("is-dragging", "is-drop-target"));
    };

    const restoreFormationOrder = drag => {
        const cards = new Map(formationCards(drag.container).map(card => [card.dataset.heroId, card]));
        drag.originalOrder.forEach(heroId => {
            const card = cards.get(heroId);
            if (card) drag.container.append(card);
        });
        syncFormationSlots(drag.container);
    };

    const onFormationPointerDown = event => {
        if (event.button !== 0 || !(event.target instanceof Element) || event.target.closest("button")) return;
        const card = event.target.closest(".mi-formation-unit");
        const container = card?.closest(".mi-formation-slots");
        if (!card || !container || !formationDotnetReference) return;
        formationDrag = {
            card,
            container,
            pointerId: event.pointerId,
            changed: false,
            originalOrder: formationCards(container).map(item => item.dataset.heroId)
        };
        card.classList.add("is-dragging");
        card.setPointerCapture?.(event.pointerId);
        event.preventDefault();
    };

    const onFormationPointerMove = event => {
        const drag = formationDrag;
        if (!drag || drag.pointerId !== event.pointerId) return;
        const hit = document.elementFromPoint(event.clientX, event.clientY);
        const target = hit?.closest?.(".mi-formation-unit");
        if (!target || target === drag.card || target.closest(".mi-formation-slots") !== drag.container) return;
        const cards = formationCards(drag.container);
        const sourceIndex = cards.indexOf(drag.card);
        const targetIndex = cards.indexOf(target);
        if (sourceIndex < targetIndex) target.after(drag.card);
        else target.before(drag.card);
        clearFormationDragClasses(drag.container);
        drag.card.classList.add("is-dragging");
        target.classList.add("is-drop-target");
        drag.changed = true;
        syncFormationSlots(drag.container);
        event.preventDefault();
    };

    const finishFormationDrag = (event, cancelled) => {
        const drag = formationDrag;
        if (!drag || drag.pointerId !== event.pointerId) return;
        formationDrag = null;
        try { drag.card.releasePointerCapture?.(event.pointerId); } catch { }
        clearFormationDragClasses(drag.container);
        if (cancelled) {
            restoreFormationOrder(drag);
            return;
        }
        if (!drag.changed) return;
        const heroIds = formationCards(drag.container).map(card => card.dataset.heroId);
        formationDotnetReference.invokeMethodAsync("CommitCompanionFormationAsync", heroIds)
            .catch(() => restoreFormationOrder(drag));
    };

    const onFormationPointerUp = event => finishFormationDrag(event, false);
    const onFormationPointerCancel = event => finishFormationDrag(event, true);

    window.maruIdle = {
        bindVisibility(dotnetReference) {
            this.unbindVisibility();
            visibilityHandler = () => dotnetReference.invokeMethodAsync(
                "SetPageVisibilityAsync",
                document.visibilityState === "visible");
            document.addEventListener("visibilitychange", visibilityHandler, { passive: true });
            return document.visibilityState === "visible";
        },

        unbindVisibility() {
            if (visibilityHandler) {
                document.removeEventListener("visibilitychange", visibilityHandler);
                visibilityHandler = null;
            }
        },

        bindIdleTimer(dotnetReference, timeoutMinutes) {
            this.unbindIdleTimer();
            idleDotnetReference = dotnetReference;
            idleEvents.forEach(name => document.addEventListener(name, onIdleActivity, { passive: true }));
            this.configureIdleTimer(timeoutMinutes);
        },

        configureIdleTimer(timeoutMinutes) {
            const minutes = Math.max(0, Number(timeoutMinutes) || 0);
            idleTimeoutMilliseconds = minutes * 60 * 1000;
            scheduleIdleTimeout();
        },

        resetIdleTimer() {
            scheduleIdleTimeout();
        },

        unbindIdleTimer() {
            clearIdleTimeout();
            idleEvents.forEach(name => document.removeEventListener(name, onIdleActivity));
            idleDotnetReference = null;
            idleTimeoutMilliseconds = 0;
        },

        bindFormationDrag(dotnetReference) {
            this.unbindFormationDrag();
            formationDotnetReference = dotnetReference;
            document.addEventListener("pointerdown", onFormationPointerDown);
            document.addEventListener("pointermove", onFormationPointerMove, { passive: false });
            document.addEventListener("pointerup", onFormationPointerUp);
            document.addEventListener("pointercancel", onFormationPointerCancel);
        },

        unbindFormationDrag() {
            document.removeEventListener("pointerdown", onFormationPointerDown);
            document.removeEventListener("pointermove", onFormationPointerMove);
            document.removeEventListener("pointerup", onFormationPointerUp);
            document.removeEventListener("pointercancel", onFormationPointerCancel);
            formationDotnetReference = null;
            formationDrag = null;
        },

        preload(url) {
            if (!url) return Promise.resolve();
            return new Promise(resolve => {
                const image = new Image();
                image.onload = resolve;
                image.onerror = resolve;
                image.src = url;
            });
        }
    };
})();
