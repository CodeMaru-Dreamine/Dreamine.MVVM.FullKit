namespace WeddingPlatform.Services;

public sealed class WeddingLocalization
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
    private static readonly Dictionary<string, string[]> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = ["Language","Idioma","Langue","Lingua","Idioma","언어","言語","语言","語言","Ngôn ngữ"],
        ["theme"] = ["Theme","Tema","Thème","Tema","Tema","화면 테마","テーマ","主题","主題","Giao diện"],
        ["system"] = ["System","Sistema","Système","Sistema","Sistema","시스템","システム","系统","系統","Hệ thống"],
        ["light"] = ["Light","Claro","Clair","Chiaro","Claro","라이트","ライト","浅色","淺色","Sáng"],
        ["dark"] = ["Dark","Oscuro","Sombre","Scuro","Escuro","다크","ダーク","深色","深色","Tối"],
        ["home"] = ["Home","Inicio","Accueil","Home","Início","홈","ホーム","首页","首頁","Trang chủ"],
        ["contact"] = ["Contact","Contacto","Contact","Contatti","Contato","문의하기","お問い合わせ","联系我们","聯絡我們","Liên hệ"],
        ["login"] = ["Sign in","Iniciar sesión","Connexion","Accedi","Entrar","로그인","ログイン","登录","登入","Đăng nhập"],
        ["logout"] = ["Sign out","Cerrar sesión","Déconnexion","Esci","Sair","로그아웃","ログアウト","退出登录","登出","Đăng xuất"],
        ["account"] = ["My account","Mi cuenta","Mon compte","Il mio account","Minha conta","내 계정","マイアカウント","我的账户","我的帳戶","Tài khoản"],
        ["nav.toggle"] = ["Open or close menu","Abrir o cerrar menú","Ouvrir ou fermer le menu","Apri o chiudi menu","Abrir ou fechar menu","메뉴 열기/닫기","メニューを開閉","打开或关闭菜单","開啟或關閉選單","Mở hoặc đóng menu"],
        ["page.title"] = ["Free mobile wedding invitation — CodeMaru Wedding","Invitación de boda móvil gratis — CodeMaru Wedding","Invitation de mariage mobile gratuite — CodeMaru Wedding","Invito di nozze mobile gratuito — CodeMaru Wedding","Convite de casamento móvel gratuito — CodeMaru Wedding","무료 모바일 청첩장 — CodeMaru Wedding","無料モバイル結婚招待状 — CodeMaru Wedding","免费移动婚礼请柬 — CodeMaru Wedding","免費行動婚禮喜帖 — CodeMaru Wedding","Thiệp cưới di động miễn phí — CodeMaru Wedding"],
        ["hero.badge"] = ["✨ Free mobile wedding invitation","✨ Invitación móvil gratuita","✨ Invitation mobile gratuite","✨ Invito mobile gratuito","✨ Convite móvel gratuito","✨ 무료 모바일 청첩장","✨ 無料モバイル結婚招待状","✨ 免费移动婚礼请柬","✨ 免費行動婚禮喜帖","✨ Thiệp cưới di động miễn phí"],
        ["hero.title"] = ["Turn our special day\ninto a mobile invitation","Convierte nuestro día especial\nen una invitación móvil","Transformez notre journée spéciale\nen invitation mobile","Trasforma il nostro giorno speciale\nin un invito mobile","Transforme nosso dia especial\nem um convite móvel","우리의 특별한 날을\n모바일 청첩장으로","特別な一日を\nモバイル招待状に","把我们的特别日子\n做成移动婚礼请柬","將我們的特別日子\n製作成行動喜帖","Biến ngày đặc biệt của chúng ta\nthành thiệp cưới di động"],
        ["hero.sub"] = ["Photos, music, maps and guestbook — ready in five minutes","Fotos, música, mapas y libro de visitas — listo en cinco minutos","Photos, musique, cartes et livre d’or — prêt en cinq minutes","Foto, musica, mappe e messaggi — pronto in cinque minuti","Fotos, música, mapas e mensagens — pronto em cinco minutos","사진·음악·지도·방명록 — 5분이면 완성","写真・音楽・地図・ゲストブック — 5分で完成","照片、音乐、地图、留言簿 — 五分钟完成","照片、音樂、地圖、留言簿 — 五分鐘完成","Ảnh, nhạc, bản đồ và sổ lưu bút — hoàn thành trong 5 phút"],
        ["create.now"] = ["Create free now →","Crear gratis ahora →","Créer gratuitement →","Crea gratis ora →","Criar grátis agora →","지금 무료로 만들기 →","今すぐ無料で作成 →","立即免费创建 →","立即免費建立 →","Tạo miễn phí ngay →"],
        ["codemaru.home"] = ["CodeMaru home","Inicio CodeMaru","Accueil CodeMaru","Home CodeMaru","Início CodeMaru","CodeMaru 홈","CodeMaru ホーム","CodeMaru 首页","CodeMaru 首頁","Trang chủ CodeMaru"],
        ["manage.mine"] = ["Manage my invitations","Gestionar mis invitaciones","Gérer mes invitations","Gestisci i miei inviti","Gerenciar meus convites","내 청첩장 관리","招待状を管理","管理我的请柬","管理我的喜帖","Quản lý thiệp của tôi"],
        ["features.photos"] = ["Photo gallery","Galería de fotos","Galerie photo","Galleria foto","Galeria de fotos","사진 갤러리","フォトギャラリー","照片画廊","照片圖庫","Thư viện ảnh"],
        ["features.map"] = ["Smart map","Mapa inteligente","Carte intelligente","Mappa intelligente","Mapa inteligente","스마트 지도","スマート地図","智能地图","智慧地圖","Bản đồ thông minh"],
        ["features.music"] = ["Background music","Música de fondo","Musique de fond","Musica di sottofondo","Música de fundo","배경 음악","BGM","背景音乐","背景音樂","Nhạc nền"],
        ["features.guestbook"] = ["Guestbook","Libro de visitas","Livre d’or","Libro degli ospiti","Livro de visitas","방명록","ゲストブック","留言簿","留言簿","Sổ lưu bút"],
        ["features.accounts"] = ["Payment details","Datos de pago","Coordonnées de paiement","Dati di pagamento","Dados de pagamento","계좌 안내","ご祝儀口座案内","收款信息","收款資訊","Thông tin mừng cưới"],
        ["features.themes"] = ["Five themes","Cinco temas","Cinq thèmes","Cinque temi","Cinco temas","5가지 테마","5つのテーマ","五种主题","五種主題","Năm chủ đề"],
        ["start.title"] = ["Start for free","Comenzar gratis","Commencer gratuitement","Inizia gratis","Começar grátis","무료로 시작하기","無料で始める","免费开始","免費開始","Bắt đầu miễn phí"],
        ["start.desc"] = ["Enter the details below to create your invitation immediately.","Introduce los datos para crear tu invitación al instante.","Saisissez les informations pour créer immédiatement votre invitation.","Inserisci i dati per creare subito il tuo invito.","Informe os dados para criar seu convite agora.","아래 정보를 입력하면 바로 청첩장이 생성됩니다.","以下を入力すると招待状がすぐ作成されます。","填写以下信息即可立即创建请柬。","填寫以下資訊即可立即建立喜帖。","Nhập thông tin dưới đây để tạo thiệp ngay."],
        ["field.slug"] = ["URL address","Dirección URL","Adresse URL","Indirizzo URL","Endereço URL","URL 주소","URL アドレス","URL 地址","URL 位址","Địa chỉ URL"],
        ["field.couple"] = ["Couple names","Nombres de la pareja","Noms du couple","Nomi della coppia","Nomes do casal","커플 이름","お二人の名前","新人姓名","新人姓名","Tên cô dâu chú rể"],
        ["field.date"] = ["Wedding date","Fecha de la boda","Date du mariage","Data del matrimonio","Data do casamento","결혼 예정일","挙式日","婚礼日期","婚禮日期","Ngày cưới"],
        ["field.password"] = ["Admin password","Contraseña de administración","Mot de passe administrateur","Password amministratore","Senha de administração","어드민 비밀번호","管理パスワード","管理员密码","管理員密碼","Mật khẩu quản trị"],
        ["create.button"] = ["✨ Create invitation","✨ Crear invitación","✨ Créer l’invitation","✨ Crea invito","✨ Criar convite","✨ 청첩장 만들기","✨ 招待状を作成","✨ 创建请柬","✨ 建立喜帖","✨ Tạo thiệp cưới"],
        ["creating"] = ["Creating...","Creando...","Création...","Creazione...","Criando...","생성 중...","作成中...","正在创建...","正在建立...","Đang tạo..."],
        ["my.invites"] = ["My invitations","Mis invitaciones","Mes invitations","I miei inviti","Meus convites","내 청첩장","マイ招待状","我的请柬","我的喜帖","Thiệp của tôi"],
        ["public.invites"] = ["💒 Invitation gallery","💒 Galería de invitaciones","💒 Galerie d’invitations","💒 Galleria inviti","💒 Galeria de convites","💒 청첩장 목록","💒 招待状一覧","💒 请柬列表","💒 喜帖列表","💒 Danh sách thiệp cưới"],
        ["admin"] = ["Admin","Administración","Administration","Amministrazione","Administração","관리자","管理者","管理","管理","Quản trị"]
    };

    public string Language { get; private set; } = "ko";
    public string HtmlLanguage => Languages.First(x => x.Code == Language).HtmlLanguage;
    public event Action? Changed;

    public string this[string key]
    {
        get
        {
            if (!Texts.TryGetValue(key, out var values)) return key;
            var index = Array.IndexOf(Codes, Language);
            return values[index < 0 ? 5 : index];
        }
    }

    public void SetLanguage(string? language)
    {
        var normalized = language?.Trim().ToLowerInvariant().Replace('_', '-');
        if (!Codes.Contains(normalized)) normalized = "ko";
        if (Language == normalized) return;
        Language = normalized!;
        Changed?.Invoke();
    }
}
