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
        ["screen.settings"] = ["Screen settings","Ajustes de pantalla","Réglages d’affichage","Impostazioni schermo","Configurações da tela","화면 설정","画面設定","屏幕设置","畫面設定","Cài đặt màn hình"],
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
        ,["seo.description"] = ["Create a free mobile wedding invitation with photos, music, maps, guestbook and payment details in five minutes.","Crea en cinco minutos una invitación móvil gratuita con fotos, música, mapas, libro de visitas y datos de pago.","Créez en cinq minutes une invitation mobile gratuite avec photos, musique, cartes, livre d’or et informations de paiement.","Crea in cinque minuti un invito mobile gratuito con foto, musica, mappe, messaggi e dati di pagamento.","Crie em cinco minutos um convite móvel gratuito com fotos, música, mapas, mensagens e dados de pagamento.","사진·음악·지도·방명록·계좌 안내까지 5분이면 완성되는 무료 모바일 청첩장 서비스입니다.","写真・音楽・地図・ゲストブック・ご祝儀口座案内まで、5分で作れる無料モバイル招待状です。","包含照片、音乐、地图、留言簿和收款信息，五分钟即可创建免费移动婚礼请柬。","包含照片、音樂、地圖、留言簿與收款資訊，五分鐘即可建立免費行動喜帖。","Tạo thiệp cưới di động miễn phí với ảnh, nhạc, bản đồ, sổ lưu bút và thông tin mừng cưới chỉ trong 5 phút."],
        ["service.notice"] = ["⚡ Temporary delays may occur on this free service. Contact:","⚡ Este servicio gratuito puede sufrir demoras temporales. Contacto:","⚡ Ce service gratuit peut subir des ralentissements temporaires. Contact :","⚡ Il servizio gratuito può subire ritardi temporanei. Contatto:","⚡ Este serviço gratuito pode apresentar atrasos temporários. Contato:","⚡ 무료 서비스 특성상 일시적인 접속 지연이 발생할 수 있습니다. 문의:","⚡ 無料サービスのため、一時的に接続が遅くなる場合があります。お問い合わせ:","⚡ 免费服务可能会暂时出现访问延迟。联系：","⚡ 免費服務可能會暫時出現連線延遲。聯絡：","⚡ Dịch vụ miễn phí đôi khi có thể truy cập chậm. Liên hệ:"],
        ["service.cleanup"] = ["🗑 Accounts without uploaded photos are automatically deleted after 24 hours.","🗑 Las cuentas sin fotos se eliminan automáticamente después de 24 horas.","🗑 Les comptes sans photo sont supprimés automatiquement après 24 heures.","🗑 Gli account senza foto vengono eliminati automaticamente dopo 24 ore.","🗑 Contas sem fotos são excluídas automaticamente após 24 horas.","🗑 사진 미업로드 계정은 24시간 후 자동 삭제됩니다.","🗑 写真未アップロードのアカウントは24時間後に自動削除されます。","🗑 未上传照片的账户将在24小时后自动删除。","🗑 未上傳照片的帳戶將於24小時後自動刪除。","🗑 Tài khoản chưa tải ảnh sẽ tự động bị xóa sau 24 giờ."],
        ["features.photos.desc"] = ["Share wedding photos in a gallery and enjoy a slideshow.","Comparte fotos de boda en una galería y disfruta de la presentación.","Partagez vos photos dans une galerie et profitez du diaporama.","Condividi le foto in una galleria e guarda la presentazione.","Compartilhe fotos em uma galeria e veja a apresentação.","웨딩 사진을 갤러리로 공유하고 슬라이드쇼로 감상","写真をギャラリーで共有し、スライドショーで楽しめます。","在画廊中分享婚礼照片并欣赏幻灯片。","在相簿中分享婚禮照片並欣賞投影片。","Chia sẻ ảnh cưới trong thư viện và xem trình chiếu."],
        ["features.map.desc"] = ["Open directions with KakaoMap, Naver, Atlan or T map in one tap.","Abre indicaciones con KakaoMap, Naver, Atlan o T map con un toque.","Ouvrez l’itinéraire avec KakaoMap, Naver, Atlan ou T map en un geste.","Apri le indicazioni con KakaoMap, Naver, Atlan o T map con un tocco.","Abra rotas com KakaoMap, Naver, Atlan ou T map com um toque.","카카오맵·네이버·아틀란·T맵 원터치 길찾기","KakaoMap・Naver・Atlan・T mapでワンタップ経路検索。","一键使用 KakaoMap、Naver、Atlan 或 T map 导航。","一鍵使用 KakaoMap、Naver、Atlan 或 T map 導航。","Mở chỉ đường bằng KakaoMap, Naver, Atlan hoặc T map chỉ với một chạm."],
        ["features.music.desc"] = ["Set your favorite music as the background.","Usa tu música favorita como fondo.","Choisissez votre musique préférée en fond sonore.","Imposta la tua musica preferita come sottofondo.","Defina sua música favorita como fundo.","좋아하는 음악을 배경으로 설정","お気に入りの音楽をBGMに設定できます。","将喜欢的音乐设为背景音乐。","將喜歡的音樂設為背景音樂。","Đặt bài nhạc yêu thích làm nhạc nền."],
        ["features.guestbook.desc"] = ["Receive congratulatory messages from guests online.","Recibe mensajes de felicitación de los invitados en línea.","Recevez en ligne les messages de félicitations de vos invités.","Ricevi online i messaggi di auguri degli invitati.","Receba online mensagens de felicitações dos convidados.","하객들의 축하 메시지를 온라인으로 받기","ゲストからのお祝いメッセージをオンラインで受け取れます。","在线接收宾客的祝福留言。","線上接收賓客的祝福留言。","Nhận lời chúc của khách mời trực tuyến."],
        ["features.accounts.desc"] = ["Copy the couple’s and parents’ payment details with one tap.","Copia con un toque los datos de pago de la pareja y sus padres.","Copiez en un geste les informations de paiement du couple et des parents.","Copia con un tocco i dati di pagamento degli sposi e dei genitori.","Copie com um toque os dados de pagamento do casal e dos pais.","신랑·신부·부모님 계좌를 탭 한 번에 복사","新郎新婦・ご両親の口座情報をワンタップでコピーできます。","一键复制新人及父母的收款信息。","一鍵複製新人與父母的收款資訊。","Sao chép thông tin mừng cưới của cô dâu chú rể và cha mẹ chỉ với một chạm."],
        ["features.themes.desc"] = ["Choose Rose Gold, Ivory, Forest, Navy or Blush.","Elige Rose Gold, Ivory, Forest, Navy o Blush.","Choisissez Rose Gold, Ivory, Forest, Navy ou Blush.","Scegli Rose Gold, Ivory, Forest, Navy o Blush.","Escolha Rose Gold, Ivory, Forest, Navy ou Blush.","로즈골드·아이보리·포레스트·네이비·블러쉬","ローズゴールド・アイボリー・フォレスト・ネイビー・ブラッシュから選べます。","可选玫瑰金、象牙白、森林、海军蓝或腮红主题。","可選玫瑰金、象牙白、森林、海軍藍或腮紅主題。","Chọn Rose Gold, Ivory, Forest, Navy hoặc Blush."],
        ["guest.account"] = ["Sign in to connect each invitation directly to your account.","Inicia sesión para vincular cada invitación directamente a tu cuenta.","Connectez-vous pour associer chaque invitation directement à votre compte.","Accedi per collegare ogni invito direttamente al tuo account.","Entre para vincular cada convite diretamente à sua conta.","로그인하면 생성한 청첩장이 내 계정에 바로 연결됩니다.","ログインすると、作成した招待状がアカウントに直接連携されます。","登录后，创建的请柬将直接关联到您的账户。","登入後，建立的喜帖將直接連結到您的帳戶。","Đăng nhập để liên kết thiệp đã tạo trực tiếp với tài khoản của bạn."],
        ["common.login"] = ["Shared sign-in","Acceso compartido","Connexion commune","Accesso condiviso","Login compartilhado","공용 로그인","共通ログイン","统一登录","統一登入","Đăng nhập chung"],
        ["field.slug.hint"] = ["Letters, numbers and hyphens only (for example: hong-gildong)","Solo letras, números y guiones (ej.: hong-gildong)","Lettres, chiffres et tirets uniquement (ex. : hong-gildong)","Solo lettere, numeri e trattini (es.: hong-gildong)","Somente letras, números e hífens (ex.: hong-gildong)","영문·숫자·하이픈만 (예: hong-gildong)","英字・数字・ハイフンのみ（例: hong-gildong）","仅限字母、数字和连字符（例如：hong-gildong）","僅限字母、數字與連字號（例如：hong-gildong）","Chỉ dùng chữ cái, số và dấu gạch nối (ví dụ: hong-gildong)"],
        ["field.couple.placeholder"] = ["Alex ♥ Jamie","Alex ♥ Jamie","Alex ♥ Jamie","Alex ♥ Jamie","Alex ♥ Jamie","홍길동 ♥ 김영희","太郎 ♥ 花子","新郎 ♥ 新娘","新郎 ♥ 新娘","Minh ♥ Lan"],
        ["field.password.hint"] = ["Used to edit the invitation","Se usa para editar la invitación","Utilisé pour modifier l’invitation","Usata per modificare l’invito","Usada para editar o convite","청첩장 편집 시 사용","招待状の編集に使用","用于编辑请柬","用於編輯喜帖","Dùng để chỉnh sửa thiệp"],
        ["field.password.placeholder"] = ["Eight or more characters recommended","Se recomiendan 8 caracteres o más","8 caractères ou plus recommandés","Consigliati almeno 8 caratteri","Recomendados 8 caracteres ou mais","8자 이상 권장","8文字以上を推奨","建议至少8个字符","建議至少8個字元","Nên dùng từ 8 ký tự"],
        ["create.notice"] = ["After creation, configure photos, music, maps and more at /{slug}/admin.","Después de crearla, configura fotos, música, mapas y más en /{slug}/admin.","Après création, configurez photos, musique, cartes et plus sur /{slug}/admin.","Dopo la creazione, configura foto, musica, mappe e altro in /{slug}/admin.","Após criar, configure fotos, música, mapas e mais em /{slug}/admin.","생성 후 /{슬러그}/admin에서 사진·음악·지도 등을 설정할 수 있습니다.","作成後、/{slug}/admin で写真・音楽・地図などを設定できます。","创建后，可在 /{slug}/admin 配置照片、音乐、地图等。","建立後，可在 /{slug}/admin 設定照片、音樂、地圖等。","Sau khi tạo, hãy cấu hình ảnh, nhạc, bản đồ và nhiều nội dung khác tại /{slug}/admin."],
        ["manage.open"] = ["Open management →","Abrir gestión →","Ouvrir la gestion →","Apri gestione →","Abrir gerenciamento →","관리 열기 →","管理を開く →","打开管理 →","開啟管理 →","Mở quản lý →"],
        ["admin.dashboard"] = ["Dashboard","Panel","Tableau de bord","Dashboard","Painel","대시보드","ダッシュボード","仪表板","儀表板","Bảng điều khiển"],
        ["admin.invitations"] = ["Invitations","Invitaciones","Invitations","Inviti","Convites","청첩장 관리","招待状管理","请柬管理","喜帖管理","Quản lý thiệp"],
        ["admin.accounts"] = ["Accounts","Cuentas","Comptes","Account","Contas","계정 관리","アカウント管理","账户管理","帳戶管理","Quản lý tài khoản"],
        ["admin.media"] = ["Media","Multimedia","Médias","Media","Mídia","미디어 관리","メディア管理","媒体管理","媒體管理","Quản lý media"],
        ["admin.layouts"] = ["Layout catalog","Catálogo de diseños","Catalogue des mises en page","Catalogo layout","Catálogo de layouts","레이아웃 카탈로그","レイアウトカタログ","布局目录","版面目錄","Danh mục bố cục"],
        ["admin.settings"] = ["Settings","Ajustes","Paramètres","Impostazioni","Configurações","설정","設定","设置","設定","Cài đặt"],
        ["admin.integrated"] = ["Wedding administration","Administración de Wedding","Administration Wedding","Amministrazione Wedding","Administração Wedding","Wedding 통합 어드민","Wedding 統合管理","Wedding 综合管理","Wedding 整合管理","Quản trị Wedding"],
        ["admin.integrated.desc"] = ["Manage registered couples and create invitations.","Gestiona parejas registradas y crea invitaciones.","Gérez les couples inscrits et créez des invitations.","Gestisci le coppie registrate e crea inviti.","Gerencie casais cadastrados e crie convites.","등록된 커플 계정을 관리하고 새 청첩장을 만듭니다.","登録済みカップルを管理し、招待状を作成します。","管理已注册新人并创建请柬。","管理已登記新人並建立喜帖。","Quản lý các cặp đôi và tạo thiệp cưới."],
        ["admin.search"] = ["Search","Buscar","Rechercher","Cerca","Pesquisar","검색","検索","搜索","搜尋","Tìm kiếm"],
        ["admin.search.placeholder"] = ["Search couple, URL or email","Buscar pareja, URL o correo","Rechercher couple, URL ou e-mail","Cerca coppia, URL o email","Pesquisar casal, URL ou e-mail","커플 이름, 슬러그, 이메일 검색","カップル名、URL、メールを検索","搜索新人、网址或邮箱","搜尋新人、網址或信箱","Tìm cặp đôi, URL hoặc email"],
        ["admin.refresh"] = ["Refresh","Actualizar","Actualiser","Aggiorna","Atualizar","새로고침","更新","刷新","重新整理","Làm mới"],
        ["admin.super"] = ["Super administrator","Superadministrador","Super administrateur","Super amministratore","Super administrador","최고 관리자","スーパー管理者","超级管理员","超級管理員","Quản trị viên cấp cao"],
        ["admin.summary"] = ["Summary","Resumen","Résumé","Riepilogo","Resumo","요약","概要","摘要","摘要","Tóm tắt"],
        ["admin.total.accounts"] = ["Total accounts","Cuentas totales","Total des comptes","Account totali","Total de contas","전체 계정 수","総アカウント数","账户总数","帳戶總數","Tổng tài khoản"],
        ["admin.registered.couples"] = ["Registered couples","Parejas registradas","Couples inscrits","Coppie registrate","Casais cadastrados","등록된 커플","登録済みカップル","已注册新人","已登記新人","Cặp đôi đã đăng ký"],
        ["admin.home.visible"] = ["Shown on home","Visible en inicio","Visible à l’accueil","Visibile in home","Visível no início","메인 노출","ホーム掲載","首页展示","首頁顯示","Hiển thị trang chủ"],
        ["admin.premium.active"] = ["Premium active","Premium activo","Premium actif","Premium attivo","Premium ativo","Premium 활성","Premium 有効","Premium 已启用","Premium 已啟用","Premium đang bật"],
        ["admin.storage"] = ["Storage","Almacenamiento","Stockage","Archiviazione","Armazenamento","저장 용량","ストレージ","存储空间","儲存空間","Dung lượng"],
        ["admin.video.usage"] = ["Video usage","Uso de vídeo","Utilisation vidéo","Utilizzo video","Uso de vídeo","동영상 사용량","動画使用量","视频用量","影片用量","Dung lượng video"],
        ["admin.registered"] = ["Registered","Registrado","Inscrit","Registrato","Cadastrado","등록됨","登録済み","已注册","已登記","Đã đăng ký"],
        ["admin.signin"] = ["Administrator sign in","Acceso de administrador","Connexion administrateur","Accesso amministratore","Login do administrador","관리자 로그인","管理者ログイン","管理员登录","管理員登入","Đăng nhập quản trị"],
        ["admin.checking"] = ["Checking management access.","Comprobando acceso de gestión.","Vérification de l’accès.","Verifica accesso gestione.","Verificando acesso de gestão.","관리 권한을 확인하고 있습니다.","管理権限を確認しています。","正在检查管理权限。","正在檢查管理權限。","Đang kiểm tra quyền quản lý."],
        ["admin.save"] = ["Save settings","Guardar ajustes","Enregistrer","Salva impostazioni","Salvar configurações","설정 저장","設定を保存","保存设置","儲存設定","Lưu cài đặt"],
        ["admin.share"] = ["Share","Compartir","Partager","Condividi","Compartilhar","공유하기","共有","分享","分享","Chia sẻ"],
        ["admin.open.invite"] = ["Open invitation","Abrir invitación","Ouvrir l’invitation","Apri invito","Abrir convite","청첩장 열기","招待状を開く","打开请柬","開啟喜帖","Mở thiệp cưới"],
        ["admin.basic"] = ["Basic information","Información básica","Informations de base","Informazioni di base","Informações básicas","기본 정보","基本情報","基本信息","基本資訊","Thông tin cơ bản"],
        ["admin.notice"] = ["Ceremony notice","Aviso de ceremonia","Informations de cérémonie","Avviso cerimonia","Aviso da cerimônia","예식 안내","挙式案内","婚礼说明","婚禮說明","Thông báo lễ cưới"],
        ["admin.design"] = ["Design","Diseño","Design","Design","Design","디자인","デザイン","设计","設計","Thiết kế"],
        ["admin.story"] = ["Story chapters","Capítulos de historia","Chapitres de l’histoire","Capitoli della storia","Capítulos da história","스토리 챕터","ストーリーチャプター","故事章节","故事章節","Chương câu chuyện"],
        ["admin.photobook"] = ["Photo book pages","Páginas del álbum","Pages de l’album","Pagine fotolibro","Páginas do álbum","포토북 페이지","フォトブックページ","相册页面","相簿頁面","Trang album ảnh"],
        ["admin.cards"] = ["Card highlights","Tarjetas destacadas","Cartes mises en avant","Schede in evidenza","Cartões em destaque","카드 강조","カード強調","卡片重点","卡片重點","Thẻ nổi bật"],
        ["admin.content"] = ["Photos and videos","Fotos y vídeos","Photos et vidéos","Foto e video","Fotos e vídeos","사진·동영상","写真・動画","照片和视频","照片與影片","Ảnh và video"],
        ["admin.map"] = ["Map","Mapa","Carte","Mappa","Mapa","지도","地図","地图","地圖","Bản đồ"],
        ["admin.payment"] = ["Payment details","Datos de pago","Informations de paiement","Dati di pagamento","Dados de pagamento","계좌 안내","ご祝儀口座案内","收款信息","收款資訊","Thông tin mừng cưới"],
        ["admin.sharing"] = ["Sharing settings","Ajustes para compartir","Paramètres de partage","Impostazioni condivisione","Configurações de compartilhamento","공유 설정","共有設定","分享设置","分享設定","Cài đặt chia sẻ"]
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
