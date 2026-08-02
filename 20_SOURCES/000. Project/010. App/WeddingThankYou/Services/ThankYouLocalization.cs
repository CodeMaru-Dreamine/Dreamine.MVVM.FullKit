namespace WeddingThankYou.Services;

public sealed class ThankYouLocalization
{
    private readonly WeddingPlatform.Services.WeddingLocalization _shared = new();
    public sealed record LanguageOption(string Code, string Region, string NativeName, string HtmlLanguage);

    public static readonly LanguageOption[] Languages =
    [
        new("en", "US", "English", "en"), new("es", "ES", "Español", "es"),
        new("fr", "FR", "Français", "fr"), new("it", "IT", "Italiano", "it"),
        new("pt", "BR", "Português", "pt-BR"), new("ko", "KR", "한국어", "ko"),
        new("ja", "JP", "日本語", "ja"), new("zh-hans", "CN", "简体中文", "zh-Hans"),
        new("zh-hant", "TW", "繁體中文", "zh-Hant"), new("vi", "VN", "Tiếng Việt", "vi")
    ];

    private static readonly string[] Codes = Languages.Select(x => x.Code).ToArray();
    private sealed record WesternTranslation(string Spanish, string French, string Italian, string Portuguese);

    // Public thank-you copy is authored for every language exposed by the selector.
    // Keeping the four western-language translations beside the English source lets
    // the compact Localized(...) calls below remain readable without an English fallback.
    private static readonly Dictionary<string, WesternTranslation> WesternPublicTexts = new(StringComparer.Ordinal)
    {
        ["{0} — Wedding thank-you"] = new("{0} — Agradecimiento de boda", "{0} — Remerciement de mariage", "{0} — Ringraziamento di nozze", "{0} — Agradecimento de casamento"),
        ["This thank-you card could not be found."] = new("No se pudo encontrar esta tarjeta de agradecimiento.", "Cette carte de remerciement est introuvable.", "Questa carta di ringraziamento non è stata trovata.", "Este cartão de agradecimento não foi encontrado."),
        ["Go home"] = new("Ir al inicio", "Retour à l’accueil", "Vai alla home", "Ir para o início"),
        ["Play music"] = new("Reproducir música", "Lire la musique", "Riproduci musica", "Reproduzir música"),
        ["Pause music"] = new("Pausar música", "Mettre la musique en pause", "Metti in pausa la musica", "Pausar música"),
        ["Playing"] = new("Reproduciendo", "Lecture en cours", "In riproduzione", "Reproduzindo"),
        ["Thank-you card menu"] = new("Menú de la tarjeta de agradecimiento", "Menu de la carte de remerciement", "Menu della carta di ringraziamento", "Menu do cartão de agradecimento"),
        ["Thank-you card menu (mobile)"] = new("Menú de la tarjeta de agradecimiento (móvil)", "Menu de la carte de remerciement (mobile)", "Menu della carta di ringraziamento (mobile)", "Menu do cartão de agradecimento (celular)"),
        ["Open menu"] = new("Abrir menú", "Ouvrir le menu", "Apri il menu", "Abrir menu"),
        ["Close menu"] = new("Cerrar menú", "Fermer le menu", "Chiudi il menu", "Fechar menu"),
        ["{0}'s wedding thank-you card"] = new("Tarjeta de agradecimiento de boda de {0}", "Carte de remerciement de mariage de {0}", "Carta di ringraziamento di nozze di {0}", "Cartão de agradecimento de casamento de {0}"),
        ["Cover"] = new("Portada", "Couverture", "Copertina", "Capa"),
        ["Use the buttons or the left and right arrow keys. You can also swipe across an empty area to turn the card."] = new("Usa los botones o las teclas de flecha izquierda y derecha. También puedes deslizar sobre una zona vacía para pasar la tarjeta.", "Utilisez les boutons ou les touches fléchées gauche et droite. Vous pouvez aussi balayer une zone vide pour tourner la carte.", "Usa i pulsanti o i tasti freccia sinistra e destra. Puoi anche scorrere su un’area vuota per voltare la carta.", "Use os botões ou as teclas de seta para a esquerda e para a direita. Você também pode deslizar em uma área vazia para virar o cartão."),
        ["{0}'s wedding thank-you photo book"] = new("Álbum de agradecimiento de boda de {0}", "Livre photo de remerciement de mariage de {0}", "Fotolibro di ringraziamento di nozze di {0}", "Livro de fotos de agradecimento de casamento de {0}"),
        ["Our thank-you memories"] = new("Nuestros recuerdos de agradecimiento", "Nos souvenirs de gratitude", "I nostri ricordi di gratitudine", "Nossas memórias de agradecimento"),
        ["Photo book"] = new("Álbum de fotos", "Livre photo", "Fotolibro", "Livro de fotos"),
        ["Please choose a photo."] = new("Elige una foto.", "Veuillez choisir une photo.", "Scegli una foto.", "Escolha uma foto."),
        ["Featured photo of {0}"] = new("Foto destacada de {0}", "Photo principale de {0}", "Foto principale di {0}", "Foto em destaque de {0}"),
        ["{0}, thank you for sharing our special day."] = new("{0}, gracias por acompañarnos en nuestro día especial.", "{0}, merci d’avoir partagé cette journée si spéciale avec nous.", "{0}, grazie per aver condiviso con noi questo giorno speciale.", "{0}, agradecemos por compartilhar conosco este dia especial."),
        ["View invitation"] = new("Ver invitación", "Voir l’invitation", "Vedi l’invito", "Ver convite"),
        ["Admin"] = new("Administración", "Administration", "Amministrazione", "Administração"),
        ["Home"] = new("Inicio", "Accueil", "Home", "Início"),
        ["Thank-you message"] = new("Mensaje de agradecimiento", "Message de remerciement", "Messaggio di ringraziamento", "Mensagem de agradecimento"),
        ["Gallery"] = new("Galería", "Galerie", "Galleria", "Galeria"),
        ["Our video"] = new("Nuestro video", "Notre vidéo", "Il nostro video", "Nosso vídeo"),
        ["Thank-you guestbook"] = new("Libro de agradecimientos", "Livre d’or de remerciement", "Guestbook dei ringraziamenti", "Livro de agradecimentos"),
        ["Our story"] = new("Nuestra historia", "Notre histoire", "La nostra storia", "Nossa história"),
        ["Card"] = new("Tarjeta", "Carte", "Carta", "Cartão"),
        ["Our photos"] = new("Nuestras fotos", "Nos photos", "Le nostre foto", "Nossas fotos"),
        ["Moments of gratitude"] = new("Momentos de gratitud", "Moments de gratitude", "Momenti di gratitudine", "Momentos de gratidão"),
        ["▶ Autoplay"] = new("▶ Reproducción automática", "▶ Lecture automatique", "▶ Riproduzione automatica", "▶ Reprodução automática"),
        ["⏸ Stop"] = new("⏸ Detener", "⏸ Arrêter", "⏸ Ferma", "⏸ Parar"),
        ["Latest {0} / all {1}"] = new("Últimas {0} / total {1}", "{0} dernières / {1} au total", "Ultime {0} / totale {1}", "Últimas {0} / total {1}"),
        ["Showing {0} / {1}"] = new("Mostrando {0} / {1}", "Affichage de {0} / {1}", "Visualizzate {0} / {1}", "Exibindo {0} / {1}"),
        ["No photos have been added yet."] = new("Aún no se han añadido fotos.", "Aucune photo n’a encore été ajoutée.", "Non sono ancora state aggiunte foto.", "Nenhuma foto foi adicionada ainda."),
        ["Photo {0}"] = new("Foto {0}", "Photo {0}", "Foto {0}", "Foto {0}"),
        ["Open photo {0}"] = new("Abrir foto {0}", "Ouvrir la photo {0}", "Apri la foto {0}", "Abrir foto {0}"),
        ["View more photos (+{0})"] = new("Ver más fotos (+{0})", "Voir plus de photos (+{0})", "Vedi altre foto (+{0})", "Ver mais fotos (+{0})"),
        ["Our moments together"] = new("Nuestros momentos juntos", "Nos moments ensemble", "I nostri momenti insieme", "Nossos momentos juntos"),
        ["{0}, we celebrated our wedding at {1} surrounded by warm wishes."] = new("{0}, celebramos nuestra boda en {1} rodeados de buenos deseos.", "{0}, nous avons célébré notre mariage à {1}, entourés de vœux chaleureux.", "{0}, abbiamo celebrato il nostro matrimonio a {1}, circondati da affettuosi auguri.", "{0}, celebramos nosso casamento em {1}, cercados de votos carinhosos."),
        ["Our story begins here."] = new("Nuestra historia comienza aquí.", "Notre histoire commence ici.", "La nostra storia comincia qui.", "Nossa história começa aqui."),
        ["We share the memories we have gathered along the way."] = new("Compartimos los recuerdos que hemos reunido en el camino.", "Nous partageons les souvenirs recueillis au fil du temps.", "Condividiamo i ricordi raccolti lungo il cammino.", "Compartilhamos as lembranças que reunimos ao longo do caminho."),
        ["We share our photos and stories from the moments filled with your blessings."] = new("Compartimos fotos e historias de los momentos llenos de sus buenos deseos.", "Nous partageons les photos et les récits de ces moments remplis de vos vœux.", "Condividiamo foto e racconti dei momenti colmi dei vostri auguri.", "Compartilhamos fotos e histórias dos momentos repletos de seus votos."),
        ["With lasting gratitude to everyone who sent us their warm wishes."] = new("Con gratitud eterna a todos los que nos enviaron sus mejores deseos.", "Avec une gratitude durable envers toutes les personnes qui nous ont adressé leurs vœux chaleureux.", "Con gratitudine duratura verso tutti coloro che ci hanno rivolto i loro auguri.", "Com gratidão duradoura a todos que nos enviaram votos carinhosos."),
        ["Play video {0}"] = new("Reproducir video {0}", "Lire la vidéo {0}", "Riproduci il video {0}", "Reproduzir vídeo {0}"),
        ["Name"] = new("Nombre", "Nom", "Nome", "Nome"),
        ["Contact (optional)"] = new("Contacto (opcional)", "Contact (facultatif)", "Contatto (facoltativo)", "Contato (opcional)"),
        ["Leave a thank-you message"] = new("Deja un mensaje de agradecimiento", "Laissez un message de remerciement", "Lascia un messaggio di ringraziamento", "Deixe uma mensagem de agradecimento"),
        ["Post"] = new("Publicar", "Publier", "Pubblica", "Publicar"),
        ["Refresh"] = new("Actualizar", "Actualiser", "Aggiorna", "Atualizar"),
        ["Close"] = new("Cerrar", "Fermer", "Chiudi", "Fechar"),
        ["Previous photo"] = new("Foto anterior", "Photo précédente", "Foto precedente", "Foto anterior"),
        ["Next photo"] = new("Foto siguiente", "Photo suivante", "Foto successiva", "Próxima foto"),
        ["Scroll"] = new("Desplazar", "Faire défiler", "Scorri", "Rolar"),
        ["Thank you"] = new("Gracias", "Merci", "Grazie", "Obrigado"),
        ["Previous video"] = new("Video anterior", "Vidéo précédente", "Video precedente", "Vídeo anterior"),
        ["Next video"] = new("Video siguiente", "Vidéo suivante", "Video successivo", "Próximo vídeo"),
        ["Page {0}"] = new("Página {0}", "Page {0}", "Pagina {0}", "Página {0}"),
        ["Open the photo for {0}"] = new("Abrir la foto de {0}", "Ouvrir la photo de {0}", "Apri la foto di {0}", "Abrir a foto de {0}"),
        ["Photo viewer"] = new("Visor de fotos", "Visionneuse de photos", "Visualizzatore foto", "Visualizador de fotos"),
        ["Unable to load the guestbook."] = new("No se pudo cargar el libro de visitas.", "Impossible de charger le livre d’or.", "Impossibile caricare il guestbook.", "Não foi possível carregar o livro de visitas."),
        ["Please enter your name."] = new("Introduce tu nombre.", "Veuillez saisir votre nom.", "Inserisci il tuo nome.", "Digite seu nome."),
        ["Please enter a message."] = new("Escribe un mensaje.", "Veuillez saisir un message.", "Inserisci un messaggio.", "Digite uma mensagem."),
        ["Your message has been posted."] = new("Tu mensaje se ha publicado.", "Votre message a été publié.", "Il tuo messaggio è stato pubblicato.", "Sua mensagem foi publicada."),
        ["Unable to post your message."] = new("No se pudo publicar tu mensaje.", "Impossible de publier votre message.", "Impossibile pubblicare il tuo messaggio.", "Não foi possível publicar sua mensagem."),
        ["The guestbook has been refreshed."] = new("El libro de visitas se ha actualizado.", "Le livre d’or a été actualisé.", "Il guestbook è stato aggiornato.", "O livro de visitas foi atualizado."),
        ["Unable to refresh the guestbook."] = new("No se pudo actualizar el libro de visitas.", "Impossible d’actualiser le livre d’or.", "Impossibile aggiornare il guestbook.", "Não foi possível atualizar o livro de visitas."),
        ["💖 Wishing your future together is filled with love and happiness. ✨"] = new("💖 Que su futuro juntos esté lleno de amor y felicidad. ✨", "💖 Que votre avenir à deux soit rempli d’amour et de bonheur. ✨", "💖 Che il vostro futuro insieme sia pieno di amore e felicità. ✨", "💖 Que o futuro de vocês seja repleto de amor e felicidade. ✨"),
        ["🎉 Heartfelt congratulations on your wedding! May you always be together. 💍"] = new("🎉 ¡Felicidades de corazón por su boda! Que permanezcan siempre juntos. 💍", "🎉 Toutes nos félicitations pour votre mariage ! Puissiez-vous rester toujours unis. 💍", "🎉 Congratulazioni di cuore per il vostro matrimonio! Che possiate restare sempre insieme. 💍", "🎉 Parabéns de coração pelo casamento! Que vocês estejam sempre juntos. 💍"),
        ["🌸 May today's promise lead to a lifetime of joy. 💕"] = new("🌸 Que la promesa de hoy les brinde toda una vida de alegría. 💕", "🌸 Que la promesse d’aujourd’hui vous apporte une vie entière de joie. 💕", "🌸 Che la promessa di oggi vi accompagni verso una vita di gioia. 💕", "🌸 Que a promessa de hoje conduza a uma vida inteira de alegria. 💕"),
        ["🕊️ May you always cherish and respect each other as you build a happy family. 🙏"] = new("🕊️ Que siempre se cuiden y respeten mientras construyen una familia feliz. 🙏", "🕊️ Puissiez-vous toujours vous chérir et vous respecter en construisant une famille heureuse. 🙏", "🕊️ Possiate sempre volervi bene e rispettarvi mentre costruite una famiglia felice. 🙏", "🕊️ Que vocês sempre cuidem e respeitem um ao outro ao construir uma família feliz. 🙏"),
        ["🌈 May your future always be bright and radiant. ✨"] = new("🌈 Que su futuro sea siempre brillante y luminoso. ✨", "🌈 Que votre avenir soit toujours lumineux et radieux. ✨", "🌈 Che il vostro futuro sia sempre luminoso e splendente. ✨", "🌈 Que o futuro de vocês seja sempre brilhante e radiante. ✨"),
    };

    private static readonly Dictionary<string, string[]> Texts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["screen.settings"] = ["Screen settings", "Ajustes de pantalla", "Paramètres d’affichage", "Impostazioni schermo", "Configurações de tela", "화면 설정", "画面設定", "屏幕设置", "畫面設定", "Cài đặt màn hình"],
        ["language"] = ["Language", "Idioma", "Langue", "Lingua", "Idioma", "언어", "言語", "语言", "語言", "Ngôn ngữ"],
        ["theme"] = ["Theme", "Tema", "Thème", "Tema", "Tema", "화면 테마", "テーマ", "主题", "主題", "Giao diện"],
        ["system"] = ["System", "Sistema", "Système", "Sistema", "Sistema", "시스템", "システム", "系统", "系統", "Hệ thống"],
        ["light"] = ["Light", "Claro", "Clair", "Chiaro", "Claro", "라이트", "ライト", "浅色", "淺色", "Sáng"],
        ["dark"] = ["Dark", "Oscuro", "Sombre", "Scuro", "Escuro", "다크", "ダーク", "深色", "深色", "Tối"],
        ["nav.toggle"] = ["Open or close menu", "Abrir o cerrar menú", "Ouvrir ou fermer le menu", "Apri o chiudi menu", "Abrir ou fechar menu", "메뉴 열기/닫기", "メニューを開閉", "打开或关闭菜单", "開啟或關閉選單", "Mở hoặc đóng menu"],
        ["login"] = ["Sign in", "Iniciar sesión", "Connexion", "Accedi", "Entrar", "로그인", "ログイン", "登录", "登入", "Đăng nhập"],
        ["logout"] = ["Sign out", "Cerrar sesión", "Déconnexion", "Esci", "Sair", "로그아웃", "ログアウト", "退出登录", "登出", "Đăng xuất"],
        ["account"] = ["My account", "Mi cuenta", "Mon compte", "Il mio account", "Minha conta", "내 계정", "マイアカウント", "我的账户", "我的帳戶", "Tài khoản"],
        ["manage.mine"] = ["Manage my thank-you cards", "Gestionar mis tarjetas", "Gérer mes cartes", "Gestisci i miei biglietti", "Gerenciar meus cartões", "내 감사장 관리", "お礼状を管理", "管理我的感谢卡", "管理我的感謝卡", "Quản lý thiệp cảm ơn"],
        ["page.title"] = ["💌 Free mobile wedding thank-you card — Wedding Thank You", "💌 Tarjeta móvil de agradecimiento gratis — Wedding Thank You", "💌 Carte de remerciement mobile gratuite — Wedding Thank You", "💌 Biglietto di ringraziamento mobile gratuito — Wedding Thank You", "💌 Cartão móvel de agradecimento grátis — Wedding Thank You", "💌 무료 모바일 감사장 — Wedding Thank You", "💌 無料のモバイルお礼状 — Wedding Thank You", "💌 免费手机婚礼感谢卡 — Wedding Thank You", "💌 免費行動婚禮感謝卡 — Wedding Thank You", "💌 Thiệp cảm ơn đám cưới miễn phí — Wedding Thank You"],
        ["seo.description"] = ["Create a free mobile wedding thank-you card with photos, music and a guestbook in five minutes.", "Crea en cinco minutos una tarjeta móvil gratuita con fotos, música y libro de visitas.", "Créez en cinq minutes une carte mobile gratuite avec photos, musique et livre d’or.", "Crea in cinque minuti un biglietto mobile gratuito con foto, musica e guestbook.", "Crie em cinco minutos um cartão móvel grátis com fotos, música e livro de visitas.", "사진·음악·방명록까지 — 5분이면 완성되는 무료 모바일 결혼식 감사장 서비스", "写真・音楽・ゲストブック付きの無料モバイルお礼状を5分で作成。", "五分钟制作包含照片、音乐和留言簿的免费手机婚礼感谢卡。", "五分鐘製作包含照片、音樂和留言簿的免費行動婚禮感謝卡。", "Tạo thiệp cảm ơn đám cưới miễn phí với ảnh, nhạc và sổ lưu bút trong năm phút."],
        ["hero.badge"] = ["Free mobile thank-you card", "Tarjeta móvil gratuita", "Carte mobile gratuite", "Biglietto mobile gratuito", "Cartão móvel grátis", "무료 모바일 감사장", "無料モバイルお礼状", "免费手机感谢卡", "免費行動感謝卡", "Thiệp cảm ơn miễn phí"],
        ["hero.title"] = ["Share your treasured wedding\nwith a heartfelt thank-you", "Comparte tu boda especial\ncon un mensaje de gratitud", "Partagez votre précieux mariage\navec un message de gratitude", "Condividi il tuo matrimonio\ncon un sincero ringraziamento", "Compartilhe seu casamento\ncom uma mensagem de gratidão", "소중한 결혼식을\n감사 인사로 전해요", "大切な結婚式を\n感謝の言葉で伝えよう", "用真挚的谢意\n分享珍贵婚礼", "用真摯的謝意\n分享珍貴婚禮", "Chia sẻ ngày cưới đáng nhớ\nbằng lời cảm ơn chân thành"],
        ["hero.sub"] = ["Photos · music · guestbook — ready in five minutes", "Fotos · música · libro de visitas — listo en cinco minutos", "Photos · musique · livre d’or — prêt en cinq minutes", "Foto · musica · guestbook — pronto in cinque minuti", "Fotos · música · livro de visitas — pronto em cinco minutos", "사진·음악·방명록 — 5분이면 완성", "写真・音楽・ゲストブック — 5分で完成", "照片·音乐·留言簿 — 五分钟完成", "照片・音樂・留言簿 — 五分鐘完成", "Ảnh · nhạc · sổ lưu bút — hoàn thành trong năm phút"],
        ["create.now"] = ["Create for free now", "Crear gratis ahora", "Créer gratuitement", "Crea gratis ora", "Criar grátis agora", "지금 무료로 만들기", "今すぐ無料で作成", "立即免费制作", "立即免費製作", "Tạo miễn phí ngay"],
        ["codemaru.home"] = ["CodeMaru home", "Inicio de CodeMaru", "Accueil CodeMaru", "Home CodeMaru", "Início CodeMaru", "CodeMaru 홈", "CodeMaru ホーム", "CodeMaru 首页", "CodeMaru 首頁", "Trang chủ CodeMaru"],
        ["service.notice"] = ["As a free service, temporary connection delays may occur.", "Al ser un servicio gratuito, puede haber retrasos temporales.", "Ce service gratuit peut connaître des ralentissements temporaires.", "Il servizio gratuito può subire rallentamenti temporanei.", "Como serviço gratuito, podem ocorrer atrasos temporários.", "무료 서비스 특성상 일시적인 접속 지연이 발생할 수 있습니다.", "無料サービスのため、一時的に接続が遅れる場合があります。", "免费服务可能会出现暂时的连接延迟。", "免費服務可能會出現暫時的連線延遲。", "Dịch vụ miễn phí đôi khi có thể kết nối chậm."],
        ["features.photos"] = ["Photo gallery", "Galería de fotos", "Galerie photo", "Galleria fotografica", "Galeria de fotos", "사진 갤러리", "フォトギャラリー", "照片画廊", "照片藝廊", "Thư viện ảnh"],
        ["features.photos.desc"] = ["Share wedding snapshots and enjoy a slideshow.", "Comparte fotos de boda y disfruta de una presentación.", "Partagez les photos du mariage en diaporama.", "Condividi le foto del matrimonio in una presentazione.", "Compartilhe fotos do casamento em uma apresentação.", "결혼식 스냅을 갤러리로 공유하고 슬라이드쇼로 감상", "結婚式の写真を共有しスライドショーで鑑賞", "分享婚礼照片并以幻灯片欣赏", "分享婚禮照片並以投影片欣賞", "Chia sẻ ảnh cưới và xem trình chiếu"],
        ["features.music"] = ["Background music", "Música de fondo", "Musique de fond", "Musica di sottofondo", "Música de fundo", "배경 음악", "BGM", "背景音乐", "背景音樂", "Nhạc nền"],
        ["features.music.desc"] = ["Set your favorite song as the background.", "Usa tu canción favorita como fondo.", "Ajoutez votre musique préférée en fond.", "Imposta la tua musica preferita come sottofondo.", "Defina sua música favorita como fundo.", "좋아하는 음악을 배경으로 설정", "お気に入りの音楽をBGMに設定", "将喜欢的音乐设为背景", "將喜歡的音樂設為背景", "Đặt bài hát yêu thích làm nhạc nền"],
        ["features.guestbook"] = ["Thank-you guestbook", "Libro de agradecimientos", "Livre d’or", "Guestbook dei ringraziamenti", "Livro de agradecimentos", "감사 방명록", "お礼ゲストブック", "感谢留言簿", "感謝留言簿", "Sổ lưu bút cảm ơn"],
        ["section.message"] = ["Thank-you message", "Mensaje de agradecimiento", "Message de remerciement", "Messaggio di ringraziamento", "Mensagem de agradecimento", "감사 인사", "お礼のメッセージ", "感谢致辞", "感謝致詞", "Lời cảm ơn"],
        ["public.page.title"] = Localized("{0} — Wedding thank-you", "{0} 감사 인사", "{0} — 結婚式のお礼", "{0} — 婚礼感谢", "{0} — 婚禮感謝", "{0} — Lời cảm ơn đám cưới"),
        ["public.notfound.message"] = Localized("This thank-you card could not be found.", "감사장을 찾을 수 없습니다.", "お礼状が見つかりません。", "未找到感谢卡。", "找不到感謝卡。", "Không tìm thấy thiệp cảm ơn."),
        ["public.home"] = Localized("Go home", "홈으로", "ホームへ", "返回首页", "返回首頁", "Về trang chủ"),
        ["public.music.play"] = Localized("Play music", "음악 켜기", "音楽を再生", "播放音乐", "播放音樂", "Phát nhạc"),
        ["public.music.pause"] = Localized("Pause music", "음악 끄기", "音楽を停止", "暂停音乐", "暫停音樂", "Tạm dừng nhạc"),
        ["public.music.playing"] = Localized("Playing", "재생 중", "再生中", "播放中", "播放中", "Đang phát"),
        ["public.menu"] = Localized("Thank-you card menu", "감사장 메뉴", "お礼状メニュー", "感谢卡菜单", "感謝卡選單", "Menu thiệp cảm ơn"),
        ["public.menu.mobile"] = Localized("Thank-you card menu (mobile)", "감사장 메뉴 (모바일)", "お礼状メニュー（モバイル）", "感谢卡菜单（手机）", "感謝卡選單（行動版）", "Menu thiệp cảm ơn (di động)"),
        ["public.menu.open"] = Localized("Open menu", "메뉴 열기", "メニューを開く", "打开菜单", "開啟選單", "Mở menu"),
        ["public.menu.close"] = Localized("Close menu", "메뉴 닫기", "メニューを閉じる", "关闭菜单", "關閉選單", "Đóng menu"),
        ["public.card.aria"] = Localized("{0}'s wedding thank-you card", "{0} 카드 감사장", "{0}の結婚式お礼カード", "{0}的婚礼感谢卡", "{0}的婚禮感謝卡", "Thiệp cảm ơn đám cưới của {0}"),
        ["public.card.cover"] = Localized("Cover", "표지", "表紙", "封面", "封面", "Bìa"),
        ["public.card.navigation.hint"] = Localized("Use the buttons or the left and right arrow keys. You can also swipe across an empty area to turn the card.", "버튼이나 키보드 좌우 방향키를 사용하고, 콘텐츠가 없는 영역에서는 좌우로 밀어 카드를 넘겨보세요.", "ボタンまたは左右の矢印キーを使用してください。内容のない部分では左右にスワイプしてカードをめくれます。", "可使用按钮或键盘左右方向键，也可在空白区域左右滑动翻页。", "可使用按鈕或鍵盤左右方向鍵，也可在空白區域左右滑動翻頁。", "Dùng nút hoặc phím mũi tên trái/phải; bạn cũng có thể vuốt trên vùng trống để lật thiệp."),
        ["public.photobook.aria"] = Localized("{0}'s wedding thank-you photo book", "{0} 포토북 감사장", "{0}の結婚式お礼フォトブック", "{0}的婚礼感谢相册", "{0}的婚禮感謝相簿", "Sách ảnh cảm ơn đám cưới của {0}"),
        ["public.photobook.title"] = Localized("Our thank-you memories", "우리의 감사 기록", "私たちの感謝の記録", "我们的感谢记录", "我們的感謝紀錄", "Kỷ niệm tri ân của chúng tôi"),
        ["public.photobook.label"] = Localized("Photo book", "포토북", "フォトブック", "相册", "相簿", "Sách ảnh"),
        ["public.photobook.cover"] = Localized("Cover", "표지", "表紙", "封面", "封面", "Bìa"),
        ["public.photobook.photo.empty"] = Localized("Please choose a photo.", "사진을 선택해 주세요.", "写真を選択してください。", "请选择照片。", "請選擇照片。", "Vui lòng chọn ảnh."),
        ["public.page.number"] = Localized("Page {0}", "{0}페이지", "{0}ページ", "第 {0} 页", "第 {0} 頁", "Trang {0}"),
        ["public.hero.photo.alt"] = Localized("Featured photo of {0}", "{0} 대표 사진", "{0}のメイン写真", "{0}的主照片", "{0}的主照片", "Ảnh đại diện của {0}"),
        ["public.hero.date.thanks"] = Localized("{0}, thank you for sharing our special day.", "{0}, 소중한 자리에 함께해 주셔서 감사합니다.", "{0}、大切な日にご一緒いただきありがとうございました。", "{0}，感谢您见证我们的珍贵时刻。", "{0}，感謝您見證我們的珍貴時刻。", "{0}, cảm ơn bạn đã hiện diện trong ngày đặc biệt của chúng tôi."),
        ["public.invitation.open"] = Localized("View invitation", "청첩장 보기", "招待状を見る", "查看请柬", "查看喜帖", "Xem thiệp mời"),
        ["public.admin.open"] = Localized("Admin", "관리자", "管理", "管理", "管理", "Quản trị"),
        ["public.section.hero"] = Localized("Home", "홈", "ホーム", "首页", "首頁", "Trang chủ"),
        ["public.section.message"] = Localized("Thank-you message", "감사 인사", "お礼のメッセージ", "感谢致辞", "感謝致詞", "Lời cảm ơn"),
        ["public.section.gallery"] = Localized("Gallery", "갤러리 사진", "ギャラリー写真", "相册照片", "相簿照片", "Thư viện ảnh"),
        ["public.section.video"] = Localized("Our video", "우리의 영상", "私たちの動画", "我们的视频", "我們的影片", "Video của chúng tôi"),
        ["public.section.guestbook"] = Localized("Thank-you guestbook", "감사 방명록", "お礼ゲストブック", "感谢留言簿", "感謝留言簿", "Sổ lưu bút cảm ơn"),
        ["public.section.story"] = Localized("Our story", "우리의 이야기", "私たちの物語", "我们的故事", "我們的故事", "Câu chuyện của chúng tôi"),
        ["public.section.card"] = Localized("Card", "카드", "カード", "卡片", "卡片", "Thiệp"),
        ["public.section.photobook"] = Localized("Photo book", "포토북", "フォトブック", "相册", "相簿", "Sách ảnh"),
        ["public.gallery.title"] = Localized("Our photos", "우리의 사진", "私たちの写真", "我们的照片", "我們的照片", "Ảnh của chúng tôi"),
        ["public.gallery.moments.title"] = Localized("Moments of gratitude", "감사의 순간들", "感謝の瞬間", "感恩时刻", "感恩時刻", "Khoảnh khắc tri ân"),
        ["public.gallery.autoplay.start"] = Localized("▶ Autoplay", "▶ 자동재생", "▶ 自動再生", "▶ 自动播放", "▶ 自動播放", "▶ Tự động phát"),
        ["public.gallery.autoplay.stop"] = Localized("⏸ Stop", "⏸ 정지", "⏸ 停止", "⏸ 停止", "⏸ 停止", "⏸ Dừng"),
        ["public.gallery.count.recent"] = Localized("Latest {0} / all {1}", "최근 {0}장 / 전체 {1}장", "最新{0}枚／全{1}枚", "最近 {0} 张 / 共 {1} 张", "最近 {0} 張 / 共 {1} 張", "Mới nhất {0} / tổng {1} ảnh"),
        ["public.gallery.count.visible"] = Localized("Showing {0} / {1}", "표시 {0}장 / 전체 {1}장", "表示{0}枚／全{1}枚", "显示 {0} 张 / 共 {1} 张", "顯示 {0} 張 / 共 {1} 張", "Đang hiển thị {0} / {1} ảnh"),
        ["public.gallery.empty"] = Localized("No photos have been added yet.", "아직 등록된 사진이 없습니다.", "まだ写真が登録されていません。", "尚未添加照片。", "尚未新增照片。", "Chưa có ảnh nào được thêm."),
        ["public.gallery.photo.alt"] = Localized("Photo {0}", "사진 {0}", "写真{0}", "照片 {0}", "照片 {0}", "Ảnh {0}"),
        ["public.gallery.photo.open"] = Localized("Open photo {0}", "사진 {0} 크게 보기", "写真{0}を拡大表示", "查看大图 {0}", "放大查看照片 {0}", "Mở ảnh {0}"),
        ["public.gallery.more"] = Localized("View more photos (+{0})", "사진 더 보기 (+{0})", "写真をもっと見る（+{0}）", "查看更多照片（+{0}）", "查看更多照片（+{0}）", "Xem thêm ảnh (+{0})"),
        ["public.gallery.dialog"] = Localized("Photo viewer", "사진 보기", "写真ビューア", "照片查看器", "相片檢視器", "Trình xem ảnh"),
        ["public.story.title"] = Localized("Our story", "우리의 이야기", "私たちの物語", "我们的故事", "我們的故事", "Câu chuyện của chúng tôi"),
        ["public.story.moments.title"] = Localized("Our moments together", "함께한 순간", "共に過ごした時間", "共同的时光", "相伴的時光", "Khoảnh khắc bên nhau"),
        ["public.story.photo.open"] = Localized("Open the photo for {0}", "{0} 사진 크게 보기", "{0}の写真を拡大表示", "查看{0}的照片", "放大查看{0}的照片", "Mở ảnh của {0}"),
        ["public.story.default.opening"] = Localized("{0}, we celebrated our wedding at {1} surrounded by warm wishes.", "{0}, {1}에서의 결혼식을 많은 분들의 축복 속에 무사히 마쳤습니다.", "{0}、{1}にて多くの皆さまの祝福に包まれ、無事に結婚式を終えました。", "{0}，我们在亲友的祝福中于{1}圆满举行了婚礼。", "{0}，我們在親友的祝福中於{1}圓滿舉行了婚禮。", "Ngày {0}, chúng tôi đã tổ chức lễ cưới trọn vẹn tại {1} trong những lời chúc phúc."),
        ["public.story.default.beginning"] = Localized("Our story begins here.", "두 사람의 이야기가 여기서부터 시작됩니다.", "二人の物語はここから始まります。", "两个人的故事从这里开始。", "兩人的故事從這裡開始。", "Câu chuyện của hai chúng tôi bắt đầu từ đây."),
        ["public.story.default.memories"] = Localized("We share the memories we have gathered along the way.", "쌓아온 시간을 담아 전해드립니다.", "積み重ねてきた時間をお届けします。", "与您分享一路积累的珍贵回忆。", "與您分享一路累積的珍貴回憶。", "Chúng tôi xin chia sẻ những kỷ niệm đã cùng nhau vun đắp."),
        ["public.story.default.blessings"] = Localized("We share our photos and stories from the moments filled with your blessings.", "함께해 주신 시간과 축복의 순간을 사진과 이야기로 천천히 전해드립니다.", "皆さまに見守っていただいた時間と祝福の瞬間を、写真と言葉でゆっくりお届けします。", "我们用照片和文字慢慢分享那些有您陪伴与祝福的时刻。", "我們用照片和文字慢慢分享那些有您陪伴與祝福的時刻。", "Chúng tôi chia sẻ bằng hình ảnh và câu chuyện những khoảnh khắc được mọi người đồng hành, chúc phúc."),
        ["public.story.default.closing"] = Localized("With lasting gratitude to everyone who sent us their warm wishes.", "소중한 마음을 보내주신 모든 분들께 오래 기억될 감사의 인사를 남깁니다.", "温かいお気持ちを寄せてくださった皆さまへ、心に残る感謝をお伝えします。", "向所有送来温暖祝福的人致以长久铭记的谢意。", "向所有送來溫暖祝福的人致上長久銘記的謝意。", "Xin gửi lời tri ân sâu sắc đến tất cả những ai đã dành cho chúng tôi tình cảm quý báu."),
        ["public.video.title"] = Localized("Our video", "우리의 영상", "私たちの動画", "我们的视频", "我們的影片", "Video của chúng tôi"),
        ["public.video.open"] = Localized("Play video {0}", "영상 {0} 재생", "動画{0}を再生", "播放视频 {0}", "播放影片 {0}", "Phát video {0}"),
        ["public.video.previous"] = Localized("Previous video", "이전 영상", "前の動画", "上一个视频", "上一部影片", "Video trước"),
        ["public.video.next"] = Localized("Next video", "다음 영상", "次の動画", "下一个视频", "下一部影片", "Video tiếp theo"),
        ["public.guestbook.title"] = Localized("Thank-you guestbook", "감사 방명록", "お礼ゲストブック", "感谢留言簿", "感謝留言簿", "Sổ lưu bút cảm ơn"),
        ["public.guestbook.name"] = Localized("Name", "이름", "お名前", "姓名", "姓名", "Tên"),
        ["public.guestbook.contact.optional"] = Localized("Contact (optional)", "연락처 (선택)", "連絡先（任意）", "联系方式（选填）", "聯絡方式（選填）", "Liên hệ (không bắt buộc)"),
        ["public.guestbook.message.placeholder"] = Localized("Leave a thank-you message", "감사 메시지를 남겨주세요", "お礼のメッセージをお寄せください", "请留下感谢留言", "請留下感謝留言", "Hãy để lại lời nhắn cảm ơn"),
        ["public.guestbook.submit"] = Localized("Post", "등록", "投稿", "提交", "送出", "Đăng"),
        ["public.guestbook.reload"] = Localized("Refresh", "새로고침", "更新", "刷新", "重新整理", "Làm mới"),
        ["public.guestbook.status.load.error"] = Localized("Unable to load the guestbook.", "방명록을 불러오지 못했습니다.", "ゲストブックを読み込めませんでした。", "无法加载留言簿。", "無法載入留言簿。", "Không thể tải sổ lưu bút."),
        ["public.guestbook.status.name.required"] = Localized("Please enter your name.", "이름을 입력해 주세요.", "お名前を入力してください。", "请输入姓名。", "請輸入姓名。", "Vui lòng nhập tên."),
        ["public.guestbook.status.message.required"] = Localized("Please enter a message.", "메시지를 입력해 주세요.", "メッセージを入力してください。", "请输入留言。", "請輸入留言。", "Vui lòng nhập lời nhắn."),
        ["public.guestbook.status.save.success"] = Localized("Your message has been posted.", "메시지가 등록되었습니다.", "メッセージを投稿しました。", "留言已发布。", "留言已送出。", "Lời nhắn của bạn đã được đăng."),
        ["public.guestbook.status.save.error"] = Localized("Unable to post your message.", "메시지를 등록하지 못했습니다.", "メッセージを投稿できませんでした。", "无法发布留言。", "無法送出留言。", "Không thể đăng lời nhắn."),
        ["public.guestbook.status.reload.success"] = Localized("The guestbook has been refreshed.", "방명록을 새로고침했습니다.", "ゲストブックを更新しました。", "留言簿已刷新。", "留言簿已重新整理。", "Sổ lưu bút đã được làm mới."),
        ["public.guestbook.status.reload.error"] = Localized("Unable to refresh the guestbook.", "방명록을 새로고침하지 못했습니다.", "ゲストブックを更新できませんでした。", "无法刷新留言簿。", "無法重新整理留言簿。", "Không thể làm mới sổ lưu bút."),
        ["public.guestbook.default.1"] = Localized("💖 Wishing your future together is filled with love and happiness. ✨", "💖 두 분의 앞날에 사랑과 행복이 가득하시길 바랍니다 ✨", "💖 お二人の未来が愛と幸せに満ちあふれますように。✨", "💖 愿两位的未来充满爱与幸福。✨", "💖 願兩位的未來充滿愛與幸福。✨", "💖 Chúc tương lai của hai bạn luôn tràn đầy yêu thương và hạnh phúc. ✨"),
        ["public.guestbook.default.2"] = Localized("🎉 Heartfelt congratulations on your wedding! May you always be together. 💍", "🎉 결혼을 진심으로 축하드립니다! 영원히 함께하세요 💍", "🎉 ご結婚を心よりお祝い申し上げます！いつまでも仲良くお過ごしください。💍", "🎉 衷心祝贺两位新婚！愿你们永远相伴。💍", "🎉 衷心祝賀兩位新婚！願你們永遠相伴。💍", "🎉 Chân thành chúc mừng đám cưới! Chúc hai bạn mãi bên nhau. 💍"),
        ["public.guestbook.default.3"] = Localized("🌸 May today's promise lead to a lifetime of joy. 💕", "🌸 오늘의 약속이 평생의 기쁨으로 이어지길 바랍니다 💕", "🌸 今日の誓いが生涯の喜びへとつながりますように。💕", "🌸 愿今天的誓言化作一生的喜悦。💕", "🌸 願今天的誓言化作一生的喜悅。💕", "🌸 Chúc lời hẹn ước hôm nay dẫn lối đến niềm vui trọn đời. 💕"),
        ["public.guestbook.default.4"] = Localized("🕊️ May you always cherish and respect each other as you build a happy family. 🙏", "🕊️ 늘 서로를 아끼고 존중하며 행복한 가정을 꾸리시길 🙏", "🕊️ いつまでも思いやりと敬意を大切に、幸せな家庭を築かれますように。🙏", "🕊️ 愿你们始终珍惜、尊重彼此，共建幸福家庭。🙏", "🕊️ 願你們始終珍惜、尊重彼此，共築幸福家庭。🙏", "🕊️ Chúc hai bạn luôn yêu thương, tôn trọng nhau và xây dựng gia đình hạnh phúc. 🙏"),
        ["public.guestbook.default.5"] = Localized("🌈 May your future always be bright and radiant. ✨", "🌈 두 분의 미래가 언제나 밝고 빛나길 바랍니다 ✨", "🌈 お二人の未来がいつも明るく輝きますように。✨", "🌈 愿两位的未来永远明亮灿烂。✨", "🌈 願兩位的未來永遠明亮燦爛。✨", "🌈 Chúc tương lai của hai bạn luôn tươi sáng và rạng rỡ. ✨"),
        ["public.lightbox.close"] = Localized("Close", "닫기", "閉じる", "关闭", "關閉", "Đóng"),
        ["public.lightbox.previous"] = Localized("Previous photo", "이전 사진", "前の写真", "上一张照片", "上一張照片", "Ảnh trước"),
        ["public.lightbox.next"] = Localized("Next photo", "다음 사진", "次の写真", "下一张照片", "下一張照片", "Ảnh tiếp theo"),
        ["public.scroll"] = Localized("Scroll", "스크롤", "スクロール", "滚动", "捲動", "Cuộn"),
        ["public.thankyou"] = Localized("Thank you", "감사합니다", "ありがとうございます", "谢谢", "謝謝", "Xin cảm ơn"),
        ["features.guestbook.desc"] = ["Receive guests’ congratulatory messages online.", "Recibe en línea los mensajes de tus invitados.", "Recevez en ligne les messages de vos invités.", "Ricevi online i messaggi degli invitati.", "Receba online as mensagens dos convidados.", "하객들의 축하 메시지를 온라인으로 받기", "ゲストのお祝いメッセージをオンラインで受付", "在线接收宾客的祝福留言", "線上接收賓客的祝福留言", "Nhận lời chúc của khách mời trực tuyến"],
        ["features.themes"] = ["Five themes", "Cinco temas", "Cinq thèmes", "Cinque temi", "Cinco temas", "5가지 테마", "5つのテーマ", "五种主题", "五種主題", "Năm giao diện"],
        ["features.themes.desc"] = ["Rose Gold · Ivory · Forest · Navy · Blush", "Oro rosa · Marfil · Bosque · Marino · Rubor", "Or rose · Ivoire · Forêt · Marine · Poudré", "Oro rosa · Avorio · Foresta · Navy · Cipria", "Ouro rosé · Marfim · Floresta · Marinho · Blush", "로즈골드·아이보리·포레스트·네이비·블러쉬", "ローズゴールド・アイボリー・フォレスト・ネイビー・ブラッシュ", "玫瑰金·象牙白·森林·海军蓝·腮红", "玫瑰金・象牙白・森林・海軍藍・腮紅", "Vàng hồng · Ngà · Rừng · Navy · Hồng phấn"],
        ["common.login"] = ["Shared sign-in", "Inicio de sesión común", "Connexion partagée", "Accesso condiviso", "Login compartilhado", "공용 로그인", "共通ログイン", "统一登录", "共用登入", "Đăng nhập chung"],
        ["account.connect"] = ["Sign in to link every card you create directly to your account.", "Inicia sesión para vincular cada tarjeta a tu cuenta.", "Connectez-vous pour lier chaque carte à votre compte.", "Accedi per collegare ogni biglietto al tuo account.", "Entre para vincular cada cartão à sua conta.", "로그인하면 생성한 감사장이 내 계정에 바로 연결됩니다.", "ログインすると作成したお礼状がアカウントに連携されます。", "登录后创建的感谢卡将直接关联到你的账户。", "登入後建立的感謝卡將直接連結至帳戶。", "Đăng nhập để liên kết thiệp đã tạo với tài khoản."],
        ["create.title"] = ["Start for free", "Empieza gratis", "Commencer gratuitement", "Inizia gratis", "Comece grátis", "무료로 시작하기", "無料で始める", "免费开始", "免費開始", "Bắt đầu miễn phí"],
        ["create.desc"] = ["Enter the details below to create your thank-you card instantly.", "Introduce los datos para crear tu tarjeta al instante.", "Saisissez les informations pour créer votre carte immédiatement.", "Inserisci i dati per creare subito il biglietto.", "Informe os dados para criar seu cartão imediatamente.", "아래 정보를 입력하면 바로 감사장이 생성됩니다.", "以下の情報を入力するとすぐにお礼状が作成されます。", "输入以下信息即可立即创建感谢卡。", "輸入以下資訊即可立即建立感謝卡。", "Nhập thông tin dưới đây để tạo thiệp ngay."],
        ["field.url"] = ["URL address", "Dirección URL", "Adresse URL", "Indirizzo URL", "Endereço URL", "URL 주소", "URLアドレス", "URL 地址", "URL 位址", "Địa chỉ URL"],
        ["field.url.hint"] = ["Letters, numbers and hyphens only (e.g. hong-gildong)", "Solo letras, números y guiones", "Lettres, chiffres et tirets uniquement", "Solo lettere, numeri e trattini", "Apenas letras, números e hífens", "영문·숫자·하이픈만 (예: hong-gildong)", "英字・数字・ハイフンのみ", "仅限字母、数字和连字符", "僅限英文字母、數字與連字號", "Chỉ chữ cái, số và dấu gạch nối"],
        ["field.couple"] = ["Couple’s names", "Nombres de la pareja", "Noms du couple", "Nomi della coppia", "Nomes do casal", "커플 이름", "お二人の名前", "新人姓名", "新人姓名", "Tên cô dâu chú rể"],
        ["field.couple.placeholder"] = ["Alex ♥ Jamie", "Alex ♥ Jamie", "Alex ♥ Jamie", "Alex ♥ Jamie", "Alex ♥ Jamie", "홍길동 ♥ 김영희", "太郎 ♥ 花子", "新郎 ♥ 新娘", "新郎 ♥ 新娘", "Chú rể ♥ Cô dâu"],
        ["field.date"] = ["Wedding date", "Fecha de la boda", "Date du mariage", "Data del matrimonio", "Data do casamento", "결혼식 날짜", "挙式日", "婚礼日期", "婚禮日期", "Ngày cưới"],
        ["field.password"] = ["Admin password", "Contraseña de administrador", "Mot de passe administrateur", "Password amministratore", "Senha de administrador", "어드민 비밀번호", "管理パスワード", "管理员密码", "管理員密碼", "Mật khẩu quản trị"],
        ["field.password.hint"] = ["Used to edit the card", "Se usa para editar la tarjeta", "Utilisé pour modifier la carte", "Usata per modificare il biglietto", "Usada para editar o cartão", "감사장 편집 시 사용", "お礼状の編集に使用", "用于编辑感谢卡", "用於編輯感謝卡", "Dùng để chỉnh sửa thiệp"],
        ["field.password.placeholder"] = ["Eight or more characters recommended", "Se recomiendan ocho caracteres", "Huit caractères ou plus recommandés", "Consigliati almeno otto caratteri", "Recomendamos oito caracteres", "8자 이상 권장", "8文字以上を推奨", "建议至少八个字符", "建議至少八個字元", "Nên dùng ít nhất tám ký tự"],
        ["creating"] = ["Creating...", "Creando...", "Création...", "Creazione...", "Criando...", "생성 중...", "作成中...", "正在创建...", "正在建立...", "Đang tạo..."],
        ["create.button"] = ["Create thank-you card", "Crear tarjeta", "Créer la carte", "Crea il biglietto", "Criar cartão", "감사장 만들기", "お礼状を作成", "创建感谢卡", "建立感謝卡", "Tạo thiệp cảm ơn"],
        ["create.after.prefix"] = ["After creation, configure photos and music at", "Después, configura fotos y música en", "Après création, configurez photos et musique dans", "Dopo la creazione, configura foto e musica in", "Depois, configure fotos e música em", "생성 후", "作成後、", "创建后，可在", "建立後，可在", "Sau khi tạo, thiết lập ảnh và nhạc tại"],
        ["create.after.suffix"] = [".", ".", ".", ".", ".", "에서 사진·음악 등을 설정할 수 있습니다.", "で写真や音楽を設定できます。", "设置照片和音乐。", "設定照片與音樂。", "."],
        ["mine.title"] = ["My thank-you cards", "Mis tarjetas", "Mes cartes", "I miei biglietti", "Meus cartões", "내 감사장", "自分のお礼状", "我的感谢卡", "我的感謝卡", "Thiệp của tôi"],
        ["mine.empty.prefix"] = ["No cards are linked to this account yet. To link an existing card, visit", "Aún no hay tarjetas vinculadas. Para vincular una, visita", "Aucune carte liée. Pour en lier une, ouvrez", "Nessun biglietto collegato. Per collegarne uno, visita", "Nenhum cartão vinculado. Para vincular um, acesse", "아직 이 계정에 연결된 감사장이 없습니다. 기존 감사장이 있다면", "まだ連携されたお礼状はありません。既存のお礼状は", "尚未关联感谢卡。如需关联现有感谢卡，请访问", "尚未連結感謝卡。如需連結現有感謝卡，請前往", "Chưa có thiệp nào được liên kết. Để liên kết thiệp cũ, hãy mở"],
        ["mine.empty.suffix"] = ["and enter its password once.", "e introduce su contraseña una vez.", "et saisissez son mot de passe une fois.", "e inserisci la password una volta.", "e informe a senha uma vez.", "에서 기존 비밀번호를 한 번 입력해 연결하세요.", "で既存パスワードを一度入力してください。", "并输入一次原密码。", "並輸入一次原密碼。", "và nhập mật khẩu một lần."],
        ["manage.open"] = ["Open admin", "Abrir administración", "Ouvrir la gestion", "Apri gestione", "Abrir administração", "관리 열기", "管理を開く", "打开管理", "開啟管理", "Mở quản lý"],
        ["list.title"] = ["Thank-you cards", "Tarjetas de agradecimiento", "Cartes de remerciement", "Biglietti di ringraziamento", "Cartões de agradecimento", "감사장 목록", "お礼状一覧", "感谢卡列表", "感謝卡清單", "Danh sách thiệp cảm ơn"],
        ["admin"] = ["Admin", "Administración", "Administration", "Amministrazione", "Administração", "관리자", "管理", "管理", "管理", "Quản trị"],
        ["admin.basic.title"] = ["Header title", "Título superior", "Titre supérieur", "Titolo superiore", "Título superior", "상단 타이틀 문구", "上部タイトル", "顶部标题", "頂部標題", "Tiêu đề đầu trang"],
        ["admin.share.settings"] = ["Sharing settings", "Ajustes para compartir", "Paramètres de partage", "Impostazioni condivisione", "Configurações de compartilhamento", "공유 설정", "共有設定", "分享设置", "分享設定", "Cài đặt chia sẻ"],
        ["admin.share.invitation.url"] = ["Linked invitation URL", "URL de invitación vinculada", "URL de l’invitation associée", "URL dell’invito collegato", "URL do convite vinculado", "연결할 청첩장 URL", "連携する招待状URL", "关联的请柬 URL", "連結的喜帖 URL", "URL thiệp cưới liên kết"],
        ["admin.share.invitation.url.hint"] = ["Leave blank to hide the invitation link at the bottom of the thank-you page.", "Déjalo vacío para ocultar el enlace de invitación al pie de la página de agradecimiento.", "Laissez vide pour masquer le lien vers l’invitation en bas de la page de remerciement.", "Lascia vuoto per nascondere il link all’invito in fondo alla pagina di ringraziamento.", "Deixe em branco para ocultar o link do convite no fim da página de agradecimento.", "비워두면 감사장 하단의 청첩장 보기 링크를 숨깁니다.", "空欄にすると、お礼状下部の招待状リンクを非表示にします。", "留空时将隐藏感谢页底部的请柬链接。", "留空時將隱藏感謝頁底部的喜帖連結。", "Để trống để ẩn liên kết thiệp cưới ở cuối trang cảm ơn."],
        ["admin.share.result.copied"] = ["Thank-you page link copied.", "Enlace de agradecimiento copiado.", "Lien de la page de remerciement copié.", "Link della pagina di ringraziamento copiato.", "Link da página de agradecimento copiado.", "감사장 링크를 복사했습니다.", "お礼状リンクをコピーしました。", "已复制感谢页链接。", "已複製感謝頁連結。", "Đã sao chép liên kết trang cảm ơn."],
        ["admin.preview.device"] = ["Phone preview device", "Dispositivo de vista previa móvil", "Appareil d’aperçu mobile", "Dispositivo di anteprima mobile", "Dispositivo da prévia móvel", "폰 미리보기 기기", "スマートフォンプレビュー端末", "手机预览设备", "手機預覽裝置", "Thiết bị xem trước điện thoại"],
        ["admin.preview.collapse"] = ["Collapse preview", "Contraer vista previa", "Réduire l’aperçu", "Comprimi anteprima", "Recolher prévia", "미리보기 접기", "プレビューを折りたたむ", "收起预览", "收合預覽", "Thu gọn xem trước"],
        ["admin.super.create.button"] = ["Create thank-you card", "Crear tarjeta", "Créer la carte", "Crea il biglietto", "Criar cartão", "감사장 생성", "お礼状を作成", "创建感谢卡", "建立感謝卡", "Tạo thiệp cảm ơn"],
        ["admin.super.free.media.policy"] = ["Free media default policy", "Política multimedia gratuita", "Politique média gratuite", "Criteri media gratuiti", "Política de mídia gratuita", "무료 미디어 기본 정책", "無料メディア基本ポリシー", "免费媒体默认策略", "免費媒體預設政策", "Chính sách media miễn phí"],
        ["admin.super.highest"] = ["Super administrator", "Superadministrador", "Super administrateur", "Super amministratore", "Superadministrador", "최고 관리자", "最高管理者", "超级管理员", "超級管理員", "Quản trị cao nhất"],
        ["admin.super.main.visible"] = ["Featured on home", "Visible en inicio", "Visible sur l’accueil", "Visibile in home", "Visível na página inicial", "메인 노출", "ホーム掲載", "首页展示", "首頁顯示", "Hiển thị trang chủ"],
        ["admin.super.menu"] = ["Super admin menu", "Menú de superadministrador", "Menu super administrateur", "Menu super amministratore", "Menu do superadministrador", "슈퍼 어드민 메뉴", "スーパー管理メニュー", "超级管理菜单", "超級管理選單", "Menu quản trị cao nhất"],
        ["admin.super.password"] = ["Admin password", "Contraseña de administrador", "Mot de passe administrateur", "Password amministratore", "Senha de administrador", "어드민 비밀번호", "管理パスワード", "管理员密码", "管理員密碼", "Mật khẩu quản trị"],
        ["admin.super.premium.active"] = ["Premium active", "Premium activo", "Premium actif", "Premium attivo", "Premium ativo", "Premium 활성", "Premium 有効", "Premium 已启用", "Premium 已啟用", "Premium đang bật"],
        ["admin.super.refresh"] = ["Refresh", "Actualizar", "Actualiser", "Aggiorna", "Atualizar", "새로고침", "更新", "刷新", "重新整理", "Làm mới"],
        ["admin.super.refresh.all"] = ["Refresh the full list", "Actualizar toda la lista", "Actualiser toute la liste", "Aggiorna l’intero elenco", "Atualizar toda a lista", "전체 목록 새로고침", "一覧全体を更新", "刷新完整列表", "重新整理完整清單", "Làm mới toàn bộ danh sách"],
        ["admin.super.search.placeholder"] = ["Search couple, slug or email", "Buscar pareja, slug o correo", "Rechercher couple, slug ou e-mail", "Cerca coppia, slug o email", "Buscar casal, slug ou e-mail", "커플 이름, 슬러그, 이메일 검색", "名前・スラッグ・メールを検索", "搜索新人、标识或邮箱", "搜尋新人、代稱或電子郵件", "Tìm tên cặp đôi, slug hoặc email"],
        ["admin.super.storage"] = ["Storage", "Almacenamiento", "Stockage", "Archiviazione", "Armazenamento", "저장 용량", "ストレージ", "存储空间", "儲存空間", "Dung lượng"],
        ["admin.super.summary"] = ["Summary", "Resumen", "Résumé", "Riepilogo", "Resumo", "요약", "概要", "摘要", "摘要", "Tóm tắt"],
        ["admin.super.total.accounts"] = ["Total accounts", "Cuentas totales", "Total des comptes", "Account totali", "Total de contas", "전체 계정 수", "アカウント総数", "账户总数", "帳戶總數", "Tổng tài khoản"],
        ["admin.super.video.usage"] = ["Video usage", "Uso de vídeo", "Utilisation vidéo", "Utilizzo video", "Uso de vídeo", "동영상 사용량", "動画使用量", "视频用量", "影片用量", "Dung lượng video"]
    };

    private static string[] Localized(
        string en,
        string ko,
        string ja,
        string zhCn,
        string zhTw,
        string vi)
    {
        if (!WesternPublicTexts.TryGetValue(en, out var western))
        {
            throw new InvalidOperationException($"Missing Spanish, French, Italian and Portuguese public translations for: {en}");
        }

        return [en, western.Spanish, western.French, western.Italian, western.Portuguese, ko, ja, zhCn, zhTw, vi];
    }

    public string Language { get; private set; } = "ko";
    public string HtmlLanguage => Languages.First(x => x.Code == Language).HtmlLanguage;
    public event Action? Changed;

    public string this[string key]
    {
        get
        {
            if (!Texts.TryGetValue(key, out var values)) return _shared[key];
            var index = Array.IndexOf(Codes, Language);
            return values[index < 0 ? 5 : index];
        }
    }

    public static string NormalizeLanguageCode(string? language)
    {
        var normalized = language?.Trim().ToLowerInvariant().Replace('_', '-');
        return normalized switch
        {
            "zh" or "zh-cn" or "zh-sg" or "zh-hans" => "zh-hans",
            "zh-tw" or "zh-hk" or "zh-mo" or "zh-hant" => "zh-hant",
            _ => normalized ?? string.Empty,
        };
    }

    public void SetLanguage(string? language)
    {
        var normalized = NormalizeLanguageCode(language);
        if (!Codes.Contains(normalized)) normalized = "ko";

        // Keep the shared Wedding/Codemaru localizer synchronized even when
        // this instance already has the requested language.
        _shared.SetLanguage(normalized);
        if (Language == normalized) return;

        Language = normalized;
        Changed?.Invoke();
    }

    public string FormatDate(DateTime date) => Language switch
    {
        "ko" => date.ToString("yyyy년 MM월 dd일"),
        "ja" => date.ToString("yyyy年MM月dd日"),
        "zh-hans" or "zh-hant" => date.ToString("yyyy年MM月dd日"),
        "en" => date.ToString("MMMM d, yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US")),
        _ => date.ToString("d", System.Globalization.CultureInfo.GetCultureInfo(Languages.First(x => x.Code == Language).HtmlLanguage))
    };

    public string FormatDateTime(DateTime date) => Language switch
    {
        "ko" => date.ToString("yyyy-MM-dd HH:mm"),
        "ja" => date.ToString("yyyy/MM/dd HH:mm"),
        "zh-hans" or "zh-hant" => date.ToString("yyyy/MM/dd HH:mm"),
        "en" => date.ToString("MMM d, yyyy h:mm tt", System.Globalization.CultureInfo.GetCultureInfo("en-US")),
        _ => date.ToString("g", System.Globalization.CultureInfo.GetCultureInfo(Languages.First(x => x.Code == Language).HtmlLanguage))
    };
}
