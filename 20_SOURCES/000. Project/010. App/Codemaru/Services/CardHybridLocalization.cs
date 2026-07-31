namespace Codemaru.Services;

public static class CardHybridLocalization
{
    public sealed record DialogCopy(
        string MigrationTitle, string MigrationQuestion, string MigrationWarning, string SaveAccount, string Discard,
        string SaveOptionsTitle, string SaveOptionsDescription, string PublishPage, string Save, string Cancel,
        string SignInAction, string ImportedFrontAlt, string NoBackImage, string ImportedBackAlt);
    public sealed record LandingCopy(string NotFound, string EmailAction, string PhoneAction, string Website, string Company, string Phone, string Address);

    private static readonly string[] Keys =
    [
        "cardInfo", "notSaved", "frontBrand", "font", "size", "frontTagline",
        "category", "email", "backBrand", "backTagline", "name", "role", "phone",
        "address", "handle", "width", "height", "accent", "backImage",
        "removeBackground", "removeImage", "shortBio", "landingDescription",
        "vcardNote", "internalMemo", "card", "landingHtml", "frontSvg", "backSvg",
        "front", "back", "landingCreate", "saveServer", "saveHtml", "mobileVcard",
        "windowsVcard", "themeLight", "themeDark", "themeBlue", "themeGreen",
        "includePhone", "includeAddress", "exportHtml", "history", "reset",
        "guest", "guestHint", "importCard", "emptyHistory", "edit", "delete",
        "refreshQr", "saveHistory", "savedHistory", "editor", "preview", "historyLabel"
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Texts =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["ko"] = Map([
                "명함 정보", "아직 저장 전", "앞면 브랜드", "폰트", "크기", "앞면 태그라인",
                "분야", "이메일", "뒷면 브랜드", "뒷면 태그라인", "이름", "직책", "전화",
                "주소", "영문 주소 이름", "가로(mm)", "세로(mm)", "강조색", "뒷면 이미지",
                "이미지 배경 제거", "이미지 제거", "짧은 소개", "랜딩 설명",
                "vCard 노트", "내부 메모", "명함", "랜딩 HTML", "앞면 SVG", "뒷면 SVG",
                "앞면", "뒷면", "랜딩 페이지 만들기", "서버 index.html 저장", "HTML 저장", "모바일 vCard",
                "Windows 연락처 vCard", "밝은", "어두운", "블루", "그린",
                "vCard에 전화 포함", "vCard에 주소 포함", "내보낼 HTML", "이력", "초기화",
                "게스트 사용 중", "로그인하면 명함이 계정에 저장됩니다.", "외부 명함 가져오기", "저장된 명함 이력이 없습니다.", "편집", "삭제",
                "QR 갱신", "이력 저장", "이력 저장", "명함 편집", "명함 및 랜딩 미리보기", "명함 이력"
            ]),
            ["en"] = Map([
                "Card details", "Not saved yet", "Front brand", "Font", "Size", "Front tagline",
                "Category", "Email", "Back brand", "Back tagline", "Name", "Role", "Phone",
                "Address", "Public URL name", "Width (mm)", "Height (mm)", "Accent color", "Back image",
                "Remove image background", "Remove image", "Short bio", "Landing description",
                "vCard note", "Internal memo", "Card", "Landing HTML", "Front SVG", "Back SVG",
                "Front", "Back", "Build landing page", "Save index.html to server", "Save HTML", "Mobile vCard",
                "Windows contact vCard", "Light", "Dark", "Blue", "Green",
                "Include phone in vCard", "Include address in vCard", "Exported HTML", "History", "Reset",
                "Using as guest", "Sign in to save cards to your account.", "Import external card", "No saved card history.", "Edit", "Delete",
                "Refresh QR", "Save history", "Save history", "Card editor", "Card and landing preview", "Card history"
            ]),
            ["es"] = Map([
                "Datos de tarjeta", "Aún sin guardar", "Marca frontal", "Fuente", "Tamaño", "Lema frontal",
                "Categoría", "Correo", "Marca trasera", "Lema trasero", "Nombre", "Cargo", "Teléfono",
                "Dirección", "Nombre de URL pública", "Ancho (mm)", "Alto (mm)", "Color de acento", "Imagen trasera",
                "Quitar fondo", "Quitar imagen", "Biografía breve", "Descripción de portada",
                "Nota vCard", "Nota interna", "Tarjeta", "HTML de portada", "SVG frontal", "SVG trasero",
                "Frente", "Reverso", "Crear portada", "Guardar index.html", "Guardar HTML", "vCard móvil",
                "vCard para Windows", "Claro", "Oscuro", "Azul", "Verde",
                "Incluir teléfono", "Incluir dirección", "HTML exportado", "Historial", "Restablecer",
                "Uso como invitado", "Inicia sesión para guardar tarjetas.", "Importar tarjeta", "No hay historial guardado.", "Editar", "Eliminar",
                "Actualizar QR", "Guardar historial", "Guardar historial", "Editor de tarjetas", "Vista previa", "Historial de tarjetas"
            ]),
            ["fr"] = Map([
                "Informations de carte", "Pas encore enregistrée", "Marque recto", "Police", "Taille", "Slogan recto",
                "Catégorie", "E-mail", "Marque verso", "Slogan verso", "Nom", "Fonction", "Téléphone",
                "Adresse", "Nom d’URL publique", "Largeur (mm)", "Hauteur (mm)", "Couleur d’accent", "Image verso",
                "Supprimer le fond", "Supprimer l’image", "Présentation courte", "Description de la page",
                "Note vCard", "Note interne", "Carte", "HTML de page", "SVG recto", "SVG verso",
                "Recto", "Verso", "Créer la page", "Enregistrer index.html", "Enregistrer HTML", "vCard mobile",
                "vCard Windows", "Clair", "Sombre", "Bleu", "Vert",
                "Inclure le téléphone", "Inclure l’adresse", "HTML exporté", "Historique", "Réinitialiser",
                "Mode invité", "Connectez-vous pour enregistrer vos cartes.", "Importer une carte", "Aucun historique.", "Modifier", "Supprimer",
                "Actualiser le QR", "Enregistrer", "Enregistrer", "Éditeur de carte", "Aperçu carte et page", "Historique des cartes"
            ]),
            ["it"] = Map([
                "Dati biglietto", "Non ancora salvato", "Marchio fronte", "Font", "Dimensione", "Slogan fronte",
                "Categoria", "Email", "Marchio retro", "Slogan retro", "Nome", "Ruolo", "Telefono",
                "Indirizzo", "Nome URL pubblico", "Larghezza (mm)", "Altezza (mm)", "Colore principale", "Immagine retro",
                "Rimuovi sfondo", "Rimuovi immagine", "Bio breve", "Descrizione pagina",
                "Nota vCard", "Nota interna", "Biglietto", "HTML pagina", "SVG fronte", "SVG retro",
                "Fronte", "Retro", "Crea pagina", "Salva index.html", "Salva HTML", "vCard mobile",
                "vCard Windows", "Chiaro", "Scuro", "Blu", "Verde",
                "Includi telefono", "Includi indirizzo", "HTML esportato", "Cronologia", "Ripristina",
                "Modalità ospite", "Accedi per salvare i biglietti.", "Importa biglietto", "Nessuna cronologia.", "Modifica", "Elimina",
                "Aggiorna QR", "Salva cronologia", "Salva cronologia", "Editor biglietto", "Anteprima", "Cronologia biglietti"
            ]),
            ["pt"] = Map([
                "Dados do cartão", "Ainda não salvo", "Marca frontal", "Fonte", "Tamanho", "Slogan frontal",
                "Categoria", "Email", "Marca traseira", "Slogan traseiro", "Nome", "Cargo", "Telefone",
                "Endereço", "Nome da URL pública", "Largura (mm)", "Altura (mm)", "Cor de destaque", "Imagem traseira",
                "Remover fundo", "Remover imagem", "Biografia curta", "Descrição da página",
                "Nota vCard", "Nota interna", "Cartão", "HTML da página", "SVG frontal", "SVG traseiro",
                "Frente", "Verso", "Criar página", "Salvar index.html", "Salvar HTML", "vCard móvel",
                "vCard Windows", "Claro", "Escuro", "Azul", "Verde",
                "Incluir telefone", "Incluir endereço", "HTML exportado", "Histórico", "Redefinir",
                "Modo visitante", "Entre para salvar cartões.", "Importar cartão", "Nenhum histórico salvo.", "Editar", "Excluir",
                "Atualizar QR", "Salvar histórico", "Salvar histórico", "Editor de cartão", "Pré-visualização", "Histórico de cartões"
            ]),
            ["vi"] = Map([
                "Thông tin danh thiếp", "Chưa lưu", "Thương hiệu mặt trước", "Phông chữ", "Kích thước", "Khẩu hiệu mặt trước",
                "Lĩnh vực", "Email", "Thương hiệu mặt sau", "Khẩu hiệu mặt sau", "Tên", "Chức danh", "Điện thoại",
                "Địa chỉ", "Tên URL công khai", "Chiều rộng (mm)", "Chiều cao (mm)", "Màu nhấn", "Ảnh mặt sau",
                "Xóa nền ảnh", "Xóa ảnh", "Giới thiệu ngắn", "Mô tả trang",
                "Ghi chú vCard", "Ghi chú nội bộ", "Danh thiếp", "HTML trang", "SVG mặt trước", "SVG mặt sau",
                "Mặt trước", "Mặt sau", "Tạo trang giới thiệu", "Lưu index.html", "Lưu HTML", "vCard di động",
                "vCard Windows", "Sáng", "Tối", "Xanh dương", "Xanh lá",
                "Gồm số điện thoại", "Gồm địa chỉ", "HTML xuất", "Lịch sử", "Đặt lại",
                "Đang dùng với tư cách khách", "Đăng nhập để lưu danh thiếp.", "Nhập danh thiếp", "Chưa có lịch sử.", "Sửa", "Xóa",
                "Cập nhật QR", "Lưu lịch sử", "Lưu lịch sử", "Trình sửa danh thiếp", "Xem trước", "Lịch sử danh thiếp"
            ]),
            ["ja"] = Map([
                "名刺情報", "未保存", "表面ブランド", "フォント", "サイズ", "表面タグライン",
                "分野", "メール", "裏面ブランド", "裏面タグライン", "氏名", "役職", "電話",
                "住所", "公開URL名", "幅 (mm)", "高さ (mm)", "アクセント色", "裏面画像",
                "画像背景を削除", "画像を削除", "短い紹介", "ランディング説明",
                "vCardメモ", "内部メモ", "名刺", "ランディングHTML", "表面SVG", "裏面SVG",
                "表面", "裏面", "ランディングページ作成", "index.htmlを保存", "HTMLを保存", "モバイルvCard",
                "Windows連絡先vCard", "ライト", "ダーク", "ブルー", "グリーン",
                "電話番号を含める", "住所を含める", "出力HTML", "履歴", "初期化",
                "ゲストとして使用中", "ログインすると名刺を保存できます。", "外部名刺を取り込む", "保存履歴はありません。", "編集", "削除",
                "QRを更新", "履歴を保存", "履歴を保存", "名刺エディター", "名刺とページのプレビュー", "名刺履歴"
            ]),
            ["zh-hans"] = Map([
                "名片信息", "尚未保存", "正面品牌", "字体", "大小", "正面标语",
                "领域", "电子邮件", "背面品牌", "背面标语", "姓名", "职位", "电话",
                "地址", "公开网址名称", "宽度 (mm)", "高度 (mm)", "强调色", "背面图片",
                "移除图片背景", "移除图片", "简短介绍", "落地页说明",
                "vCard 备注", "内部备注", "名片", "落地页 HTML", "正面 SVG", "背面 SVG",
                "正面", "背面", "创建落地页", "保存 index.html", "保存 HTML", "移动 vCard",
                "Windows 联系人 vCard", "浅色", "深色", "蓝色", "绿色",
                "包含电话", "包含地址", "导出 HTML", "历史记录", "重置",
                "访客模式", "登录后可将名片保存到账户。", "导入外部名片", "暂无保存记录。", "编辑", "删除",
                "更新 QR", "保存记录", "保存记录", "名片编辑器", "名片与落地页预览", "名片历史"
            ]),
            ["zh-hant"] = Map([
                "名片資訊", "尚未儲存", "正面品牌", "字型", "大小", "正面標語",
                "領域", "電子郵件", "背面品牌", "背面標語", "姓名", "職稱", "電話",
                "地址", "公開網址名稱", "寬度 (mm)", "高度 (mm)", "強調色", "背面圖片",
                "移除圖片背景", "移除圖片", "簡短介紹", "到達頁說明",
                "vCard 備註", "內部備註", "名片", "到達頁 HTML", "正面 SVG", "背面 SVG",
                "正面", "背面", "建立到達頁", "儲存 index.html", "儲存 HTML", "行動 vCard",
                "Windows 聯絡人 vCard", "淺色", "深色", "藍色", "綠色",
                "包含電話", "包含地址", "匯出 HTML", "歷史紀錄", "重設",
                "訪客模式", "登入後可將名片儲存至帳戶。", "匯入外部名片", "尚無儲存紀錄。", "編輯", "刪除",
                "更新 QR", "儲存紀錄", "儲存紀錄", "名片編輯器", "名片與到達頁預覽", "名片歷史"
            ])
        };

    public static string Get(string language, string key)
    {
        var selected = Texts.TryGetValue(language, out var localized) ? localized : Texts["en"];
        return selected.TryGetValue(key, out var value) ? value : Texts["en"][key];
    }

    public static DialogCopy Dialog(string language) => language switch
    {
        "ko" => new("로그인 전에 편집하시던 명함이 있어요", "이 명함을 계정에 저장할까요?", "계정에 이미 저장된 명함이 있으면 덮어씁니다.", "계정에 저장", "폐기", "명함 저장 옵션", "이력과 함께 공개 랜딩 페이지 및 vCard 항목을 설정합니다.", "공개용 index.html 생성", "저장", "취소", "로그인하거나 새 계정을 만드세요.", "가져온 명함 앞면", "뒷면 이미지 없음", "가져온 명함 뒷면"),
        "es" => new("Tienes una tarjeta editada antes de iniciar sesión", "¿Quieres guardarla en tu cuenta?", "Se reemplazará la tarjeta ya guardada en la cuenta.", "Guardar en la cuenta", "Descartar", "Opciones de guardado", "Configura la página pública y los campos de vCard junto con el historial.", "Crear index.html público", "Guardar", "Cancelar", "Inicia sesión o crea una cuenta.", "Frente de tarjeta importada", "Sin imagen trasera", "Reverso de tarjeta importada"),
        "fr" => new("Une carte a été modifiée avant la connexion", "Voulez-vous l’enregistrer dans votre compte ?", "La carte déjà enregistrée dans le compte sera remplacée.", "Enregistrer dans le compte", "Ignorer", "Options d’enregistrement", "Configurez la page publique et les champs vCard avec l’historique.", "Créer un index.html public", "Enregistrer", "Annuler", "Connectez-vous ou créez un compte.", "Recto de la carte importée", "Aucune image verso", "Verso de la carte importée"),
        "it" => new("Hai modificato un biglietto prima dell’accesso", "Vuoi salvarlo nel tuo account?", "Il biglietto già salvato nell’account verrà sostituito.", "Salva nell’account", "Scarta", "Opzioni di salvataggio", "Configura la pagina pubblica e i campi vCard insieme alla cronologia.", "Crea index.html pubblico", "Salva", "Annulla", "Accedi o crea un account.", "Fronte del biglietto importato", "Nessuna immagine posteriore", "Retro del biglietto importato"),
        "pt" => new("Você editou um cartão antes de entrar", "Deseja salvá-lo em sua conta?", "O cartão já salvo na conta será substituído.", "Salvar na conta", "Descartar", "Opções de salvamento", "Configure a página pública e os campos do vCard junto com o histórico.", "Criar index.html público", "Salvar", "Cancelar", "Entre ou crie uma conta.", "Frente do cartão importado", "Sem imagem traseira", "Verso do cartão importado"),
        "vi" => new("Bạn đã sửa danh thiếp trước khi đăng nhập", "Bạn có muốn lưu vào tài khoản không?", "Danh thiếp đang lưu trong tài khoản sẽ bị thay thế.", "Lưu vào tài khoản", "Hủy bỏ", "Tùy chọn lưu", "Thiết lập trang công khai và các trường vCard cùng với lịch sử.", "Tạo index.html công khai", "Lưu", "Hủy", "Đăng nhập hoặc tạo tài khoản.", "Mặt trước danh thiếp đã nhập", "Không có ảnh mặt sau", "Mặt sau danh thiếp đã nhập"),
        "ja" => new("ログイン前に編集した名刺があります", "この名刺をアカウントに保存しますか？", "アカウントに保存済みの名刺は上書きされます。", "アカウントに保存", "破棄", "名刺の保存オプション", "履歴とともに公開ページとvCard項目を設定します。", "公開index.htmlを作成", "保存", "キャンセル", "ログインまたは新規登録してください。", "取り込んだ名刺の表面", "裏面画像なし", "取り込んだ名刺の裏面"),
        "zh-hans" => new("登录前编辑的名片尚未保存", "要将此名片保存到账户吗？", "账户中已有的名片将被覆盖。", "保存到账户", "丢弃", "名片保存选项", "同时设置公开页面、vCard 字段和历史记录。", "创建公开 index.html", "保存", "取消", "请登录或创建账户。", "导入名片正面", "没有背面图片", "导入名片背面"),
        "zh-hant" => new("登入前編輯的名片尚未儲存", "要將此名片儲存至帳戶嗎？", "帳戶中已有的名片將被覆寫。", "儲存至帳戶", "捨棄", "名片儲存選項", "同時設定公開頁面、vCard 欄位與歷史紀錄。", "建立公開 index.html", "儲存", "取消", "請登入或建立帳戶。", "匯入名片正面", "沒有背面圖片", "匯入名片背面"),
        _ => new("You edited a card before signing in", "Save this card to your account?", "Any card already saved to the account will be replaced.", "Save to account", "Discard", "Card save options", "Configure the public page and vCard fields together with history.", "Create public index.html", "Save", "Cancel", "Sign in or create an account.", "Imported card front", "No back image", "Imported card back")
    };

    public static LandingCopy Landing(string language) => language switch
    {
        "ko" => new("명함을 찾을 수 없습니다.", "메일 보내기", "전화하기", "웹사이트", "회사", "전화", "주소"),
        "es" => new("No se encontró la tarjeta.", "Enviar correo", "Llamar", "Sitio web", "Empresa", "Teléfono", "Dirección"),
        "fr" => new("Carte introuvable.", "Envoyer un e-mail", "Appeler", "Site web", "Entreprise", "Téléphone", "Adresse"),
        "it" => new("Biglietto non trovato.", "Invia email", "Chiama", "Sito web", "Azienda", "Telefono", "Indirizzo"),
        "pt" => new("Cartão não encontrado.", "Enviar email", "Ligar", "Site", "Empresa", "Telefone", "Endereço"),
        "vi" => new("Không tìm thấy danh thiếp.", "Gửi email", "Gọi điện", "Trang web", "Công ty", "Điện thoại", "Địa chỉ"),
        "ja" => new("名刺が見つかりません。", "メールを送る", "電話する", "ウェブサイト", "会社", "電話", "住所"),
        "zh-hans" => new("找不到名片。", "发送邮件", "拨打电话", "网站", "公司", "电话", "地址"),
        "zh-hant" => new("找不到名片。", "傳送郵件", "撥打電話", "網站", "公司", "電話", "地址"),
        _ => new("Card not found.", "Send email", "Call", "Website", "Company", "Phone", "Address")
    };

    private static IReadOnlyDictionary<string, string> Map(string[] values)
    {
        if (values.Length != Keys.Length)
        {
            throw new InvalidOperationException(
                $"CardHybrid localization contains {values.Length} values; expected {Keys.Length}.");
        }

        return Keys.Zip(values).ToDictionary(pair => pair.First, pair => pair.Second);
    }
}
