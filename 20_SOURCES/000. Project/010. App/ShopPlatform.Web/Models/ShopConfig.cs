namespace ShopPlatform.Models;

/// <summary>
/// \if KO
/// <para>샵 테넌트 전체 설정. App_Data/Shops/{slug}/config.json 으로 저장.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop config functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopConfig
{
    /// <summary>
    /// \if KO
    /// <para>URL 슬러그 (소문자, 영숫자/하이픈).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the slug value.</para>
    /// \endif
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>상점 이름.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the shop name value.</para>
    /// \endif
    /// </summary>
    public string ShopName { get; set; } = "내 쇼핑몰";

    /// <summary>
    /// \if KO
    /// <para>상점 설명.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the description value.</para>
    /// \endif
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>로고/대표 이미지 경로.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the logo path value.</para>
    /// \endif
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// \if KO
    /// <para>히어로 배너 문구 (예: "신선한 농산물을 직접 배송합니다").</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hero text value.</para>
    /// \endif
    /// </summary>
    public string HeroText { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>히어로 서브 문구.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the hero sub text value.</para>
    /// \endif
    /// </summary>
    public string HeroSubText { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>배너 배경 이미지 경로.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the banner image path value.</para>
    /// \endif
    /// </summary>
    public string? BannerImagePath { get; set; }

    /// <summary>
    /// \if KO
    /// <para>추천 상품 표시 개수 (기본 4).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the featured count value.</para>
    /// \endif
    /// </summary>
    public int FeaturedCount { get; set; } = 4;

    /// <summary>
    /// \if KO
    /// <para>카카오톡·SNS 링크 미리보기 제목 (비우면 ShopName 사용).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og title value.</para>
    /// \endif
    /// </summary>
    public string OgTitle { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>카카오톡·SNS 링크 미리보기 설명 (비우면 Description 사용).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og description value.</para>
    /// \endif
    /// </summary>
    public string OgDescription { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>카카오톡·SNS 링크 미리보기 이미지 경로 (비우면 LogoPath 사용).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the og image path value.</para>
    /// \endif
    /// </summary>
    public string? OgImagePath { get; set; }

    /// <summary>
    /// \if KO
    /// <para>운영 여부.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is active value.</para>
    /// \endif
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// \if KO
    /// <para>샵 화면에서 ShopStore 홈으로 이동하는 버튼 표시 여부.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the show platform home link value.</para>
    /// \endif
    /// </summary>
    public bool ShowPlatformHomeLink { get; set; } = true;

    /// <summary>
    /// \if KO
    /// <para>생성 시각 (UTC).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the created at value.</para>
    /// \endif
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// \if KO
    /// <para>어드민 비밀번호 해시 (BCrypt).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the admin password hash value.</para>
    /// \endif
    /// </summary>
    public string AdminPasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>CodeMaru 공용 로그인 기준 대표 관리자 ID.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner user id value.</para>
    /// \endif
    /// </summary>
    public string OwnerUserId { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>Owner Provider 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner provider value.</para>
    /// \endif
    /// </summary>
    public string OwnerProvider { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Owner Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner email value.</para>
    /// \endif
    /// </summary>
    public string OwnerEmail { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Owner Display Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner display name value.</para>
    /// \endif
    /// </summary>
    public string OwnerDisplayName { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Owner Linked At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the owner linked at value.</para>
    /// \endif
    /// </summary>
    public DateTime? OwnerLinkedAt { get; set; }

    /// <summary>
    /// \if KO
    /// <para>추가 운영 관리자 목록.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the admin users value.</para>
    /// \endif
    /// </summary>
    public List<ShopAdminUser> AdminUsers { get; set; } = new();

    /// <summary>
    /// \if KO
    /// <para>사업자 정보 (푸터 표시용).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the business value.</para>
    /// \endif
    /// </summary>
    public ShopBusinessInfo Business { get; set; } = new();

    /// <summary>
    /// \if KO
    /// <para>환불·교환·배송 정책 (상품 상세 페이지 하단 표시).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the policy value.</para>
    /// \endif
    /// </summary>
    public ShopPolicy Policy { get; set; } = new();

    /// <summary>
    /// \if KO
    /// <para>결제 설정 (샵 오너가 직접 PG 가입 후 키 입력).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the payment value.</para>
    /// \endif
    /// </summary>
    public ShopPaymentConfig Payment { get; set; } = new();
}

/// <summary>
/// \if KO
/// <para>사업자 정보. 푸터에 표시되는 법적 고지 정보.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop business info functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopBusinessInfo
{
    /// <summary>
    /// \if KO
    /// <para>Company Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the company name value.</para>
    /// \endif
    /// </summary>
    public string CompanyName      { get; set; } = string.Empty; // 상호명
    /// <summary>
    /// \if KO
    /// <para>Representative 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the representative value.</para>
    /// \endif
    /// </summary>
    public string Representative   { get; set; } = string.Empty; // 대표자명
    /// <summary>
    /// \if KO
    /// <para>Business Number 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the business number value.</para>
    /// \endif
    /// </summary>
    public string BusinessNumber   { get; set; } = string.Empty; // 사업자등록번호
    /// <summary>
    /// \if KO
    /// <para>Address 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the address value.</para>
    /// \endif
    /// </summary>
    public string Address          { get; set; } = string.Empty; // 주소
    /// <summary>
    /// \if KO
    /// <para>Manager Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the manager name value.</para>
    /// \endif
    /// </summary>
    public string ManagerName      { get; set; } = string.Empty; // 운영관리자
    /// <summary>
    /// \if KO
    /// <para>Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the email value.</para>
    /// \endif
    /// </summary>
    public string Email            { get; set; } = string.Empty; // 이메일
    /// <summary>
    /// \if KO
    /// <para>Phone 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the phone value.</para>
    /// \endif
    /// </summary>
    public string Phone            { get; set; } = string.Empty; // 전화
    /// <summary>
    /// \if KO
    /// <para>Tax Type 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the tax type value.</para>
    /// \endif
    /// </summary>
    public string TaxType          { get; set; } = string.Empty; // 간이과세자 등 세금 유형 문구
    /// <summary>
    /// \if KO
    /// <para>Copyright Text 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the copyright text value.</para>
    /// \endif
    /// </summary>
    public string CopyrightText    { get; set; } = string.Empty; // © 문구
}

/// <summary>
/// \if KO
/// <para>Shop Admin User 기능과 관련 상태를 캡슐화합니다.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop admin user functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopAdminUser
{
    /// <summary>
    /// \if KO
    /// <para>User Id 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the user id value.</para>
    /// \endif
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Provider 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the provider value.</para>
    /// \endif
    /// </summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Email 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the email value.</para>
    /// \endif
    /// </summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Display Name 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the display name value.</para>
    /// \endif
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// \if KO
    /// <para>Role 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the role value.</para>
    /// \endif
    /// </summary>
    public string Role { get; set; } = "Admin";
    /// <summary>
    /// \if KO
    /// <para>Added At 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the added at value.</para>
    /// \endif
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// \if KO
/// <para>환불·교환·배송 정책 텍스트 (HTML 허용). 상품 상세 페이지 하단 아코디언에 표시.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop policy functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopPolicy
{
    /// <summary>
    /// \if KO
    /// <para>Refund Policy 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the refund policy value.</para>
    /// \endif
    /// </summary>
    public string RefundPolicy   { get; set; } = string.Empty; // 환불 정책
    /// <summary>
    /// \if KO
    /// <para>Exchange Policy 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the exchange policy value.</para>
    /// \endif
    /// </summary>
    public string ExchangePolicy { get; set; } = string.Empty; // 교환/반품 안내
    /// <summary>
    /// \if KO
    /// <para>Delivery Policy 값을 가져오거나 설정합니다.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the delivery policy value.</para>
    /// \endif
    /// </summary>
    public string DeliveryPolicy { get; set; } = string.Empty; // 배송 안내
}

/// <summary>
/// \if KO
/// <para>샵별 결제 설정. 오너가 토스 콘솔에서 발급받은 키를 입력.</para>
/// \endif
/// \if EN
/// <para>Encapsulates shop payment config functionality and related state.</para>
/// \endif
/// </summary>
public sealed class ShopPaymentConfig
{
    /// <summary>
    /// \if KO
    /// <para>결제 활성화 여부 (false = 더미 모드로 동작).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is enabled value.</para>
    /// \endif
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// \if KO
    /// <para>테스트 모드 여부. true면 test_ 키 사용.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the is test mode value.</para>
    /// \endif
    /// </summary>
    public bool IsTestMode { get; set; } = true;

    /// <summary>
    /// \if KO
    /// <para>토스 클라이언트 키 (브라우저 노출용).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the toss client key value.</para>
    /// \endif
    /// </summary>
    public string TossClientKey { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>토스 시크릿 키 (서버 전용, 암호화 저장).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the toss secret key encrypted value.</para>
    /// \endif
    /// </summary>
    public string TossSecretKeyEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>결제 성공 리디렉션 URL (플랫폼이 자동 생성).</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the success return url value.</para>
    /// \endif
    /// </summary>
    public string SuccessReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// \if KO
    /// <para>결제 실패 리디렉션 URL.</para>
    /// \endif
    /// \if EN
    /// <para>Gets or sets the fail return url value.</para>
    /// \endif
    /// </summary>
    public string FailReturnUrl { get; set; } = string.Empty;
}
