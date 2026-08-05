using System.Globalization;

namespace ShopPlatform.Services;

/// <summary>Localized copy used by the tenant-facing storefront.</summary>
public static class ShopStorefrontText
{
    private static readonly string[] LanguageOrder =
        ["en", "es", "fr", "it", "pt", "ko", "ja", "zh-hans", "zh-hant", "vi"];

    private static readonly IReadOnlyDictionary<string, string[]> Values =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Shop"] = ["Shop", "Tienda", "Boutique", "Negozio", "Loja", "쇼핑몰", "ショップ", "商店", "商店", "Cửa hàng"],
            ["ShopUnavailable"] = ["This shop does not exist.", "Esta tienda no existe.", "Cette boutique n’existe pas.", "Questo negozio non esiste.", "Esta loja não existe.", "존재하지 않는 쇼핑몰입니다.", "このショップは存在しません。", "此商店不存在。", "此商店不存在。", "Cửa hàng này không tồn tại."],
            ["ShopPreparing"] = ["This shop is getting ready.", "Esta tienda está en preparación.", "Cette boutique est en préparation.", "Questo negozio è in preparazione.", "Esta loja está em preparação.", "준비 중인 쇼핑몰입니다.", "このショップは準備中です。", "此商店正在准备中。", "此商店正在準備中。", "Cửa hàng này đang được chuẩn bị."],
            ["ShopHere"] = ["Shop at {0}", "Compra en {0}", "Faites vos achats chez {0}", "Acquista su {0}", "Compre na {0}", "{0}에서 쇼핑하세요", "{0}でお買い物をお楽しみください", "在 {0} 购物", "在 {0} 購物", "Mua sắm tại {0}"],
            ["Home"] = ["Home", "Inicio", "Accueil", "Home", "Início", "홈", "ホーム", "首页", "首頁", "Trang chủ"],
            ["AllProducts"] = ["All products", "Todos los productos", "Tous les produits", "Tutti i prodotti", "Todos os produtos", "전체 상품", "すべての商品", "全部商品", "全部商品", "Tất cả sản phẩm"],
            ["PlatformHome"] = ["ShopStore home", "Inicio de ShopStore", "Accueil ShopStore", "Home di ShopStore", "Início do ShopStore", "ShopStore 홈", "ShopStore ホーム", "ShopStore 首页", "ShopStore 首頁", "Trang chủ ShopStore"],
            ["Login"] = ["Sign in", "Iniciar sesión", "Se connecter", "Accedi", "Entrar", "로그인", "ログイン", "登录", "登入", "Đăng nhập"],
            ["Logout"] = ["Sign out", "Cerrar sesión", "Se déconnecter", "Esci", "Sair", "로그아웃", "ログアウト", "退出登录", "登出", "Đăng xuất"],
            ["Register"] = ["Create account", "Crear cuenta", "Créer un compte", "Crea account", "Criar conta", "회원가입", "会員登録", "注册", "註冊", "Đăng ký"],
            ["Cart"] = ["Cart", "Carrito", "Panier", "Carrello", "Carrinho", "장바구니", "カート", "购物车", "購物車", "Giỏ hàng"],
            ["ViewAllProducts"] = ["View all products", "Ver todos los productos", "Voir tous les produits", "Vedi tutti i prodotti", "Ver todos os produtos", "전체 상품 보기", "すべての商品を見る", "查看全部商品", "查看全部商品", "Xem tất cả sản phẩm"],
            ["FeaturedProducts"] = ["Featured products", "Productos destacados", "Produits vedettes", "Prodotti in evidenza", "Produtos em destaque", "추천 상품", "おすすめ商品", "推荐商品", "推薦商品", "Sản phẩm nổi bật"],
            ["Loading"] = ["Loading…", "Cargando…", "Chargement…", "Caricamento…", "Carregando…", "로딩 중...", "読み込み中…", "加载中…", "載入中…", "Đang tải…"],
            ["NoProducts"] = ["No products have been added yet.", "Aún no hay productos.", "Aucun produit n’a encore été ajouté.", "Non è stato ancora aggiunto alcun prodotto.", "Ainda não há produtos cadastrados.", "등록된 상품이 없습니다.", "商品はまだ登録されていません。", "尚未添加商品。", "尚未新增商品。", "Chưa có sản phẩm nào."],
            ["AddProductsInAdmin"] = ["Add products in Admin", "Añadir productos en Administración", "Ajouter des produits dans l’administration", "Aggiungi prodotti in Amministrazione", "Adicionar produtos no painel administrativo", "어드민에서 상품 등록하기", "管理画面で商品を追加", "在管理后台添加商品", "在管理後台新增商品", "Thêm sản phẩm trong trang quản trị"],
            ["SoldOut"] = ["Sold out", "Agotado", "Épuisé", "Esaurito", "Esgotado", "품절", "売り切れ", "售罄", "售罄", "Hết hàng"],
            ["Details"] = ["Details", "Detalles", "Détails", "Dettagli", "Detalhes", "자세히", "詳細", "详情", "詳情", "Chi tiết"],
            ["Add"] = ["Add", "Añadir", "Ajouter", "Aggiungi", "Adicionar", "담기", "追加", "加入", "加入", "Thêm"],
            ["ViewProductCount"] = ["View all {0} products →", "Ver los {0} productos →", "Voir les {0} produits →", "Vedi tutti i {0} prodotti →", "Ver todos os {0} produtos →", "전체 {0}개 상품 보기 →", "全{0}商品を見る →", "查看全部 {0} 件商品 →", "查看全部 {0} 件商品 →", "Xem tất cả {0} sản phẩm →"],
            ["ProductCount"] = ["{0} products", "{0} productos", "{0} produits", "{0} prodotti", "{0} produtos", "총 {0}개", "全{0}件", "共 {0} 件", "共 {0} 件", "{0} sản phẩm"],
            ["SearchProducts"] = ["Search products…", "Buscar productos…", "Rechercher des produits…", "Cerca prodotti…", "Pesquisar produtos…", "상품명 검색...", "商品を検索…", "搜索商品…", "搜尋商品…", "Tìm sản phẩm…"],
            ["NameAsc"] = ["Name A–Z", "Nombre A–Z", "Nom A–Z", "Nome A–Z", "Nome A–Z", "이름 ↑", "名前 昇順", "名称升序", "名稱升冪", "Tên A–Z"],
            ["NameDesc"] = ["Name Z–A", "Nombre Z–A", "Nom Z–A", "Nome Z–A", "Nome Z–A", "이름 ↓", "名前 降順", "名称降序", "名稱降冪", "Tên Z–A"],
            ["PriceLow"] = ["Price: low to high", "Precio: menor a mayor", "Prix croissant", "Prezzo crescente", "Preço: menor para maior", "가격 낮은순", "価格の安い順", "价格从低到高", "價格由低至高", "Giá thấp đến cao"],
            ["PriceHigh"] = ["Price: high to low", "Precio: mayor a menor", "Prix décroissant", "Prezzo decrescente", "Preço: maior para menor", "가격 높은순", "価格の高い順", "价格从高到低", "價格由高至低", "Giá cao đến thấp"],
            ["NoSearchResults"] = ["No products match your search.", "No hay productos que coincidan con la búsqueda.", "Aucun produit ne correspond à votre recherche.", "Nessun prodotto corrisponde alla ricerca.", "Nenhum produto corresponde à pesquisa.", "검색 결과가 없습니다.", "検索結果がありません。", "没有匹配的商品。", "找不到相符的商品。", "Không tìm thấy sản phẩm phù hợp."],
            ["ResetFilters"] = ["Reset filters", "Restablecer filtros", "Réinitialiser les filtres", "Reimposta filtri", "Redefinir filtros", "필터 초기화", "フィルターをリセット", "重置筛选条件", "重設篩選條件", "Đặt lại bộ lọc"],
            ["ProductDetails"] = ["Product details", "Detalles del producto", "Détails du produit", "Dettagli prodotto", "Detalhes do produto", "상품 상세", "商品詳細", "商品详情", "商品詳情", "Chi tiết sản phẩm"],
            ["ProductNotFound"] = ["Product not found.", "No se encontró el producto.", "Produit introuvable.", "Prodotto non trovato.", "Produto não encontrado.", "상품을 찾을 수 없습니다.", "商品が見つかりません。", "未找到商品。", "找不到商品。", "Không tìm thấy sản phẩm."],
            ["BackToList"] = ["← Back to products", "← Volver a productos", "← Retour aux produits", "← Torna ai prodotti", "← Voltar aos produtos", "← 목록으로", "← 商品一覧へ", "← 返回商品列表", "← 返回商品列表", "← Về danh sách sản phẩm"],
            ["InStock"] = ["In stock", "Disponible", "En stock", "Disponibile", "Em estoque", "재고 충분", "在庫あり", "有货", "有現貨", "Còn hàng"],
            ["StockCount"] = ["{0} in stock", "{0} disponibles", "{0} en stock", "{0} disponibili", "{0} em estoque", "재고 {0}개", "在庫 {0}点", "库存 {0} 件", "庫存 {0} 件", "Còn {0} sản phẩm"],
            ["Quantity"] = ["Quantity", "Cantidad", "Quantité", "Quantità", "Quantidade", "수량", "数量", "数量", "數量", "Số lượng"],
            ["DecreaseQuantity"] = ["Decrease quantity", "Reducir cantidad", "Réduire la quantité", "Riduci quantità", "Diminuir quantidade", "수량 줄이기", "数量を減らす", "减少数量", "減少數量", "Giảm số lượng"],
            ["IncreaseQuantity"] = ["Increase quantity", "Aumentar cantidad", "Augmenter la quantité", "Aumenta quantità", "Aumentar quantidade", "수량 늘리기", "数量を増やす", "增加数量", "增加數量", "Tăng số lượng"],
            ["Total"] = ["Total", "Total", "Total", "Totale", "Total", "합계", "合計", "合计", "合計", "Tổng cộng"],
            ["AddedToCart"] = ["Added {0} item(s) to your cart!", "¡Se añadieron {0} artículo(s) al carrito!", "{0} article(s) ajouté(s) au panier !", "Aggiunti {0} articoli al carrello!", "{0} item(ns) adicionado(s) ao carrinho!", "장바구니에 {0}개 담았습니다!", "カートに{0}点追加しました！", "已将 {0} 件商品加入购物车！", "已將 {0} 件商品加入購物車！", "Đã thêm {0} sản phẩm vào giỏ hàng!"],
            ["AddToCart"] = ["Add to cart", "Añadir al carrito", "Ajouter au panier", "Aggiungi al carrello", "Adicionar ao carrinho", "장바구니 담기", "カートに追加", "加入购物车", "加入購物車", "Thêm vào giỏ hàng"],
            ["GoToCart"] = ["Go to cart", "Ir al carrito", "Voir le panier", "Vai al carrello", "Ir para o carrinho", "장바구니로 이동", "カートへ", "前往购物车", "前往購物車", "Đi đến giỏ hàng"],
            ["BuyNow"] = ["Buy now", "Comprar ahora", "Acheter maintenant", "Acquista ora", "Comprar agora", "바로 결제", "今すぐ購入", "立即购买", "立即購買", "Mua ngay"],
            ["ProductVideo"] = ["{0} video", "Vídeo de {0}", "Vidéo de {0}", "Video di {0}", "Vídeo de {0}", "{0} 동영상", "{0}の動画", "{0} 视频", "{0} 影片", "Video {0}"],
            ["DetailImage"] = ["Product detail image", "Imagen de detalle del producto", "Image détaillée du produit", "Immagine di dettaglio del prodotto", "Imagem de detalhe do produto", "상세 이미지", "商品詳細画像", "商品详情图片", "商品詳情圖片", "Hình ảnh chi tiết sản phẩm"],
            ["PoliciesTitle"] = ["Refund, returns and delivery policies", "Políticas de reembolso, devoluciones y entrega", "Politiques de remboursement, retour et livraison", "Politiche di rimborso, reso e consegna", "Políticas de reembolso, devolução e entrega", "환불 · 교환/반품 · 배송 정책", "返金・返品・配送ポリシー", "退款、退换货和配送政策", "退款、退換貨及配送政策", "Chính sách hoàn tiền, đổi trả và giao hàng"],
            ["RefundPolicy"] = ["Refund policy", "Política de reembolso", "Politique de remboursement", "Politica di rimborso", "Política de reembolso", "환불정책", "返金ポリシー", "退款政策", "退款政策", "Chính sách hoàn tiền"],
            ["ExchangePolicy"] = ["Exchanges and returns", "Cambios y devoluciones", "Échanges et retours", "Cambi e resi", "Trocas e devoluções", "교환 및 반품 안내", "交換・返品について", "换货与退货说明", "換貨與退貨說明", "Đổi và trả hàng"],
            ["DeliveryPolicy"] = ["Delivery information", "Información de entrega", "Informations de livraison", "Informazioni sulla consegna", "Informações de entrega", "배송 안내", "配送について", "配送说明", "配送說明", "Thông tin giao hàng"],
            ["ContinueShopping"] = ["Continue shopping", "Seguir comprando", "Continuer mes achats", "Continua lo shopping", "Continuar comprando", "쇼핑 계속하기", "買い物を続ける", "继续购物", "繼續購物", "Tiếp tục mua sắm"],
            ["CartEmpty"] = ["Your cart is empty.", "Tu carrito está vacío.", "Votre panier est vide.", "Il carrello è vuoto.", "Seu carrinho está vazio.", "장바구니가 비어 있습니다.", "カートは空です。", "购物车是空的。", "購物車是空的。", "Giỏ hàng của bạn đang trống."],
            ["ProductName"] = ["Product", "Producto", "Produit", "Prodotto", "Produto", "상품명", "商品名", "商品", "商品", "Sản phẩm"],
            ["UnitPrice"] = ["Unit price", "Precio unitario", "Prix unitaire", "Prezzo unitario", "Preço unitário", "단가", "単価", "单价", "單價", "Đơn giá"],
            ["Subtotal"] = ["Subtotal", "Subtotal", "Sous-total", "Subtotale", "Subtotal", "소계", "小計", "小计", "小計", "Thành tiền"],
            ["RemoveItem"] = ["Remove {0} from cart", "Eliminar {0} del carrito", "Retirer {0} du panier", "Rimuovi {0} dal carrello", "Remover {0} do carrinho", "장바구니에서 {0} 삭제", "{0}をカートから削除", "从购物车移除 {0}", "從購物車移除 {0}", "Xóa {0} khỏi giỏ hàng"],
            ["Checkout"] = ["Checkout", "Pagar", "Paiement", "Pagamento", "Finalizar compra", "결제하기", "お支払い", "结账", "結帳", "Thanh toán"],
            ["Payment"] = ["Payment", "Pago", "Paiement", "Pagamento", "Pagamento", "결제", "お支払い", "支付", "付款", "Thanh toán"],
            ["PaymentProcess"] = ["Checkout", "Proceder al pago", "Passer au paiement", "Procedi al pagamento", "Finalizar pagamento", "결제 진행", "お支払い手続き", "进行结账", "進行結帳", "Tiến hành thanh toán"],
            ["BackToCart"] = ["Back to cart", "Volver al carrito", "Retour au panier", "Torna al carrello", "Voltar ao carrinho", "장바구니로", "カートに戻る", "返回购物车", "返回購物車", "Quay lại giỏ hàng"],
            ["OrderItems"] = ["Order items", "Artículos del pedido", "Articles commandés", "Articoli dell’ordine", "Itens do pedido", "주문 상품", "注文商品", "订单商品", "訂單商品", "Sản phẩm đặt mua"],
            ["CustomerInfo"] = ["Customer information", "Datos del cliente", "Informations client", "Dati del cliente", "Informações do cliente", "주문자 정보", "注文者情報", "订购人信息", "訂購人資訊", "Thông tin người đặt hàng"],
            ["OrderingAs"] = ["Ordering as {0}.", "Pedido a nombre de {0}.", "Commande au nom de {0}.", "Ordine a nome di {0}.", "Pedido em nome de {0}.", "{0} 님으로 주문합니다.", "{0}として注文します。", "将以 {0} 的身份下单。", "將以 {0} 的身分下單。", "Đặt hàng với tên {0}."],
            ["Name"] = ["Name", "Nombre", "Nom", "Nome", "Nome", "이름", "氏名", "姓名", "姓名", "Họ tên"],
            ["Phone"] = ["Phone", "Teléfono", "Téléphone", "Telefono", "Telefone", "연락처", "電話番号", "联系电话", "聯絡電話", "Số điện thoại"],
            ["Email"] = ["Email", "Correo electrónico", "E-mail", "Email", "E-mail", "이메일", "メールアドレス", "电子邮箱", "電子郵件", "Email"],
            ["ShippingInfo"] = ["Shipping information", "Datos de envío", "Informations de livraison", "Dati di spedizione", "Informações de entrega", "배송지 정보", "配送先情報", "配送信息", "配送資訊", "Thông tin giao hàng"],
            ["ShippingAddress"] = ["Shipping address", "Dirección de envío", "Adresse de livraison", "Indirizzo di spedizione", "Endereço de entrega", "배송 주소", "配送先住所", "收货地址", "收貨地址", "Địa chỉ giao hàng"],
            ["RequestNotes"] = ["Delivery notes", "Indicaciones de entrega", "Instructions de livraison", "Note di consegna", "Instruções de entrega", "요청 사항", "配送メモ", "配送备注", "配送備註", "Ghi chú giao hàng"],
            ["RequestPlaceholder"] = ["e.g. Leave it at the door", "Ej.: Déjalo en la puerta", "Ex. : Laissez-le devant la porte", "Es.: Lasciare davanti alla porta", "Ex.: Deixe na porta", "예) 문 앞에 놓아주세요", "例：玄関前に置いてください", "例如：请放在门口", "例如：請放在門口", "Ví dụ: Vui lòng để trước cửa"],
            ["PaymentMethod"] = ["Payment method", "Método de pago", "Mode de paiement", "Metodo di pagamento", "Forma de pagamento", "결제 수단", "支払い方法", "支付方式", "付款方式", "Phương thức thanh toán"],
            ["CardMethod"] = ["Card (including Kakao/Naver)", "Tarjeta (incluye Kakao/Naver)", "Carte (Kakao/Naver inclus)", "Carta (inclusi Kakao/Naver)", "Cartão (inclui Kakao/Naver)", "카드 (카카오/네이버 포함)", "カード（Kakao/Naverを含む）", "银行卡（含 Kakao/Naver）", "信用卡（含 Kakao/Naver）", "Thẻ (bao gồm Kakao/Naver)"],
            ["TransferDemo"] = ["Bank transfer (demo)", "Transferencia bancaria (demo)", "Virement bancaire (démo)", "Bonifico bancario (demo)", "Transferência bancária (demo)", "계좌 이체 (데모)", "銀行振込（デモ）", "银行转账（演示）", "銀行轉帳（示範）", "Chuyển khoản ngân hàng (demo)"],
            ["VirtualDemo"] = ["Virtual account (demo)", "Cuenta virtual (demo)", "Compte virtuel (démo)", "Conto virtuale (demo)", "Conta virtual (demo)", "가상 계좌 (데모)", "バーチャル口座（デモ）", "虚拟账户（演示）", "虛擬帳戶（示範）", "Tài khoản ảo (demo)"],
            ["DemoMode"] = ["Demo mode", "Modo de demostración", "Mode démo", "Modalità demo", "Modo de demonstração", "데모 모드", "デモモード", "演示模式", "示範模式", "Chế độ demo"],
            ["DemoModeHelp"] = ["Payment integration is enabled after a Toss API key is entered in Admin → Payment settings.", "La integración de pagos se activa al introducir una clave API de Toss en Administración → Configuración de pagos.", "L’intégration du paiement s’active après avoir saisi une clé API Toss dans Administration → Paramètres de paiement.", "L’integrazione dei pagamenti si attiva inserendo una chiave API Toss in Amministrazione → Impostazioni di pagamento.", "A integração de pagamento é ativada ao inserir uma chave de API Toss em Administração → Configurações de pagamento.", "결제 연동은 어드민 → 결제 설정에서 토스 API 키를 입력하면 활성화됩니다.", "管理画面 → 支払い設定でToss APIキーを入力すると決済連携が有効になります。", "在管理后台 → 支付设置中输入 Toss API 密钥后即可启用支付集成。", "在管理後台 → 付款設定中輸入 Toss API 金鑰後即可啟用付款整合。", "Tích hợp thanh toán sẽ được bật sau khi nhập khóa API Toss trong Quản trị → Cài đặt thanh toán."],
            ["Processing"] = ["Processing…", "Procesando…", "Traitement…", "Elaborazione…", "Processando…", "처리 중...", "処理中…", "处理中…", "處理中…", "Đang xử lý…"],
            ["PlaceDemoOrder"] = ["Place demo order — {0}", "Hacer pedido de prueba — {0}", "Passer une commande démo — {0}", "Effettua ordine demo — {0}", "Fazer pedido de demonstração — {0}", "주문하기 (데모) — {0}", "デモ注文を確定 — {0}", "提交演示订单 — {0}", "送出演示訂單 — {0}", "Đặt đơn demo — {0}"],
            ["PayByCard"] = ["Pay by card", "Pagar con tarjeta", "Payer par carte", "Paga con carta", "Pagar com cartão", "카드로 결제하기", "カードで支払う", "银行卡支付", "信用卡付款", "Thanh toán bằng thẻ"],
            ["OpenCardMethods"] = ["Open card payment options", "Abrir opciones de pago con tarjeta", "Ouvrir les options de paiement par carte", "Apri le opzioni di pagamento con carta", "Abrir opções de pagamento com cartão", "카드 결제 수단 열기", "カード決済方法を開く", "打开银行卡支付方式", "開啟信用卡付款方式", "Mở tùy chọn thanh toán bằng thẻ"],
            ["LoginAutoFill"] = [" to fill in your shipping information automatically.", " para completar automáticamente los datos de envío.", " pour remplir automatiquement vos informations de livraison.", " per compilare automaticamente i dati di spedizione.", " para preencher automaticamente os dados de entrega.", "하면 배송 정보가 자동으로 채워집니다.", "すると配送情報が自動入力されます。", "后将自动填写配送信息。", "後將自動填寫配送資訊。", " để tự động điền thông tin giao hàng."],
            ["RequiredCheckout"] = ["Name and shipping address are required.", "El nombre y la dirección de envío son obligatorios.", "Le nom et l’adresse de livraison sont obligatoires.", "Nome e indirizzo di spedizione sono obbligatori.", "Nome e endereço de entrega são obrigatórios.", "이름과 배송 주소는 필수입니다.", "氏名と配送先住所は必須です。", "姓名和收货地址为必填项。", "姓名和收貨地址為必填項目。", "Họ tên và địa chỉ giao hàng là bắt buộc."],
            ["PaymentError"] = ["Unable to open the payment service. Please try again.", "No se pudo abrir el servicio de pago. Inténtalo de nuevo.", "Impossible d’ouvrir le service de paiement. Veuillez réessayer.", "Impossibile aprire il servizio di pagamento. Riprova.", "Não foi possível abrir o serviço de pagamento. Tente novamente.", "결제 서비스를 열 수 없습니다. 다시 시도해 주세요.", "決済サービスを開けませんでした。もう一度お試しください。", "无法打开支付服务，请重试。", "無法開啟付款服務，請再試一次。", "Không thể mở dịch vụ thanh toán. Vui lòng thử lại."],
            ["ShopOrder"] = ["{0} order", "Pedido de {0}", "Commande {0}", "Ordine {0}", "Pedido da {0}", "{0} 주문", "{0}の注文", "{0} 订单", "{0} 訂單", "Đơn hàng {0}"],
            ["OrderComplete"] = ["Order complete", "Pedido completado", "Commande terminée", "Ordine completato", "Pedido concluído", "주문 완료", "注文完了", "订单完成", "訂單完成", "Đặt hàng thành công"],
            ["LoadingOrder"] = ["Loading order details…", "Cargando los datos del pedido…", "Chargement de la commande…", "Caricamento dei dettagli dell’ordine…", "Carregando os dados do pedido…", "주문 정보를 불러오는 중...", "注文情報を読み込み中…", "正在加载订单信息…", "正在載入訂單資訊…", "Đang tải thông tin đơn hàng…"],
            ["OrderCompletedMessage"] = ["Your order is complete!", "¡Tu pedido se ha completado!", "Votre commande est terminée !", "Il tuo ordine è stato completato!", "Seu pedido foi concluído!", "주문이 완료되었습니다!", "ご注文が完了しました！", "订单已完成！", "訂單已完成！", "Đơn hàng của bạn đã hoàn tất!"],
            ["ThanksForShopping"] = ["Thank you for shopping at {0}.", "Gracias por comprar en {0}.", "Merci d’avoir choisi {0}.", "Grazie per aver acquistato su {0}.", "Obrigado por comprar na {0}.", "{0}을 이용해 주셔서 감사합니다.", "{0}をご利用いただきありがとうございます。", "感谢您在 {0} 购物。", "感謝您在 {0} 購物。", "Cảm ơn bạn đã mua sắm tại {0}."],
            ["OrderInfo"] = ["Order information", "Datos del pedido", "Informations de commande", "Informazioni sull’ordine", "Informações do pedido", "주문 정보", "注文情報", "订单信息", "訂單資訊", "Thông tin đơn hàng"],
            ["OrderNumber"] = ["Order number", "Número de pedido", "Numéro de commande", "Numero d’ordine", "Número do pedido", "주문번호", "注文番号", "订单编号", "訂單編號", "Mã đơn hàng"],
            ["Customer"] = ["Customer", "Cliente", "Client", "Cliente", "Cliente", "주문자", "注文者", "订购人", "訂購人", "Người đặt hàng"],
            ["Destination"] = ["Shipping address", "Dirección de entrega", "Adresse de livraison", "Indirizzo di consegna", "Endereço de entrega", "배송지", "配送先", "收货地址", "收貨地址", "Địa chỉ giao hàng"],
            ["PaymentAmount"] = ["Amount paid", "Importe pagado", "Montant payé", "Importo pagato", "Valor pago", "결제금액", "支払金額", "支付金额", "付款金額", "Số tiền thanh toán"],
            ["PaymentStatus"] = ["Payment status", "Estado del pago", "Statut du paiement", "Stato del pagamento", "Status do pagamento", "결제상태", "支払い状況", "支付状态", "付款狀態", "Trạng thái thanh toán"],
            ["Paid"] = ["Paid", "Pagado", "Payé", "Pagato", "Pago", "결제완료", "支払い済み", "已支付", "已付款", "Đã thanh toán"],
            ["Pending"] = ["Pending", "Pendiente", "En attente", "In attesa", "Pendente", "결제 대기", "保留中", "待支付", "待付款", "Đang chờ"],
            ["Failed"] = ["Failed", "Fallido", "Échec", "Non riuscito", "Falhou", "결제 실패", "失敗", "失败", "失敗", "Thất bại"],
            ["Cancelled"] = ["Cancelled", "Cancelado", "Annulé", "Annullato", "Cancelado", "취소됨", "キャンセル済み", "已取消", "已取消", "Đã hủy"],
            ["Transaction"] = ["Transaction", "Transacción", "Transaction", "Transazione", "Transação", "거래번호", "取引番号", "交易编号", "交易編號", "Mã giao dịch"],
            ["Amount"] = ["Amount", "Importe", "Montant", "Importo", "Valor", "금액", "金額", "金额", "金額", "Số tiền"],
            ["Member"] = ["member", "miembro", "membre", "membro", "membro", "회원", "会員", "会员", "會員", "thành viên"],
            ["ContinueWithCodeMaru"] = ["Continue with a CodeMaru account", "Continuar con una cuenta de CodeMaru", "Continuer avec un compte CodeMaru", "Continua con un account CodeMaru", "Continuar com uma conta CodeMaru", "CodeMaru 계정으로 계속하기", "CodeMaruアカウントで続行", "使用 CodeMaru 账户继续", "使用 CodeMaru 帳戶繼續", "Tiếp tục bằng tài khoản CodeMaru"],
            ["OrLocalLogin"] = ["or sign in with an existing shop account", "o inicia sesión con una cuenta existente de la tienda", "ou connectez-vous avec un compte existant de la boutique", "oppure accedi con un account esistente del negozio", "ou entre com uma conta existente da loja", "또는 기존 쇼핑몰 회원 로그인", "または既存のショップアカウントでログイン", "或使用已有商店账户登录", "或使用現有商店帳戶登入", "hoặc đăng nhập bằng tài khoản cửa hàng hiện có"],
            ["Password"] = ["Password", "Contraseña", "Mot de passe", "Password", "Senha", "비밀번호", "パスワード", "密码", "密碼", "Mật khẩu"],
            ["SigningIn"] = ["Signing in…", "Iniciando sesión…", "Connexion…", "Accesso…", "Entrando…", "로그인 중...", "ログイン中…", "正在登录…", "正在登入…", "Đang đăng nhập…"],
            ["CheckoutProfileHelp"] = ["You can add your shipping address and phone number at checkout.", "Puedes añadir tu dirección y teléfono al pagar.", "Vous pourrez ajouter votre adresse et votre téléphone lors du paiement.", "Puoi aggiungere indirizzo e telefono al pagamento.", "Você pode adicionar endereço e telefone no checkout.", "배송지·연락처는 결제 단계에서 추가로 입력할 수 있습니다.", "配送先と電話番号はお支払い時に追加できます。", "您可以在结账时填写收货地址和电话。", "您可在結帳時填寫收貨地址與電話。", "Bạn có thể thêm địa chỉ và số điện thoại khi thanh toán."],
            ["BackToShop"] = ["Back to shop", "Volver a la tienda", "Retour à la boutique", "Torna al negozio", "Voltar à loja", "쇼핑몰로 돌아가기", "ショップに戻る", "返回商店", "返回商店", "Quay lại cửa hàng"],
            ["LoginRequiredFields"] = ["Enter your email and password.", "Introduce tu correo y contraseña.", "Saisissez votre e-mail et votre mot de passe.", "Inserisci email e password.", "Digite seu e-mail e sua senha.", "이메일과 비밀번호를 입력해 주세요.", "メールアドレスとパスワードを入力してください。", "请输入电子邮箱和密码。", "請輸入電子郵件與密碼。", "Vui lòng nhập email và mật khẩu."],
            ["InvalidCredentials"] = ["The email or password is incorrect.", "El correo o la contraseña no son correctos.", "L’e-mail ou le mot de passe est incorrect.", "Email o password non corretti.", "O e-mail ou a senha está incorreto.", "이메일 또는 비밀번호가 올바르지 않습니다.", "メールアドレスまたはパスワードが正しくありません。", "电子邮箱或密码不正确。", "電子郵件或密碼不正確。", "Email hoặc mật khẩu không đúng."],
            ["ConfirmPassword"] = ["Confirm password", "Confirmar contraseña", "Confirmer le mot de passe", "Conferma password", "Confirmar senha", "비밀번호 확인", "パスワード（確認）", "确认密码", "確認密碼", "Xác nhận mật khẩu"],
            ["Address"] = ["Shipping address", "Dirección de envío", "Adresse de livraison", "Indirizzo di spedizione", "Endereço de entrega", "배송지 주소", "配送先住所", "收货地址", "收貨地址", "Địa chỉ giao hàng"],
            ["Registering"] = ["Creating account…", "Creando cuenta…", "Création du compte…", "Creazione account…", "Criando conta…", "가입 중...", "登録中…", "正在注册…", "正在註冊…", "Đang đăng ký…"],
            ["AlreadyMember"] = ["Already have an account?", "¿Ya tienes una cuenta?", "Vous avez déjà un compte ?", "Hai già un account?", "Já tem uma conta?", "이미 회원이신가요?", "すでに会員ですか？", "已有账户？", "已有帳戶？", "Đã có tài khoản?"],
            ["RegisterRequiredFields"] = ["Name, email and password are required.", "El nombre, correo y contraseña son obligatorios.", "Le nom, l’e-mail et le mot de passe sont obligatoires.", "Nome, email e password sono obbligatori.", "Nome, e-mail e senha são obrigatórios.", "이름, 이메일, 비밀번호는 필수입니다.", "氏名、メールアドレス、パスワードは必須です。", "姓名、电子邮箱和密码为必填项。", "姓名、電子郵件與密碼為必填項目。", "Họ tên, email và mật khẩu là bắt buộc."],
            ["PasswordMismatch"] = ["Passwords do not match.", "Las contraseñas no coinciden.", "Les mots de passe ne correspondent pas.", "Le password non corrispondono.", "As senhas não coincidem.", "비밀번호가 일치하지 않습니다.", "パスワードが一致しません。", "两次输入的密码不一致。", "兩次輸入的密碼不一致。", "Mật khẩu không khớp."],
            ["PasswordMinimum"] = ["Password must be at least 8 characters.", "La contraseña debe tener al menos 8 caracteres.", "Le mot de passe doit comporter au moins 8 caractères.", "La password deve contenere almeno 8 caratteri.", "A senha deve ter pelo menos 8 caracteres.", "비밀번호는 8자 이상이어야 합니다.", "パスワードは8文字以上にしてください。", "密码必须至少包含 8 个字符。", "密碼必須至少包含 8 個字元。", "Mật khẩu phải có ít nhất 8 ký tự."],
            ["EmailExists"] = ["An account with this email already exists.", "Ya existe una cuenta con este correo.", "Un compte existe déjà avec cet e-mail.", "Esiste già un account con questa email.", "Já existe uma conta com este e-mail.", "이미 가입된 이메일입니다.", "このメールアドレスは登録済みです。", "此电子邮箱已注册。", "此電子郵件已註冊。", "Email này đã được đăng ký."],
            ["PolicyGuide"] = ["Policy information", "Información de políticas", "Informations sur les politiques", "Informazioni sulle politiche", "Informações de políticas", "정책 안내", "ポリシーについて", "政策说明", "政策說明", "Thông tin chính sách"],
            ["Breadcrumb"] = ["Breadcrumb", "Ruta de navegación", "Fil d’Ariane", "Percorso di navigazione", "Trilha de navegação", "현재 위치", "パンくずリスト", "面包屑导航", "麵包屑導覽", "Đường dẫn điều hướng"],
            ["NoPolicyContent"] = ["No content has been added yet.", "Aún no se ha añadido contenido.", "Aucun contenu n’a encore été ajouté.", "Non è stato ancora aggiunto alcun contenuto.", "Ainda não há conteúdo cadastrado.", "아직 등록된 내용이 없습니다.", "内容はまだ登録されていません。", "尚未添加内容。", "尚未新增內容。", "Chưa có nội dung nào."],
            ["CompanyName"] = ["Company", "Empresa", "Entreprise", "Azienda", "Empresa", "상호명", "事業者名", "公司名称", "公司名稱", "Tên công ty"],
            ["Representative"] = ["Representative", "Representante", "Représentant", "Rappresentante", "Representante", "대표자명", "代表者", "负责人", "負責人", "Người đại diện"],
            ["BusinessNumber"] = ["Business registration no.", "N.º de registro mercantil", "N° d’immatriculation", "N. di registrazione impresa", "N.º de registro empresarial", "사업자등록번호", "事業者登録番号", "营业执照编号", "商業登記號碼", "Mã số doanh nghiệp"],
            ["BusinessAddress"] = ["Address", "Dirección", "Adresse", "Indirizzo", "Endereço", "주소", "住所", "地址", "地址", "Địa chỉ"],
            ["Manager"] = ["Operations manager", "Responsable de operaciones", "Responsable des opérations", "Responsabile operativo", "Gerente de operações", "운영관리자", "運営責任者", "运营负责人", "營運負責人", "Quản lý vận hành"],
            ["FooterPhone"] = ["Phone", "Teléfono", "Téléphone", "Telefono", "Telefone", "전화", "電話", "电话", "電話", "Điện thoại"],
            ["PoweredBy"] = ["Powered by", "Con tecnología de", "Propulsé par", "Offerto da", "Desenvolvido por", "Powered by", "Powered by", "技术支持", "技術支援", "Được cung cấp bởi"]
        };

    public static string Get(string key, string? language)
    {
        if (!Values.TryGetValue(key, out var translations))
            return key;

        var index = Array.IndexOf(LanguageOrder, Normalize(language));
        return translations[index < 0 ? 0 : index];
    }

    public static string Format(string key, string? language, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, Get(key, language), args);

    public static string Money(decimal amount, string? language)
    {
        var value = amount.ToString("N0", CultureInfo.InvariantCulture);
        return Normalize(language) == "ko" ? $"{value}원" : $"₩{value}";
    }

    private static string Normalize(string? language) =>
        LanguageOrder.Contains(language, StringComparer.OrdinalIgnoreCase)
            ? language!.ToLowerInvariant()
            : "en";
}
