// ShopPlatform — Toss Payments v2 위젯 연동 (ShopStore pay.js 방식)
(function () {
    "use strict";

    const __state = {
        widgets: null,
        initialized: false,
        lastParams: null
    };

    async function loadTossScript() {
        return new Promise((resolve, reject) => {
            const url = "https://js.tosspayments.com/v2/standard";
            if (document.querySelector(`script[src="${url}"]`)) { resolve(); return; }
            const s = document.createElement("script");
            s.src = url;
            s.onload = () => resolve();
            s.onerror = (e) => reject(e);
            document.head.appendChild(s);
        });
    }

    function normalizeCustomerKey(raw) {
        let base = (raw ?? "").toString().trim();
        if (!base) base = "guest";
        base = base
            .replace(/@/g, "_at_")
            .normalize("NFKD")
            .replace(/[^\w\-\.~]/g, "_");
        if (base.length < 2) base = (base + "_x").slice(0, 2);
        if (base.length > 500) base = base.slice(0, 500);
        return base;
    }

    async function waitForElement(selector, timeoutMs = 4000) {
        const t0 = Date.now();
        while (!document.querySelector(selector)) {
            if (Date.now() - t0 > timeoutMs) throw new Error(`Element not found: ${selector}`);
            await new Promise(r => setTimeout(r, 100));
        }
    }

    window.openTossWidget = async function (
        orderId,
        amount,
        clientKey,
        customerIdRaw,
        orderName,
        methodsSelector = "#payment-methods",
        agreementSelector = "#agreement"
    ) {
        await waitForElement(methodsSelector);
        await waitForElement(agreementSelector);

        const customerKey = normalizeCustomerKey(customerIdRaw);
        const customerEmail = (customerIdRaw && /@/.test(customerIdRaw)) ? customerIdRaw : undefined;

        if (typeof TossPayments === "undefined") {
            await loadTossScript();
        }

        try {
            const tossPayments = TossPayments(clientKey);
            const widgets = tossPayments.widgets({ customerKey });

            await widgets.setAmount({ currency: "KRW", value: amount });

            await Promise.all([
                widgets.renderPaymentMethods({ selector: methodsSelector, variantKey: "DEFAULT" }),
                widgets.renderAgreement({ selector: agreementSelector, variantKey: "AGREEMENT" }),
            ]);

            __state.widgets = widgets;
            __state.initialized = true;
            __state.lastParams = { orderId, orderName, amount, customerKey, customerEmail };
        } catch (err) {
            console.error("[openTossWidget] Error:", err);
            alert("결제 위젯 초기화 중 오류가 발생했습니다.\n" + (err?.message ?? err));
        }
    };

    window.requestTossPayment = async function (successUrl, failUrl) {
        if (!__state.initialized || !__state.widgets) {
            alert("결제 위젯이 아직 준비되지 않았습니다. 먼저 결제 수단을 열어 주세요.");
            return;
        }
        const { orderId, orderName, customerKey, customerEmail } = __state.lastParams;
        try {
            await __state.widgets.requestPayment({
                orderId,
                orderName,
                successUrl,
                failUrl,
                customerEmail,
                customerName: customerKey
            });
        } catch (err) {
            console.error("[requestTossPayment] Error:", err);
            alert("결제 요청 중 오류가 발생했습니다.\n" + (err?.message ?? err));
        }
    };
})();
