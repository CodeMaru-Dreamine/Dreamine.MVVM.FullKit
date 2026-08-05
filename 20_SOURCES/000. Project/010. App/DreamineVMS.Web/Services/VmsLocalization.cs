using System.Globalization;

namespace DreamineVMS.Web.Services;

/// <summary>Scoped UI language state shared by the VMS portal.</summary>
public sealed class VmsLocalization
{
    public sealed record LanguageOption(string Code, string Region, string NativeName, string HtmlLanguage);
    public static readonly LanguageOption[] Languages =
    [
        new("en", "US", "English", "en"), new("es", "ES", "Español", "es"),
        new("fr", "FR", "Français", "fr"), new("it", "IT", "Italiano", "it"),
        new("pt", "PT", "Português", "pt"), new("ko", "KR", "한국어", "ko"),
        new("ja", "JP", "日本語", "ja"), new("zh-hans", "CN", "简体中文", "zh-Hans"),
        new("zh-hant", "HK", "繁體中文", "zh-Hant"), new("vi", "VN", "Tiếng Việt", "vi")
    ];
    private static readonly string[] Codes = Languages.Select(x => x.Code).ToArray();
    // en, es, fr, it, pt, ko, ja, zh-Hans, zh-Hant, vi
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
        ["guide"] = ["Guide", "Guía", "Guide", "Guida", "Guia", "사용 안내", "利用ガイド", "使用指南", "使用指南", "Hướng dẫn"],
        ["cameras"] = ["Camera management", "Gestión de cámaras", "Gestion des caméras", "Gestione telecamere", "Gerenciar câmeras", "카메라 관리", "カメラ管理", "摄像头管理", "攝影機管理", "Quản lý camera"],
        ["page.title"] = ["CodeMaru CCTV Viewer", "Visor CCTV CodeMaru", "Visionneuse CCTV CodeMaru", "Visualizzatore CCTV CodeMaru", "Visualizador CCTV CodeMaru", "CodeMaru CCTV 뷰어", "CodeMaru CCTV ビューアー", "CodeMaru CCTV 查看器", "CodeMaru CCTV 檢視器", "Trình xem CCTV CodeMaru"],
        ["seo.description"] = ["Watch IP cameras live in your browser and on mobile.", "Vea cámaras IP en directo desde el navegador y el móvil.", "Regardez vos caméras IP en direct sur navigateur et mobile.", "Guarda le telecamere IP in diretta da browser e mobile.", "Veja câmeras IP ao vivo no navegador e celular.", "IP 카메라를 브라우저와 모바일에서 실시간으로 확인하세요.", "IPカメラをブラウザーやモバイルでリアルタイム確認。", "通过浏览器和手机实时查看 IP 摄像头。", "透過瀏覽器與手機即時查看 IP 攝影機。", "Xem camera IP trực tiếp trên trình duyệt và điện thoại."],
        ["continue.login"] = ["Continue to sign in", "Continuar para iniciar sesión", "Continuer vers la connexion", "Continua per accedere", "Continuar para entrar", "로그인 계속하기", "ログインを続ける", "继续登录", "繼續登入", "Tiếp tục đăng nhập"],
        ["continue.signup"] = ["Continue to sign up", "Continuar para registrarse", "Continuer vers l’inscription", "Continua la registrazione", "Continuar cadastro", "회원가입 계속하기", "登録を続ける", "继续注册", "繼續註冊", "Tiếp tục đăng ký"],
        ["redirect.login"] = ["Opening CodeMaru shared sign-in.", "Abriendo el inicio de sesión compartido de CodeMaru.", "Ouverture de la connexion CodeMaru.", "Apertura dell’accesso condiviso CodeMaru.", "Abrindo o login compartilhado CodeMaru.", "CodeMaru 공통 로그인으로 이동합니다.", "CodeMaru共通ログインを開きます。", "正在打开 CodeMaru 统一登录。", "正在開啟 CodeMaru 共用登入。", "Đang mở đăng nhập chung CodeMaru."],
        ["redirect.signup"] = ["Opening CodeMaru account creation.", "Abriendo la creación de cuenta CodeMaru.", "Ouverture de la création de compte CodeMaru.", "Apertura della creazione account CodeMaru.", "Abrindo a criação de conta CodeMaru.", "CodeMaru 공통 계정 생성 화면으로 이동합니다.", "CodeMaru共通アカウント作成を開きます。", "正在打开 CodeMaru 账户创建。", "正在開啟 CodeMaru 帳戶建立。", "Đang mở trang tạo tài khoản CodeMaru."],
        ["signup"] = ["Sign up", "Registrarse", "S’inscrire", "Registrati", "Cadastre-se", "회원가입", "新規登録", "注册", "註冊", "Đăng ký"],
        ["signing.out"] = ["Clearing your sign-in session.", "Cerrando su sesión.", "Fermeture de votre session.", "Chiusura della sessione.", "Encerrando sua sessão.", "로그인 상태를 정리하고 있습니다.", "ログイン状態を解除しています。", "正在清除登录会话。", "正在清除登入工作階段。", "Đang xóa phiên đăng nhập."],
        ["not.found.user"] = ["User not found.", "Usuario no encontrado.", "Utilisateur introuvable.", "Utente non trovato.", "Usuário não encontrado.", "사용자를 찾을 수 없습니다.", "ユーザーが見つかりません。", "找不到用户。", "找不到使用者。", "Không tìm thấy người dùng."],
        ["check.url"] = ["Check the URL.", "Compruebe la URL.", "Vérifiez l’URL.", "Controlla l’URL.", "Verifique a URL.", "URL을 확인해 주세요.", "URLをご確認ください。", "请检查网址。", "請檢查網址。", "Hãy kiểm tra URL."],
        ["live.stream"] = ["Live camera stream", "Transmisión de cámara en directo", "Flux caméra en direct", "Streaming telecamera in diretta", "Câmera ao vivo", "실시간 카메라 스트림", "カメラライブ映像", "实时摄像头画面", "即時攝影機串流", "Luồng camera trực tiếp"],
        ["public.live.title"] = ["{0} live CCTV", "CCTV en directo de {0}", "CCTV en direct de {0}", "CCTV in diretta di {0}", "CCTV ao vivo de {0}", "{0} 실시간 CCTV", "{0}のCCTVライブ", "{0} 的 CCTV 直播", "{0} 的 CCTV 直播", "CCTV trực tiếp của {0}"],
        ["public.live.description"] = ["Watch {0}'s live camera stream.", "Mira la transmisión en directo de las cámaras de {0}.", "Regardez le flux en direct des caméras de {0}.", "Guarda lo streaming in diretta delle telecamere di {0}.", "Assista ao vivo às câmeras de {0}.", "{0}의 실시간 카메라 스트림입니다.", "{0}のカメラライブ映像です。", "观看 {0} 的实时摄像头画面。", "觀看 {0} 的即時攝影機串流。", "Xem luồng camera trực tiếp của {0}."],
        ["no.cameras"] = ["No active cameras.", "No hay cámaras activas.", "Aucune caméra active.", "Nessuna telecamera attiva.", "Nenhuma câmera ativa.", "활성화된 카메라가 없습니다.", "有効なカメラがありません。", "没有启用的摄像头。", "沒有啟用的攝影機。", "Không có camera đang hoạt động."],
        ["connecting"] = ["Connecting to stream…", "Conectando al flujo…", "Connexion au flux…", "Connessione allo streaming…", "Conectando ao stream…", "스트림 연결 중…", "ストリームに接続中…", "正在连接视频流…", "正在連接串流…", "Đang kết nối luồng…"],
        ["page.notfound"] = ["Page not found.", "Página no encontrada.", "Page introuvable.", "Pagina non trovata.", "Página não encontrada.", "페이지를 찾을 수 없습니다.", "ページが見つかりません。", "找不到页面。", "找不到頁面。", "Không tìm thấy trang."]
    };

    public string Language { get; private set; } = "ko";
    public string HtmlLanguage => GetHtmlLanguage(Language);
    public event Action? Changed;
    public string this[string key] => GetText(Language, key);
    public string Format(string key, params object?[] args) => FormatText(Language, key, args);

    public static string GetText(string? language, string key)
    {
        var index = GetLanguageIndex(language);
        return Texts.TryGetValue(key, out var values) ? values[index] : key;
    }

    public static string GetHtmlLanguage(string? language) => Languages[GetLanguageIndex(language)].HtmlLanguage;

    public static string GetLanguageCode(string? language) => Languages[GetLanguageIndex(language)].Code;

    public static string FormatText(string? language, string key, params object?[] args)
    {
        var value = GetText(language, key);
        try { return string.Format(CultureInfo.CurrentCulture, value, args); }
        catch { return value; }
    }

    private static int GetLanguageIndex(string? language)
    {
        var value = NormalizeLanguageCode(language);
        var index = Array.FindIndex(Codes, code => string.Equals(code, value, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : Array.IndexOf(Codes, "ko");
    }

    public static string NormalizeLanguageCode(string? language)
    {
        var value = language?.Trim().ToLowerInvariant().Replace('_', '-');
        return value switch { "zh" or "zh-cn" or "zh-sg" => "zh-hans", "zh-tw" or "zh-hk" or "zh-mo" => "zh-hant", _ => value ?? "ko" };
    }
    public void SetLanguage(string? language)
    {
        var value = NormalizeLanguageCode(language);
        if (!Codes.Contains(value, StringComparer.OrdinalIgnoreCase)) value = "ko";
        if (string.Equals(Language, value, StringComparison.OrdinalIgnoreCase)) return;
        Language = value; Changed?.Invoke();
    }
}
