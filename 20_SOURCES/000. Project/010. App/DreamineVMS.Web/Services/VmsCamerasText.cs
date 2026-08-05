using System.Globalization;

namespace DreamineVMS.Web.Services;

/// <summary>Localized copy used exclusively by the camera management page.</summary>
public static class VmsCamerasText
{
    private static readonly string[] Codes =
    [
        "en", "es", "fr", "it", "pt", "ko", "ja", "zh-hans", "zh-hant", "vi"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> Texts =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["page.title"] = L(
                "Camera management — Dreamine VMS",
                "Gestión de cámaras — Dreamine VMS",
                "Gestion des caméras — Dreamine VMS",
                "Gestione telecamere — Dreamine VMS",
                "Gerenciamento de câmeras — Dreamine VMS",
                "카메라 관리 — Dreamine VMS",
                "カメラ管理 — Dreamine VMS",
                "摄像头管理 — Dreamine VMS",
                "攝影機管理 — Dreamine VMS",
                "Quản lý camera — Dreamine VMS"),
            ["back.home"] = L(
                "Back to home", "Volver al inicio", "Retour à l’accueil", "Torna alla home", "Voltar ao início",
                "홈으로", "ホームへ戻る", "返回首页", "返回首頁", "Về trang chủ"),
            ["heading"] = L(
                "Camera management", "Gestión de cámaras", "Gestion des caméras", "Gestione telecamere", "Gerenciamento de câmeras",
                "카메라 관리", "カメラ管理", "摄像头管理", "攝影機管理", "Quản lý camera"),
            ["public.link"] = L(
                "Public link", "Enlace público", "Lien public", "Link pubblico", "Link público",
                "공개 링크", "公開リンク", "公开链接", "公開連結", "Liên kết công khai"),
            ["my.live"] = L(
                "My live view", "Mi directo", "Mon direct", "La mia diretta", "Minha transmissão",
                "내 라이브", "自分のライブ", "我的直播", "我的直播", "Luồng trực tiếp của tôi"),
            ["logout"] = L(
                "Sign out", "Cerrar sesión", "Déconnexion", "Esci", "Sair",
                "로그아웃", "ログアウト", "退出登录", "登出", "Đăng xuất"),

            ["agent.account"] = L(
                "Agent connection account", "Cuenta de conexión del agente", "Compte de connexion de l’agent", "Account di connessione dell’agente", "Conta de conexão do agente",
                "에이전트 연결 계정", "エージェント接続アカウント", "代理程序连接账户", "代理程式連線帳戶", "Tài khoản kết nối tác nhân"),
            ["agent.description"] = L(
                "Sign in to the Dreamine VMS agent installed on your PC with the email below and the password you set here.",
                "Inicia sesión en el agente de Dreamine VMS instalado en tu PC con el correo de abajo y la contraseña que configures aquí.",
                "Connectez-vous à l’agent Dreamine VMS installé sur votre PC avec l’adresse e-mail ci-dessous et le mot de passe défini ici.",
                "Accedi all’agente Dreamine VMS installato sul PC con l’e-mail qui sotto e la password impostata qui.",
                "Inicie sessão no agente Dreamine VMS instalado no PC com o e-mail abaixo e a palavra-passe definida aqui.",
                "PC에 설치한 Dreamine VMS 에이전트에서 아래 이메일과 여기서 설정한 비밀번호로 로그인합니다.",
                "PCにインストールしたDreamine VMSエージェントで、以下のメールアドレスとここで設定したパスワードを使ってログインします。",
                "请在电脑上安装的 Dreamine VMS 代理程序中，使用下方邮箱和在此设置的密码登录。",
                "請在電腦上安裝的 Dreamine VMS 代理程式中，使用下方電子郵件與在此設定的密碼登入。",
                "Đăng nhập vào tác nhân Dreamine VMS đã cài trên PC bằng email bên dưới và mật khẩu bạn đặt tại đây."),
            ["agent.email"] = L(
                "Agent email", "Correo del agente", "E-mail de l’agent", "E-mail dell’agente", "E-mail do agente",
                "에이전트 이메일", "エージェントのメールアドレス", "代理程序邮箱", "代理程式電子郵件", "Email tác nhân"),
            ["password.status"] = L(
                "Password status", "Estado de la contraseña", "État du mot de passe", "Stato della password", "Estado da palavra-passe",
                "비밀번호 상태", "パスワードの状態", "密码状态", "密碼狀態", "Trạng thái mật khẩu"),
            ["status.configured"] = L(
                "Set", "Configurada", "Défini", "Impostata", "Definida",
                "설정됨", "設定済み", "已设置", "已設定", "Đã đặt"),
            ["status.not.configured"] = L(
                "Not set", "No configurada", "Non défini", "Non impostata", "Não definida",
                "미설정", "未設定", "未设置", "未設定", "Chưa đặt"),
            ["new.agent.password"] = L(
                "New agent password", "Nueva contraseña del agente", "Nouveau mot de passe de l’agent", "Nuova password dell’agente", "Nova palavra-passe do agente",
                "새 에이전트 비밀번호", "新しいエージェントパスワード", "新代理程序密码", "新的代理程式密碼", "Mật khẩu tác nhân mới"),
            ["placeholder.password.min"] = L(
                "8 or more characters", "8 caracteres o más", "8 caractères minimum", "Almeno 8 caratteri", "8 ou mais caracteres",
                "8자 이상", "8文字以上", "至少 8 个字符", "至少 8 個字元", "Từ 8 ký tự"),
            ["password.confirmation"] = L(
                "Confirm password", "Confirmar contraseña", "Confirmer le mot de passe", "Conferma password", "Confirmar palavra-passe",
                "비밀번호 확인", "パスワードの確認", "确认密码", "確認密碼", "Xác nhận mật khẩu"),
            ["placeholder.password.repeat"] = L(
                "Enter it again", "Vuelve a introducirla", "Saisissez-le à nouveau", "Inseriscila di nuovo", "Introduza novamente",
                "한 번 더 입력", "もう一度入力", "请再次输入", "請再次輸入", "Nhập lại mật khẩu"),
            ["saving"] = L(
                "Saving…", "Guardando…", "Enregistrement…", "Salvataggio…", "A guardar…",
                "저장 중...", "保存中…", "正在保存…", "正在儲存…", "Đang lưu…"),
            ["save.password"] = L(
                "Save password", "Guardar contraseña", "Enregistrer le mot de passe", "Salva password", "Guardar palavra-passe",
                "비밀번호 저장", "パスワードを保存", "保存密码", "儲存密碼", "Lưu mật khẩu"),

            ["share.settings"] = L(
                "Kakao share preview settings", "Vista previa al compartir en Kakao", "Aperçu du partage Kakao", "Anteprima di condivisione Kakao", "Pré-visualização de partilha no Kakao",
                "카카오 공유 미리보기 설정", "Kakao共有プレビュー設定", "Kakao 分享预览设置", "Kakao 分享預覽設定", "Cài đặt bản xem trước khi chia sẻ Kakao"),
            ["share.description"] = L(
                "Set the title, description, and image shown when you share your live link on KakaoTalk.",
                "Configura el título, la descripción y la imagen que se muestran al compartir tu enlace en directo por KakaoTalk.",
                "Définissez le titre, la description et l’image affichés lorsque vous partagez votre lien en direct sur KakaoTalk.",
                "Imposta titolo, descrizione e immagine mostrati quando condividi il link della diretta su KakaoTalk.",
                "Defina o título, a descrição e a imagem apresentados ao partilhar o link da transmissão no KakaoTalk.",
                "카카오톡으로 내 라이브 링크를 공유할 때 표시되는 제목, 설명, 이미지를 설정합니다.",
                "KakaoTalkでライブリンクを共有したときに表示されるタイトル、説明、画像を設定します。",
                "设置在 KakaoTalk 中分享直播链接时显示的标题、说明和图片。",
                "設定在 KakaoTalk 分享直播連結時顯示的標題、說明與圖片。",
                "Đặt tiêu đề, mô tả và hình ảnh hiển thị khi bạn chia sẻ liên kết trực tiếp trên KakaoTalk."),
            ["field.title"] = L(
                "Title", "Título", "Titre", "Titolo", "Título", "제목", "タイトル", "标题", "標題", "Tiêu đề"),
            ["placeholder.share.title"] = L(
                "{0} CCTV Live", "CCTV en directo de {0}", "CCTV en direct de {0}", "CCTV in diretta di {0}", "CCTV ao vivo de {0}",
                "{0} CCTV Live", "{0} CCTVライブ", "{0} CCTV 直播", "{0} CCTV 直播", "CCTV trực tiếp của {0}"),
            ["field.description"] = L(
                "Description", "Descripción", "Description", "Descrizione", "Descrição", "설명", "説明", "说明", "說明", "Mô tả"),
            ["placeholder.share.description"] = L(
                "Live camera stream.", "Transmisión de cámara en directo.", "Flux de caméra en direct.", "Streaming della telecamera in diretta.", "Transmissão de câmara em direto.",
                "실시간 카메라 스트림입니다.", "カメラのライブ映像です。", "实时摄像头画面。", "即時攝影機串流。", "Luồng camera trực tiếp."),
            ["field.image"] = L(
                "Image", "Imagen", "Image", "Immagine", "Imagem", "이미지", "画像", "图片", "圖片", "Hình ảnh"),
            ["upload.image"] = L(
                "📁 Upload image", "📁 Subir imagen", "📁 Importer une image", "📁 Carica immagine", "📁 Carregar imagem",
                "📁 이미지 업로드", "📁 画像をアップロード", "📁 上传图片", "📁 上傳圖片", "📁 Tải ảnh lên"),
            ["upload.hint"] = L(
                "Or enter a URL directly (JPG, PNG, or WebP; up to 5 MB)",
                "O introduce una URL directamente (JPG, PNG o WebP; máximo 5 MB)",
                "Ou saisissez directement une URL (JPG, PNG ou WebP ; 5 Mo maximum)",
                "Oppure inserisci direttamente un URL (JPG, PNG o WebP; massimo 5 MB)",
                "Ou introduza diretamente um URL (JPG, PNG ou WebP; máximo de 5 MB)",
                "또는 URL 직접 입력 (JPG·PNG·WebP, 최대 5MB)",
                "またはURLを直接入力（JPG・PNG・WebP、最大5MB）",
                "或直接输入 URL（JPG、PNG、WebP，最大 5 MB）",
                "或直接輸入 URL（JPG、PNG、WebP，最大 5 MB）",
                "Hoặc nhập URL trực tiếp (JPG, PNG hoặc WebP; tối đa 5 MB)"),
            ["preview.alt"] = L(
                "Share image preview", "Vista previa de la imagen compartida", "Aperçu de l’image de partage", "Anteprima dell’immagine condivisa", "Pré-visualização da imagem de partilha",
                "공유 이미지 미리보기", "共有画像のプレビュー", "分享图片预览", "分享圖片預覽", "Bản xem trước ảnh chia sẻ"),
            ["save"] = L(
                "Save", "Guardar", "Enregistrer", "Salva", "Guardar", "저장", "保存", "保存", "儲存", "Lưu"),
            ["open.my.live"] = L(
                "Open my live view", "Abrir mi directo", "Ouvrir mon direct", "Apri la mia diretta", "Abrir a minha transmissão",
                "내 라이브 열기", "自分のライブを開く", "打开我的直播", "開啟我的直播", "Mở luồng trực tiếp của tôi"),

            ["cameras.registered"] = L(
                "Registered cameras ({0})", "Cámaras registradas ({0})", "Caméras enregistrées ({0})", "Telecamere registrate ({0})", "Câmaras registadas ({0})",
                "등록된 카메라 ({0}개)", "登録済みカメラ（{0}台）", "已注册的摄像头（{0}）", "已註冊的攝影機（{0}）", "Camera đã đăng ký ({0})"),
            ["cameras.description"] = L(
                "Manage cameras in the Dreamine VMS agent app. They are synchronized automatically when the app connects.",
                "Gestiona las cámaras en la aplicación del agente Dreamine VMS. Se sincronizan automáticamente cuando la aplicación se conecta.",
                "Gérez les caméras dans l’application de l’agent Dreamine VMS. Elles sont synchronisées automatiquement lors de la connexion.",
                "Gestisci le telecamere nell’app dell’agente Dreamine VMS. Vengono sincronizzate automaticamente quando l’app si connette.",
                "Faça a gestão das câmaras na aplicação do agente Dreamine VMS. São sincronizadas automaticamente quando a aplicação se liga.",
                "카메라는 Dreamine VMS 에이전트 앱에서 관리합니다. 앱이 연결되면 자동으로 동기화됩니다.",
                "カメラはDreamine VMSエージェントアプリで管理します。アプリが接続されると自動的に同期されます。",
                "请在 Dreamine VMS 代理程序中管理摄像头。程序连接后会自动同步。",
                "請在 Dreamine VMS 代理程式中管理攝影機。程式連線後會自動同步。",
                "Quản lý camera trong ứng dụng tác nhân Dreamine VMS. Camera sẽ tự động đồng bộ khi ứng dụng kết nối."),
            ["cameras.empty"] = L(
                "Add cameras in the agent app, then connect it.", "Añade cámaras en la aplicación del agente y conéctala.", "Ajoutez des caméras dans l’application de l’agent, puis connectez-la.", "Aggiungi le telecamere nell’app dell’agente, quindi connettila.", "Adicione câmaras na aplicação do agente e ligue-a.",
                "에이전트 앱에서 카메라를 추가하고 연결하세요.", "エージェントアプリでカメラを追加して接続してください。", "请在代理程序中添加摄像头并连接。", "請在代理程式中新增攝影機並連線。", "Thêm camera trong ứng dụng tác nhân rồi kết nối."),
            ["table.name"] = L(
                "Name", "Nombre", "Nom", "Nome", "Nome", "이름", "名前", "名称", "名稱", "Tên"),
            ["table.host"] = L(
                "Host", "Host", "Hôte", "Host", "Anfitrião", "호스트", "ホスト", "主机", "主機", "Máy chủ"),
            ["table.status"] = L(
                "Status", "Estado", "État", "Stato", "Estado", "상태", "状態", "状态", "狀態", "Trạng thái"),
            ["table.visibility"] = L(
                "Visibility", "Visibilidad", "Visibilité", "Visibilità", "Visibilidade", "공개 여부", "公開範囲", "可见性", "可見性", "Khả năng hiển thị"),
            ["table.actions"] = L(
                "Actions", "Acciones", "Actions", "Azioni", "Ações", "작업", "操作", "操作", "操作", "Thao tác"),
            ["status.enabled"] = L(
                "Enabled", "Activa", "Active", "Attiva", "Ativa", "활성", "有効", "已启用", "已啟用", "Đã bật"),
            ["status.disabled"] = L(
                "Disabled", "Inactiva", "Inactive", "Disattivata", "Inativa", "비활성", "無効", "已禁用", "已停用", "Đã tắt"),
            ["visibility.public"] = L(
                "Public", "Pública", "Publique", "Pubblica", "Pública", "공개", "公開", "公开", "公開", "Công khai"),
            ["visibility.private"] = L(
                "Private", "Privada", "Privée", "Privata", "Privada", "비공개", "非公開", "私密", "私人", "Riêng tư"),
            ["delete"] = L(
                "Delete", "Eliminar", "Supprimer", "Elimina", "Eliminar", "삭제", "削除", "删除", "刪除", "Xóa"),

            ["layout.settings"] = L(
                "Live layout settings", "Diseño de la vista en directo", "Disposition de la vue en direct", "Layout della vista in diretta", "Esquema da vista em direto",
                "라이브 레이아웃 설정", "ライブレイアウト設定", "直播布局设置", "直播版面配置", "Cài đặt bố cục trực tiếp"),
            ["layout.description"] = L(
                "Choose how the live view is divided. Camera positions follow their order.",
                "Elige cómo se divide la vista en directo. La posición de las cámaras sigue su orden.",
                "Choisissez le découpage de la vue en direct. La position des caméras suit leur ordre.",
                "Scegli come suddividere la vista in diretta. La posizione delle telecamere segue il loro ordine.",
                "Escolha como dividir a vista em direto. A posição das câmaras segue a respetiva ordem.",
                "라이브 뷰 화면 분할 방식을 선택합니다. 카메라 순서에 따라 위치가 결정됩니다.",
                "ライブビューの分割方法を選択します。カメラの並び順に応じて配置が決まります。",
                "选择直播画面的分屏方式。摄像头位置按排列顺序决定。",
                "選擇直播畫面的分割方式。攝影機位置依排列順序決定。",
                "Chọn cách chia màn hình trực tiếp. Vị trí camera được xác định theo thứ tự."),
            ["layout.auto"] = L(
                "Automatic", "Automático", "Automatique", "Automatico", "Automático", "자동", "自動", "自动", "自動", "Tự động"),
            ["layout.1"] = L(
                "1 panel", "1 panel", "1 panneau", "1 riquadro", "1 painel", "1분할", "1分割", "单画面", "單一畫面", "1 ô"),
            ["layout.2h"] = L(
                "2 panels (horizontal)", "2 paneles (horizontal)", "2 panneaux (horizontal)", "2 riquadri (orizzontale)", "2 painéis (horizontal)",
                "2분할 (가로)", "2分割（横）", "双画面（横向）", "雙畫面（橫向）", "2 ô (ngang)"),
            ["layout.2v"] = L(
                "2 panels (vertical)", "2 paneles (vertical)", "2 panneaux (vertical)", "2 riquadri (verticale)", "2 painéis (vertical)",
                "2분할 (세로)", "2分割（縦）", "双画面（纵向）", "雙畫面（縱向）", "2 ô (dọc)"),
            ["layout.3-left"] = L(
                "3 panels (large left)", "3 paneles (izquierda grande)", "3 panneaux (grand à gauche)", "3 riquadri (grande a sinistra)", "3 painéis (grande à esquerda)",
                "3분할 (좌 대)", "3分割（左を大きく）", "三画面（左侧大）", "三畫面（左側大）", "3 ô (trái lớn)"),
            ["layout.3-top"] = L(
                "3 panels (large top)", "3 paneles (arriba grande)", "3 panneaux (grand en haut)", "3 riquadri (grande in alto)", "3 painéis (grande em cima)",
                "3분할 (상 대)", "3分割（上を大きく）", "三画面（上方大）", "三畫面（上方大）", "3 ô (trên lớn)"),
            ["layout.4"] = L(
                "4 panels", "4 paneles", "4 panneaux", "4 riquadri", "4 painéis", "4분할", "4分割", "四画面", "四畫面", "4 ô"),

            ["message.layout.saved"] = L(
                "The layout was saved as “{0}”.", "El diseño se guardó como «{0}».", "La disposition « {0} » a été enregistrée.", "Il layout è stato salvato come “{0}”.", "O esquema foi guardado como “{0}”.",
                "레이아웃을 '{0}'로 저장했습니다.", "レイアウトを「{0}」として保存しました。", "布局已保存为“{0}”。", "版面配置已儲存為「{0}」。", "Đã lưu bố cục “{0}”."),
            ["error.file.too.large"] = L(
                "The file is too large (maximum 5 MB).", "El archivo es demasiado grande (máximo 5 MB).", "Le fichier est trop volumineux (5 Mo maximum).", "Il file è troppo grande (massimo 5 MB).", "O ficheiro é demasiado grande (máximo de 5 MB).",
                "파일이 너무 큽니다 (최대 5MB).", "ファイルが大きすぎます（最大5MB）。", "文件过大（最大 5 MB）。", "檔案過大（最大 5 MB）。", "Tệp quá lớn (tối đa 5 MB)."),
            ["error.session.expired"] = L(
                "Your session has expired.", "Tu sesión ha caducado.", "Votre session a expiré.", "La sessione è scaduta.", "A sua sessão expirou.",
                "세션이 만료되었습니다.", "セッションの有効期限が切れました。", "会话已过期。", "工作階段已過期。", "Phiên của bạn đã hết hạn."),
            ["error.upload.failed"] = L(
                "Upload failed: {0}", "Error al subir: {0}", "Échec de l’importation : {0}", "Caricamento non riuscito: {0}", "Falha no carregamento: {0}",
                "업로드 실패: {0}", "アップロードに失敗しました：{0}", "上传失败：{0}", "上傳失敗：{0}", "Tải lên thất bại: {0}"),
            ["message.share.saved"] = L(
                "Kakao share settings were saved.", "Se guardó la configuración para compartir en Kakao.", "Les paramètres de partage Kakao ont été enregistrés.", "Le impostazioni di condivisione Kakao sono state salvate.", "As definições de partilha no Kakao foram guardadas.",
                "카카오 공유 설정을 저장했습니다.", "Kakao共有設定を保存しました。", "Kakao 分享设置已保存。", "Kakao 分享設定已儲存。", "Đã lưu cài đặt chia sẻ Kakao."),
            ["error.password.min"] = L(
                "The agent connection password must be at least 8 characters.", "La contraseña de conexión del agente debe tener al menos 8 caracteres.", "Le mot de passe de connexion de l’agent doit comporter au moins 8 caractères.", "La password di connessione dell’agente deve contenere almeno 8 caratteri.", "A palavra-passe de conexão do agente deve ter pelo menos 8 caracteres.",
                "에이전트 연결 비밀번호는 8자 이상이어야 합니다.", "エージェント接続パスワードは8文字以上にしてください。", "代理程序连接密码必须至少为 8 个字符。", "代理程式連線密碼必須至少 8 個字元。", "Mật khẩu kết nối tác nhân phải có ít nhất 8 ký tự."),
            ["error.password.mismatch"] = L(
                "The password confirmation does not match.", "La confirmación de la contraseña no coincide.", "La confirmation du mot de passe ne correspond pas.", "La conferma della password non corrisponde.", "A confirmação da palavra-passe não corresponde.",
                "비밀번호 확인이 일치하지 않습니다.", "確認用パスワードが一致しません。", "两次输入的密码不一致。", "兩次輸入的密碼不一致。", "Mật khẩu xác nhận không khớp."),
            ["error.user.not.found"] = L(
                "The user could not be found.", "No se encontró al usuario.", "L’utilisateur est introuvable.", "Utente non trovato.", "Não foi possível encontrar o utilizador.",
                "사용자를 찾을 수 없습니다.", "ユーザーが見つかりません。", "找不到用户。", "找不到使用者。", "Không tìm thấy người dùng."),
            ["error.password.save"] = L(
                "The agent connection password could not be saved.", "No se pudo guardar la contraseña de conexión del agente.", "Impossible d’enregistrer le mot de passe de connexion de l’agent.", "Impossibile salvare la password di connessione dell’agente.", "Não foi possível guardar a palavra-passe de conexão do agente.",
                "에이전트 연결 비밀번호를 저장하지 못했습니다.", "エージェント接続パスワードを保存できませんでした。", "无法保存代理程序连接密码。", "無法儲存代理程式連線密碼。", "Không thể lưu mật khẩu kết nối tác nhân."),
            ["message.password.saved"] = L(
                "The agent connection password was saved.", "Se guardó la contraseña de conexión del agente.", "Le mot de passe de connexion de l’agent a été enregistré.", "La password di connessione dell’agente è stata salvata.", "A palavra-passe de conexão do agente foi guardada.",
                "에이전트 연결 비밀번호를 저장했습니다.", "エージェント接続パスワードを保存しました。", "代理程序连接密码已保存。", "代理程式連線密碼已儲存。", "Đã lưu mật khẩu kết nối tác nhân."),
            ["confirm.camera.delete"] = L(
                "Delete the camera “{0}”?", "¿Eliminar la cámara «{0}»?", "Supprimer la caméra « {0} » ?", "Eliminare la telecamera “{0}”?", "Eliminar a câmara “{0}”?",
                "'{0}' 카메라를 삭제하시겠습니까?", "カメラ「{0}」を削除しますか？", "要删除摄像头“{0}”吗？", "要刪除攝影機「{0}」嗎？", "Xóa camera “{0}”?"),
            ["message.camera.deleted"] = L(
                "“{0}” was deleted.", "Se eliminó «{0}».", "« {0} » a été supprimée.", "“{0}” è stata eliminata.", "“{0}” foi eliminada.",
                "'{0}' 삭제됨.", "「{0}」を削除しました。", "已删除“{0}”。", "已刪除「{0}」。", "Đã xóa “{0}”.")
        };

    public static string Get(string? language, string key)
    {
        if (!Texts.TryGetValue(key, out var values))
        {
            return key;
        }

        return values[LanguageIndex(language)];
    }

    public static string Format(string? language, string key, params object?[] args)
    {
        var value = Get(language, key);
        try
        {
            return string.Format(Culture(language), value, args);
        }
        catch (FormatException)
        {
            return value;
        }
    }

    private static string[] L(params string[] values)
    {
        if (values.Length != Codes.Length)
        {
            throw new InvalidOperationException($"A camera translation must contain exactly {Codes.Length} values.");
        }

        return values;
    }

    private static int LanguageIndex(string? language)
    {
        var normalized = VmsLocalization.NormalizeLanguageCode(language);
        var index = Array.FindIndex(Codes, code => string.Equals(code, normalized, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : Array.IndexOf(Codes, "ko");
    }

    private static CultureInfo Culture(string? language)
    {
        var cultureName = VmsLocalization.NormalizeLanguageCode(language) switch
        {
            "en" => "en-US",
            "es" => "es-ES",
            "fr" => "fr-FR",
            "it" => "it-IT",
            "pt" => "pt-PT",
            "ja" => "ja-JP",
            "zh-hans" => "zh-CN",
            "zh-hant" => "zh-HK",
            "vi" => "vi-VN",
            _ => "ko-KR"
        };
        return CultureInfo.GetCultureInfo(cultureName);
    }
}
