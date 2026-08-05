using System.Globalization;
using System.Text.RegularExpressions;

namespace ShopPlatform.Services;

/// <summary>Stores the UI language for a ShopStore Blazor circuit.</summary>
public sealed partial class ShopLocalization
{
    public sealed record LanguageOption(string Code, string Region, string NativeName, string HtmlLanguage, string CultureName);

    public static readonly LanguageOption[] Languages =
    [
        new("en", "US", "English", "en", "en-US"),
        new("es", "ES", "Español", "es", "es-ES"),
        new("fr", "FR", "Français", "fr", "fr-FR"),
        new("it", "IT", "Italiano", "it", "it-IT"),
        new("pt", "PT", "Português", "pt", "pt-PT"),
        new("ko", "KR", "한국어", "ko", "ko-KR"),
        new("ja", "JP", "日本語", "ja", "ja-JP"),
        new("zh-hans", "CN", "简体中文", "zh-Hans", "zh-CN"),
        new("zh-hant", "HK", "繁體中文", "zh-Hant", "zh-HK"),
        new("vi", "VN", "Tiếng Việt", "vi", "vi-VN")
    ];

    private static readonly string[] Codes = Languages.Select(item => item.Code).ToArray();

    // Keep this order aligned with Languages: en, es, fr, it, pt, ko, ja, zh-Hans, zh-Hant, vi.
    private static readonly Dictionary<string, string[]> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = ["Language", "Idioma", "Langue", "Lingua", "Idioma", "언어", "言語", "语言", "語言", "Ngôn ngữ"],
        ["theme"] = ["Theme", "Tema", "Thème", "Tema", "Tema", "화면 테마", "テーマ", "主题", "主題", "Giao diện"],
        ["system"] = ["System", "Sistema", "Système", "Sistema", "Sistema", "시스템", "システム", "系统", "系統", "Hệ thống"],
        ["light"] = ["Light", "Claro", "Clair", "Chiaro", "Claro", "라이트", "ライト", "浅色", "淺色", "Sáng"],
        ["dark"] = ["Dark", "Oscuro", "Sombre", "Scuro", "Escuro", "다크", "ダーク", "深色", "深色", "Tối"],
        ["home"] = ["Home", "Inicio", "Accueil", "Home", "Início", "홈", "ホーム", "首页", "首頁", "Trang chủ"],
        ["contact"] = ["Contact", "Contacto", "Contact", "Contatti", "Contato", "문의하기", "お問い合わせ", "联系我们", "聯絡我們", "Liên hệ"],
        ["login"] = ["Sign in", "Iniciar sesión", "Connexion", "Accedi", "Entrar", "로그인", "ログイン", "登录", "登入", "Đăng nhập"],
        ["logout"] = ["Sign out", "Cerrar sesión", "Déconnexion", "Esci", "Sair", "로그아웃", "ログアウト", "退出登录", "登出", "Đăng xuất"],
        ["account"] = ["My account", "Mi cuenta", "Mon compte", "Il mio account", "Minha conta", "내 계정", "マイアカウント", "我的账户", "我的帳戶", "Tài khoản"],
        ["screen.settings"] = ["Screen settings", "Ajustes de pantalla", "Réglages d’affichage", "Impostazioni schermo", "Configurações da tela", "화면 설정", "画面設定", "屏幕设置", "畫面設定", "Cài đặt màn hình"],
        ["nav.toggle"] = ["Open or close menu", "Abrir o cerrar menú", "Ouvrir ou fermer le menu", "Apri o chiudi menu", "Abrir ou fechar menu", "메뉴 열기/닫기", "メニューを開閉", "打开或关闭菜单", "開啟或關閉選單", "Mở hoặc đóng menu"],
        ["nav.services"] = ["CodeMaru services", "Servicios de CodeMaru", "Services CodeMaru", "Servizi CodeMaru", "Serviços CodeMaru", "CodeMaru 서비스", "CodeMaru サービス", "CodeMaru 服务", "CodeMaru 服務", "Dịch vụ CodeMaru"],
        ["my.shops"] = ["My shops", "Mis tiendas", "Mes boutiques", "I miei negozi", "Minhas lojas", "내 쇼핑몰", "マイショップ", "我的商店", "我的商店", "Cửa hàng của tôi"],
        ["open.shop"] = ["Open a shop", "Abrir una tienda", "Ouvrir une boutique", "Apri un negozio", "Abrir uma loja", "쇼핑몰 개설", "ショップを開設", "开设商店", "開設商店", "Mở cửa hàng"],
        ["admin"] = ["Admin", "Administración", "Administration", "Amministrazione", "Administração", "관리", "管理", "管理", "管理", "Quản trị"],
        ["page.title"] = ["ShopStore — Your online shop", "ShopStore — Tu tienda en línea", "ShopStore — Votre boutique en ligne", "ShopStore — Il tuo negozio online", "ShopStore — Sua loja online", "ShopStore — 나만의 온라인 쇼핑몰", "ShopStore — あなたのオンラインショップ", "ShopStore — 您的在线商店", "ShopStore — 您的網上商店", "ShopStore — Cửa hàng trực tuyến của bạn"],
        ["seo.description"] = ["Create a store, add products, and start taking payments in minutes.", "Crea una tienda, añade productos y empieza a cobrar en minutos.", "Créez votre boutique, ajoutez vos produits et acceptez des paiements en quelques minutes.", "Crea un negozio, aggiungi prodotti e accetta pagamenti in pochi minuti.", "Crie uma loja, adicione produtos e receba pagamentos em minutos.", "쇼핑몰 개설부터 상품 등록과 결제까지 몇 분 안에 시작하세요.", "ショップ開設から商品登録、決済まで数分で始められます。", "几分钟内即可创建商店、添加商品并开始收款。", "幾分鐘內即可建立商店、新增商品並開始收款。", "Tạo cửa hàng, thêm sản phẩm và bắt đầu nhận thanh toán chỉ trong vài phút."],
        ["page.notfound"] = ["Page not found", "Página no encontrada", "Page introuvable", "Pagina non trovata", "Página não encontrada", "페이지를 찾을 수 없습니다", "ページが見つかりません", "找不到页面", "找不到頁面", "Không tìm thấy trang"],
        ["page.notfound.description"] = ["The requested page does not exist or has moved.", "La página solicitada no existe o se ha movido.", "La page demandée n’existe pas ou a été déplacée.", "La pagina richiesta non esiste o è stata spostata.", "A página solicitada não existe ou foi movida.", "요청하신 페이지가 없거나 이동되었습니다.", "指定されたページは存在しないか移動しました。", "请求的页面不存在或已移动。", "要求的頁面不存在或已移動。", "Trang bạn yêu cầu không tồn tại hoặc đã được chuyển."],
        ["go.home"] = ["Go to ShopStore home", "Ir al inicio de ShopStore", "Aller à l’accueil ShopStore", "Vai alla home di ShopStore", "Ir para o início da ShopStore", "ShopStore 홈으로", "ShopStore ホームへ", "前往 ShopStore 首页", "前往 ShopStore 首頁", "Về trang chủ ShopStore"],
        ["payment.failed"] = ["Payment could not be completed. Please try again.", "No se pudo completar el pago. Inténtalo de nuevo.", "Le paiement n’a pas pu être effectué. Veuillez réessayer.", "Impossibile completare il pagamento. Riprova.", "Não foi possível concluir o pagamento. Tente novamente.", "결제를 완료하지 못했습니다. 다시 시도해 주세요.", "決済を完了できませんでした。もう一度お試しください。", "无法完成付款，请重试。", "無法完成付款，請再試一次。", "Không thể hoàn tất thanh toán. Vui lòng thử lại."],
        ["payment.cancelled"] = ["Payment was cancelled.", "El pago fue cancelado.", "Le paiement a été annulé.", "Il pagamento è stato annullato.", "O pagamento foi cancelado.", "결제가 취소되었습니다.", "決済がキャンセルされました。", "付款已取消。", "付款已取消。", "Thanh toán đã bị hủy."],
        ["payment.keyMissing"] = ["Payment is not available for this store yet.", "El pago aún no está disponible para esta tienda.", "Le paiement n’est pas encore disponible pour cette boutique.", "Il pagamento non è ancora disponibile per questo negozio.", "O pagamento ainda não está disponível para esta loja.", "이 쇼핑몰은 아직 결제를 사용할 수 없습니다.", "このショップではまだ決済を利用できません。", "此商店暂未开通付款功能。", "此商店尚未開通付款功能。", "Cửa hàng này chưa hỗ trợ thanh toán."],
        ["shop.here"] = ["Shop at {0}", "Compra en {0}", "Achetez chez {0}", "Acquista su {0}", "Compre na {0}", "{0}에서 쇼핑하세요", "{0}でお買い物をお楽しみください", "在 {0} 购物", "在 {0} 購物", "Mua sắm tại {0}"]
    };

    public string Language { get; private set; } = "ko";
    public string HtmlLanguage => CurrentOption.HtmlLanguage;
    public CultureInfo Culture => CultureInfo.GetCultureInfo(CurrentOption.CultureName);
    public event Action? Changed;

    private LanguageOption CurrentOption =>
        Languages.First(item => string.Equals(item.Code, Language, StringComparison.OrdinalIgnoreCase));

    public string this[string key] => Texts.TryGetValue(key, out var values)
        ? values[Math.Max(0, Array.IndexOf(Codes, Language))]
        : key;

    public static string NormalizeLanguageCode(string? language)
    {
        var value = language?.Trim().ToLowerInvariant().Replace('_', '-');
        return value switch
        {
            "zh" or "zh-cn" or "zh-sg" => "zh-hans",
            "zh-tw" or "zh-hk" or "zh-mo" => "zh-hant",
            _ => value ?? "ko"
        };
    }

    public void SetLanguage(string? language)
    {
        var value = NormalizeLanguageCode(language);
        if (!Codes.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            value = "ko";
        }

        if (string.Equals(Language, value, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Language = value;
        Changed?.Invoke();
    }

    /// <summary>Adds or replaces the language query while preserving any fragment.</summary>
    public string WithLanguage(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.StartsWith('#'))
        {
            return url;
        }

        var hashIndex = url.IndexOf('#');
        var fragment = hashIndex >= 0 ? url[hashIndex..] : string.Empty;
        var baseUrl = hashIndex >= 0 ? url[..hashIndex] : url;
        var language = Uri.EscapeDataString(Language);

        if (LanguageQueryRegex().IsMatch(baseUrl))
        {
            return LanguageQueryRegex().Replace(baseUrl, $"${{1}}lang={language}", 1) + fragment;
        }

        var separator = baseUrl.Contains('?') ? '&' : '?';
        return $"{baseUrl}{separator}lang={language}{fragment}";
    }

    [GeneratedRegex("([?&])lang=[^&#]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LanguageQueryRegex();
}
