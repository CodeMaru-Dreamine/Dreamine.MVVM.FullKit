namespace Codemaru.Services;

public sealed record RouteCopy(
    string AdminRequired, string AdminRequiredDetail, string Home,
    string SignInRequired, string SignInRequiredDetail, string SignIn,
    string NotFound, string NotFoundDetail, string Guides);

public static class RouteLocalization
{
    public static RouteCopy Get(string language) => language switch
    {
        "ko" => new("관리자 권한이 필요합니다", "현재 계정에는 운영 대시보드 접근 권한이 없습니다.", "CodeMaru 홈", "로그인이 필요합니다", "운영 대시보드에 접근하려면 관리자 계정으로 로그인하세요.", "로그인하고 계속하기", "페이지를 찾을 수 없습니다", "주소가 바뀌었거나 더 이상 제공하지 않는 페이지입니다.", "이용 설명서"),
        "es" => new("Se requieren permisos de administrador", "Esta cuenta no puede acceder al panel de operaciones.", "Inicio de CodeMaru", "Debes iniciar sesión", "Inicia sesión con una cuenta administradora para continuar.", "Iniciar sesión y continuar", "Página no encontrada", "La dirección cambió o la página ya no está disponible.", "Guías de uso"),
        "fr" => new("Droits administrateur requis", "Ce compte ne peut pas accéder au tableau des opérations.", "Accueil CodeMaru", "Connexion requise", "Connectez-vous avec un compte administrateur pour continuer.", "Se connecter et continuer", "Page introuvable", "L’adresse a changé ou la page n’est plus disponible.", "Guides d’utilisation"),
        "it" => new("Sono richiesti i permessi di amministratore", "Questo account non può accedere al dashboard operativo.", "Home CodeMaru", "Accesso richiesto", "Accedi con un account amministratore per continuare.", "Accedi e continua", "Pagina non trovata", "L’indirizzo è cambiato o la pagina non è più disponibile.", "Guide utente"),
        "pt" => new("Permissão de administrador necessária", "Esta conta não pode acessar o painel de operações.", "Início CodeMaru", "Login necessário", "Entre com uma conta administrativa para continuar.", "Entrar e continuar", "Página não encontrada", "O endereço mudou ou a página não está mais disponível.", "Guias de uso"),
        "vi" => new("Cần quyền quản trị viên", "Tài khoản này không thể truy cập bảng điều hành.", "Trang chủ CodeMaru", "Cần đăng nhập", "Đăng nhập bằng tài khoản quản trị để tiếp tục.", "Đăng nhập và tiếp tục", "Không tìm thấy trang", "Địa chỉ đã thay đổi hoặc trang không còn tồn tại.", "Hướng dẫn sử dụng"),
        "ja" => new("管理者権限が必要です", "このアカウントには運用ダッシュボードへのアクセス権がありません。", "CodeMaruホーム", "ログインが必要です", "管理者アカウントでログインしてください。", "ログインして続行", "ページが見つかりません", "URLが変更されたか、ページの提供が終了しました。", "利用ガイド"),
        "zh-hans" => new("需要管理员权限", "当前账户无权访问运营仪表板。", "CodeMaru 首页", "需要登录", "请使用管理员账户登录后继续。", "登录并继续", "找不到页面", "网址已更改或页面已停止提供。", "使用指南"),
        "zh-hant" => new("需要管理員權限", "目前帳戶無權存取營運儀表板。", "CodeMaru 首頁", "需要登入", "請使用管理員帳戶登入後繼續。", "登入並繼續", "找不到頁面", "網址已變更或頁面已停止提供。", "使用指南"),
        _ => new("Administrator access required", "This account cannot access the operations dashboard.", "CodeMaru home", "Sign-in required", "Sign in with an administrator account to continue.", "Sign in and continue", "Page not found", "The address changed or the page is no longer available.", "User guides")
    };
}
