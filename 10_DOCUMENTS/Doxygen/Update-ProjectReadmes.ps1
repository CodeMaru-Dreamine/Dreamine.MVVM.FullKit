param(
    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [switch]$SkipRichReadmes
)

$ErrorActionPreference = 'Stop'
$sourcesRoot = Join-Path $RepositoryRoot '20_SOURCES'
$appsRoot = Join-Path $sourcesRoot '000. Project\010. App'
$utf8 = [Text.UTF8Encoding]::new($false)

function Get-XmlValue {
    param([xml]$Xml, [string[]]$Names)

    foreach ($name in $Names) {
        $node = $Xml.SelectSingleNode("//*[local-name()='$name']")
        if ($node -and -not [string]::IsNullOrWhiteSpace($node.InnerText) -and $node.InnerText -notmatch '^\$\(') {
            return $node.InnerText.Trim()
        }
    }
    return $null
}

[xml]$rootProperties = [IO.File]::ReadAllText((Join-Path $sourcesRoot 'Directory.Build.props'))
$inheritedVersion = Get-XmlValue $rootProperties @('AppVersion', 'Version', 'VersionPrefix')
if ([string]::IsNullOrWhiteSpace($inheritedVersion)) { $inheritedVersion = '1.0.0' }

function Get-ProjectMetadata {
    param([string]$ProjectPath)

    [xml]$projectXml = [IO.File]::ReadAllText($ProjectPath)
    $name = Get-XmlValue $projectXml @('PackageId', 'AssemblyName', 'Title')
    if ([string]::IsNullOrWhiteSpace($name)) { $name = [IO.Path]::GetFileNameWithoutExtension($ProjectPath) }
    $version = Get-XmlValue $projectXml @('Version', 'VersionPrefix', 'PackageVersion')
    if ([string]::IsNullOrWhiteSpace($version)) { $version = $inheritedVersion }
    $framework = Get-XmlValue $projectXml @('TargetFramework', 'TargetFrameworks')
    if ([string]::IsNullOrWhiteSpace($framework)) { $framework = 'See project file' }

    return [pscustomobject]@{
        Name = $name
        Version = $version
        Framework = $framework
        ProjectFile = [IO.Path]::GetFileName($ProjectPath)
    }
}

function Add-Section {
    param([Text.StringBuilder]$Builder, [string]$Title, [string[]]$Lines)

    [void]$Builder.AppendLine("## $Title")
    [void]$Builder.AppendLine()
    foreach ($line in $Lines) { [void]$Builder.AppendLine($line) }
    [void]$Builder.AppendLine()
}

function New-RichReadme {
    param(
        [pscustomobject]$Definition,
        [pscustomobject]$Metadata,
        [bool]$Korean
    )

    $b = [Text.StringBuilder]::new()
    $displayName = $Definition.DisplayName
    $summary = if ($Korean) { $Definition.SummaryKo } else { $Definition.SummaryEn }
    $role = if ($Korean) { $Definition.RoleKo } else { $Definition.RoleEn }
    $features = if ($Korean) { $Definition.FeaturesKo } else { $Definition.FeaturesEn }
    $steps = if ($Korean) { $Definition.StepsKo } else { $Definition.StepsEn }
    $frameworkBadge = $Metadata.Framework.Replace('-', '--').Replace(';', '%20%7C%20').Replace(' ', '%20')
    $versionBadge = $Metadata.Version.Replace('-', '--').Replace(' ', '%20')

    [void]$b.AppendLine("# $displayName")
    [void]$b.AppendLine()
    [void]$b.AppendLine("> $summary")
    [void]$b.AppendLine()
    [void]$b.AppendLine("![.NET](https://img.shields.io/badge/.NET-$frameworkBadge-512BD4) ![Version](https://img.shields.io/badge/version-$versionBadge-2563EB) ![Source](https://img.shields.io/badge/source-open-16A34A)")
    [void]$b.AppendLine()

    $links = [Collections.Generic.List[string]]::new()
    if ($Definition.Site) { $links.Add("[$(if ($Korean) { '서비스 열기' } else { 'Open service' })]($($Definition.Site))") }
    if ($Definition.Guide) { $links.Add("[$(if ($Korean) { '이용 설명서' } else { 'User guide' })]($($Definition.Guide))") }
    $links.Add("[GitHub](https://github.com/CodeMaru-Dreamine)")
    [void]$b.AppendLine(($links -join ' · '))
    [void]$b.AppendLine()

    if ($Korean) {
        Add-Section $b '프로젝트 소개' @($summary, '', $role)
        Add-Section $b '주요 기능' @($features | ForEach-Object { "- $_" })
        Add-Section $b '이용 순서' @($steps | ForEach-Object -Begin { $i = 0 } -Process { $i++; "$i. $_" })
        Add-Section $b '프로젝트 정보' @(
            '| 항목 | 값 |',
            '|---|---|',
            "| 프로젝트 | $($Metadata.Name) |",
            "| 버전 | $($Metadata.Version) |",
            "| 대상 프레임워크 | $($Metadata.Framework) |",
            "| 프로젝트 파일 | $($Metadata.ProjectFile) |"
        )
        Add-Section $b '개발 환경에서 실행' @(
            '```powershell',
            "dotnet run --project `"$($Metadata.ProjectFile)`"",
            '```'
        )
        Add-Section $b 'API 문서 생성' @(
            '```powershell',
            'doxygen Doxyfile.kr',
            '```',
            '영문 문서는 `Doxyfile.en`으로 생성합니다.'
        )
    }
    else {
        Add-Section $b 'Overview' @($summary, '', $role)
        Add-Section $b 'Key features' @($features | ForEach-Object { "- $_" })
        Add-Section $b 'How to use' @($steps | ForEach-Object -Begin { $i = 0 } -Process { $i++; "$i. $_" })
        Add-Section $b 'Project information' @(
            '| Item | Value |',
            '|---|---|',
            "| Project | $($Metadata.Name) |",
            "| Version | $($Metadata.Version) |",
            "| Target framework | $($Metadata.Framework) |",
            "| Project file | $($Metadata.ProjectFile) |"
        )
        Add-Section $b 'Run for development' @(
            '```powershell',
            "dotnet run --project `"$($Metadata.ProjectFile)`"",
            '```'
        )
        Add-Section $b 'Generate API documentation' @(
            '```powershell',
            'doxygen Doxyfile.en',
            '```',
            'Generate the Korean documentation with `Doxyfile.kr`.'
        )
    }

    return $b.ToString().Trim() + "`r`n"
}

$definitions = @(
    [pscustomobject]@{
        Project='Codemaru'; DisplayName='CodeMaru · CardHybrid'; Site='https://codemaru.co.kr/cardhybrid'; Guide='https://codemaru.co.kr/guide/cardhybrid'
        SummaryKo='QR 코드, 모바일 랜딩 페이지, vCard 연락처 저장과 명함 디자인을 한 화면에서 제공하는 디지털 명함 서비스입니다.'
        SummaryEn='A digital business-card service combining QR codes, mobile landing pages, vCard contact export, and card design in one workflow.'
        RoleKo='CodeMaru 서비스 허브와 CardHybrid 편집기, 공개 랜딩 페이지, 인증·저장 흐름을 호스팅하는 애플리케이션입니다.'
        RoleEn='Hosts the CodeMaru service hub, CardHybrid editor, public landing pages, authentication, and saved-card workflows.'
        FeaturesKo=@('SVG QR 코드와 모바일 랜딩 페이지 실시간 생성','vCard 연락처 파일 내려받기','앞·뒤 명함 레이아웃과 색상·폰트 편집','AI 로고 배경 제거와 SVG/HTML 내보내기','로그인 기반 명함 버전 저장·복원')
        FeaturesEn=@('Real-time SVG QR code and mobile landing-page generation','Downloadable vCard contacts','Front/back card layout, color, and font editing','AI logo background removal and SVG/HTML export','Authenticated card version history and restore')
        StepsKo=@('CardHybrid 화면을 열고 이름·소속·연락처를 입력합니다.','브랜드 색상과 로고, 명함 앞·뒤 레이아웃을 편집합니다.','실시간 QR과 모바일 랜딩 페이지를 확인합니다.','SVG/HTML로 내보내거나 QR 링크를 공유합니다.','로그인 사용자는 원하는 버전을 이력으로 저장합니다.')
        StepsEn=@('Open CardHybrid and enter identity and contact details.','Customize brand colors, logo, and both card faces.','Verify the live QR code and mobile landing page.','Export SVG/HTML or share the QR link.','Signed-in users can save and restore versions.')
    },
    [pscustomobject]@{
        Project='Dreamine.Web'; DisplayName='Dreamine'; Site='https://dreamine.kr/'; Guide='https://codemaru.co.kr/guide/dreamine'
        SummaryKo='WPF와 Blazor를 한 코드 흐름으로 연결하고 반복적인 MVVM 코드를 줄이는 오픈소스 FullKit 공식 웹 애플리케이션입니다.'
        SummaryEn='The official web application for the open-source FullKit that connects WPF and Blazor workflows while reducing repetitive MVVM code.'
        RoleKo='FullKit 소개, 패키지·예제 탐색, 지식 그래프와 개발 문서를 제공하는 공식 포털입니다.'
        RoleEn='Provides the FullKit portal, package and example discovery, knowledge graph, and developer documentation.'
        FeaturesKo=@('FullKit 패키지·레이어 소개','초보자용 레시피와 문제 해결 문서','프로젝트별 API·Doxygen 문서 진입점','전체 소스 지식 그래프','GitHub·NuGet 공식 링크')
        FeaturesEn=@('FullKit package and layer overview','Beginner recipes and troubleshooting','Project-level API and Doxygen entry points','Whole-solution knowledge graph','Official GitHub and NuGet links')
        StepsKo=@('시작 페이지에서 5분 빠른 시작을 확인합니다.','목적에 맞는 WPF·통신·PLC·Hybrid 레시피를 선택합니다.','API 페이지에서 클래스·메서드 계약을 확인합니다.','구조가 필요하면 지식 그래프를 새 창에서 엽니다.')
        StepsEn=@('Read the five-minute quick start.','Choose a WPF, communication, PLC, or Hybrid recipe.','Inspect class and method contracts in the API section.','Open the knowledge graph when architectural context is needed.')
    },
    [pscustomobject]@{
        Project='WeddingPlatform.Web'; DisplayName='Wedding'; Site='https://wedding.codemaru.co.kr/'; Guide='https://codemaru.co.kr/guide/wedding'
        SummaryKo='지도, 갤러리, 방명록, 계좌 안내와 배경음악을 링크 하나에 담는 무료 모바일 청첩장 서비스입니다.'
        SummaryEn='A free mobile invitation service combining maps, galleries, guestbook, account details, and music behind one shareable link.'
        RoleKo='청첩장 작성·관리, 공개 초대 페이지, 미디어 업로드와 하객 방명록 흐름을 제공합니다.'
        RoleEn='Provides invitation authoring and management, public invitation pages, media uploads, and guestbook workflows.'
        FeaturesKo=@('OpenStreetMap 및 카카오·네이버 길찾기','사진 갤러리와 동영상·배경음악','하객 방명록과 CSV 관리','계좌번호 복사·카카오페이 링크','OG 이미지 기반 카카오톡 미리보기')
        FeaturesEn=@('OpenStreetMap with Kakao/Naver directions','Photo gallery, video, and background music','Guestbook with CSV administration','Copyable account and KakaoPay links','Open Graph previews for social sharing')
        StepsKo=@('로그인 후 신랑·신부, 일시와 장소를 입력합니다.','사진·음악·동영상과 계좌 안내를 등록합니다.','테마와 소개 문구를 확인합니다.','생성된 링크를 카카오톡·문자·SNS로 공유합니다.')
        StepsEn=@('Sign in and enter names, date, and venue.','Add photos, music, video, and account information.','Review the theme and invitation copy.','Share the generated link through messaging or social media.')
    },
    [pscustomobject]@{
        Project='WeddingThankYou'; DisplayName='ThankYou'; Site='https://thankyou.codemaru.co.kr/'; Guide='https://codemaru.co.kr/guide/thankyou'
        SummaryKo='결혼식 이후 감사 인사와 사진, 계좌 안내, 연락처를 모바일 페이지로 전달하는 감사장 서비스입니다.'
        SummaryEn='A post-wedding thank-you service for sharing messages, photos, account details, and contact information on a mobile page.'
        RoleKo='Wedding 이후의 감사 메시지를 별도 주소로 작성·관리하고 하객에게 공유하는 흐름을 담당합니다.'
        RoleEn='Manages the post-Wedding thank-you workflow on a separate address for focused sharing with guests.'
        FeaturesKo=@('모바일 감사 인사 페이지','본식·스냅 사진 갤러리','선택적 계좌 안내와 복사 버튼','청첩장과 분리된 공유 링크','감사장 전용 문구와 테마 편집')
        FeaturesEn=@('Mobile thank-you pages','Wedding and snapshot photo galleries','Optional account information with copy actions','Share links separate from the invitation','Thank-you-specific copy and theme editing')
        StepsKo=@('신랑·신부 이름과 감사 인사말을 입력합니다.','결혼식 사진과 대표 이미지를 올립니다.','필요한 연락처·계좌·공유 문구를 정리합니다.','감사장 링크를 하객에게 전달합니다.')
        StepsEn=@('Enter the couple names and thank-you message.','Upload wedding photos and a representative image.','Add optional contact, account, and sharing text.','Send the thank-you link to guests.')
    },
    [pscustomobject]@{
        Project='Families.Web'; DisplayName='Families'; Site='https://families.codemaru.co.kr/'; Guide='https://codemaru.co.kr/guide/families'
        SummaryKo='사진, 동영상, 글, 댓글과 반응을 가족끼리만 공유하는 비공개 앨범·타임라인 서비스입니다.'
        SummaryEn='A private family album and timeline for sharing photos, videos, stories, comments, and reactions.'
        RoleKo='가족 그룹 인증, 비공개 포스트·앨범, 댓글·반응과 미디어 제공을 담당하는 웹 애플리케이션입니다.'
        RoleEn='Provides family-group access, private posts and albums, comments, reactions, and media delivery.'
        FeaturesKo=@('그룹 비밀번호 기반 비공개 접근','사진·영상·YouTube·Markdown 포스트','이벤트별 앨범 폴더','포스트 고정·댓글·이모지 반응','라이트·다크 테마와 그룹 커버')
        FeaturesEn=@('Password-protected private groups','Photo, video, YouTube, and Markdown posts','Event-oriented album folders','Pinned posts, comments, and emoji reactions','Light/dark themes and group covers')
        StepsKo=@('가족 그룹을 만들고 비밀번호를 지정합니다.','그룹 링크와 비밀번호를 가족에게 공유합니다.','포스트 또는 앨범을 만들어 미디어와 이야기를 올립니다.','댓글과 반응으로 가족 기록을 이어갑니다.')
        StepsEn=@('Create a family group and set its password.','Share the group link and password with family.','Create posts or albums for media and stories.','Continue the family record through comments and reactions.')
    },
    [pscustomobject]@{
        Project='Families.AutoWriter'; DisplayName='Families AutoWriter'; Site='https://families.codemaru.co.kr/'; Guide='https://codemaru.co.kr/guide/families'
        SummaryKo='Families 콘텐츠를 반복 작업 없이 작성·등록하도록 돕는 자동화 작성 도구입니다.'
        SummaryEn='An authoring automation tool that helps prepare and publish Families content without repetitive manual work.'
        RoleKo='Families.Web의 포스트·미디어 입력을 보조하는 운영자용 작성 애플리케이션입니다.'
        RoleEn='An operator-facing authoring companion for preparing posts and media consumed by Families.Web.'
        FeaturesKo=@('가족 콘텐츠 초안 작성','미디어·본문 입력 자동화','Families.Web 연계','운영자 중심 반복 작업 단축')
        FeaturesEn=@('Family-content draft authoring','Automated media and body entry','Families.Web integration','Reduced repetitive operator work')
        StepsKo=@('연결할 Families 환경을 확인합니다.','게시할 본문과 미디어를 준비합니다.','자동 작성 결과를 검토합니다.','Families.Web에서 최종 게시 상태를 확인합니다.')
        StepsEn=@('Verify the target Families environment.','Prepare post text and media.','Review the generated authoring result.','Confirm final publication in Families.Web.')
    },
    [pscustomobject]@{
        Project='Portfolio.Web'; DisplayName='Portfolio'; Site='https://portfolio.codemaru.co.kr/'; Guide='https://codemaru.co.kr/guide/portfolio'
        SummaryKo='.NET, Blazor, WPF와 서비스 운영 경험을 프로젝트·이력·기술 스택 단위로 보여주는 개발자 포트폴리오입니다.'
        SummaryEn='A developer portfolio presenting .NET, Blazor, WPF, and service-operation experience through projects, history, and skills.'
        RoleKo='프로젝트 데이터, 갤러리·동영상, 이력서, 검색·필터와 OG 공유 화면을 제공합니다.'
        RoleEn='Provides project data, galleries and video, resume content, search and filters, and Open Graph sharing.'
        FeaturesKo=@('프로젝트 카드 검색·카테고리 필터','다중 이미지와 동영상 갤러리','GitHub·라이브·문서 링크','기술 스택·경력·학력 타임라인','보호가 필요한 이미지 단계별 블러')
        FeaturesEn=@('Project search and category filters','Multi-image and video galleries','GitHub, live, and documentation links','Skills, career, and education timelines','Tiered blur protection for sensitive images')
        StepsKo=@('홈에서 검색어와 카테고리로 프로젝트를 찾습니다.','상세 페이지에서 이미지·동영상과 사용 기술을 확인합니다.','GitHub·Live·문서 링크로 결과물을 엽니다.','이력서 섹션에서 경력과 기술 스택을 확인합니다.')
        StepsEn=@('Find projects using search and category filters.','Inspect media and technologies on a project detail page.','Open GitHub, live, or documentation links.','Review career and skills in the resume section.')
    },
    [pscustomobject]@{
        Project='DreamineVMS'; DisplayName='DreamineVMS Agent'; Site='https://cctvviewer.codemaru.co.kr/'; Guide='https://codemaru.co.kr/guide/cctv'
        SummaryKo='Windows PC의 RTSP·USB 카메라 영상을 HLS로 변환해 원격 CCTV Viewer에 전달하는 데스크톱 에이전트입니다.'
        SummaryEn='A Windows desktop agent that converts RTSP or USB-camera streams to HLS for the remote CCTV Viewer.'
        RoleKo='카메라 연결, FFmpeg 트랜스코딩, 계정·장치 등록과 스트림 상태 관리를 담당합니다.'
        RoleEn='Handles camera connectivity, FFmpeg transcoding, account/device registration, and stream state management.'
        FeaturesKo=@('RTSP 및 USB 카메라 연결','FFmpeg 기반 HLS 변환','다중 카메라와 표시 순서 관리','자동 재접속과 스트림 상태 확인','계정 기반 원격 서버 등록')
        FeaturesEn=@('RTSP and USB-camera connectivity','FFmpeg-based HLS conversion','Multiple cameras and display ordering','Automatic reconnect and stream health','Account-based remote server registration')
        StepsKo=@('Windows 10 이상 PC에 에이전트를 실행합니다.','CCTV Viewer 계정 정보를 입력해 장치를 등록합니다.','RTSP URL 또는 연결된 카메라를 추가합니다.','웹 라이브 뷰에서 실시간 스트림을 확인합니다.')
        StepsEn=@('Run the agent on Windows 10 or later.','Register the device with CCTV Viewer credentials.','Add an RTSP URL or connected camera.','Verify the live stream in the web viewer.')
    },
    [pscustomobject]@{
        Project='DreamineVMS.Web'; DisplayName='CCTV Viewer'; Site='https://cctvviewer.codemaru.co.kr/'; Guide='https://codemaru.co.kr/guide/cctv'
        SummaryKo='DreamineVMS 에이전트가 제공하는 HLS 카메라 영상을 브라우저에서 관리·재생하는 원격 CCTV 웹 서비스입니다.'
        SummaryEn='A remote CCTV web service for managing and playing HLS camera streams supplied by DreamineVMS agents.'
        RoleKo='사용자 인증, 장치·카메라 관리, 라이브 뷰, 공개 링크와 OG 메타데이터를 제공합니다.'
        RoleEn='Provides authentication, device and camera management, live viewing, public links, and Open Graph metadata.'
        FeaturesKo=@('브라우저 기반 실시간 HLS 재생','계정별 장치·다중 카메라 관리','로그인 없는 공개 라이브 링크','카메라별 OG 제목·설명·이미지','PBKDF2 인증과 장기 세션')
        FeaturesEn=@('Browser-based live HLS playback','Per-account devices and multiple cameras','Public live links without sign-in','Per-camera Open Graph title, description, and image','PBKDF2 authentication and persistent sessions')
        StepsKo=@('서비스에서 계정을 생성합니다.','카메라 PC에 DreamineVMS 에이전트를 연결합니다.','카메라 관리 화면에서 스트림을 등록합니다.','라이브 뷰 또는 공개 링크로 영상을 확인합니다.')
        StepsEn=@('Create a service account.','Connect a DreamineVMS agent on the camera PC.','Register streams in camera management.','Watch through live view or a public link.')
    },
    [pscustomobject]@{
        Project='ShopPlatform.Web'; DisplayName='Shop Store'; Site='https://shop.codemaru.co.kr/'; Guide='https://codemaru.co.kr/guide/shop'
        SummaryKo='농산물, 소프트웨어 라이선스와 개발 용역을 직접 판매하는 CodeMaru 직영 쇼핑몰입니다.'
        SummaryEn='The CodeMaru direct store for agricultural products, software licenses, and development services.'
        RoleKo='상품 탐색, 장바구니, 주문·회원 관리, 배송·환불 정책과 입점 신청 흐름을 제공합니다.'
        RoleEn='Provides product discovery, cart, orders and members, shipping/refund policies, and seller applications.'
        FeaturesKo=@('검색·카테고리·가격/최신순 정렬','실시간 합계 장바구니','배송·환불·교환 정책 페이지','농산물·소프트웨어·개발 용역 상품','외부 판매자 입점 신청')
        FeaturesEn=@('Search, categories, and price/latest sorting','Cart with live totals','Shipping, refund, and exchange policies','Agriculture, software, and development products','External seller applications')
        StepsKo=@('상품을 검색하거나 카테고리에서 선택합니다.','상세 설명과 이미지를 확인해 장바구니에 담습니다.','수량과 합계를 확인하고 배송·결제 정보를 입력합니다.','주문 완료 화면에서 주문 내역을 확인합니다.')
        StepsEn=@('Find a product through search or categories.','Review details and add it to the cart.','Confirm quantity and totals, then enter delivery and payment information.','Review the order confirmation.')
    }
)

$richCount = 0
if (-not $SkipRichReadmes) {
    foreach ($definition in $definitions) {
        $projectPath = Get-ChildItem -LiteralPath $appsRoot -Recurse -Filter "$($definition.Project).csproj" | Select-Object -First 1
        if (-not $projectPath) { throw "Project not found: $($definition.Project)" }
        $metadata = Get-ProjectMetadata $projectPath.FullName
        $directory = Split-Path $projectPath.FullName
        [IO.File]::WriteAllText((Join-Path $directory 'README.md'), (New-RichReadme $definition $metadata $false), $utf8)
        [IO.File]::WriteAllText((Join-Path $directory 'README_KO.md'), (New-RichReadme $definition $metadata $true), $utf8)
        $richCount += 2
    }
}

$placeholderCount = 0
$readmePaths = @(rg --files $sourcesRoot -g 'README*.md' -g '!**/bin/**' -g '!**/obj/**' -g '!**/wwwroot/**')
foreach ($relativePath in $readmePaths) {
    $path = (Resolve-Path $relativePath).Path
    $content = [IO.File]::ReadAllText($path)
    if ($content -notmatch '\$(Version|TargetFramework)') { continue }

    $directory = Split-Path $path
    $projectPath = Get-ChildItem -LiteralPath $directory -Filter '*.csproj' | Select-Object -First 1
    if (-not $projectPath) { continue }
    $metadata = Get-ProjectMetadata $projectPath.FullName
    $updated = $content.Replace('$Version', $metadata.Version).Replace('$TargetFramework', $metadata.Framework)
    [IO.File]::WriteAllText($path, $updated, $utf8)
    $placeholderCount++
}

Write-Output "RichReadmes=$richCount"
Write-Output "PlaceholderReadmesUpdated=$placeholderCount"
