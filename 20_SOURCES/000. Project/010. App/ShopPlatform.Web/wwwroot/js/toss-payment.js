// ShopPlatform — Toss Payments v2 위젯 연동 (ShopStore pay.js 방식)
(function () {
    "use strict";

    const __state = {
        widgets: null,
        initialized: false,
        lastParams: null
    };

    const messages = {
        en: {
            widgetInit: "The payment widget could not be initialized. Please try again.",
            widgetNotReady: "The payment widget is not ready yet. Open the payment methods first.",
            paymentRequest: "The payment request could not be completed. Please try again."
        },
        es: {
            widgetInit: "No se pudo iniciar el widget de pago. Inténtalo de nuevo.",
            widgetNotReady: "El widget de pago aún no está listo. Abre primero los métodos de pago.",
            paymentRequest: "No se pudo completar la solicitud de pago. Inténtalo de nuevo."
        },
        fr: {
            widgetInit: "Impossible d’initialiser le module de paiement. Veuillez réessayer.",
            widgetNotReady: "Le module de paiement n’est pas encore prêt. Ouvrez d’abord les moyens de paiement.",
            paymentRequest: "La demande de paiement n’a pas pu aboutir. Veuillez réessayer."
        },
        it: {
            widgetInit: "Impossibile inizializzare il widget di pagamento. Riprova.",
            widgetNotReady: "Il widget di pagamento non è ancora pronto. Apri prima i metodi di pagamento.",
            paymentRequest: "Impossibile completare la richiesta di pagamento. Riprova."
        },
        pt: {
            widgetInit: "Não foi possível iniciar o widget de pagamento. Tente novamente.",
            widgetNotReady: "O widget de pagamento ainda não está pronto. Abra primeiro as formas de pagamento.",
            paymentRequest: "Não foi possível concluir a solicitação de pagamento. Tente novamente."
        },
        ko: {
            widgetInit: "결제 위젯을 초기화하지 못했습니다. 다시 시도해 주세요.",
            widgetNotReady: "결제 위젯이 아직 준비되지 않았습니다. 먼저 결제 수단을 열어 주세요.",
            paymentRequest: "결제 요청을 완료하지 못했습니다. 다시 시도해 주세요."
        },
        ja: {
            widgetInit: "決済ウィジェットを初期化できませんでした。もう一度お試しください。",
            widgetNotReady: "決済ウィジェットの準備ができていません。先に支払い方法を開いてください。",
            paymentRequest: "決済リクエストを完了できませんでした。もう一度お試しください。"
        },
        "zh-hans": {
            widgetInit: "无法初始化付款组件，请重试。",
            widgetNotReady: "付款组件尚未准备就绪，请先打开付款方式。",
            paymentRequest: "无法完成付款请求，请重试。"
        },
        "zh-hant": {
            widgetInit: "無法初始化付款元件，請再試一次。",
            widgetNotReady: "付款元件尚未準備就緒，請先開啟付款方式。",
            paymentRequest: "無法完成付款要求，請再試一次。"
        },
        vi: {
            widgetInit: "Không thể khởi tạo tiện ích thanh toán. Vui lòng thử lại.",
            widgetNotReady: "Tiện ích thanh toán chưa sẵn sàng. Hãy mở phương thức thanh toán trước.",
            paymentRequest: "Không thể hoàn tất yêu cầu thanh toán. Vui lòng thử lại."
        }
    };

    function currentMessages() {
        const raw = (document.documentElement.lang || "ko").toLowerCase().replaceAll("_", "-");
        const code = raw.startsWith("zh-hant") || raw.startsWith("zh-tw") || raw.startsWith("zh-hk")
            ? "zh-hant"
            : raw.startsWith("zh")
                ? "zh-hans"
                : raw.split("-")[0];
        return messages[code] || messages.ko;
    }

    async function loadTossScript() {
        return new Promise((resolve, reject) => {
            const url = "https://js.tosspayments.com/v2/standard";
            if (document.querySelector(`script[src="${url}"]`)) { resolve(); return; }
            const s = document.createElement("script");
            s.src = url;
            s.onload = () => resolve();
            s.onerror = () => reject(new Error(`Failed to load payment script: ${url}`));
            document.head.appendChild(s);
        });
    }

    function normalizeCustomerKey(raw) {
        let base = (raw ?? "").toString().trim();
        if (!base) base = "guest";
        base = base
            .replaceAll("@", "_at_")
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
            alert(currentMessages().widgetInit);
        }
    };

    window.requestTossPayment = async function (successUrl, failUrl) {
        if (!__state.initialized || !__state.widgets) {
            alert(currentMessages().widgetNotReady);
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
            alert(currentMessages().paymentRequest);
        }
    };
})();
