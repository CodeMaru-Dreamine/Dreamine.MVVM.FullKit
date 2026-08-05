namespace DreamineVMS.Web.Services;

/// <summary>Localized copy used exclusively by the Dreamine VMS installation guide.</summary>
public static class VmsGuideText
{
    private static readonly string[] Codes =
    [
        "en", "es", "fr", "it", "pt", "ko", "ja", "zh-hans", "zh-hant", "vi"
    ];

    private static readonly Dictionary<string, string[]> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["page.title"] =
        [
            "Installation and Usage Guide — Dreamine VMS",
            "Guía de instalación y uso — Dreamine VMS",
            "Guide d’installation et d’utilisation — Dreamine VMS",
            "Guida all’installazione e all’uso — Dreamine VMS",
            "Guia de instalação e uso — Dreamine VMS",
            "설치 및 사용 방법 — Dreamine VMS",
            "インストール・使用ガイド — Dreamine VMS",
            "安装与使用指南 — Dreamine VMS",
            "安裝與使用指南 — Dreamine VMS",
            "Hướng dẫn cài đặt và sử dụng — Dreamine VMS"
        ],
        ["back.home"] =
        [
            "← Home", "← Inicio", "← Accueil", "← Home", "← Início", "← 홈으로",
            "← ホームへ", "← 返回首页", "← 返回首頁", "← Trang chủ"
        ],
        ["hero.title"] =
        [
            "Dreamine VMS Installation &amp; Usage Guide",
            "Guía de instalación y uso de Dreamine VMS",
            "Guide d’installation et d’utilisation de Dreamine VMS",
            "Guida all’installazione e all’uso di Dreamine VMS",
            "Guia de instalação e uso do Dreamine VMS",
            "Dreamine VMS 설치 · 사용 가이드",
            "Dreamine VMS インストール・使用ガイド",
            "Dreamine VMS 安装与使用指南",
            "Dreamine VMS 安裝與使用指南",
            "Hướng dẫn cài đặt và sử dụng Dreamine VMS"
        ],
        ["hero.subtitle"] =
        [
            "Share an IP camera on the web in just five steps.",
            "Comparta una cámara IP en la web en solo cinco pasos.",
            "Partagez une caméra IP sur le Web en seulement cinq étapes.",
            "Condividi una telecamera IP sul Web in soli cinque passaggi.",
            "Compartilhe uma câmera IP na Web em apenas cinco etapas.",
            "IP 카메라를 웹으로 공유하기까지 5단계면 충분합니다.",
            "5つの手順だけでIPカメラをWebに共有できます。",
            "只需五个步骤，即可在网页上共享 IP 摄像头。",
            "只需五個步驟，即可在網頁上分享 IP 攝影機。",
            "Chỉ với năm bước, bạn có thể chia sẻ camera IP lên web."
        ],
        ["step1.title"] =
        [
            "Create an account", "Crear una cuenta", "Créer un compte", "Crea un account", "Criar uma conta",
            "회원가입", "アカウント登録", "注册账户", "註冊帳戶", "Tạo tài khoản"
        ],
        ["step1.intro"] =
        [
            "Go to cctvviewer.codemaru.co.kr, click <strong>Start for free</strong>, then sign in or register with your shared CodeMaru account.",
            "Visite cctvviewer.codemaru.co.kr, haga clic en <strong>Comenzar gratis</strong> e inicie sesión o regístrese con su cuenta compartida de CodeMaru.",
            "Accédez à cctvviewer.codemaru.co.kr, cliquez sur <strong>Commencer gratuitement</strong>, puis connectez-vous ou inscrivez-vous avec votre compte CodeMaru commun.",
            "Vai su cctvviewer.codemaru.co.kr, fai clic su <strong>Inizia gratis</strong>, quindi accedi o registrati con il tuo account CodeMaru condiviso.",
            "Acesse cctvviewer.codemaru.co.kr, clique em <strong>Começar grátis</strong> e entre ou cadastre-se com sua conta compartilhada do CodeMaru.",
            "cctvviewer.codemaru.co.kr에 접속해 <strong>무료로 시작하기</strong>를 클릭하고 CodeMaru 공통 계정으로 로그인하거나 가입합니다.",
            "cctvviewer.codemaru.co.kr にアクセスして<strong>無料で始める</strong>をクリックし、CodeMaru共通アカウントでログインまたは登録します。",
            "访问 cctvviewer.codemaru.co.kr，点击<strong>免费开始</strong>，然后使用 CodeMaru 通用账户登录或注册。",
            "前往 cctvviewer.codemaru.co.kr，按一下<strong>免費開始</strong>，然後使用 CodeMaru 共用帳戶登入或註冊。",
            "Truy cập cctvviewer.codemaru.co.kr, nhấp <strong>Bắt đầu miễn phí</strong>, rồi đăng nhập hoặc đăng ký bằng tài khoản CodeMaru dùng chung."
        ],
        ["step1.item1"] =
        [
            "After signing up, you receive a dedicated public link in the format <strong>/{nickname}/live</strong>.",
            "Tras registrarse, recibirá un enlace público exclusivo con el formato <strong>/{nickname}/live</strong>.",
            "Après l’inscription, un lien public dédié au format <strong>/{nickname}/live</strong> vous est attribué.",
            "Dopo la registrazione riceverai un link pubblico dedicato nel formato <strong>/{nickname}/live</strong>.",
            "Após o cadastro, você receberá um link público exclusivo no formato <strong>/{nickname}/live</strong>.",
            "가입 후 <strong>/{닉네임}/live</strong> 형식의 전용 공개 링크가 발급됩니다.",
            "登録後、<strong>/{nickname}/live</strong>形式の専用公開リンクが発行されます。",
            "注册后，系统会生成格式为 <strong>/{nickname}/live</strong> 的专属公开链接。",
            "註冊後，系統會產生格式為 <strong>/{nickname}/live</strong> 的專屬公開連結。",
            "Sau khi đăng ký, bạn sẽ nhận được liên kết công khai riêng theo định dạng <strong>/{nickname}/live</strong>."
        ],
        ["step1.item2"] =
        [
            "You can set the Kakao share title and image on the camera management page.",
            "Puede configurar el título y la imagen para compartir en Kakao desde la página de gestión de cámaras.",
            "Vous pouvez définir le titre et l’image du partage Kakao sur la page de gestion des caméras.",
            "Puoi impostare il titolo e l’immagine di condivisione Kakao nella pagina di gestione delle telecamere.",
            "Você pode definir o título e a imagem de compartilhamento do Kakao na página de gerenciamento de câmeras.",
            "카메라 관리 페이지에서 카카오 공유 제목·이미지를 설정할 수 있습니다.",
            "カメラ管理ページでKakao共有のタイトルと画像を設定できます。",
            "您可以在摄像头管理页面设置 Kakao 分享标题和图片。",
            "您可以在攝影機管理頁面設定 Kakao 分享標題與圖片。",
            "Bạn có thể đặt tiêu đề và hình ảnh chia sẻ Kakao trên trang quản lý camera."
        ],
        ["step1.item3"] =
        [
            "Set a separate password for the PC agent connection on the camera management page.",
            "Configure una contraseña independiente para conectar el agente de PC en la página de gestión de cámaras.",
            "Définissez un mot de passe distinct pour la connexion de l’agent PC sur la page de gestion des caméras.",
            "Imposta una password separata per la connessione dell’agente PC nella pagina di gestione delle telecamere.",
            "Defina uma senha separada para a conexão do agente de PC na página de gerenciamento de câmeras.",
            "PC 에이전트 연결용 비밀번호는 카메라 관리 페이지에서 별도로 설정합니다.",
            "PCエージェント接続用のパスワードは、カメラ管理ページで別途設定します。",
            "请在摄像头管理页面单独设置 PC 代理连接密码。",
            "請在攝影機管理頁面另外設定 PC 代理程式連線密碼。",
            "Đặt riêng mật khẩu kết nối tác nhân PC trên trang quản lý camera."
        ],
        ["step1.action"] =
        [
            "Sign up now →", "Registrarse ahora →", "S’inscrire maintenant →", "Registrati ora →", "Cadastre-se agora →",
            "지금 가입하기 →", "今すぐ登録 →", "立即注册 →", "立即註冊 →", "Đăng ký ngay →"
        ],
        ["step2.title"] =
        [
            "Download and install the agent app", "Descargar e instalar la aplicación del agente", "Télécharger et installer l’application agent",
            "Scarica e installa l’app agente", "Baixar e instalar o aplicativo agente", "에이전트 앱 다운로드 · 설치",
            "エージェントアプリのダウンロード・インストール", "下载并安装代理应用", "下載並安裝代理程式應用程式", "Tải xuống và cài đặt ứng dụng tác nhân"
        ],
        ["step2.intro"] =
        [
            "Install the <strong>Dreamine VMS</strong> app on the Windows PC connected to the camera.",
            "Instale la aplicación <strong>Dreamine VMS</strong> en el PC con Windows conectado a la cámara.",
            "Installez l’application <strong>Dreamine VMS</strong> sur le PC Windows connecté à la caméra.",
            "Installa l’app <strong>Dreamine VMS</strong> sul PC Windows collegato alla telecamera.",
            "Instale o aplicativo <strong>Dreamine VMS</strong> no PC Windows conectado à câmera.",
            "카메라가 연결된 Windows PC에 <strong>Dreamine VMS</strong> 앱을 설치합니다.",
            "カメラが接続されたWindows PCに<strong>Dreamine VMS</strong>アプリをインストールします。",
            "在连接摄像头的 Windows PC 上安装 <strong>Dreamine VMS</strong> 应用。",
            "在連接攝影機的 Windows PC 上安裝 <strong>Dreamine VMS</strong> 應用程式。",
            "Cài đặt ứng dụng <strong>Dreamine VMS</strong> trên PC Windows được kết nối với camera."
        ],
        ["step2.item1"] =
        [
            "Windows 10 / 11 (64-bit) required", "Se requiere Windows 10 / 11 (64 bits)", "Windows 10 / 11 (64 bits) requis",
            "È richiesto Windows 10 / 11 (64 bit)", "É necessário Windows 10 / 11 (64 bits)", "Windows 10 / 11 (64비트) 필요",
            "Windows 10 / 11（64ビット）が必要", "需要 Windows 10 / 11（64 位）", "需要 Windows 10 / 11（64 位元）", "Yêu cầu Windows 10 / 11 (64 bit)"
        ],
        ["step2.item2"] =
        [
            "FFmpeg included automatically — no separate installation required", "FFmpeg incluido automáticamente; no requiere instalación adicional",
            "FFmpeg inclus automatiquement — aucune installation séparée requise", "FFmpeg incluso automaticamente — non serve un’installazione separata",
            "FFmpeg incluído automaticamente — não requer instalação separada", "FFmpeg 자동 포함 — 별도 설치 불필요",
            "FFmpegを自動同梱 — 別途インストール不要", "自动包含 FFmpeg — 无需单独安装", "自動包含 FFmpeg — 無須另外安裝", "Tự động tích hợp FFmpeg — không cần cài đặt riêng"
        ],
        ["step2.item3"] =
        [
            "The IP camera’s RTSP address is required", "Se necesita la dirección RTSP de la cámara IP", "L’adresse RTSP de la caméra IP est requise",
            "È necessario l’indirizzo RTSP della telecamera IP", "O endereço RTSP da câmera IP é necessário", "IP 카메라 RTSP 주소가 필요합니다",
            "IPカメラのRTSPアドレスが必要です", "需要 IP 摄像头的 RTSP 地址", "需要 IP 攝影機的 RTSP 位址", "Cần địa chỉ RTSP của camera IP"
        ],
        ["step2.item4"] =
        [
            "About 300 MB installed · about 90 MB installer", "Aproximadamente 300 MB instalados · instalador de unos 90 MB",
            "Environ 300 Mo installés · fichier d’installation d’environ 90 Mo", "Circa 300 MB installati · file di installazione di circa 90 MB",
            "Cerca de 300 MB instalados · instalador de aproximadamente 90 MB", "설치 용량 약 300MB · 설치 파일 약 90MB",
            "インストール容量約300MB・インストーラー約90MB", "安装后约 300MB · 安装文件约 90MB", "安裝後約 300MB · 安裝檔約 90MB", "Dung lượng cài đặt khoảng 300MB · tệp cài đặt khoảng 90MB"
        ],
        ["step2.item5"] =
        [
            "Disk usage while streaming: about <strong>180 MB/hour</strong> per camera (at 400 kbps)<br />Old segment files are cleaned automatically when the app or camera restarts.<br />During long periods of uninterrupted operation, usage can grow to several GB, so make sure enough free space is available.",
            "Uso de disco durante la transmisión: unos <strong>180 MB/hora</strong> por cámara (a 400 kbps)<br />Los archivos de segmentos antiguos se eliminan automáticamente al reiniciar la aplicación o la cámara.<br />Durante un funcionamiento continuo prolongado, pueden acumularse varios GB; asegúrese de disponer de espacio libre suficiente.",
            "Espace disque utilisé pendant la diffusion : environ <strong>180 Mo/heure</strong> par caméra (à 400 kbps)<br />Les anciens fichiers de segments sont supprimés automatiquement au redémarrage de l’application ou de la caméra.<br />En fonctionnement continu prolongé, plusieurs Go peuvent s’accumuler ; prévoyez suffisamment d’espace libre.",
            "Uso del disco durante lo streaming: circa <strong>180 MB/ora</strong> per telecamera (a 400 kbps)<br />I vecchi file dei segmenti vengono eliminati automaticamente al riavvio dell’app o della telecamera.<br />Durante lunghi periodi senza interruzioni possono accumularsi diversi GB, quindi assicurati di avere spazio libero sufficiente.",
            "Uso de disco durante o streaming: cerca de <strong>180 MB/hora</strong> por câmera (a 400 kbps)<br />Os arquivos de segmentos antigos são removidos automaticamente quando o aplicativo ou a câmera reinicia.<br />Em operação contínua por longos períodos, o uso pode chegar a vários GB; mantenha espaço livre suficiente.",
            "스트리밍 중 디스크 사용량: 카메라 1대당 약 <strong>180MB/시간</strong> (400kbps 기준)<br />앱 또는 카메라 재시작 시 오래된 세그먼트 파일이 자동 정리됩니다.<br />장시간 무중단 운영 시 수 GB까지 쌓일 수 있으니 여유 공간을 확보하세요.",
            "ストリーミング中のディスク使用量：カメラ1台あたり約<strong>180MB/時間</strong>（400kbpsの場合）<br />アプリまたはカメラの再起動時に古いセグメントファイルは自動削除されます。<br />長時間連続稼働すると数GBまで増える場合があるため、十分な空き容量を確保してください。",
            "流式传输时的磁盘用量：每台摄像头约 <strong>180MB/小时</strong>（按 400kbps 计算）<br />应用或摄像头重启时会自动清理旧分段文件。<br />长时间不间断运行时可能累积到数 GB，请确保有足够的可用空间。",
            "串流時的磁碟用量：每台攝影機約 <strong>180MB/小時</strong>（以 400kbps 計算）<br />應用程式或攝影機重新啟動時會自動清除舊分段檔案。<br />長時間不間斷運作時可能累積至數 GB，請確保有足夠的可用空間。",
            "Dung lượng đĩa khi truyền phát: khoảng <strong>180MB/giờ</strong> cho mỗi camera (ở 400kbps)<br />Các tệp phân đoạn cũ được tự động dọn dẹp khi ứng dụng hoặc camera khởi động lại.<br />Khi hoạt động liên tục trong thời gian dài, dung lượng có thể tăng đến vài GB; hãy đảm bảo đủ không gian trống."
        ],
        ["step2.warning"] =
        [
            "⚠ <strong>Microsoft WebView2 Runtime is downloaded and installed automatically during the first installation.</strong><br />This may take several minutes depending on your internet speed. Please wait on the installation screen until it finishes.",
            "⚠ <strong>Microsoft WebView2 Runtime se descarga e instala automáticamente durante la primera instalación.</strong><br />Puede tardar varios minutos según la velocidad de Internet. Espere en la pantalla de instalación hasta que finalice.",
            "⚠ <strong>Microsoft WebView2 Runtime est téléchargé et installé automatiquement lors de la première installation.</strong><br />Cela peut prendre plusieurs minutes selon votre connexion Internet. Patientez sur l’écran d’installation jusqu’à la fin.",
            "⚠ <strong>Microsoft WebView2 Runtime viene scaricato e installato automaticamente alla prima installazione.</strong><br />L’operazione può richiedere alcuni minuti in base alla velocità Internet. Attendi nella schermata di installazione fino al completamento.",
            "⚠ <strong>O Microsoft WebView2 Runtime é baixado e instalado automaticamente na primeira instalação.</strong><br />Isso pode levar alguns minutos, dependendo da velocidade da Internet. Aguarde na tela de instalação até a conclusão.",
            "⚠ <strong>최초 설치 시 Microsoft WebView2 Runtime을 자동으로 다운로드·설치</strong>합니다.<br />인터넷 속도에 따라 수 분이 걸릴 수 있으며, 설치 진행 중 화면에서 기다려 주세요.",
            "⚠ <strong>初回インストール時にMicrosoft WebView2 Runtimeを自動でダウンロード・インストールします。</strong><br />インターネット速度によっては数分かかる場合があります。完了するまでインストール画面でお待ちください。",
            "⚠ <strong>首次安装时会自动下载并安装 Microsoft WebView2 Runtime。</strong><br />根据网速，可能需要数分钟。请在安装界面等待完成。",
            "⚠ <strong>首次安裝時會自動下載並安裝 Microsoft WebView2 Runtime。</strong><br />視網路速度而定，可能需要數分鐘。請在安裝畫面等待完成。",
            "⚠ <strong>Microsoft WebView2 Runtime sẽ được tự động tải xuống và cài đặt trong lần cài đặt đầu tiên.</strong><br />Quá trình có thể mất vài phút tùy tốc độ Internet. Vui lòng chờ trên màn hình cài đặt cho đến khi hoàn tất."
        ],
        ["step2.tip"] =
        [
            "💡 <strong>Administrator permission</strong><br />A normal launch is sufficient when using the default installation path (the user’s AppData folder).<br />Only when installed in a folder without write permission, such as <code>C:\\Program Files</code>,<br />right-click and select <strong>Run as administrator</strong>.",
            "💡 <strong>Permisos de administrador</strong><br />La ejecución normal es suficiente con la ruta de instalación predeterminada (carpeta AppData del usuario).<br />Solo si se instala en una carpeta sin permiso de escritura, como <code>C:\\Program Files</code>,<br />haga clic con el botón derecho y seleccione <strong>Ejecutar como administrador</strong>.",
            "💡 <strong>Autorisation administrateur</strong><br />Un lancement normal suffit avec le chemin d’installation par défaut (dossier AppData de l’utilisateur).<br />Uniquement si l’application est installée dans un dossier sans droit d’écriture, tel que <code>C:\\Program Files</code>,<br />faites un clic droit et choisissez <strong>Exécuter en tant qu’administrateur</strong>.",
            "💡 <strong>Permessi di amministratore</strong><br />Con il percorso di installazione predefinito (cartella AppData dell’utente) è sufficiente l’avvio normale.<br />Solo se l’app è installata in una cartella senza permessi di scrittura, come <code>C:\\Program Files</code>,<br />fai clic con il pulsante destro e scegli <strong>Esegui come amministratore</strong>.",
            "💡 <strong>Permissão de administrador</strong><br />A execução normal é suficiente no caminho de instalação padrão (pasta AppData do usuário).<br />Somente se o aplicativo for instalado em uma pasta sem permissão de gravação, como <code>C:\\Program Files</code>,<br />clique com o botão direito e escolha <strong>Executar como administrador</strong>.",
            "💡 <strong>관리자 권한 안내</strong><br />기본 설치 경로(사용자 AppData 폴더)에서는 일반 실행으로 충분합니다.<br /><code>C:\\Program Files</code> 등 쓰기 권한이 없는 폴더에 설치한 경우에만<br />우클릭 → <strong>관리자 권한으로 실행</strong>이 필요합니다.",
            "💡 <strong>管理者権限について</strong><br />既定のインストール先（ユーザーのAppDataフォルダー）では通常起動で十分です。<br /><code>C:\\Program Files</code>など書き込み権限のないフォルダーにインストールした場合のみ、<br />右クリックして<strong>管理者として実行</strong>を選択してください。",
            "💡 <strong>管理员权限说明</strong><br />使用默认安装路径（用户的 AppData 文件夹）时，正常启动即可。<br />仅当安装在 <code>C:\\Program Files</code> 等无写入权限的文件夹时，<br />才需要右键选择<strong>以管理员身份运行</strong>。",
            "💡 <strong>系統管理員權限說明</strong><br />使用預設安裝路徑（使用者的 AppData 資料夾）時，正常啟動即可。<br />只有安裝在 <code>C:\\Program Files</code> 等無寫入權限的資料夾時，<br />才需要按右鍵並選擇<strong>以系統管理員身分執行</strong>。",
            "💡 <strong>Quyền quản trị viên</strong><br />Chỉ cần chạy bình thường khi dùng đường dẫn cài đặt mặc định (thư mục AppData của người dùng).<br />Chỉ khi cài vào thư mục không có quyền ghi, chẳng hạn <code>C:\\Program Files</code>,<br />hãy nhấp chuột phải và chọn <strong>Chạy với quyền quản trị viên</strong>."
        ],
        ["step2.action"] =
        [
            "⬇ Install Dreamine VMS (v1.0.0)", "⬇ Instalar Dreamine VMS (v1.0.0)", "⬇ Installer Dreamine VMS (v1.0.0)",
            "⬇ Installa Dreamine VMS (v1.0.0)", "⬇ Instalar Dreamine VMS (v1.0.0)", "⬇ Dreamine VMS 설치 (v1.0.0)",
            "⬇ Dreamine VMSをインストール（v1.0.0）", "⬇ 安装 Dreamine VMS（v1.0.0）", "⬇ 安裝 Dreamine VMS（v1.0.0）", "⬇ Cài đặt Dreamine VMS (v1.0.0)"
        ],
        ["step3.title"] =
        [
            "Sign in to the server from the app", "Iniciar sesión en el servidor desde la aplicación", "Se connecter au serveur depuis l’application",
            "Accedi al server dall’app", "Entrar no servidor pelo aplicativo", "앱에서 서버 로그인", "アプリからサーバーにログイン",
            "在应用中登录服务器", "在應用程式中登入伺服器", "Đăng nhập máy chủ từ ứng dụng"
        ],
        ["step3.intro"] =
        [
            "When the app starts, the <strong>Agent settings</strong> tab opens. Enter the agent email shown on the camera management page and the separately configured agent password, then select <strong>Save → Reconnect</strong>.",
            "Al iniciar la aplicación se abre la pestaña <strong>Configuración del agente</strong>. Introduzca el correo del agente que aparece en la página de gestión de cámaras y la contraseña configurada por separado; después seleccione <strong>Guardar → Reconectar</strong>.",
            "Au démarrage de l’application, l’onglet <strong>Paramètres de l’agent</strong> s’ouvre. Saisissez l’adresse e-mail de l’agent affichée sur la page de gestion des caméras et le mot de passe défini séparément, puis sélectionnez <strong>Enregistrer → Reconnecter</strong>.",
            "All’avvio dell’app si apre la scheda <strong>Impostazioni agente</strong>. Inserisci l’e-mail dell’agente mostrata nella pagina di gestione delle telecamere e la password configurata separatamente, quindi seleziona <strong>Salva → Riconnetti</strong>.",
            "Ao iniciar o aplicativo, a guia <strong>Configurações do agente</strong> é aberta. Informe o e-mail do agente exibido na página de gerenciamento de câmeras e a senha configurada separadamente; depois selecione <strong>Salvar → Reconectar</strong>.",
            "앱을 실행하면 <strong>에이전트 설정</strong> 탭이 열립니다. 카메라 관리 페이지에 표시된 에이전트 이메일과 별도로 설정한 에이전트 비밀번호를 입력하고 <strong>저장 → 재연결</strong>을 누릅니다.",
            "アプリを起動すると<strong>エージェント設定</strong>タブが開きます。カメラ管理ページに表示されたエージェントのメールアドレスと、別途設定したパスワードを入力し、<strong>保存 → 再接続</strong>を押します。",
            "启动应用后会打开<strong>代理设置</strong>选项卡。输入摄像头管理页面显示的代理邮箱和单独设置的代理密码，然后点击<strong>保存 → 重新连接</strong>。",
            "啟動應用程式後會開啟<strong>代理程式設定</strong>分頁。輸入攝影機管理頁面顯示的代理程式電子郵件與另外設定的密碼，然後按<strong>儲存 → 重新連線</strong>。",
            "Khi mở ứng dụng, tab <strong>Cài đặt tác nhân</strong> sẽ xuất hiện. Nhập email tác nhân hiển thị trên trang quản lý camera và mật khẩu tác nhân đã đặt riêng, sau đó chọn <strong>Lưu → Kết nối lại</strong>."
        ],
        ["step3.item1"] =
        [
            "Server URL: <code>https://cctvviewer.codemaru.co.kr</code> (default)", "URL del servidor: <code>https://cctvviewer.codemaru.co.kr</code> (predeterminado)",
            "URL du serveur : <code>https://cctvviewer.codemaru.co.kr</code> (par défaut)", "URL server: <code>https://cctvviewer.codemaru.co.kr</code> (predefinito)",
            "URL do servidor: <code>https://cctvviewer.codemaru.co.kr</code> (padrão)", "서버 URL: <code>https://cctvviewer.codemaru.co.kr</code> (기본값)",
            "サーバーURL：<code>https://cctvviewer.codemaru.co.kr</code>（既定値）", "服务器 URL：<code>https://cctvviewer.codemaru.co.kr</code>（默认值）",
            "伺服器 URL：<code>https://cctvviewer.codemaru.co.kr</code>（預設值）", "URL máy chủ: <code>https://cctvviewer.codemaru.co.kr</code> (mặc định)"
        ],
        ["step3.item2"] =
        [
            "After a successful connection, <strong style=\"color:#4ade80;\">Connected</strong> appears in the bottom status bar.",
            "Tras conectarse correctamente, aparecerá <strong style=\"color:#4ade80;\">Conectado</strong> en la barra de estado inferior.",
            "Une fois la connexion établie, <strong style=\"color:#4ade80;\">Connecté</strong> apparaît dans la barre d’état inférieure.",
            "Dopo la connessione, nella barra di stato in basso appare <strong style=\"color:#4ade80;\">Connesso</strong>.",
            "Após a conexão, <strong style=\"color:#4ade80;\">Conectado</strong> aparece na barra de status inferior.",
            "연결 성공 시 하단 상태 표시줄에 <strong style=\"color:#4ade80;\">연결됨</strong>이 표시됩니다.",
            "接続に成功すると、下部のステータスバーに<strong style=\"color:#4ade80;\">接続済み</strong>と表示されます。",
            "连接成功后，底部状态栏会显示<strong style=\"color:#4ade80;\">已连接</strong>。",
            "連線成功後，底部狀態列會顯示<strong style=\"color:#4ade80;\">已連線</strong>。",
            "Sau khi kết nối thành công, thanh trạng thái phía dưới sẽ hiển thị <strong style=\"color:#4ade80;\">Đã kết nối</strong>."
        ],
        ["step4.title"] =
        [
            "Add cameras", "Añadir cámaras", "Ajouter des caméras", "Aggiungi telecamere", "Adicionar câmeras", "카메라 등록",
            "カメラを登録", "添加摄像头", "新增攝影機", "Thêm camera"
        ],
        ["step4.intro"] =
        [
            "Add a camera from the app’s <strong>Camera management</strong> tab.", "Añada una cámara desde la pestaña <strong>Gestión de cámaras</strong> de la aplicación.",
            "Ajoutez une caméra depuis l’onglet <strong>Gestion des caméras</strong> de l’application.", "Aggiungi una telecamera dalla scheda <strong>Gestione telecamere</strong> dell’app.",
            "Adicione uma câmera na guia <strong>Gerenciamento de câmeras</strong> do aplicativo.", "앱의 <strong>카메라 관리</strong> 탭에서 카메라를 추가합니다.",
            "アプリの<strong>カメラ管理</strong>タブでカメラを追加します。", "在应用的<strong>摄像头管理</strong>选项卡中添加摄像头。",
            "在應用程式的<strong>攝影機管理</strong>分頁中新增攝影機。", "Thêm camera trong tab <strong>Quản lý camera</strong> của ứng dụng."
        ],
        ["step4.item1"] =
        [
            "<strong>Automatic RTSP URL generation</strong> — select a brand (Hikvision, Dahua, Hanwha, and others), then enter only the IP, ID, and password to create the URL automatically.",
            "<strong>Generación automática de URL RTSP</strong> — seleccione una marca (Hikvision, Dahua, Hanwha, etc.) e introduzca solo la IP, el ID y la contraseña para crear la URL automáticamente.",
            "<strong>Génération automatique de l’URL RTSP</strong> — choisissez une marque (Hikvision, Dahua, Hanwha, etc.), puis saisissez uniquement l’IP, l’identifiant et le mot de passe pour générer automatiquement l’URL.",
            "<strong>Generazione automatica dell’URL RTSP</strong> — seleziona una marca (Hikvision, Dahua, Hanwha e altre), quindi inserisci solo IP, ID e password per creare automaticamente l’URL.",
            "<strong>Geração automática da URL RTSP</strong> — selecione uma marca (Hikvision, Dahua, Hanwha e outras) e informe apenas IP, ID e senha para criar a URL automaticamente.",
            "<strong>RTSP URL 자동 생성</strong> — 브랜드(하이크비전, 다화, 한화 등) 선택 후 IP·ID·비밀번호만 입력하면 URL이 자동으로 만들어집니다.",
            "<strong>RTSP URL自動生成</strong> — ブランド（Hikvision、Dahua、Hanwhaなど）を選び、IP・ID・パスワードを入力するだけでURLが自動生成されます。",
            "<strong>自动生成 RTSP URL</strong> — 选择品牌（Hikvision、Dahua、Hanwha 等），只需输入 IP、ID 和密码即可自动生成 URL。",
            "<strong>自動產生 RTSP URL</strong> — 選擇品牌（Hikvision、Dahua、Hanwha 等），只需輸入 IP、ID 與密碼即可自動產生 URL。",
            "<strong>Tự động tạo URL RTSP</strong> — chọn thương hiệu (Hikvision, Dahua, Hanwha, v.v.), sau đó chỉ cần nhập IP, ID và mật khẩu để URL được tạo tự động."
        ],
        ["step4.item2"] =
        [
            "If you already know the RTSP URL, you can enter it directly in the stream URL field.", "Si ya conoce la URL RTSP, puede introducirla directamente en el campo URL de transmisión.",
            "Si vous connaissez déjà l’URL RTSP, vous pouvez la saisir directement dans le champ URL du flux.", "Se conosci già l’URL RTSP, puoi inserirlo direttamente nel campo URL dello stream.",
            "Se você já souber a URL RTSP, poderá informá-la diretamente no campo de URL do stream.", "직접 RTSP URL을 알고 있다면 스트림 URL 칸에 바로 입력해도 됩니다.",
            "RTSP URLが分かっている場合は、ストリームURL欄に直接入力できます。", "如果您已经知道 RTSP URL，也可以直接输入到流 URL 字段中。",
            "如果您已知道 RTSP URL，也可以直接輸入串流 URL 欄位。", "Nếu đã biết URL RTSP, bạn có thể nhập trực tiếp vào trường URL luồng."
        ],
        ["step4.item3"] =
        [
            "When <strong>Public</strong> is checked, the camera appears in the live list on the home page and anyone can view its link.",
            "Al marcar <strong>Público</strong>, la cámara aparece en la lista en directo de la página de inicio y cualquiera puede ver el enlace.",
            "Lorsque <strong>Public</strong> est coché, la caméra apparaît dans la liste des directs de la page d’accueil et son lien est accessible à tous.",
            "Se selezioni <strong>Pubblica</strong>, la telecamera appare nell’elenco live della home page e chiunque può visualizzarne il link.",
            "Ao marcar <strong>Público</strong>, a câmera aparece na lista ao vivo da página inicial e qualquer pessoa pode acessar o link.",
            "<strong>외부 공개</strong> 체크 시 홈 화면 라이브 목록에 노출되고 링크를 아무나 볼 수 있습니다.",
            "<strong>外部公開</strong>を選択すると、ホーム画面のライブ一覧に表示され、誰でもリンクを閲覧できます。",
            "勾选<strong>公开</strong>后，摄像头会显示在首页直播列表中，任何人都可以通过链接观看。",
            "勾選<strong>公開</strong>後，攝影機會顯示在首頁直播清單中，任何人都能透過連結觀看。",
            "Khi chọn <strong>Công khai</strong>, camera sẽ xuất hiện trong danh sách trực tiếp trên trang chủ và bất kỳ ai cũng có thể xem qua liên kết."
        ],
        ["step4.item4"] =
        [
            "Even when set to <strong>Private</strong>, you can watch the camera normally on your live page while signed in.",
            "Incluso en estado <strong>Privado</strong>, podrá ver la cámara con normalidad en su página en directo mientras haya iniciado sesión.",
            "Même en mode <strong>Privé</strong>, vous pouvez regarder la caméra normalement sur votre page de direct lorsque vous êtes connecté.",
            "Anche in modalità <strong>Privata</strong>, puoi vedere normalmente la telecamera nella tua pagina live dopo aver effettuato l’accesso.",
            "Mesmo no modo <strong>Privado</strong>, você pode assistir normalmente à câmera na sua página ao vivo quando estiver conectado.",
            "<strong>비공개</strong> 상태에서도 본인이 로그인하면 내 라이브 페이지에서 정상 시청 가능합니다.",
            "<strong>非公開</strong>でも、本人がログインしていれば自分のライブページで視聴できます。",
            "即使设为<strong>私密</strong>，本人登录后仍可在自己的直播页面正常观看。",
            "即使設為<strong>私人</strong>，本人登入後仍可在自己的直播頁面正常觀看。",
            "Ngay cả khi đặt là <strong>Riêng tư</strong>, bạn vẫn có thể xem camera bình thường trên trang trực tiếp của mình khi đã đăng nhập."
        ],
        ["step4.item5"] =
        [
            "Adding a camera automatically syncs it with the server and starts streaming.", "Al añadir una cámara, se sincroniza automáticamente con el servidor y comienza la transmisión.",
            "L’ajout d’une caméra la synchronise automatiquement avec le serveur et lance la diffusion.", "Quando aggiungi una telecamera, questa viene sincronizzata automaticamente con il server e avvia lo streaming.",
            "Ao adicionar uma câmera, ela é sincronizada automaticamente com o servidor e o streaming é iniciado.", "카메라를 추가하면 서버에 자동 동기화되고 스트리밍이 시작됩니다.",
            "カメラを追加するとサーバーに自動同期され、ストリーミングが始まります。", "添加摄像头后，系统会自动与服务器同步并开始流式传输。",
            "新增攝影機後，系統會自動與伺服器同步並開始串流。", "Khi thêm camera, camera sẽ tự động đồng bộ với máy chủ và bắt đầu truyền phát."
        ],
        ["step4.tip1"] =
        [
            "💡 The default RTSP port for most camera brands is <strong>554</strong>.<br />The camera can connect only within the same network (LAN), without router port forwarding.",
            "💡 El puerto RTSP predeterminado de la mayoría de las marcas es <strong>554</strong>.<br />La cámara solo puede conectarse dentro de la misma red (LAN), sin reenvío de puertos del router.",
            "💡 Le port RTSP par défaut de la plupart des marques est <strong>554</strong>.<br />La caméra ne peut se connecter qu’au sein du même réseau (LAN), sans redirection de port sur le routeur.",
            "💡 La porta RTSP predefinita per la maggior parte delle marche è <strong>554</strong>.<br />La telecamera può connettersi solo nella stessa rete (LAN), senza port forwarding del router.",
            "💡 A porta RTSP padrão da maioria das marcas é <strong>554</strong>.<br />A câmera só pode se conectar dentro da mesma rede (LAN), sem encaminhamento de portas no roteador.",
            "💡 카메라 브랜드별 기본 RTSP 포트는 대부분 <strong>554</strong>입니다.<br />공유기 포트포워딩 없이 같은 네트워크(LAN) 내에서만 연결 가능합니다.",
            "💡 多くのカメラブランドの既定RTSPポートは<strong>554</strong>です。<br />ルーターのポート転送なしで、同じネットワーク（LAN）内からのみ接続できます。",
            "💡 大多数摄像头品牌的默认 RTSP 端口是 <strong>554</strong>。<br />无需路由器端口转发，但只能在同一网络（LAN）内连接。",
            "💡 大多數攝影機品牌的預設 RTSP 連接埠是 <strong>554</strong>。<br />無須路由器連接埠轉送，但只能在同一網路（LAN）內連線。",
            "💡 Cổng RTSP mặc định của hầu hết thương hiệu camera là <strong>554</strong>.<br />Camera chỉ có thể kết nối trong cùng mạng (LAN), không cần chuyển tiếp cổng trên bộ định tuyến."
        ],
        ["step4.tip2"] =
        [
            "💡 <strong>Public vs. private</strong><br />Public: Shown in the live list on the home page; anyone with the link can watch<br />Private: Not shown on the home page; the camera is hidden even when someone opens its link<br />However, <strong>while you are signed in</strong>, you can also see private cameras on your own live page.",
            "💡 <strong>Público y privado</strong><br />Público: aparece en la lista en directo de la página de inicio; cualquiera con el enlace puede verlo<br />Privado: no aparece en la página de inicio; la cámara permanece oculta incluso al abrir el enlace<br />Sin embargo, <strong>mientras haya iniciado sesión</strong>, también podrá ver las cámaras privadas en su propia página en directo.",
            "💡 <strong>Public ou privé</strong><br />Public : apparaît dans la liste des directs de la page d’accueil ; toute personne disposant du lien peut regarder<br />Privé : n’apparaît pas sur la page d’accueil ; la caméra reste masquée même si le lien est ouvert<br />Cependant, <strong>lorsque vous êtes connecté</strong>, vous pouvez aussi voir les caméras privées sur votre propre page de direct.",
            "💡 <strong>Pubblica o privata</strong><br />Pubblica: appare nell’elenco live della home page; chiunque abbia il link può guardare<br />Privata: non appare nella home page; la telecamera resta nascosta anche aprendo il link<br />Tuttavia, <strong>quando hai effettuato l’accesso</strong>, puoi vedere anche le telecamere private nella tua pagina live.",
            "💡 <strong>Público ou privado</strong><br />Público: aparece na lista ao vivo da página inicial; qualquer pessoa com o link pode assistir<br />Privado: não aparece na página inicial; a câmera fica oculta mesmo ao abrir o link<br />Porém, <strong>quando você estiver conectado</strong>, também poderá ver câmeras privadas na sua própria página ao vivo.",
            "💡 <strong>공개 vs 비공개</strong><br />공개: 홈 화면 라이브 목록에 표시, 링크 공유 시 누구나 시청 가능<br />비공개: 홈 목록에 미표시, 링크로 접속해도 해당 카메라 화면 안 보임<br />단, <strong>본인이 로그인한 상태</strong>에서는 비공개 카메라도 내 라이브에서 볼 수 있습니다.",
            "💡 <strong>公開と非公開</strong><br />公開：ホーム画面のライブ一覧に表示され、リンクを知っている人は誰でも視聴可能<br />非公開：ホーム一覧に表示されず、リンクを開いてもカメラ映像は非表示<br />ただし、<strong>本人がログイン中</strong>であれば、非公開カメラも自分のライブページで視聴できます。",
            "💡 <strong>公开与私密</strong><br />公开：显示在首页直播列表中，任何获得链接的人都可观看<br />私密：不显示在首页列表中，即使打开链接也看不到该摄像头画面<br />但<strong>本人登录后</strong>，仍可在自己的直播页面查看私密摄像头。",
            "💡 <strong>公開與私人</strong><br />公開：顯示在首頁直播清單中，任何取得連結的人都能觀看<br />私人：不顯示在首頁清單中，即使開啟連結也看不到該攝影機畫面<br />但<strong>本人登入後</strong>，仍可在自己的直播頁面查看私人攝影機。",
            "💡 <strong>Công khai và riêng tư</strong><br />Công khai: hiển thị trong danh sách trực tiếp trên trang chủ; bất kỳ ai có liên kết đều có thể xem<br />Riêng tư: không hiển thị trên trang chủ; camera vẫn bị ẩn ngay cả khi mở liên kết<br />Tuy nhiên, <strong>khi bạn đã đăng nhập</strong>, bạn vẫn có thể xem camera riêng tư trên trang trực tiếp của mình."
        ],
        ["step5.title"] =
        [
            "Share the live link", "Compartir el enlace en directo", "Partager le lien du direct", "Condividi il link live", "Compartilhar o link ao vivo",
            "라이브 링크 공유", "ライブリンクを共有", "分享直播链接", "分享直播連結", "Chia sẻ liên kết trực tiếp"
        ],
        ["step5.intro"] =
        [
            "On the web <strong>Camera management</strong> page, set the Kakao share title, description, and preview image, then select <strong>Save</strong>.",
            "En la página web de <strong>Gestión de cámaras</strong>, configure el título, la descripción y la imagen de vista previa para compartir en Kakao; después seleccione <strong>Guardar</strong>.",
            "Sur la page Web <strong>Gestion des caméras</strong>, définissez le titre, la description et l’image d’aperçu du partage Kakao, puis sélectionnez <strong>Enregistrer</strong>.",
            "Nella pagina Web <strong>Gestione telecamere</strong>, imposta titolo, descrizione e immagine di anteprima per la condivisione Kakao, quindi seleziona <strong>Salva</strong>.",
            "Na página Web de <strong>Gerenciamento de câmeras</strong>, defina o título, a descrição e a imagem de prévia do compartilhamento no Kakao; depois selecione <strong>Salvar</strong>.",
            "웹의 <strong>카메라 관리</strong> 페이지에서 카카오 공유 제목·설명·미리보기 이미지를 설정하고 <strong>저장</strong>합니다.",
            "Webの<strong>カメラ管理</strong>ページでKakao共有のタイトル・説明・プレビュー画像を設定し、<strong>保存</strong>します。",
            "在网页的<strong>摄像头管理</strong>页面设置 Kakao 分享标题、说明和预览图片，然后点击<strong>保存</strong>。",
            "在網頁的<strong>攝影機管理</strong>頁面設定 Kakao 分享標題、說明與預覽圖片，然後按<strong>儲存</strong>。",
            "Trên trang web <strong>Quản lý camera</strong>, đặt tiêu đề, mô tả và ảnh xem trước khi chia sẻ Kakao, sau đó chọn <strong>Lưu</strong>."
        ],
        ["step5.item1"] =
        [
            "Public link: <code>https://cctvviewer.codemaru.co.kr/{nickname}/live</code>", "Enlace público: <code>https://cctvviewer.codemaru.co.kr/{nickname}/live</code>",
            "Lien public : <code>https://cctvviewer.codemaru.co.kr/{nickname}/live</code>", "Link pubblico: <code>https://cctvviewer.codemaru.co.kr/{nickname}/live</code>",
            "Link público: <code>https://cctvviewer.codemaru.co.kr/{nickname}/live</code>", "공개 링크: <code>https://cctvviewer.codemaru.co.kr/{닉네임}/live</code>",
            "公開リンク：<code>https://cctvviewer.codemaru.co.kr/{nickname}/live</code>", "公开链接：<code>https://cctvviewer.codemaru.co.kr/{nickname}/live</code>",
            "公開連結：<code>https://cctvviewer.codemaru.co.kr/{nickname}/live</code>", "Liên kết công khai: <code>https://cctvviewer.codemaru.co.kr/{nickname}/live</code>"
        ],
        ["step5.item2"] =
        [
            "When you share the link on KakaoTalk, the configured image and title appear in the preview.", "Al compartir el enlace en KakaoTalk, la imagen y el título configurados aparecerán en la vista previa.",
            "Lorsque vous partagez le lien sur KakaoTalk, l’image et le titre définis apparaissent dans l’aperçu.", "Quando condividi il link su KakaoTalk, l’immagine e il titolo impostati vengono mostrati nell’anteprima.",
            "Ao compartilhar o link no KakaoTalk, a imagem e o título configurados aparecem na prévia.", "카카오톡으로 링크를 공유하면 설정한 이미지와 제목이 미리보기로 표시됩니다.",
            "KakaoTalkでリンクを共有すると、設定した画像とタイトルがプレビューに表示されます。", "在 KakaoTalk 中分享链接时，设置的图片和标题会显示在预览中。",
            "在 KakaoTalk 中分享連結時，設定的圖片與標題會顯示於預覽中。", "Khi chia sẻ liên kết trên KakaoTalk, hình ảnh và tiêu đề đã đặt sẽ xuất hiện trong phần xem trước."
        ],
        ["step5.item3"] =
        [
            "Viewers can watch immediately in a mobile or PC browser without installing the app.", "Los espectadores pueden verlo inmediatamente desde un navegador móvil o de PC, sin instalar la aplicación.",
            "Les spectateurs peuvent regarder immédiatement depuis un navigateur mobile ou PC, sans installer l’application.", "Gli spettatori possono guardare subito da un browser per smartphone o PC, senza installare l’app.",
            "Os espectadores podem assistir imediatamente em um navegador de celular ou PC, sem instalar o aplicativo.", "보는 사람은 앱 설치 없이 스마트폰·PC 브라우저로 바로 시청 가능합니다.",
            "視聴者はアプリをインストールせずに、スマートフォンやPCのブラウザーですぐに視聴できます。", "观看者无需安装应用，使用手机或 PC 浏览器即可直接观看。",
            "觀看者無須安裝應用程式，使用手機或 PC 瀏覽器即可直接觀看。", "Người xem có thể xem ngay trên trình duyệt điện thoại hoặc PC mà không cần cài đặt ứng dụng."
        ],
        ["step5.action"] =
        [
            "Open camera management →", "Abrir gestión de cámaras →", "Ouvrir la gestion des caméras →", "Apri gestione telecamere →",
            "Abrir gerenciamento de câmeras →", "카메라 관리 페이지 열기 →", "カメラ管理ページを開く →", "打开摄像头管理页面 →",
            "開啟攝影機管理頁面 →", "Mở trang quản lý camera →"
        ],
        ["faq.title"] =
        [
            "Frequently asked questions", "Preguntas frecuentes", "Questions fréquentes", "Domande frequenti", "Perguntas frequentes",
            "자주 묻는 질문", "よくある質問", "常见问题", "常見問題", "Câu hỏi thường gặp"
        ],
        ["faq1.question"] =
        [
            "Q. My camera does not appear in the list.", "P. Mi cámara no aparece en la lista.", "Q. Ma caméra n’apparaît pas dans la liste.",
            "D. La telecamera non appare nell’elenco.", "P. Minha câmera não aparece na lista.", "Q. 카메라가 목록에 나타나지 않아요.",
            "Q. カメラが一覧に表示されません。", "问：摄像头没有显示在列表中。", "問：攝影機沒有顯示在清單中。", "H. Camera không xuất hiện trong danh sách."
        ],
        ["faq1.answer"] =
        [
            "Check that the email and password are entered correctly in the Agent settings tab, then select <strong>Reconnect</strong>. The app will sync with the server again.",
            "Compruebe que el correo y la contraseña sean correctos en la pestaña Configuración del agente y seleccione <strong>Reconectar</strong>. La aplicación volverá a sincronizarse con el servidor.",
            "Vérifiez que l’adresse e-mail et le mot de passe sont corrects dans l’onglet Paramètres de l’agent, puis sélectionnez <strong>Reconnecter</strong>. L’application se synchronisera à nouveau avec le serveur.",
            "Verifica che e-mail e password siano corrette nella scheda Impostazioni agente, quindi seleziona <strong>Riconnetti</strong>. L’app si sincronizzerà di nuovo con il server.",
            "Verifique se o e-mail e a senha estão corretos na guia Configurações do agente e selecione <strong>Reconectar</strong>. O aplicativo sincronizará novamente com o servidor.",
            "에이전트 설정 탭에서 이메일·비밀번호가 올바르게 입력되었는지 확인 후 <strong>재연결</strong>을 누르세요. 서버와 다시 동기화됩니다.",
            "エージェント設定タブでメールアドレスとパスワードが正しいか確認し、<strong>再接続</strong>を押してください。サーバーと再同期されます。",
            "请检查代理设置选项卡中的邮箱和密码是否正确，然后点击<strong>重新连接</strong>。应用会再次与服务器同步。",
            "請檢查代理程式設定分頁中的電子郵件與密碼是否正確，然後按<strong>重新連線</strong>。應用程式會再次與伺服器同步。",
            "Kiểm tra email và mật khẩu trong tab Cài đặt tác nhân, sau đó chọn <strong>Kết nối lại</strong>. Ứng dụng sẽ đồng bộ lại với máy chủ."
        ],
        ["faq2.question"] =
        [
            "Q. The live video is black.", "P. La imagen en directo aparece negra.", "Q. L’image en direct est noire.", "D. Il video live è nero.",
            "P. A imagem ao vivo fica preta.", "Q. 라이브 화면이 검게 나와요.", "Q. ライブ映像が真っ黒です。", "问：直播画面是黑屏。", "問：直播畫面是黑畫面。", "H. Màn hình trực tiếp bị đen."
        ],
        ["faq2.answer"] =
        [
            "Check that the RTSP URL is correct. The status in the camera list on the left side of the app must be <span style=\"color:#4ade80;\">●</span> green while streaming. Check the camera ID, password, and port number again.",
            "Compruebe que la URL RTSP sea correcta. El estado en la lista de cámaras situada a la izquierda de la aplicación debe mostrarse en verde <span style=\"color:#4ade80;\">●</span> durante la transmisión. Revise de nuevo el ID, la contraseña y el puerto de la cámara.",
            "Vérifiez que l’URL RTSP est correcte. Dans la liste des caméras à gauche de l’application, l’état doit être vert <span style=\"color:#4ade80;\">●</span> pendant la diffusion. Vérifiez à nouveau l’identifiant, le mot de passe et le port de la caméra.",
            "Verifica che l’URL RTSP sia corretto. Durante lo streaming, lo stato nell’elenco delle telecamere a sinistra dell’app deve essere verde <span style=\"color:#4ade80;\">●</span>. Controlla di nuovo ID, password e porta della telecamera.",
            "Verifique se a URL RTSP está correta. Durante o streaming, o status na lista de câmeras à esquerda do aplicativo deve estar verde <span style=\"color:#4ade80;\">●</span>. Confira novamente o ID, a senha e a porta da câmera.",
            "RTSP URL이 올바른지 확인하세요. 앱 왼쪽 카메라 목록에서 상태가 <span style=\"color:#4ade80;\">●</span> 녹색이어야 스트리밍 중입니다. 카메라 ID·비밀번호, 포트 번호를 다시 확인해 보세요.",
            "RTSP URLが正しいか確認してください。ストリーミング中は、アプリ左側のカメラ一覧の状態が<span style=\"color:#4ade80;\">●</span>緑になります。カメラのID・パスワード・ポート番号を再確認してください。",
            "请确认 RTSP URL 是否正确。进行流式传输时，应用左侧摄像头列表中的状态应为绿色 <span style=\"color:#4ade80;\">●</span>。请再次检查摄像头 ID、密码和端口号。",
            "請確認 RTSP URL 是否正確。進行串流時，應用程式左側攝影機清單中的狀態應為綠色 <span style=\"color:#4ade80;\">●</span>。請再次檢查攝影機 ID、密碼與連接埠號碼。",
            "Kiểm tra URL RTSP có chính xác không. Khi đang truyền phát, trạng thái trong danh sách camera bên trái ứng dụng phải có màu xanh <span style=\"color:#4ade80;\">●</span>. Hãy kiểm tra lại ID, mật khẩu và số cổng của camera."
        ],
        ["faq3.question"] =
        [
            "Q. Does live streaming stop when the PC is turned off?", "P. ¿La transmisión se detiene cuando se apaga el PC?",
            "Q. Le direct s’arrête-t-il lorsque le PC est éteint ?", "D. Lo streaming si interrompe quando il PC viene spento?",
            "P. A transmissão é interrompida quando o PC é desligado?", "Q. PC가 꺼지면 라이브가 중단되나요?",
            "Q. PCの電源を切るとライブ配信は停止しますか？", "问：PC 关机后直播会中断吗？", "問：PC 關機後直播會中斷嗎？", "H. Phát trực tiếp có dừng khi tắt PC không?"
        ],
        ["faq3.answer"] =
        [
            "Yes. The PC running the agent app must remain on for streaming. We recommend leaving the PC on 24 hours a day or using a NAS or mini PC.",
            "Sí. El PC que ejecuta la aplicación del agente debe permanecer encendido para transmitir. Recomendamos dejarlo encendido las 24 horas o usar un NAS o mini PC.",
            "Oui. Le PC exécutant l’application agent doit rester allumé pour diffuser. Nous recommandons de laisser le PC allumé 24 h/24 ou d’utiliser un NAS ou un mini-PC.",
            "Sì. Il PC su cui è in esecuzione l’app agente deve restare acceso per lo streaming. Consigliamo di tenerlo acceso 24 ore su 24 o di usare un NAS o un mini PC.",
            "Sim. O PC que executa o aplicativo agente deve permanecer ligado para transmitir. Recomendamos deixá-lo ligado 24 horas ou usar um NAS ou mini PC.",
            "네. 에이전트 앱이 실행 중인 PC가 켜져 있어야 스트리밍됩니다. PC를 24시간 켜두거나 NAS·미니PC 활용을 권장합니다.",
            "はい。ストリーミングには、エージェントアプリを実行するPCの電源が入っている必要があります。PCを24時間稼働させるか、NASやミニPCの利用をおすすめします。",
            "是的。运行代理应用的 PC 必须保持开机才能进行流式传输。建议让 PC 24 小时开机，或使用 NAS、迷你 PC。",
            "是的。執行代理程式應用程式的 PC 必須保持開機才能進行串流。建議讓 PC 24 小時開機，或使用 NAS、迷你 PC。",
            "Có. PC chạy ứng dụng tác nhân phải được bật thì mới truyền phát được. Bạn nên để PC hoạt động 24 giờ hoặc sử dụng NAS hay mini PC."
        ],
        ["faq4.question"] =
        [
            "Q. I cannot connect from outside the network (the Internet).", "P. No puedo conectarme desde fuera de la red (Internet).",
            "Q. Je n’arrive pas à me connecter depuis l’extérieur (Internet).", "D. Non riesco a collegarmi dall’esterno (Internet).",
            "P. Não consigo conectar de fora da rede (Internet).", "Q. 외부(인터넷)에서 접속이 안 돼요.",
            "Q. 外部（インターネット）から接続できません。", "问：无法从外部（互联网）连接。", "問：無法從外部（網際網路）連線。", "H. Tôi không thể kết nối từ bên ngoài (Internet)."
        ],
        ["faq4.answer"] =
        [
            "The PC with the agent app only needs an Internet connection. Router port forwarding is not required. However, if the PC firewall blocks the app, you must allow it through the firewall.",
            "El PC con la aplicación del agente solo necesita conexión a Internet. No se requiere reenvío de puertos del router. Sin embargo, si el firewall del PC bloquea la aplicación, deberá permitirla.",
            "Le PC équipé de l’application agent a seulement besoin d’une connexion Internet. La redirection de port du routeur n’est pas nécessaire. Toutefois, si le pare-feu du PC bloque l’application, vous devez l’autoriser.",
            "Il PC con l’app agente necessita solo di una connessione Internet. Il port forwarding del router non è richiesto. Se però il firewall del PC blocca l’app, devi autorizzarla.",
            "O PC com o aplicativo agente precisa apenas de conexão com a Internet. O encaminhamento de portas do roteador não é necessário. Porém, se o firewall do PC bloquear o aplicativo, será preciso permiti-lo.",
            "에이전트 앱이 설치된 PC는 인터넷 연결만 있으면 됩니다. 공유기 포트포워딩은 필요하지 않습니다. 단, PC의 방화벽이 앱을 차단하고 있으면 허용 설정이 필요합니다.",
            "エージェントアプリをインストールしたPCはインターネット接続だけで利用できます。ルーターのポート転送は不要です。ただし、PCのファイアウォールがアプリをブロックしている場合は許可設定が必要です。",
            "安装代理应用的 PC 只需连接互联网，无需设置路由器端口转发。但如果 PC 防火墙阻止了该应用，则需要将其设为允许。",
            "安裝代理程式應用程式的 PC 只需連線至網際網路，無須設定路由器連接埠轉送。但若 PC 防火牆封鎖該應用程式，則需要將其設為允許。",
            "PC cài ứng dụng tác nhân chỉ cần có kết nối Internet. Không cần chuyển tiếp cổng trên bộ định tuyến. Tuy nhiên, nếu tường lửa của PC chặn ứng dụng, bạn phải cho phép ứng dụng qua tường lửa."
        ],
        ["faq5.question"] =
        [
            "Q. My camera brand is not in the list.", "P. La marca de mi cámara no aparece en la lista.", "Q. La marque de ma caméra n’est pas dans la liste.",
            "D. La marca della telecamera non è nell’elenco.", "P. A marca da minha câmera não está na lista.", "Q. 카메라 브랜드가 목록에 없어요.",
            "Q. カメラのブランドが一覧にありません。", "问：列表中没有我的摄像头品牌。", "問：清單中沒有我的攝影機品牌。", "H. Thương hiệu camera của tôi không có trong danh sách."
        ],
        ["faq5.answer"] =
        [
            "Enter the RTSP URL directly. Search the camera manufacturer’s manual or Google for <code>[brand name] RTSP URL</code> to find the format.",
            "Introduzca la URL RTSP directamente. Consulte el manual del fabricante o busque en Google <code>[nombre de la marca] RTSP URL</code> para encontrar el formato.",
            "Saisissez directement l’URL RTSP. Consultez le manuel du fabricant ou recherchez <code>[nom de la marque] RTSP URL</code> sur Google pour trouver le format.",
            "Inserisci direttamente l’URL RTSP. Consulta il manuale del produttore o cerca su Google <code>[nome marca] RTSP URL</code> per trovare il formato.",
            "Informe a URL RTSP diretamente. Consulte o manual do fabricante ou pesquise no Google por <code>[nome da marca] RTSP URL</code> para encontrar o formato.",
            "RTSP URL을 직접 입력하세요. 카메라 제조사 매뉴얼이나 구글에서 <code>[브랜드명] RTSP URL</code>로 검색하면 형식을 확인할 수 있습니다.",
            "RTSP URLを直接入力してください。カメラメーカーのマニュアルを確認するか、Googleで<code>[ブランド名] RTSP URL</code>と検索すると形式を確認できます。",
            "请直接输入 RTSP URL。可查看摄像头厂商手册，或在 Google 搜索 <code>[品牌名称] RTSP URL</code> 以确认格式。",
            "請直接輸入 RTSP URL。可查看攝影機廠商手冊，或在 Google 搜尋 <code>[品牌名稱] RTSP URL</code> 以確認格式。",
            "Hãy nhập trực tiếp URL RTSP. Xem hướng dẫn của nhà sản xuất camera hoặc tìm trên Google với <code>[tên thương hiệu] RTSP URL</code> để biết định dạng."
        ],
        ["footer.action"] =
        [
            "Start for free now →", "Comenzar gratis ahora →", "Commencer gratuitement →", "Inizia gratis ora →", "Comece grátis agora →",
            "지금 무료로 시작 →", "今すぐ無料で始める →", "立即免费开始 →", "立即免費開始 →", "Bắt đầu miễn phí ngay →"
        ]
    };

    public static string Get(string? language, string key)
    {
        var code = VmsLocalization.NormalizeLanguageCode(language);
        var index = Array.FindIndex(Codes, item => string.Equals(item, code, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
            index = Array.IndexOf(Codes, "ko");

        return Texts.TryGetValue(key, out var values) && index < values.Length
            ? values[index]
            : key;
    }
}
