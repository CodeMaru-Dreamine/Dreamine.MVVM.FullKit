using Dreamine.UI.Blazor.Localization;

namespace GamePlatform.Services;

/// <summary>
/// Shared CodeMaru language state for the game library and individual game landing pages.
/// The language codes and storage key intentionally match the other CodeMaru services.
/// </summary>
public sealed class GamesLocalization : DreamineLocalizationService
{
    public string HtmlLanguage => Languages.First(item =>
        string.Equals(item.Code, Language, StringComparison.OrdinalIgnoreCase)).HtmlLanguage ?? Language;

    public GamesLocalization()
        : base(new DreamineLocalizationCatalog(
            "ko",
            SupportedLanguages,
            BuildTexts(),
            "en"))
    {
    }

    private static readonly IReadOnlyList<DreamineLanguage> SupportedLanguages =
    [
        new("en", "US", "English"),
        new("es", "ES", "Español"),
        new("fr", "FR", "Français"),
        new("it", "IT", "Italiano"),
        new("pt", "PT", "Português"),
        new("ko", "KR", "한국어"),
        new("ja", "JP", "日本語"),
        new("zh-hans", "CN", "简体中文", "zh-Hans"),
        new("zh-hant", "HK", "繁體中文", "zh-Hant"),
        new("vi", "VN", "Tiếng Việt")
    ];

    private static Dictionary<string, Dictionary<string, string>> BuildTexts()
    {
        var english = English();
        return new(StringComparer.OrdinalIgnoreCase)
        {
            ["ko"] = Korean(),
            ["en"] = english,
            ["es"] = With(english, Spanish()),
            ["fr"] = With(english, French()),
            ["it"] = With(english, Italian()),
            ["pt"] = With(english, Portuguese()),
            ["ja"] = With(english, Japanese()),
            ["zh-hans"] = With(english, SimplifiedChinese()),
            ["zh-hant"] = With(english, TraditionalChinese()),
            ["vi"] = With(english, Vietnamese())
        };
    }

    private static Dictionary<string, string> With(
        Dictionary<string, string> source,
        Dictionary<string, string> overrides)
    {
        var result = new Dictionary<string, string>(source, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in overrides)
        {
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    private static Dictionary<string, string> Korean() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "언어", ["nav.home"] = "홈", ["nav.toggle"] = "메뉴 열기/닫기", ["contact"] = "문의하기", ["login"] = "로그인",
        ["theme.label"] = "화면 테마", ["theme.system"] = "시스템", ["theme.light"] = "라이트", ["theme.dark"] = "다크",
        ["screenSettings"] = "화면 설정", ["account"] = "내 계정", ["logout"] = "로그아웃",
        ["footer.games"] = "CodeMaru Games", ["footer.account"] = "하나의 계정 · 여러 게임 · 어디서나 계속",
        ["catalog.pageTitle"] = "CodeMaru Games | 게임 라이브러리", ["catalog.kicker"] = "하나의 계정, 계속 확장되는 세계",
        ["catalog.title"] = "다음 원정을 선택하세요.", ["catalog.lead"] = "설치 없이 즐기는 CodeMaru 게임 라이브러리입니다. 새로운 게임이 추가되어도 같은 계정으로 이어서 플레이할 수 있습니다.",
        ["catalog.browse"] = "게임 둘러보기", ["catalog.library"] = "게임 라이브러리", ["catalog.libraryLead"] = "각 게임의 진행 데이터는 독립적으로 저장되고, 로그인·언어·테마는 모든 CodeMaru 서비스가 함께 사용합니다.",
        ["catalog.live"] = "지금 플레이", ["catalog.play"] = "원정 시작", ["catalog.maru.genre"] = "방치형 · 무협 RPG", ["catalog.maru.desc"] = "동료를 모아 장비와 검술을 계승하고, 접속하지 않은 순간에도 성장하는 무협 방치 RPG.",
        ["catalog.coming"] = "새로운 세계 준비 중", ["catalog.coming.desc"] = "다음 게임은 독립된 진행 데이터와 함께 이 라이브러리에 계속 추가됩니다.",
        ["catalog.shared.title"] = "하나의 CodeMaru 계정", ["catalog.shared.desc"] = "게임이 달라도 로그인과 화면 설정은 그대로 이어집니다.",
        ["maru.pageTitle"] = "마루 원정대 | CodeMaru Games", ["maru.status"] = "서비스 중 · 서버 RPG", ["maru.original"] = "CODEMARU GAMES ORIGINAL",
        ["maru.title"] = "마루 원정대", ["maru.titleAccent"] = "마루", ["maru.titleRest"] = "원정대", ["maru.tagline"] = "달빛이 삼킨 세계,\n당신의 원정은 멈추지 않는다.",
        ["maru.description"] = "동료를 모아 끝없는 지역을 돌파하고, 검술과 장비를 계승하세요. 접속하지 않은 순간에도 원정대는 계속 성장합니다.",
        ["maru.start"] = "무료로 원정 시작", ["maru.continue"] = "원정 계속하기", ["maru.login"] = "로그인하고 시작", ["maru.local"] = "로컬 테스트 계정",
        ["maru.intro"] = "게임 소개", ["maru.fact.idle"] = "방치 성장", ["maru.fact.party"] = "원정 편성", ["maru.fact.trials"] = "시련 콘텐츠", ["maru.fact.save"] = "계정 저장",
        ["maru.palace"] = "일식의 황금 궁전", ["maru.caption"] = "황금 그림자를 추격하라", ["maru.scroll"] = "아래에서 더 보기",
        ["maru.worldKicker"] = "하나의 세계 · 끝없는 여정", ["maru.worldTitle"] = "성장할수록 더 넓어지는 원정",
        ["maru.worldLead"] = "전투, 동료, 시련, 농원이 하나의 성장으로 이어집니다. PC와 모바일 어디서든 같은 원정을 계속하세요.",
        ["maru.mainKicker"] = "주요 원정", ["maru.main"] = "끝없이 이어지는 실시간 원정", ["maru.mainDesc"] = "자동 전투와 직접 공격을 오가며 지역 보스와 정예 몬스터를 돌파하세요.", ["maru.play"] = "바로 플레이하기",
        ["maru.growthKicker"] = "영원한 성장", ["maru.growth"] = "계정에 남는 성장", ["maru.growthDesc"] = "검·검술·계승 장비가 모든 원정대를 강하게 만듭니다.",
        ["maru.trialKicker"] = "시련 회랑", ["maru.trials"] = "여섯 가지 시련", ["maru.trialsDesc"] = "생존, 질주, 수호, 농원, 성채와 탑에서 희귀 보상을 획득하세요.",
        ["maru.farmKicker"] = "영초 농원", ["maru.farm"] = "살아 움직이는 농원", ["maru.farmDesc"] = "영초를 재배하고 시설과 토양을 관리해 장기 성장 자원을 생산하세요.",
        ["maru.ctaKicker"] = "당신의 원정이 기다립니다", ["maru.ready"] = "준비됐나요?\n첫 번째 길을 여세요.", ["maru.enter"] = "마루 원정대 입장"
    };

    private static Dictionary<string, string> Spanish() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "Idioma", ["nav.home"] = "Inicio", ["nav.toggle"] = "Abrir o cerrar el menú", ["contact"] = "Contacto", ["login"] = "Iniciar sesión",
        ["theme.label"] = "Tema", ["theme.system"] = "Sistema", ["theme.light"] = "Claro", ["theme.dark"] = "Oscuro", ["screenSettings"] = "Ajustes de pantalla",
        ["account"] = "Mi cuenta", ["logout"] = "Cerrar sesión",
        ["catalog.pageTitle"] = "CodeMaru Games | Biblioteca de juegos", ["catalog.kicker"] = "UNA CUENTA · MUNDOS EN EXPANSIÓN", ["catalog.title"] = "Elige tu próxima expedición.",
        ["catalog.lead"] = "Juega a los títulos de CodeMaru sin instalar nada. Los nuevos juegos comparten la misma biblioteca, cuenta y experiencia entre dispositivos.",
        ["catalog.browse"] = "Explorar juegos", ["catalog.library"] = "Biblioteca de juegos", ["catalog.libraryLead"] = "Cada juego conserva su progreso, mientras el inicio de sesión, el idioma y el tema se comparten entre los servicios de CodeMaru.",
        ["catalog.live"] = "JUGAR AHORA", ["catalog.play"] = "Iniciar expedición", ["catalog.maru.genre"] = "Idle · RPG wuxia", ["catalog.maru.desc"] = "Recluta compañeros, hereda equipo y artes de espada, y sigue creciendo incluso cuando no estás.",
        ["catalog.coming"] = "Un nuevo mundo está en preparación", ["catalog.coming.desc"] = "Los próximos juegos se añadirán a esta biblioteca con su propio progreso independiente.",
        ["catalog.shared.title"] = "Una cuenta de CodeMaru", ["catalog.shared.desc"] = "Tu inicio de sesión y tus preferencias de pantalla te acompañan entre juegos.",
        ["maru.pageTitle"] = "Expedición Maru | CodeMaru Games", ["maru.status"] = "EN SERVICIO · RPG DE SERVIDOR", ["maru.original"] = "ORIGINAL DE CODEMARU GAMES",
        ["maru.title"] = "Expedición Maru", ["maru.titleAccent"] = "Maru", ["maru.titleRest"] = "Expedición", ["maru.tagline"] = "Un mundo devorado por la luz de la luna.\nTu expedición nunca se detiene.",
        ["maru.description"] = "Recluta compañeros, conquista regiones sin fin y hereda artes de espada y equipo. Tu grupo sigue creciendo incluso cuando no estás.",
        ["maru.start"] = "Empezar gratis", ["maru.continue"] = "Continuar expedición", ["maru.login"] = "Inicia sesión para empezar", ["maru.local"] = "Cuenta de prueba local", ["maru.intro"] = "Acerca del juego",
        ["maru.fact.idle"] = "Progreso inactivo", ["maru.fact.party"] = "Formación de grupo", ["maru.fact.trials"] = "Modos de prueba", ["maru.fact.save"] = "Guardado en cuenta",
        ["maru.palace"] = "EL PALACIO ECLIPSADO", ["maru.caption"] = "Persigue la sombra dorada", ["maru.scroll"] = "DESLIZA PARA EXPLORAR",
        ["maru.worldKicker"] = "UN MUNDO · UN VIAJE SIN FIN", ["maru.worldTitle"] = "Una expedición más amplia con cada paso", ["maru.worldLead"] = "Combate, compañeros, pruebas y cultivo alimentan un único viaje. Continúa en PC o móvil con la misma cuenta.",
        ["maru.mainKicker"] = "EXPEDICIÓN PRINCIPAL", ["maru.main"] = "Una expedición en tiempo real sin fin", ["maru.mainDesc"] = "Alterna el combate automático y los ataques directos para vencer élites y jefes regionales.", ["maru.play"] = "Jugar ahora",
        ["maru.growthKicker"] = "CRECIMIENTO ETERNO", ["maru.growth"] = "Progreso que permanece en tu cuenta", ["maru.growthDesc"] = "La espada, sus artes y el equipo heredado fortalecen a toda la expedición.",
        ["maru.trialKicker"] = "GALERÍA DE PRUEBAS", ["maru.trials"] = "Seis pruebas distintas", ["maru.trialsDesc"] = "Obtén recompensas raras en desafíos de supervivencia, carrera, defensa, cultivo, fortaleza y torre.",
        ["maru.farmKicker"] = "GRANJA ESPIRITUAL", ["maru.farm"] = "Una granja espiritual viva", ["maru.farmDesc"] = "Cultiva hierbas espirituales y gestiona instalaciones y suelo para obtener recursos a largo plazo.",
        ["maru.ctaKicker"] = "TU EXPEDICIÓN TE ESPERA", ["maru.ready"] = "¿Estás listo?\nAbre el primer camino.", ["maru.enter"] = "Entrar en Expedición Maru"
    };

    private static Dictionary<string, string> French() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "Langue", ["nav.home"] = "Accueil", ["nav.toggle"] = "Ouvrir ou fermer le menu", ["contact"] = "Contact", ["login"] = "Connexion",
        ["theme.label"] = "Thème", ["theme.system"] = "Système", ["theme.light"] = "Clair", ["theme.dark"] = "Sombre", ["screenSettings"] = "Réglages d’affichage",
        ["account"] = "Mon compte", ["logout"] = "Déconnexion",
        ["catalog.pageTitle"] = "CodeMaru Games | Bibliothèque de jeux", ["catalog.kicker"] = "UN COMPTE · DES MONDES EN EXPANSION", ["catalog.title"] = "Choisissez votre prochaine expédition.",
        ["catalog.lead"] = "Jouez aux jeux CodeMaru sans installation. Les nouveaux titres rejoignent la même bibliothèque, le même compte et la même expérience multiappareil.",
        ["catalog.browse"] = "Voir les jeux", ["catalog.library"] = "Bibliothèque de jeux", ["catalog.libraryLead"] = "Chaque jeu conserve sa progression, tandis que la connexion, la langue et le thème sont partagés entre les services CodeMaru.",
        ["catalog.live"] = "JOUER", ["catalog.play"] = "Lancer l’expédition", ["catalog.maru.genre"] = "Idle · RPG wuxia", ["catalog.maru.desc"] = "Recrutez des compagnons, héritez d’équipements et d’arts de l’épée, et progressez même en votre absence.",
        ["catalog.coming"] = "Un nouveau monde se prépare", ["catalog.coming.desc"] = "Les prochains jeux rejoindront cette bibliothèque avec leur propre progression.",
        ["catalog.shared.title"] = "Un compte CodeMaru", ["catalog.shared.desc"] = "Votre connexion et vos préférences d’affichage vous suivent d’un jeu à l’autre.",
        ["maru.pageTitle"] = "Expédition Maru | CodeMaru Games", ["maru.status"] = "EN LIGNE · RPG SERVEUR", ["maru.original"] = "UNE CRÉATION CODEMARU GAMES",
        ["maru.title"] = "Expédition Maru", ["maru.titleAccent"] = "Maru", ["maru.titleRest"] = "Expédition", ["maru.tagline"] = "Un monde englouti par le clair de lune.\nVotre expédition ne s’arrête jamais.",
        ["maru.description"] = "Recrutez des compagnons, conquérez des régions sans fin et héritez d’arts de l’épée et d’équipements. Votre groupe progresse même en votre absence.",
        ["maru.start"] = "Commencer gratuitement", ["maru.continue"] = "Continuer l’expédition", ["maru.login"] = "Connectez-vous pour commencer", ["maru.local"] = "Compte de test local", ["maru.intro"] = "À propos du jeu",
        ["maru.fact.idle"] = "Progression inactive", ["maru.fact.party"] = "Formation du groupe", ["maru.fact.trials"] = "Modes d’épreuve", ["maru.fact.save"] = "Sauvegarde du compte",
        ["maru.palace"] = "LE PALAIS ÉCLIPSÉ", ["maru.caption"] = "Pourchassez l’ombre dorée", ["maru.scroll"] = "FAITES DÉFILER POUR EXPLORER",
        ["maru.worldKicker"] = "UN MONDE · UN VOYAGE SANS FIN", ["maru.worldTitle"] = "Une expédition plus vaste à chaque pas", ["maru.worldLead"] = "Combat, compagnons, épreuves et culture nourrissent un même voyage. Continuez sur PC ou mobile avec le même compte.",
        ["maru.mainKicker"] = "EXPÉDITION PRINCIPALE", ["maru.main"] = "Une expédition en temps réel sans fin", ["maru.mainDesc"] = "Alternez combat automatique et attaques directes pour vaincre les élites et les boss régionaux.", ["maru.play"] = "Jouer",
        ["maru.growthKicker"] = "PROGRESSION ÉTERNELLE", ["maru.growth"] = "Une progression liée à votre compte", ["maru.growthDesc"] = "L’épée, ses arts et l’équipement hérité renforcent toute l’expédition.",
        ["maru.trialKicker"] = "GALERIE DES ÉPREUVES", ["maru.trials"] = "Six épreuves distinctes", ["maru.trialsDesc"] = "Gagnez des récompenses rares en survie, course, défense, culture, forteresse et tour.",
        ["maru.farmKicker"] = "FERME SPIRITUELLE", ["maru.farm"] = "Une ferme spirituelle vivante", ["maru.farmDesc"] = "Cultivez des herbes spirituelles et gérez les installations et le sol pour obtenir des ressources durables.",
        ["maru.ctaKicker"] = "VOTRE EXPÉDITION VOUS ATTEND", ["maru.ready"] = "Êtes-vous prêt ?\nOuvrez la première voie.", ["maru.enter"] = "Entrer dans Expédition Maru"
    };

    private static Dictionary<string, string> Italian() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "Lingua", ["nav.home"] = "Home", ["nav.toggle"] = "Apri o chiudi il menu", ["contact"] = "Contatti", ["login"] = "Accedi",
        ["theme.label"] = "Tema", ["theme.system"] = "Sistema", ["theme.light"] = "Chiaro", ["theme.dark"] = "Scuro", ["screenSettings"] = "Impostazioni schermo",
        ["account"] = "Il mio account", ["logout"] = "Esci",
        ["catalog.pageTitle"] = "CodeMaru Games | Libreria giochi", ["catalog.kicker"] = "UN ACCOUNT · MONDI IN ESPANSIONE", ["catalog.title"] = "Scegli la tua prossima spedizione.",
        ["catalog.lead"] = "Gioca ai titoli CodeMaru senza installazione. I nuovi giochi condividono la stessa libreria, lo stesso account e l’esperienza tra dispositivi.",
        ["catalog.browse"] = "Scopri i giochi", ["catalog.library"] = "Libreria giochi", ["catalog.libraryLead"] = "Ogni gioco mantiene i propri progressi, mentre accesso, lingua e tema sono condivisi tra i servizi CodeMaru.",
        ["catalog.live"] = "GIOCA ORA", ["catalog.play"] = "Inizia la spedizione", ["catalog.maru.genre"] = "Idle · GDR wuxia", ["catalog.maru.desc"] = "Recluta compagni, eredita equipaggiamenti e arti della spada e continua a crescere anche quando sei assente.",
        ["catalog.coming"] = "Un nuovo mondo è in preparazione", ["catalog.coming.desc"] = "I prossimi giochi entreranno in questa libreria con progressi indipendenti.",
        ["catalog.shared.title"] = "Un account CodeMaru", ["catalog.shared.desc"] = "Accesso e preferenze di visualizzazione ti seguono tra i giochi.",
        ["maru.pageTitle"] = "Spedizione Maru | CodeMaru Games", ["maru.status"] = "ONLINE · GDR SU SERVER", ["maru.original"] = "UN’OPERA ORIGINALE CODEMARU GAMES",
        ["maru.title"] = "Spedizione Maru", ["maru.titleAccent"] = "Maru", ["maru.titleRest"] = "Spedizione", ["maru.tagline"] = "Un mondo inghiottito dalla luce lunare.\nLa tua spedizione non si ferma mai.",
        ["maru.description"] = "Recluta compagni, conquista regioni infinite ed eredita arti della spada ed equipaggiamenti. Il gruppo cresce anche mentre sei assente.",
        ["maru.start"] = "Inizia gratis", ["maru.continue"] = "Continua la spedizione", ["maru.login"] = "Accedi per iniziare", ["maru.local"] = "Account di test locale", ["maru.intro"] = "Informazioni sul gioco",
        ["maru.fact.idle"] = "Crescita inattiva", ["maru.fact.party"] = "Formazione gruppo", ["maru.fact.trials"] = "Modalità prova", ["maru.fact.save"] = "Salvataggio account",
        ["maru.palace"] = "IL PALAZZO ECLISSATO", ["maru.caption"] = "Insegui l’ombra dorata", ["maru.scroll"] = "SCORRI PER ESPLORARE",
        ["maru.worldKicker"] = "UN MONDO · UN VIAGGIO INFINITO", ["maru.worldTitle"] = "Una spedizione più vasta a ogni passo", ["maru.worldLead"] = "Combattimento, compagni, prove e coltivazione alimentano un unico viaggio. Continua su PC o mobile con lo stesso account.",
        ["maru.mainKicker"] = "SPEDIZIONE PRINCIPALE", ["maru.main"] = "Una spedizione in tempo reale senza fine", ["maru.mainDesc"] = "Alterna combattimento automatico e attacchi diretti per superare élite e boss regionali.", ["maru.play"] = "Gioca ora",
        ["maru.growthKicker"] = "CRESCITA ETERNA", ["maru.growth"] = "Progressi legati al tuo account", ["maru.growthDesc"] = "Spada, arti della spada ed equipaggiamento ereditato rafforzano l’intera spedizione.",
        ["maru.trialKicker"] = "GALLERIA DELLE PROVE", ["maru.trials"] = "Sei prove distinte", ["maru.trialsDesc"] = "Ottieni ricompense rare in sfide di sopravvivenza, corsa, difesa, coltivazione, fortezza e torre.",
        ["maru.farmKicker"] = "FATTORIA SPIRITUALE", ["maru.farm"] = "Una fattoria spirituale viva", ["maru.farmDesc"] = "Coltiva erbe spirituali e gestisci strutture e terreno per produrre risorse a lungo termine.",
        ["maru.ctaKicker"] = "LA TUA SPEDIZIONE TI ATTENDE", ["maru.ready"] = "Sei pronto?\nApri il primo sentiero.", ["maru.enter"] = "Entra in Spedizione Maru"
    };

    private static Dictionary<string, string> Portuguese() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "Idioma", ["nav.home"] = "Início", ["nav.toggle"] = "Abrir ou fechar o menu", ["contact"] = "Contato", ["login"] = "Entrar",
        ["theme.label"] = "Tema", ["theme.system"] = "Sistema", ["theme.light"] = "Claro", ["theme.dark"] = "Escuro", ["screenSettings"] = "Configurações de tela",
        ["account"] = "Minha conta", ["logout"] = "Sair",
        ["catalog.pageTitle"] = "CodeMaru Games | Biblioteca de jogos", ["catalog.kicker"] = "UMA CONTA · MUNDOS EM EXPANSÃO", ["catalog.title"] = "Escolha sua próxima expedição.",
        ["catalog.lead"] = "Jogue os títulos CodeMaru sem instalar. Novos jogos compartilham a mesma biblioteca, conta e experiência entre dispositivos.",
        ["catalog.browse"] = "Explorar jogos", ["catalog.library"] = "Biblioteca de jogos", ["catalog.libraryLead"] = "Cada jogo mantém seu próprio progresso, enquanto login, idioma e tema são compartilhados entre os serviços CodeMaru.",
        ["catalog.live"] = "JOGAR AGORA", ["catalog.play"] = "Iniciar expedição", ["catalog.maru.genre"] = "Idle · RPG wuxia", ["catalog.maru.desc"] = "Recrute companheiros, herde equipamentos e artes da espada e continue evoluindo mesmo enquanto estiver ausente.",
        ["catalog.coming"] = "Um novo mundo está sendo preparado", ["catalog.coming.desc"] = "Os próximos jogos entrarão nesta biblioteca com progressão independente.",
        ["catalog.shared.title"] = "Uma conta CodeMaru", ["catalog.shared.desc"] = "Seu login e suas preferências de tela acompanham você entre os jogos.",
        ["maru.pageTitle"] = "Expedição Maru | CodeMaru Games", ["maru.status"] = "ONLINE · RPG EM SERVIDOR", ["maru.original"] = "ORIGINAL CODEMARU GAMES",
        ["maru.title"] = "Expedição Maru", ["maru.titleAccent"] = "Maru", ["maru.titleRest"] = "Expedição", ["maru.tagline"] = "Um mundo engolido pelo luar.\nSua expedição nunca para.",
        ["maru.description"] = "Recrute companheiros, conquiste regiões sem fim e herde artes da espada e equipamentos. Seu grupo continua crescendo mesmo quando você está ausente.",
        ["maru.start"] = "Começar grátis", ["maru.continue"] = "Continuar expedição", ["maru.login"] = "Entre para começar", ["maru.local"] = "Conta de teste local", ["maru.intro"] = "Sobre o jogo",
        ["maru.fact.idle"] = "Evolução ociosa", ["maru.fact.party"] = "Formação do grupo", ["maru.fact.trials"] = "Modos de desafio", ["maru.fact.save"] = "Salvo na conta",
        ["maru.palace"] = "O PALÁCIO ECLIPSADO", ["maru.caption"] = "Persiga a sombra dourada", ["maru.scroll"] = "ROLE PARA EXPLORAR",
        ["maru.worldKicker"] = "UM MUNDO · UMA JORNADA SEM FIM", ["maru.worldTitle"] = "Uma expedição maior a cada passo", ["maru.worldLead"] = "Combate, companheiros, desafios e cultivo alimentam uma só jornada. Continue no PC ou celular com a mesma conta.",
        ["maru.mainKicker"] = "EXPEDIÇÃO PRINCIPAL", ["maru.main"] = "Uma expedição em tempo real sem fim", ["maru.mainDesc"] = "Alterne entre combate automático e ataques diretos para vencer elites e chefes regionais.", ["maru.play"] = "Jogar agora",
        ["maru.growthKicker"] = "EVOLUÇÃO ETERNA", ["maru.growth"] = "Evolução que fica na sua conta", ["maru.growthDesc"] = "Espada, artes da espada e equipamentos herdados fortalecem toda a expedição.",
        ["maru.trialKicker"] = "GALERIA DE DESAFIOS", ["maru.trials"] = "Seis desafios distintos", ["maru.trialsDesc"] = "Ganhe recompensas raras em sobrevivência, corrida, defesa, cultivo, fortaleza e torre.",
        ["maru.farmKicker"] = "FAZENDA ESPIRITUAL", ["maru.farm"] = "Uma fazenda espiritual viva", ["maru.farmDesc"] = "Cultive ervas espirituais e gerencie instalações e solo para produzir recursos de longo prazo.",
        ["maru.ctaKicker"] = "SUA EXPEDIÇÃO AGUARDA", ["maru.ready"] = "Você está pronto?\nAbra o primeiro caminho.", ["maru.enter"] = "Entrar na Expedição Maru"
    };

    private static Dictionary<string, string> Japanese() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "言語", ["nav.home"] = "ホーム", ["nav.toggle"] = "メニューを開閉", ["contact"] = "お問い合わせ", ["login"] = "ログイン",
        ["theme.label"] = "テーマ", ["theme.system"] = "システム", ["theme.light"] = "ライト", ["theme.dark"] = "ダーク", ["screenSettings"] = "表示設定", ["account"] = "マイアカウント", ["logout"] = "ログアウト",
        ["catalog.pageTitle"] = "CodeMaru Games｜ゲームライブラリ", ["catalog.kicker"] = "ひとつのアカウント・広がり続ける世界", ["catalog.title"] = "次の遠征を選ぼう。",
        ["catalog.lead"] = "CodeMaruのゲームをインストールなしで楽しめます。新作も同じライブラリとアカウントで、端末を越えて続けられます。",
        ["catalog.browse"] = "ゲームを見る", ["catalog.library"] = "ゲームライブラリ", ["catalog.libraryLead"] = "ゲームごとに進行データを保存し、ログイン・言語・テーマはCodeMaruの全サービスで共有します。",
        ["catalog.live"] = "プレイ中", ["catalog.play"] = "遠征を始める", ["catalog.maru.genre"] = "放置・武侠RPG", ["catalog.maru.desc"] = "仲間を集め、装備と剣術を継承しながら、離れている間も成長する武侠放置RPG。",
        ["catalog.coming"] = "新しい世界を準備中", ["catalog.coming.desc"] = "今後のゲームも独立した進行データとともに、このライブラリへ追加されます。",
        ["catalog.shared.title"] = "ひとつのCodeMaruアカウント", ["catalog.shared.desc"] = "ログインと表示設定はゲームを越えて引き継がれます。",
        ["maru.pageTitle"] = "マル遠征隊｜CodeMaru Games", ["maru.status"] = "サービス中・サーバーRPG", ["maru.original"] = "CODEMARU GAMES オリジナル",
        ["maru.title"] = "マル遠征隊", ["maru.titleAccent"] = "マル", ["maru.titleRest"] = "遠征隊", ["maru.tagline"] = "月明かりに呑まれた世界。\nあなたの遠征は止まらない。",
        ["maru.description"] = "仲間を集め、果てしない地域を制覇し、剣術と装備を継承しよう。離れている間も遠征隊は成長し続けます。",
        ["maru.start"] = "無料で始める", ["maru.continue"] = "遠征を続ける", ["maru.login"] = "ログインして始める", ["maru.local"] = "ローカルテストアカウント", ["maru.intro"] = "ゲーム紹介",
        ["maru.fact.idle"] = "放置成長", ["maru.fact.party"] = "遠征編成", ["maru.fact.trials"] = "試練モード", ["maru.fact.save"] = "アカウント保存",
        ["maru.palace"] = "蝕まれた黄金宮", ["maru.caption"] = "黄金の影を追え", ["maru.scroll"] = "スクロールして探索",
        ["maru.worldKicker"] = "ひとつの世界・果てなき旅", ["maru.worldTitle"] = "進むほど広がる遠征", ["maru.worldLead"] = "戦闘、仲間、試練、農園がひとつの成長へつながります。同じアカウントでPCでもモバイルでも続けられます。",
        ["maru.mainKicker"] = "メイン遠征", ["maru.main"] = "終わりなく続くリアルタイム遠征", ["maru.mainDesc"] = "自動戦闘と直接攻撃を切り替え、精鋭モンスターと地域ボスを突破しよう。", ["maru.play"] = "今すぐプレイ",
        ["maru.growthKicker"] = "永遠の成長", ["maru.growth"] = "アカウントに残る成長", ["maru.growthDesc"] = "剣、剣術、継承装備が遠征隊全体を強くします。",
        ["maru.trialKicker"] = "試練回廊", ["maru.trials"] = "六つの異なる試練", ["maru.trialsDesc"] = "生存、疾走、防衛、農園、城塞、塔の挑戦で希少報酬を獲得しよう。",
        ["maru.farmKicker"] = "霊草農園", ["maru.farm"] = "息づく霊草農園", ["maru.farmDesc"] = "霊草を育て、施設と土壌を管理して長期成長の資源を生産します。",
        ["maru.ctaKicker"] = "遠征があなたを待っている", ["maru.ready"] = "準備はいい？\n最初の道を開こう。", ["maru.enter"] = "マル遠征隊へ"
    };

    private static Dictionary<string, string> SimplifiedChinese() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "语言", ["nav.home"] = "首页", ["nav.toggle"] = "打开或关闭菜单", ["contact"] = "联系我们", ["login"] = "登录",
        ["theme.label"] = "主题", ["theme.system"] = "系统", ["theme.light"] = "浅色", ["theme.dark"] = "深色", ["screenSettings"] = "显示设置", ["account"] = "我的账户", ["logout"] = "退出登录",
        ["catalog.pageTitle"] = "CodeMaru Games｜游戏库", ["catalog.kicker"] = "一个账户・不断扩展的世界", ["catalog.title"] = "选择你的下一场远征。",
        ["catalog.lead"] = "无需安装即可畅玩CodeMaru游戏。新作也会加入同一游戏库，并可用同一账户跨设备继续。",
        ["catalog.browse"] = "浏览游戏", ["catalog.library"] = "游戏库", ["catalog.libraryLead"] = "各游戏独立保存进度，而登录、语言和主题在所有CodeMaru服务间共享。",
        ["catalog.live"] = "立即游玩", ["catalog.play"] = "开始远征", ["catalog.maru.genre"] = "放置・武侠RPG", ["catalog.maru.desc"] = "召集伙伴、继承装备与剑术，即使离线也持续成长的武侠放置RPG。",
        ["catalog.coming"] = "新世界筹备中", ["catalog.coming.desc"] = "未来游戏将带着独立进度加入此游戏库。",
        ["catalog.shared.title"] = "一个CodeMaru账户", ["catalog.shared.desc"] = "你的登录与显示偏好会在不同游戏间沿用。",
        ["maru.pageTitle"] = "玛鲁远征队｜CodeMaru Games", ["maru.status"] = "运营中・服务器RPG", ["maru.original"] = "CODEMARU GAMES 原创",
        ["maru.title"] = "玛鲁远征队", ["maru.titleAccent"] = "玛鲁", ["maru.titleRest"] = "远征队", ["maru.tagline"] = "被月光吞没的世界。\n你的远征永不停歇。",
        ["maru.description"] = "召集伙伴，征服无尽区域，继承剑术与装备。即使你不在线，远征队也会继续成长。",
        ["maru.start"] = "免费开始", ["maru.continue"] = "继续远征", ["maru.login"] = "登录后开始", ["maru.local"] = "本地测试账户", ["maru.intro"] = "游戏介绍",
        ["maru.fact.idle"] = "放置成长", ["maru.fact.party"] = "远征编队", ["maru.fact.trials"] = "试炼模式", ["maru.fact.save"] = "账户保存",
        ["maru.palace"] = "蚀月黄金宫", ["maru.caption"] = "追击黄金之影", ["maru.scroll"] = "向下探索",
        ["maru.worldKicker"] = "一个世界・无尽旅程", ["maru.worldTitle"] = "每一步都让远征更加辽阔", ["maru.worldLead"] = "战斗、伙伴、试炼与农园汇成一段持续旅程。使用同一账户在电脑或手机上继续。",
        ["maru.mainKicker"] = "主线远征", ["maru.main"] = "永不终止的实时远征", ["maru.mainDesc"] = "在自动战斗与直接攻击间切换，突破精英怪物和区域首领。", ["maru.play"] = "立即游玩",
        ["maru.growthKicker"] = "永恒成长", ["maru.growth"] = "留存在账户中的成长", ["maru.growthDesc"] = "剑、剑术与传承装备会强化整支远征队。",
        ["maru.trialKicker"] = "试炼回廊", ["maru.trials"] = "六种不同试炼", ["maru.trialsDesc"] = "在生存、竞速、防守、农园、要塞和高塔挑战中赢取稀有奖励。",
        ["maru.farmKicker"] = "灵草农园", ["maru.farm"] = "生机勃勃的灵草农园", ["maru.farmDesc"] = "种植灵草并管理设施与土壤，生产长期成长资源。",
        ["maru.ctaKicker"] = "你的远征正在等待", ["maru.ready"] = "准备好了吗？\n开启第一条道路。", ["maru.enter"] = "进入玛鲁远征队"
    };

    private static Dictionary<string, string> TraditionalChinese() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "語言", ["nav.home"] = "首頁", ["nav.toggle"] = "開啟或關閉選單", ["contact"] = "聯絡我們", ["login"] = "登入",
        ["theme.label"] = "主題", ["theme.system"] = "系統", ["theme.light"] = "淺色", ["theme.dark"] = "深色", ["screenSettings"] = "顯示設定", ["account"] = "我的帳戶", ["logout"] = "登出",
        ["catalog.pageTitle"] = "CodeMaru Games｜遊戲庫", ["catalog.kicker"] = "一個帳戶・不斷擴展的世界", ["catalog.title"] = "選擇你的下一場遠征。",
        ["catalog.lead"] = "無需安裝即可暢玩CodeMaru遊戲。新作也會加入同一遊戲庫，並可用同一帳戶跨裝置繼續。",
        ["catalog.browse"] = "瀏覽遊戲", ["catalog.library"] = "遊戲庫", ["catalog.libraryLead"] = "各遊戲獨立儲存進度，而登入、語言與主題會在所有CodeMaru服務間共享。",
        ["catalog.live"] = "立即遊玩", ["catalog.play"] = "開始遠征", ["catalog.maru.genre"] = "放置・武俠RPG", ["catalog.maru.desc"] = "召集夥伴、繼承裝備與劍術，即使離線也持續成長的武俠放置RPG。",
        ["catalog.coming"] = "新世界籌備中", ["catalog.coming.desc"] = "未來遊戲將帶著獨立進度加入此遊戲庫。",
        ["catalog.shared.title"] = "一個CodeMaru帳戶", ["catalog.shared.desc"] = "你的登入與顯示偏好會在不同遊戲間沿用。",
        ["maru.pageTitle"] = "瑪魯遠征隊｜CodeMaru Games", ["maru.status"] = "營運中・伺服器RPG", ["maru.original"] = "CODEMARU GAMES 原創",
        ["maru.title"] = "瑪魯遠征隊", ["maru.titleAccent"] = "瑪魯", ["maru.titleRest"] = "遠征隊", ["maru.tagline"] = "被月光吞沒的世界。\n你的遠征永不停歇。",
        ["maru.description"] = "召集夥伴，征服無盡區域，繼承劍術與裝備。即使你不在線，遠征隊也會繼續成長。",
        ["maru.start"] = "免費開始", ["maru.continue"] = "繼續遠征", ["maru.login"] = "登入後開始", ["maru.local"] = "本機測試帳戶", ["maru.intro"] = "遊戲介紹",
        ["maru.fact.idle"] = "放置成長", ["maru.fact.party"] = "遠征編隊", ["maru.fact.trials"] = "試煉模式", ["maru.fact.save"] = "帳戶儲存",
        ["maru.palace"] = "蝕月黃金宮", ["maru.caption"] = "追擊黃金之影", ["maru.scroll"] = "向下探索",
        ["maru.worldKicker"] = "一個世界・無盡旅程", ["maru.worldTitle"] = "每一步都讓遠征更加遼闊", ["maru.worldLead"] = "戰鬥、夥伴、試煉與農園匯成一段持續旅程。使用同一帳戶在電腦或手機上繼續。",
        ["maru.mainKicker"] = "主線遠征", ["maru.main"] = "永不終止的即時遠征", ["maru.mainDesc"] = "在自動戰鬥與直接攻擊間切換，突破精英怪物與區域首領。", ["maru.play"] = "立即遊玩",
        ["maru.growthKicker"] = "永恆成長", ["maru.growth"] = "留存在帳戶中的成長", ["maru.growthDesc"] = "劍、劍術與傳承裝備會強化整支遠征隊。",
        ["maru.trialKicker"] = "試煉迴廊", ["maru.trials"] = "六種不同試煉", ["maru.trialsDesc"] = "在生存、競速、防守、農園、要塞與高塔挑戰中贏取稀有獎勵。",
        ["maru.farmKicker"] = "靈草農園", ["maru.farm"] = "生機勃勃的靈草農園", ["maru.farmDesc"] = "種植靈草並管理設施與土壤，生產長期成長資源。",
        ["maru.ctaKicker"] = "你的遠征正在等待", ["maru.ready"] = "準備好了嗎？\n開啟第一條道路。", ["maru.enter"] = "進入瑪魯遠征隊"
    };

    private static Dictionary<string, string> Vietnamese() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "Ngôn ngữ", ["nav.home"] = "Trang chủ", ["nav.toggle"] = "Mở hoặc đóng menu", ["contact"] = "Liên hệ", ["login"] = "Đăng nhập",
        ["theme.label"] = "Giao diện", ["theme.system"] = "Hệ thống", ["theme.light"] = "Sáng", ["theme.dark"] = "Tối", ["screenSettings"] = "Cài đặt hiển thị", ["account"] = "Tài khoản", ["logout"] = "Đăng xuất",
        ["catalog.pageTitle"] = "CodeMaru Games | Thư viện trò chơi", ["catalog.kicker"] = "MỘT TÀI KHOẢN · THẾ GIỚI KHÔNG NGỪNG MỞ RỘNG", ["catalog.title"] = "Chọn chuyến viễn chinh tiếp theo.",
        ["catalog.lead"] = "Chơi các trò chơi CodeMaru mà không cần cài đặt. Trò chơi mới dùng chung thư viện, tài khoản và trải nghiệm đa thiết bị.",
        ["catalog.browse"] = "Khám phá trò chơi", ["catalog.library"] = "Thư viện trò chơi", ["catalog.libraryLead"] = "Mỗi trò chơi lưu tiến trình riêng, còn đăng nhập, ngôn ngữ và giao diện được dùng chung trên các dịch vụ CodeMaru.",
        ["catalog.live"] = "CHƠI NGAY", ["catalog.play"] = "Bắt đầu viễn chinh", ["catalog.maru.genre"] = "Nhàn rỗi · RPG võ hiệp", ["catalog.maru.desc"] = "Chiêu mộ đồng đội, kế thừa trang bị và kiếm pháp, tiếp tục trưởng thành ngay cả khi bạn vắng mặt.",
        ["catalog.coming"] = "Một thế giới mới đang được chuẩn bị", ["catalog.coming.desc"] = "Các trò chơi tương lai sẽ gia nhập thư viện với tiến trình độc lập.",
        ["catalog.shared.title"] = "Một tài khoản CodeMaru", ["catalog.shared.desc"] = "Thông tin đăng nhập và tùy chọn hiển thị đi theo bạn giữa các trò chơi.",
        ["maru.pageTitle"] = "Viễn chinh Maru | CodeMaru Games", ["maru.status"] = "ĐANG HOẠT ĐỘNG · RPG MÁY CHỦ", ["maru.original"] = "BẢN GỐC CODEMARU GAMES",
        ["maru.title"] = "Viễn chinh Maru", ["maru.titleAccent"] = "Maru", ["maru.titleRest"] = "Viễn chinh", ["maru.tagline"] = "Thế giới bị ánh trăng nuốt chửng.\nChuyến viễn chinh của bạn không bao giờ dừng lại.",
        ["maru.description"] = "Chiêu mộ đồng đội, chinh phục vô số vùng đất, kế thừa kiếm pháp và trang bị. Đội hình vẫn tiếp tục trưởng thành khi bạn vắng mặt.",
        ["maru.start"] = "Bắt đầu miễn phí", ["maru.continue"] = "Tiếp tục viễn chinh", ["maru.login"] = "Đăng nhập để bắt đầu", ["maru.local"] = "Tài khoản thử nghiệm cục bộ", ["maru.intro"] = "Giới thiệu trò chơi",
        ["maru.fact.idle"] = "Tăng trưởng nhàn rỗi", ["maru.fact.party"] = "Lập đội viễn chinh", ["maru.fact.trials"] = "Chế độ thử thách", ["maru.fact.save"] = "Lưu theo tài khoản",
        ["maru.palace"] = "CUNG ĐIỆN NHẬT THỰC", ["maru.caption"] = "Truy đuổi bóng vàng", ["maru.scroll"] = "CUỘN ĐỂ KHÁM PHÁ",
        ["maru.worldKicker"] = "MỘT THẾ GIỚI · HÀNH TRÌNH BẤT TẬN", ["maru.worldTitle"] = "Mỗi bước mở rộng chuyến viễn chinh", ["maru.worldLead"] = "Chiến đấu, đồng đội, thử thách và nông trại hòa vào một hành trình liên tục. Tiếp tục trên PC hoặc điện thoại bằng cùng tài khoản.",
        ["maru.mainKicker"] = "VIỄN CHINH CHÍNH", ["maru.main"] = "Chuyến viễn chinh thời gian thực bất tận", ["maru.mainDesc"] = "Chuyển đổi giữa chiến đấu tự động và tấn công trực tiếp để đánh bại tinh anh và boss khu vực.", ["maru.play"] = "Chơi ngay",
        ["maru.growthKicker"] = "TRƯỞNG THÀNH VĨNH CỬU", ["maru.growth"] = "Sức mạnh lưu lại trong tài khoản", ["maru.growthDesc"] = "Kiếm, kiếm pháp và trang bị kế thừa tăng sức mạnh cho toàn bộ đội viễn chinh.",
        ["maru.trialKicker"] = "HÀNH LANG THỬ THÁCH", ["maru.trials"] = "Sáu thử thách khác biệt", ["maru.trialsDesc"] = "Nhận phần thưởng hiếm qua sinh tồn, đua tốc độ, phòng thủ, nông trại, pháo đài và tháp.",
        ["maru.farmKicker"] = "NÔNG TRẠI LINH THẢO", ["maru.farm"] = "Nông trại linh thảo sống động", ["maru.farmDesc"] = "Trồng linh thảo, quản lý cơ sở và đất để tạo tài nguyên phát triển lâu dài.",
        ["maru.ctaKicker"] = "CHUYẾN VIỄN CHINH ĐANG CHỜ", ["maru.ready"] = "Bạn đã sẵn sàng?\nMở con đường đầu tiên.", ["maru.enter"] = "Vào Viễn chinh Maru"
    };

    private static Dictionary<string, string> English() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["language"] = "Language", ["nav.home"] = "Home", ["nav.toggle"] = "Open or close menu", ["contact"] = "Contact", ["login"] = "Sign in",
        ["theme.label"] = "Theme", ["theme.system"] = "System", ["theme.light"] = "Light", ["theme.dark"] = "Dark",
        ["screenSettings"] = "Display settings", ["account"] = "My account", ["logout"] = "Sign out",
        ["footer.games"] = "CodeMaru Games", ["footer.account"] = "One account · Multiple games · Continue anywhere",
        ["catalog.pageTitle"] = "CodeMaru Games | Game Library", ["catalog.kicker"] = "ONE ACCOUNT · EVER-EXPANDING WORLDS",
        ["catalog.title"] = "Choose your next expedition.", ["catalog.lead"] = "Play CodeMaru games without an install. New titles join the same library, account, and cross-device experience.",
        ["catalog.browse"] = "Explore games", ["catalog.library"] = "Game library", ["catalog.libraryLead"] = "Each game keeps independent progress while sign-in, language, and theme are shared across CodeMaru services.",
        ["catalog.live"] = "PLAY NOW", ["catalog.play"] = "Start expedition", ["catalog.maru.genre"] = "Idle · Wuxia RPG", ["catalog.maru.desc"] = "Recruit companions, inherit equipment and sword arts, and keep growing while you are away in a wuxia idle RPG.",
        ["catalog.coming"] = "A new world is being prepared", ["catalog.coming.desc"] = "Future games will join this library with their own independent progression data.",
        ["catalog.shared.title"] = "One CodeMaru account", ["catalog.shared.desc"] = "Your sign-in and display preferences follow you between games.",
        ["maru.pageTitle"] = "Maru Expedition | CodeMaru Games", ["maru.status"] = "LIVE · SERVER RPG", ["maru.original"] = "CODEMARU GAMES ORIGINAL",
        ["maru.title"] = "Maru Expedition", ["maru.titleAccent"] = "Maru", ["maru.titleRest"] = "Expedition", ["maru.tagline"] = "A world swallowed by moonlight.\nYour expedition never stops.",
        ["maru.description"] = "Recruit companions, conquer endless regions, and inherit sword arts and equipment. Your party keeps growing even while you are away.",
        ["maru.start"] = "Start for free", ["maru.continue"] = "Continue expedition", ["maru.login"] = "Sign in to start", ["maru.local"] = "Local test account",
        ["maru.intro"] = "About the game", ["maru.fact.idle"] = "Idle growth", ["maru.fact.party"] = "Party formation", ["maru.fact.trials"] = "Trial modes", ["maru.fact.save"] = "Account save",
        ["maru.palace"] = "THE ECLIPSED PALACE", ["maru.caption"] = "Pursue the golden shadow", ["maru.scroll"] = "SCROLL TO EXPLORE",
        ["maru.worldKicker"] = "ONE WORLD · ENDLESS JOURNEY", ["maru.worldTitle"] = "A wider expedition with every step",
        ["maru.worldLead"] = "Combat, companions, trials, and farming feed one continuous journey. Continue on PC or mobile with the same account.",
        ["maru.mainKicker"] = "MAIN EXPEDITION", ["maru.main"] = "A real-time expedition without end", ["maru.mainDesc"] = "Move between automatic combat and direct attacks to break through elite monsters and regional bosses.", ["maru.play"] = "Play now",
        ["maru.growthKicker"] = "ETERNAL GROWTH", ["maru.growth"] = "Growth that stays with your account", ["maru.growthDesc"] = "Sword, sword arts, and inherited equipment empower the entire expedition.",
        ["maru.trialKicker"] = "TRIAL ARCADE", ["maru.trials"] = "Six distinct trials", ["maru.trialsDesc"] = "Earn rare rewards through survival, racing, defense, farming, fortress, and tower challenges.",
        ["maru.farmKicker"] = "SPIRIT FARM", ["maru.farm"] = "A living spirit farm", ["maru.farmDesc"] = "Grow spirit herbs and manage facilities and soil for long-term resources.",
        ["maru.ctaKicker"] = "YOUR EXPEDITION AWAITS", ["maru.ready"] = "Are you ready?\nOpen the first path.", ["maru.enter"] = "Enter Maru Expedition"
    };
}
