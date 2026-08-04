using System.Globalization;

namespace PortfolioApp.Services;

/// <summary>
/// Shared UI language state for the Portfolio portal and its administration screens.
/// Tenant-authored portfolio content is intentionally not translated.
/// </summary>
public sealed class PortfolioLocalization
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

    private static readonly string[] Codes = Languages.Select(item => item.Code).ToArray();

    // Order: en, es, fr, it, pt, ko, ja, zh-Hans, zh-Hant, vi.
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
        ["portfolio"] = ["Portfolio", "Portafolio", "Portfolio", "Portfolio", "Portfólio", "포트폴리오", "ポートフォリオ", "作品集", "作品集", "Hồ sơ năng lực"],
        ["guide"] = ["Guide", "Guía", "Guide", "Guida", "Guia", "사용 안내", "利用ガイド", "使用指南", "使用指南", "Hướng dẫn"],
        ["admin"] = ["Admin", "Administración", "Administration", "Amministrazione", "Administração", "관리", "管理", "管理", "管理", "Quản trị"],
        ["super.admin"] = ["Super admin", "Superadministrador", "Super administrateur", "Super amministratore", "Super administrador", "슈퍼 어드민", "スーパー管理者", "超级管理员", "超級管理員", "Quản trị cấp cao"],
        ["page.title"] = ["Free online portfolio — CodeMaru Portfolio", "Portafolio en línea gratis — CodeMaru Portfolio", "Portfolio en ligne gratuit — CodeMaru Portfolio", "Portfolio online gratuito — CodeMaru Portfolio", "Portfólio online gratuito — CodeMaru Portfolio", "무료 온라인 포트폴리오 — CodeMaru Portfolio", "無料オンラインポートフォリオ — CodeMaru Portfolio", "免费在线作品集 — CodeMaru Portfolio", "免費線上作品集 — CodeMaru Portfolio", "Hồ sơ năng lực trực tuyến miễn phí — CodeMaru Portfolio"],
        ["seo.description"] = ["Present projects, work experience and achievements in one clear portfolio.", "Presenta proyectos, experiencia y logros en un portafolio claro.", "Présentez projets, expérience et réalisations dans un portfolio clair.", "Presenta progetti, esperienza e risultati in un portfolio chiaro.", "Apresente projetos, experiência e resultados em um portfólio claro.", "프로젝트와 업무 경험, 성과를 하나의 보기 쉬운 포트폴리오로 정리하세요.", "プロジェクト、職務経験、実績を見やすいポートフォリオにまとめましょう。", "用一个清晰的作品集展示项目、工作经历和成果。", "以清晰的作品集展示專案、工作經歷與成果。", "Trình bày dự án, kinh nghiệm và thành quả trong một hồ sơ rõ ràng."],
        ["hero.badge"] = ["✨ Free online portfolio", "✨ Portafolio en línea gratis", "✨ Portfolio en ligne gratuit", "✨ Portfolio online gratuito", "✨ Portfólio online gratuito", "✨ 무료 온라인 포트폴리오", "✨ 無料オンラインポートフォリオ", "✨ 免费在线作品集", "✨ 免費線上作品集", "✨ Hồ sơ năng lực trực tuyến miễn phí"],
        ["hero.title"] = ["Turn your experience into\na clear portfolio", "Convierte tu experiencia en\nun portafolio claro", "Transformez votre expérience en\nun portfolio clair", "Trasforma la tua esperienza in\nun portfolio chiaro", "Transforme sua experiência em\num portfólio claro", "나의 경험을\n보기 좋은 포트폴리오로", "経験を\n伝わるポートフォリオに", "把你的经历整理成\n清晰的作品集", "將你的經歷整理成\n清晰的作品集", "Biến kinh nghiệm của bạn thành\nmột hồ sơ rõ ràng"],
        ["hero.sub"] = ["Projects · work experience · career profile · contact — all in one place", "Proyectos · experiencia · perfil profesional · contacto — todo en un lugar", "Projets · expérience · profil professionnel · contact — tout au même endroit", "Progetti · esperienza · profilo professionale · contatti — tutto in un unico posto", "Projetos · experiência · perfil profissional · contato — tudo em um só lugar", "프로젝트 · 업무 경험 · 경력 정보 · 연락처 — 한 곳에서", "プロジェクト・職務経験・キャリア情報・連絡先を一か所に", "项目、工作经历、职业资料和联系方式，一站整理", "專案、工作經歷、職涯資料與聯絡方式，一站整理", "Dự án · kinh nghiệm · hồ sơ nghề nghiệp · liên hệ — tất cả ở một nơi"],
        ["create.now"] = ["Create free now →", "Crear gratis ahora →", "Créer gratuitement →", "Crea gratis ora →", "Criar grátis agora →", "지금 무료로 만들기 →", "今すぐ無料で作成 →", "立即免费创建 →", "立即免費建立 →", "Tạo miễn phí ngay →"],
        ["manage.mine"] = ["Manage my portfolios", "Gestionar mis portafolios", "Gérer mes portfolios", "Gestisci i miei portfolio", "Gerenciar meus portfólios", "내 포트폴리오 관리", "自分のポートフォリオを管理", "管理我的作品集", "管理我的作品集", "Quản lý hồ sơ của tôi"],
        ["service.notice"] = ["⚡ Temporary delays may occur on this free service.", "⚡ Este servicio gratuito puede tener demoras temporales.", "⚡ Ce service gratuit peut subir des ralentissements.", "⚡ Il servizio gratuito può subire ritardi.", "⚡ Este serviço gratuito pode apresentar atrasos.", "⚡ 무료 서비스 특성상 일시적인 접속 지연이 발생할 수 있습니다.", "⚡ 無料サービスのため一時的に接続が遅くなる場合があります。", "⚡ 免费服务可能会暂时出现访问延迟。", "⚡ 免費服務可能會暫時出現連線延遲。", "⚡ Dịch vụ miễn phí đôi khi có thể truy cập chậm."],
        ["service.cleanup"] = ["Accounts without any projects are removed after 24 hours.", "Las cuentas sin proyectos se eliminan después de 24 horas.", "Les comptes sans projet sont supprimés après 24 heures.", "Gli account senza progetti vengono rimossi dopo 24 ore.", "Contas sem projetos são removidas após 24 horas.", "프로젝트가 없는 계정은 24시간 후 자동 삭제됩니다.", "プロジェクトのないアカウントは24時間後に削除されます。", "没有项目的账户将在24小时后删除。", "沒有專案的帳戶將於24小時後刪除。", "Tài khoản không có dự án sẽ bị xóa sau 24 giờ."],
        ["feature.projects"] = ["Work samples", "Muestras de trabajo", "Réalisations", "Lavori", "Trabalhos", "업무 사례", "実績・制作例", "工作案例", "工作案例", "Sản phẩm công việc"],
        ["feature.projects.desc"] = ["Organize services, projects and results in clear sections.", "Organiza servicios, proyectos y resultados en secciones claras.", "Organisez services, projets et résultats en sections claires.", "Organizza servizi, progetti e risultati in sezioni chiare.", "Organize serviços, projetos e resultados em seções claras.", "서비스, 프로젝트, 성과를 보기 쉬운 영역으로 정리합니다.", "サービス、プロジェクト、成果を見やすく整理します。", "清晰整理服务、项目与成果。", "清楚整理服務、專案與成果。", "Sắp xếp dịch vụ, dự án và kết quả theo từng mục rõ ràng."],
        ["feature.career"] = ["Career profile", "Perfil profesional", "Profil professionnel", "Profilo professionale", "Perfil profissional", "경력 정보", "キャリア情報", "职业资料", "職涯資料", "Hồ sơ nghề nghiệp"],
        ["feature.career.desc"] = ["Summarize experience, education and strengths without technical jargon.", "Resume experiencia, formación y fortalezas sin jerga técnica.", "Résumez expérience, formation et points forts sans jargon.", "Riassumi esperienza, studi e punti di forza senza gergo tecnico.", "Resuma experiência, formação e pontos fortes sem jargão técnico.", "경력, 학력, 강점을 어려운 기술 용어 없이 정리합니다.", "職歴、学歴、強みを難しい専門用語なしでまとめます。", "不用技术术语也能清晰整理经历、教育和优势。", "不用艱深術語也能清楚整理經歷、教育與優勢。", "Tóm tắt kinh nghiệm, học vấn và thế mạnh bằng ngôn ngữ dễ hiểu."],
        ["feature.search"] = ["Easy to browse", "Fácil de explorar", "Navigation simple", "Facile da consultare", "Fácil de navegar", "쉬운 탐색", "見つけやすい構成", "轻松浏览", "輕鬆瀏覽", "Dễ tìm kiếm"],
        ["feature.search.desc"] = ["Visitors can find the right work by category, keyword or tag.", "Los visitantes encuentran trabajos por categoría, palabra o etiqueta.", "Les visiteurs trouvent vos travaux par catégorie, mot-clé ou tag.", "I visitatori trovano i lavori per categoria, parola o tag.", "Visitantes encontram trabalhos por categoria, palavra ou tag.", "방문자가 분야, 검색어, 태그로 원하는 사례를 빠르게 찾습니다.", "分野、キーワード、タグから実績を探せます。", "访客可按分类、关键词或标签快速查找案例。", "訪客可依分類、關鍵字或標籤快速查找案例。", "Khách xem có thể tìm theo danh mục, từ khóa hoặc thẻ."],
        ["feature.message"] = ["Inquiries", "Consultas", "Demandes", "Richieste", "Consultas", "문의 받기", "お問い合わせ", "接收咨询", "接收洽詢", "Nhận liên hệ"],
        ["feature.message.desc"] = ["Receive work and collaboration inquiries directly from the public page.", "Recibe consultas de trabajo y colaboración desde la página pública.", "Recevez des demandes de mission et de collaboration depuis la page publique.", "Ricevi richieste di lavoro e collaborazione dalla pagina pubblica.", "Receba consultas de trabalho e parceria pela página pública.", "공개 페이지에서 업무 및 협업 문의를 바로 받을 수 있습니다.", "公開ページから仕事や協業の相談を受け取れます。", "可直接从公开页面接收工作与合作咨询。", "可直接從公開頁面接收工作與合作洽詢。", "Nhận yêu cầu công việc và hợp tác trực tiếp từ trang công khai."],
        ["feature.themes"] = ["Five designs", "Cinco diseños", "Cinq styles", "Cinque design", "Cinco designs", "5가지 디자인", "5種類のデザイン", "五种设计", "五種設計", "5 kiểu thiết kế"],
        ["feature.links"] = ["Useful links", "Enlaces útiles", "Liens utiles", "Link utili", "Links úteis", "자료 링크", "関連リンク", "相关链接", "相關連結", "Liên kết hữu ích"],
        ["create.title"] = ["Start free", "Empieza gratis", "Commencer gratuitement", "Inizia gratis", "Comece grátis", "무료로 시작하기", "無料で始める", "免费开始", "免費開始", "Bắt đầu miễn phí"],
        ["create.desc"] = ["Enter the basic information and your portfolio will be ready to edit.", "Introduce la información básica y tu portafolio quedará listo para editar.", "Saisissez les informations de base et votre portfolio sera prêt à modifier.", "Inserisci le informazioni di base e il portfolio sarà pronto da modificare.", "Insira as informações básicas e o portfólio ficará pronto para editar.", "기본 정보를 입력하면 바로 편집할 수 있는 포트폴리오가 만들어집니다.", "基本情報を入力すると、すぐ編集できるポートフォリオが作成されます。", "填写基本信息后即可开始编辑作品集。", "填寫基本資料後即可開始編輯作品集。", "Nhập thông tin cơ bản để tạo hồ sơ sẵn sàng chỉnh sửa."],
        ["url.address"] = ["Public address", "Dirección pública", "Adresse publique", "Indirizzo pubblico", "Endereço público", "공개 주소", "公開アドレス", "公开地址", "公開網址", "Địa chỉ công khai"],
        ["url.hint"] = ["Letters, numbers and hyphens only", "Solo letras, números y guiones", "Lettres, chiffres et tirets uniquement", "Solo lettere, numeri e trattini", "Apenas letras, números e hífens", "영문, 숫자, 하이픈만 사용", "英数字とハイフンのみ", "仅限字母、数字和连字符", "僅限英文字母、數字與連字號", "Chỉ dùng chữ cái, số và dấu gạch nối"],
        ["owner.name"] = ["Name or organization", "Nombre u organización", "Nom ou organisation", "Nome o organizzazione", "Nome ou organização", "이름 또는 조직명", "氏名または組織名", "姓名或组织名称", "姓名或組織名稱", "Tên hoặc tổ chức"],
        ["admin.password"] = ["Admin password", "Contraseña de administrador", "Mot de passe administrateur", "Password amministratore", "Senha de administrador", "관리자 비밀번호", "管理者パスワード", "管理员密码", "管理員密碼", "Mật khẩu quản trị"],
        ["admin.password.hint"] = ["Used when editing the portfolio", "Se usa para editar el portafolio", "Utilisé pour modifier le portfolio", "Usata per modificare il portfolio", "Usada ao editar o portfólio", "포트폴리오 편집 시 사용", "ポートフォリオ編集時に使用", "编辑作品集时使用", "編輯作品集時使用", "Dùng khi chỉnh sửa hồ sơ"],
        ["create.portfolio"] = ["Create portfolio", "Crear portafolio", "Créer le portfolio", "Crea portfolio", "Criar portfólio", "포트폴리오 만들기", "ポートフォリオを作成", "创建作品集", "建立作品集", "Tạo hồ sơ"],
        ["creating"] = ["Creating…", "Creando…", "Création…", "Creazione…", "Criando…", "생성 중…", "作成中…", "正在创建…", "建立中…", "Đang tạo…"],
        ["my.portfolios"] = ["My portfolios", "Mis portafolios", "Mes portfolios", "I miei portfolio", "Meus portfólios", "내 포트폴리오", "自分のポートフォリオ", "我的作品集", "我的作品集", "Hồ sơ của tôi"],
        ["public.portfolios"] = ["Public portfolios", "Portafolios públicos", "Portfolios publics", "Portfolio pubblici", "Portfólios públicos", "공개 포트폴리오", "公開ポートフォリオ", "公开作品集", "公開作品集", "Hồ sơ công khai"],
        ["overview"] = ["Overview", "Resumen", "Vue d’ensemble", "Panoramica", "Visão geral", "개요", "概要", "概览", "總覽", "Tổng quan"],
        ["projects"] = ["Work & projects", "Trabajos y proyectos", "Travaux et projets", "Lavori e progetti", "Trabalhos e projetos", "업무 사례", "実績・プロジェクト", "工作与项目", "工作與專案", "Công việc & dự án"],
        ["career.profile"] = ["Career profile", "Perfil profesional", "Profil professionnel", "Profilo professionale", "Perfil profissional", "경력 정보", "キャリア情報", "职业资料", "職涯資料", "Hồ sơ nghề nghiệp"],
        ["messages"] = ["Messages", "Mensajes", "Messages", "Messaggi", "Mensagens", "메시지", "メッセージ", "消息", "訊息", "Tin nhắn"],
        ["design"] = ["Design", "Diseño", "Design", "Design", "Design", "디자인", "デザイン", "设计", "設計", "Thiết kế"],
        ["settings"] = ["Portfolio settings", "Configuración del portafolio", "Paramètres du portfolio", "Impostazioni portfolio", "Configurações do portfólio", "포트폴리오 설정", "ポートフォリオ設定", "作品集设置", "作品集設定", "Cài đặt hồ sơ"],
        ["public.page"] = ["View public page", "Ver página pública", "Voir la page publique", "Vedi pagina pubblica", "Ver página pública", "공개 페이지 보기", "公開ページを見る", "查看公开页面", "查看公開頁面", "Xem trang công khai"],
        ["refresh"] = ["Refresh", "Actualizar", "Actualiser", "Aggiorna", "Atualizar", "새로고침", "更新", "刷新", "重新整理", "Làm mới"],
        ["save"] = ["Save", "Guardar", "Enregistrer", "Salva", "Salvar", "저장", "保存", "保存", "儲存", "Lưu"],
        ["cancel"] = ["Cancel", "Cancelar", "Annuler", "Annulla", "Cancelar", "취소", "キャンセル", "取消", "取消", "Hủy"],
        ["edit"] = ["Edit", "Editar", "Modifier", "Modifica", "Editar", "편집", "編集", "编辑", "編輯", "Chỉnh sửa"],
        ["delete"] = ["Delete", "Eliminar", "Supprimer", "Elimina", "Excluir", "삭제", "削除", "删除", "刪除", "Xóa"],
        ["add"] = ["Add", "Añadir", "Ajouter", "Aggiungi", "Adicionar", "추가", "追加", "添加", "新增", "Thêm"],
        ["back"] = ["Back", "Volver", "Retour", "Indietro", "Voltar", "뒤로", "戻る", "返回", "返回", "Quay lại"],
        ["search"] = ["Search", "Buscar", "Rechercher", "Cerca", "Pesquisar", "검색", "検索", "搜索", "搜尋", "Tìm kiếm"],
        ["dashboard"] = ["Dashboard", "Panel", "Tableau de bord", "Dashboard", "Painel", "대시보드", "ダッシュボード", "仪表板", "儀表板", "Bảng điều khiển"],
        ["dashboard.super.title"] = ["Portfolio dashboard", "Panel de portafolios", "Tableau de bord Portfolio", "Dashboard Portfolio", "Painel de portfólios", "Portfolio 대시보드", "Portfolio ダッシュボード", "作品集仪表板", "作品集儀表板", "Bảng điều khiển Portfolio"],
        ["dashboard.super.sub"] = ["Review portfolio service status at a glance.", "Revisa el estado del servicio de un vistazo.", "Consultez l’état du service en un coup d’œil.", "Controlla lo stato del servizio a colpo d’occhio.", "Veja o status do serviço rapidamente.", "포트폴리오 서비스 현황을 한눈에 확인하세요.", "ポートフォリオサービスの状況を一目で確認できます。", "一览作品集服务状态。", "一覽作品集服務狀態。", "Xem nhanh tình trạng dịch vụ hồ sơ."],
        ["dashboard.tenant.sub"] = ["Manage the content and public status of your portfolio.", "Gestiona el contenido y el estado público de tu portafolio.", "Gérez le contenu et la visibilité de votre portfolio.", "Gestisci contenuti e visibilità del portfolio.", "Gerencie o conteúdo e a visibilidade do portfólio.", "내 포트폴리오의 콘텐츠와 공개 상태를 관리하세요.", "ポートフォリオの内容と公開状態を管理します。", "管理作品集内容和公开状态。", "管理作品集內容與公開狀態。", "Quản lý nội dung và trạng thái công khai của hồ sơ."],
        ["stat.portfolios"] = ["Total portfolios", "Portafolios totales", "Total des portfolios", "Portfolio totali", "Total de portfólios", "전체 포트폴리오", "ポートフォリオ総数", "作品集总数", "作品集總數", "Tổng hồ sơ"],
        ["stat.projects"] = ["Registered work", "Trabajos registrados", "Travaux enregistrés", "Lavori registrati", "Trabalhos cadastrados", "등록된 업무 사례", "登録済み実績", "已登记案例", "已登記案例", "Công việc đã đăng"],
        ["stat.featured"] = ["Featured on home", "Destacados en inicio", "Mis en avant", "In evidenza", "Em destaque", "홈 노출", "ホーム掲載", "首页展示", "首頁展示", "Hiển thị trang chủ"],
        ["stat.public"] = ["Public work", "Trabajos públicos", "Travaux publics", "Lavori pubblici", "Trabalhos públicos", "공개 업무 사례", "公開実績", "公开案例", "公開案例", "Công việc công khai"],
        ["stat.unread"] = ["Unread messages", "Mensajes sin leer", "Messages non lus", "Messaggi non letti", "Mensagens não lidas", "미확인 메시지", "未読メッセージ", "未读消息", "未讀訊息", "Tin nhắn chưa đọc"],
        ["stat.completion"] = ["Profile completion", "Perfil completado", "Profil complété", "Completamento profilo", "Perfil completo", "프로필 완성도", "プロフィール完成度", "资料完整度", "資料完整度", "Mức hoàn thiện hồ sơ"],
        ["recent.portfolios"] = ["Recent portfolios", "Portafolios recientes", "Portfolios récents", "Portfolio recenti", "Portfólios recentes", "최근 포트폴리오", "最近のポートフォリオ", "最近作品集", "最近作品集", "Hồ sơ gần đây"],
        ["recent.projects"] = ["Recent work", "Trabajos recientes", "Travaux récents", "Lavori recenti", "Trabalhos recentes", "최근 업무 사례", "最近の実績", "最近案例", "最近案例", "Công việc gần đây"],
        ["new.portfolio"] = ["New portfolio", "Nuevo portafolio", "Nouveau portfolio", "Nuovo portfolio", "Novo portfólio", "새 포트폴리오", "新しいポートフォリオ", "新建作品集", "新增作品集", "Hồ sơ mới"],
        ["new.project"] = ["Add work", "Añadir trabajo", "Ajouter un travail", "Aggiungi lavoro", "Adicionar trabalho", "업무 사례 추가", "実績を追加", "添加案例", "新增案例", "Thêm công việc"],
        ["project.personal"] = ["Personal projects", "Proyectos personales", "Projets personnels", "Progetti personali", "Projetos pessoais", "개인 프로젝트", "個人プロジェクト", "个人项目", "個人專案", "Dự án cá nhân"],
        ["project.work"] = ["Professional work", "Trabajo profesional", "Réalisations professionnelles", "Lavori professionali", "Trabalhos profissionais", "업무 사례", "業務実績", "工作案例", "工作案例", "Công việc chuyên môn"],
        ["project.public"] = ["Public activities", "Actividades públicas", "Activités publiques", "Attività pubbliche", "Atividades públicas", "공개 활동", "公開活動", "公开活动", "公開活動", "Hoạt động công khai"],
        ["project.none"] = ["No work has been added yet.", "Aún no se ha añadido ningún trabajo.", "Aucun travail n’a encore été ajouté.", "Nessun lavoro è stato ancora aggiunto.", "Nenhum trabalho foi adicionado ainda.", "아직 등록된 업무 사례가 없습니다.", "実績はまだ登録されていません。", "尚未添加案例。", "尚未新增案例。", "Chưa có công việc nào."],
        ["detail.back"] = ["Back to portfolio", "Volver al portafolio", "Retour au portfolio", "Torna al portfolio", "Voltar ao portfólio", "포트폴리오로 돌아가기", "ポートフォリオに戻る", "返回作品集", "返回作品集", "Quay lại hồ sơ"],
        ["detail.overview"] = ["Overview", "Resumen", "Présentation", "Panoramica", "Visão geral", "소개", "概要", "简介", "簡介", "Giới thiệu"],
        ["detail.results"] = ["Key results", "Resultados clave", "Résultats clés", "Risultati principali", "Principais resultados", "주요 성과", "主な成果", "主要成果", "主要成果", "Kết quả chính"],
        ["detail.tools"] = ["Tools & capabilities", "Herramientas y capacidades", "Outils et compétences", "Strumenti e competenze", "Ferramentas e capacidades", "활용 도구 및 역량", "使用ツール・スキル", "工具与能力", "工具與能力", "Công cụ & năng lực"],
        ["detail.gallery"] = ["Images", "Imágenes", "Images", "Immagini", "Imagens", "이미지", "画像", "图片", "圖片", "Hình ảnh"],
        ["lightbox.open"] = ["Open image {0}", "Abrir imagen {0}", "Ouvrir l’image {0}", "Apri immagine {0}", "Abrir imagem {0}", "이미지 {0} 크게 보기", "画像{0}を拡大表示", "放大查看图片 {0}", "放大檢視圖片 {0}", "Mở rộng hình {0}"],
        ["lightbox.close"] = ["Close enlarged image", "Cerrar imagen ampliada", "Fermer l’image agrandie", "Chiudi immagine ingrandita", "Fechar imagem ampliada", "확대 이미지 닫기", "拡大画像を閉じる", "关闭大图", "關閉大圖", "Đóng hình phóng to"],
        ["lightbox.previous"] = ["Previous image", "Imagen anterior", "Image précédente", "Immagine precedente", "Imagem anterior", "이전 이미지", "前の画像", "上一张图片", "上一張圖片", "Hình trước"],
        ["lightbox.next"] = ["Next image", "Imagen siguiente", "Image suivante", "Immagine successiva", "Próxima imagem", "다음 이미지", "次の画像", "下一张图片", "下一張圖片", "Hình tiếp theo"],
        ["detail.video"] = ["Videos", "Vídeos", "Vidéos", "Video", "Vídeos", "동영상", "動画", "视频", "影片", "Video"],
        ["detail.links"] = ["Related links", "Enlaces relacionados", "Liens associés", "Link correlati", "Links relacionados", "관련 자료", "関連リンク", "相关资料", "相關資料", "Liên kết liên quan"],
        ["view.details"] = ["View details", "Ver detalles", "Voir le détail", "Vedi dettagli", "Ver detalhes", "자세히 보기", "詳細を見る", "查看详情", "查看詳情", "Xem chi tiết"],
        ["resume"] = ["Career", "Trayectoria", "Parcours", "Carriera", "Carreira", "경력", "経歴", "经历", "經歷", "Kinh nghiệm"],
        ["skills"] = ["Strengths & tools", "Fortalezas y herramientas", "Compétences et outils", "Punti di forza e strumenti", "Pontos fortes e ferramentas", "강점 및 활용 도구", "強み・使用ツール", "优势与工具", "優勢與工具", "Thế mạnh & công cụ"],
        ["experience"] = ["Experience", "Experiencia", "Expérience", "Esperienza", "Experiência", "경력", "職歴", "工作经历", "工作經歷", "Kinh nghiệm"],
        ["education"] = ["Education", "Formación", "Formation", "Formazione", "Formação", "학력", "学歴", "教育经历", "教育經歷", "Học vấn"],
        ["send"] = ["Send message", "Enviar mensaje", "Envoyer", "Invia messaggio", "Enviar mensagem", "메시지 보내기", "送信", "发送消息", "傳送訊息", "Gửi tin nhắn"],
        ["name"] = ["Name", "Nombre", "Nom", "Nome", "Nome", "이름", "名前", "姓名", "姓名", "Tên"],
        ["email"] = ["Email", "Correo", "E-mail", "Email", "E-mail", "이메일", "メール", "邮箱", "電子郵件", "Email"],
        ["message"] = ["Message", "Mensaje", "Message", "Messaggio", "Mensagem", "메시지", "メッセージ", "消息", "訊息", "Tin nhắn"],
        ["notfound.portfolio"] = ["This portfolio could not be found.", "No se encontró este portafolio.", "Ce portfolio est introuvable.", "Questo portfolio non è stato trovato.", "Este portfólio não foi encontrado.", "포트폴리오를 찾을 수 없습니다.", "ポートフォリオが見つかりません。", "找不到此作品集。", "找不到此作品集。", "Không tìm thấy hồ sơ này."],
        ["filter.search"] = ["Search by title, description or tag", "Buscar por título, descripción o etiqueta", "Rechercher par titre, description ou tag", "Cerca per titolo, descrizione o tag", "Pesquisar por título, descrição ou tag", "제목, 설명 또는 태그 검색", "タイトル・説明・タグで検索", "按标题、说明或标签搜索", "依標題、說明或標籤搜尋", "Tìm theo tiêu đề, mô tả hoặc thẻ"],
        ["tag"] = ["Tag", "Etiqueta", "Tag", "Tag", "Etiqueta", "태그", "タグ", "标签", "標籤", "Thẻ"],
        ["all.tags"] = ["All tags", "Todas las etiquetas", "Tous les tags", "Tutti i tag", "Todas as etiquetas", "모든 태그", "すべてのタグ", "全部标签", "所有標籤", "Tất cả thẻ"],
        ["category"] = ["Category", "Categoría", "Catégorie", "Categoria", "Categoria", "구분", "分類", "分类", "分類", "Danh mục"],
        ["all"] = ["All", "Todo", "Tout", "Tutto", "Tudo", "전체", "すべて", "全部", "全部", "Tất cả"],
        ["email.optional"] = ["Email (optional)", "Correo (opcional)", "E-mail (facultatif)", "Email (facoltativa)", "E-mail (opcional)", "이메일 (선택)", "メール（任意）", "邮箱（选填）", "電子郵件（選填）", "Email (không bắt buộc)"],
        ["contact.placeholder"] = ["Tell me briefly what you would like to discuss.", "Cuéntame brevemente qué te gustaría conversar.", "Décrivez brièvement votre demande.", "Descrivi brevemente di cosa vuoi parlare.", "Conte brevemente o que deseja conversar.", "문의하실 내용을 간단히 적어 주세요.", "ご相談内容を簡単にご記入ください。", "请简要填写咨询内容。", "請簡要填寫洽詢內容。", "Hãy mô tả ngắn gọn nội dung bạn muốn trao đổi."],
        ["contact.success"] = ["Your message has been sent.", "Tu mensaje ha sido enviado.", "Votre message a été envoyé.", "Il messaggio è stato inviato.", "Sua mensagem foi enviada.", "메시지를 보냈습니다.", "メッセージを送信しました。", "消息已发送。", "訊息已傳送。", "Đã gửi tin nhắn."],
        ["contact.required"] = ["Please enter your name and message.", "Escribe tu nombre y mensaje.", "Saisissez votre nom et votre message.", "Inserisci nome e messaggio.", "Informe seu nome e a mensagem.", "이름과 메시지를 입력해 주세요.", "名前とメッセージを入力してください。", "请输入姓名和消息。", "請輸入姓名與訊息。", "Vui lòng nhập tên và tin nhắn."],
        ["contact.too.long"] = ["One or more fields are too long.", "Uno o más campos son demasiado largos.", "Un ou plusieurs champs sont trop longs.", "Uno o più campi sono troppo lunghi.", "Um ou mais campos são longos demais.", "입력 가능한 글자 수를 초과했습니다.", "入力可能な文字数を超えています。", "一个或多个字段过长。", "一個或多個欄位過長。", "Một hoặc nhiều trường quá dài."],
        ["contact.invalid"] = ["Check the email address and entered content.", "Revisa el correo y el contenido.", "Vérifiez l’adresse e-mail et le contenu.", "Controlla l’indirizzo email e il contenuto.", "Verifique o e-mail e o conteúdo.", "이메일 또는 입력 내용을 확인해 주세요.", "メールアドレスと入力内容をご確認ください。", "请检查邮箱地址和填写内容。", "請檢查電子郵件與填寫內容。", "Kiểm tra email và nội dung đã nhập."],
        ["contact.rate"] = ["Please wait a moment before sending again.", "Espera un momento antes de volver a enviar.", "Veuillez patienter avant de renvoyer.", "Attendi un momento prima di inviare di nuovo.", "Aguarde um momento antes de enviar novamente.", "잠시 후 다시 시도해 주세요.", "しばらくしてからもう一度お試しください。", "请稍后再试。", "請稍後再試。", "Vui lòng đợi một chút trước khi gửi lại."]
    };

    public string Language { get; private set; } = "ko";
    public string HtmlLanguage => Languages.First(item => item.Code == Language).HtmlLanguage;
    public event Action? Changed;

    public string this[string key]
    {
        get
        {
            if (!Texts.TryGetValue(key, out string[]? values))
            {
                return key;
            }

            int index = Array.IndexOf(Codes, Language);
            return values[index < 0 ? 5 : index];
        }
    }

    public string Format(string key, params object?[] args)
    {
        string template = this[key];
        if (args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(CultureInfo.CurrentCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public static string NormalizeLanguageCode(string? language)
    {
        string? normalized = language?.Trim().ToLowerInvariant().Replace('_', '-');
        return normalized switch
        {
            "zh" or "zh-cn" or "zh-sg" => "zh-hans",
            "zh-tw" or "zh-hk" or "zh-mo" => "zh-hant",
            _ => normalized ?? "ko"
        };
    }

    public void SetLanguage(string? language)
    {
        string normalized = NormalizeLanguageCode(language);
        if (!Codes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            normalized = "ko";
        }

        if (string.Equals(Language, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Language = normalized;
        Changed?.Invoke();
    }
}
