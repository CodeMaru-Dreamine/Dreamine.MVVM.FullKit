namespace ShopPlatform.Models;

/// <summary>샵 테넌트 전체 설정. App_Data/Shops/{slug}/config.json 으로 저장.</summary>
public sealed class ShopConfig
{
    /// <summary>URL 슬러그 (소문자, 영숫자/하이픈).</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>상점 이름.</summary>
    public string ShopName { get; set; } = "내 쇼핑몰";

    /// <summary>상점 설명.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>로고/대표 이미지 경로.</summary>
    public string? LogoPath { get; set; }

    /// <summary>히어로 배너 문구 (예: "신선한 농산물을 직접 배송합니다").</summary>
    public string HeroText { get; set; } = string.Empty;

    /// <summary>히어로 서브 문구.</summary>
    public string HeroSubText { get; set; } = string.Empty;

    /// <summary>배너 배경 이미지 경로.</summary>
    public string? BannerImagePath { get; set; }

    /// <summary>추천 상품 표시 개수 (기본 4).</summary>
    public int FeaturedCount { get; set; } = 4;

    /// <summary>카카오톡·SNS 링크 미리보기 제목 (비우면 ShopName 사용).</summary>
    public string OgTitle { get; set; } = string.Empty;

    /// <summary>카카오톡·SNS 링크 미리보기 설명 (비우면 Description 사용).</summary>
    public string OgDescription { get; set; } = string.Empty;

    /// <summary>카카오톡·SNS 링크 미리보기 이미지 경로 (비우면 LogoPath 사용).</summary>
    public string? OgImagePath { get; set; }

    /// <summary>운영 여부.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>샵 화면에서 ShopStore 홈으로 이동하는 버튼 표시 여부.</summary>
    public bool ShowPlatformHomeLink { get; set; } = true;

    /// <summary>생성 시각 (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>어드민 비밀번호 해시 (BCrypt).</summary>
    public string AdminPasswordHash { get; set; } = string.Empty;

    /// <summary>CodeMaru 공용 로그인 기준 대표 관리자 ID.</summary>
    public string OwnerUserId { get; set; } = string.Empty;

    public string OwnerProvider { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerDisplayName { get; set; } = string.Empty;
    public DateTime? OwnerLinkedAt { get; set; }

    /// <summary>추가 운영 관리자 목록.</summary>
    public List<ShopAdminUser> AdminUsers { get; set; } = new();

    /// <summary>사업자 정보 (푸터 표시용).</summary>
    public ShopBusinessInfo Business { get; set; } = new();

    /// <summary>환불·교환·배송 정책 (상품 상세 페이지 하단 표시).</summary>
    public ShopPolicy Policy { get; set; } = new();

    /// <summary>결제 설정 (샵 오너가 직접 PG 가입 후 키 입력).</summary>
    public ShopPaymentConfig Payment { get; set; } = new();
}

/// <summary>사업자 정보. 푸터에 표시되는 법적 고지 정보.</summary>
public sealed class ShopBusinessInfo
{
    public string CompanyName      { get; set; } = string.Empty; // 상호명
    public string Representative   { get; set; } = string.Empty; // 대표자명
    public string BusinessNumber   { get; set; } = string.Empty; // 사업자등록번호
    public string Address          { get; set; } = string.Empty; // 주소
    public string ManagerName      { get; set; } = string.Empty; // 운영관리자
    public string Email            { get; set; } = string.Empty; // 이메일
    public string Phone            { get; set; } = string.Empty; // 전화
    public string TaxType          { get; set; } = string.Empty; // 간이과세자 등 세금 유형 문구
    public string CopyrightText    { get; set; } = string.Empty; // © 문구
}

public sealed class ShopAdminUser
{
    public string UserId { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Admin";
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>환불·교환·배송 정책 텍스트 (HTML 허용). 상품 상세 페이지 하단 아코디언에 표시.</summary>
public sealed class ShopPolicy
{
    public string RefundPolicy   { get; set; } = string.Empty; // 환불 정책
    public string ExchangePolicy { get; set; } = string.Empty; // 교환/반품 안내
    public string DeliveryPolicy { get; set; } = string.Empty; // 배송 안내
}

/// <summary>샵별 결제 설정. 오너가 토스 콘솔에서 발급받은 키를 입력.</summary>
public sealed class ShopPaymentConfig
{
    /// <summary>결제 활성화 여부 (false = 더미 모드로 동작).</summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>테스트 모드 여부. true면 test_ 키 사용.</summary>
    public bool IsTestMode { get; set; } = true;

    /// <summary>토스 클라이언트 키 (브라우저 노출용).</summary>
    public string TossClientKey { get; set; } = string.Empty;

    /// <summary>토스 시크릿 키 (서버 전용, 암호화 저장).</summary>
    public string TossSecretKeyEncrypted { get; set; } = string.Empty;

    /// <summary>결제 성공 리디렉션 URL (플랫폼이 자동 생성).</summary>
    public string SuccessReturnUrl { get; set; } = string.Empty;

    /// <summary>결제 실패 리디렉션 URL.</summary>
    public string FailReturnUrl { get; set; } = string.Empty;
}
