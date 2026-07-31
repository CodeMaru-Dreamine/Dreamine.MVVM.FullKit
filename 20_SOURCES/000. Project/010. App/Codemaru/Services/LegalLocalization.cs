namespace Codemaru.Services;

public sealed record LegalSection(string Heading, string? Paragraph = null, string[]? Items = null);

public sealed record LegalDocument(
    string Title,
    string Description,
    string Updated,
    IReadOnlyList<LegalSection> Sections);

public static class LegalLocalization
{
    public static LegalDocument Privacy(string language) => language switch
    {
        "ko" => new(
            "개인정보처리방침",
            "CodeMaru 서비스의 개인정보 수집, 이용, 보관, 파기 및 이용자 권리 안내입니다.",
            "최종 업데이트: 2026년 1월 1일",
            [
                new("1. 수집하는 개인정보 항목", "CodeMaru는 서비스 이용 과정에서 아래와 같은 최소한의 정보를 수집합니다.", ["문의하기: 이름, 이메일 주소, 문의 내용", "서비스 이용 기록 (접속 IP, 접속 일시, 브라우저 정보)"]),
                new("2. 개인정보의 수집 및 이용 목적", Items: ["문의 답변 및 서비스 안내", "서비스 품질 개선 및 오류 분석"]),
                new("3. 개인정보의 보유 및 이용 기간", "수집된 개인정보는 목적 달성 후 즉시 파기합니다. 단, 관계 법령에 따라 보존이 필요한 경우 해당 기간 동안 보관합니다."),
                new("4. 개인정보의 제3자 제공", "CodeMaru는 이용자의 동의 없이 개인정보를 외부에 제공하지 않습니다. 단, 법령에 의거하거나 수사 목적으로 관계 기관의 요청이 있는 경우는 예외로 합니다."),
                new("5. 개인정보 보호 책임자", "이름: 장민수"),
                new("6. 정책 변경 안내", "본 방침은 법령·정책 변경에 따라 사전 공지 후 개정될 수 있습니다.")
            ]),
        "ja" => new(
            "プライバシーポリシー",
            "CodeMaruサービスにおける個人情報の収集、利用、保管、破棄、および利用者の権利についてご案内します。",
            "最終更新日：2026年1月1日",
            [
                new("1. 収集する個人情報", "CodeMaruはサービス利用時に必要最小限の情報を収集します。", ["お問い合わせ：氏名、メールアドレス、お問い合わせ内容", "サービス利用記録（IPアドレス、アクセス日時、ブラウザー情報）"]),
                new("2. 個人情報の利用目的", Items: ["お問い合わせへの回答およびサービス案内", "サービス品質の改善およびエラー分析"]),
                new("3. 保有および利用期間", "収集した個人情報は目的達成後速やかに破棄します。法令により保存が必要な場合は、定められた期間保管します。"),
                new("4. 第三者への提供", "CodeMaruは利用者の同意なく個人情報を外部へ提供しません。ただし、法令または関係機関からの適法な要請がある場合を除きます。"),
                new("5. 個人情報保護責任者", "氏名：チャン・ミンス"),
                new("6. ポリシーの変更", "法令または方針の変更により、本ポリシーを事前告知のうえ改定する場合があります。")
            ]),
        "es" => PrivacyDocument("Política de privacidad", "Información sobre la recogida, uso, conservación y eliminación de datos personales.", "Última actualización: 1 de enero de 2026",
            "Datos personales recopilados", "Solo recogemos los datos mínimos: nombre, correo y mensaje de contacto, además de IP, hora de acceso y navegador.",
            "Finalidad del tratamiento", "Responder consultas, prestar el servicio, mejorar la calidad y analizar errores.",
            "Conservación", "Eliminamos los datos al cumplir su finalidad, salvo conservación exigida por ley.",
            "Cesión a terceros", "No cedemos datos sin consentimiento, salvo obligación legal o solicitud válida de una autoridad.",
            "Contacto de privacidad", "Responsable: Minsu Jang · admin@codemaru.co.kr",
            "Cambios", "Podemos actualizar esta política con aviso previo."),
        "fr" => PrivacyDocument("Politique de confidentialité", "Informations sur la collecte, l’utilisation, la conservation et la suppression des données personnelles.", "Dernière mise à jour : 1er janvier 2026",
            "Données collectées", "Nous recueillons le minimum nécessaire : nom, e-mail et message, ainsi que l’adresse IP, l’heure d’accès et le navigateur.",
            "Finalités", "Répondre aux demandes, fournir le service, améliorer sa qualité et analyser les erreurs.",
            "Conservation", "Les données sont supprimées une fois leur finalité atteinte, sauf obligation légale.",
            "Transmission à des tiers", "Aucune transmission sans consentement, sauf obligation légale ou demande valide d’une autorité.",
            "Contact confidentialité", "Responsable : Minsu Jang · admin@codemaru.co.kr",
            "Modifications", "Cette politique peut être mise à jour après information préalable."),
        "it" => PrivacyDocument("Informativa sulla privacy", "Informazioni su raccolta, uso, conservazione e cancellazione dei dati personali.", "Ultimo aggiornamento: 1 gennaio 2026",
            "Dati raccolti", "Raccogliamo solo il minimo necessario: nome, email e messaggio, oltre a IP, ora di accesso e browser.",
            "Finalità", "Rispondere alle richieste, fornire il servizio, migliorarne la qualità e analizzare gli errori.",
            "Conservazione", "I dati vengono cancellati al raggiungimento dello scopo, salvo obblighi di legge.",
            "Comunicazione a terzi", "Non comunichiamo dati senza consenso, salvo obbligo di legge o richiesta valida di un’autorità.",
            "Contatto privacy", "Responsabile: Minsu Jang · admin@codemaru.co.kr",
            "Modifiche", "Questa informativa può essere aggiornata con preavviso."),
        "pt" => PrivacyDocument("Política de privacidade", "Informações sobre coleta, uso, retenção e exclusão de dados pessoais.", "Última atualização: 1 de janeiro de 2026",
            "Dados coletados", "Coletamos somente o mínimo necessário: nome, email e mensagem, além de IP, horário de acesso e navegador.",
            "Finalidade", "Responder consultas, prestar o serviço, melhorar a qualidade e analisar erros.",
            "Retenção", "Os dados são excluídos após cumprir a finalidade, salvo obrigação legal.",
            "Compartilhamento", "Não compartilhamos dados sem consentimento, exceto por obrigação legal ou pedido válido de autoridade.",
            "Contato de privacidade", "Responsável: Minsu Jang · admin@codemaru.co.kr",
            "Alterações", "Esta política pode ser atualizada mediante aviso prévio."),
        "vi" => PrivacyDocument("Chính sách quyền riêng tư", "Thông tin về việc thu thập, sử dụng, lưu giữ và xóa dữ liệu cá nhân.", "Cập nhật lần cuối: 1 tháng 1 năm 2026",
            "Dữ liệu thu thập", "Chúng tôi chỉ thu thập dữ liệu tối thiểu: tên, email, nội dung liên hệ, IP, thời gian truy cập và trình duyệt.",
            "Mục đích sử dụng", "Trả lời yêu cầu, cung cấp dịch vụ, cải thiện chất lượng và phân tích lỗi.",
            "Thời gian lưu giữ", "Dữ liệu được xóa khi hoàn thành mục đích, trừ khi pháp luật yêu cầu lưu giữ.",
            "Chia sẻ với bên thứ ba", "Không chia sẻ khi chưa có đồng ý, trừ nghĩa vụ pháp lý hoặc yêu cầu hợp lệ.",
            "Liên hệ quyền riêng tư", "Người phụ trách: Minsu Jang · admin@codemaru.co.kr",
            "Thay đổi", "Chính sách có thể được cập nhật sau khi thông báo trước."),
        "zh-hans" => PrivacyDocument("隐私政策", "关于个人数据收集、使用、保存、删除及用户权利的说明。", "最后更新：2026年1月1日",
            "收集的数据", "仅收集提供服务所需的最少数据：姓名、电子邮件、留言、IP、访问时间和浏览器信息。",
            "使用目的", "回复咨询、提供服务、改进质量并分析错误。",
            "保存期限", "目的完成后删除数据，法律要求保存的情况除外。",
            "向第三方披露", "未经同意不会披露，法律义务或主管机关的合法要求除外。",
            "隐私联系人", "负责人：Minsu Jang · admin@codemaru.co.kr",
            "政策变更", "本政策可能在事先通知后更新。"),
        "zh-hant" => PrivacyDocument("隱私權政策", "關於個人資料蒐集、使用、保存、刪除及使用者權利的說明。", "最後更新：2026年1月1日",
            "蒐集的資料", "僅蒐集提供服務所需的最少資料：姓名、電子郵件、留言、IP、存取時間與瀏覽器資訊。",
            "使用目的", "回覆諮詢、提供服務、改善品質並分析錯誤。",
            "保存期限", "目的完成後刪除資料，法律要求保存的情況除外。",
            "向第三方揭露", "未經同意不會揭露，法律義務或主管機關的合法要求除外。",
            "隱私聯絡人", "負責人：Minsu Jang · admin@codemaru.co.kr",
            "政策變更", "本政策可能在事先通知後更新。"),
        _ => new(
            "Privacy Policy",
            "Information about how CodeMaru collects, uses, retains, and deletes personal data and about your privacy rights.",
            "Last updated: January 1, 2026",
            [
                new("1. Personal data we collect", "CodeMaru collects only the minimum information needed to provide its services.", ["Contact requests: name, email address, and message", "Service usage records: IP address, access time, and browser information"]),
                new("2. How we use personal data", Items: ["Responding to inquiries and providing service information", "Improving service quality and analyzing errors"]),
                new("3. Retention period", "Personal data is deleted after its purpose has been fulfilled, unless applicable law requires retention for a specified period."),
                new("4. Disclosure to third parties", "CodeMaru does not disclose personal data without consent, except when required by law or a lawful request from a competent authority."),
                new("5. Privacy contact", "Name: Minsu Jang"),
                new("6. Changes to this policy", "This policy may be revised after prior notice when laws or operating policies change.")
            ])
    };

    public static LegalDocument Terms(string language) => language switch
    {
        "ko" => new(
            "이용약관",
            "CodeMaru가 제공하는 서비스의 이용 조건, 권리와 의무, 책임 사항을 안내합니다.",
            "최종 업데이트: 2026년 1월 1일",
            [
                new("제1조 (목적)", "본 약관은 CodeMaru가 제공하는 서비스의 이용 조건과 절차, 이용자와 서비스 간의 권리·의무 및 책임 사항을 규정합니다."),
                new("제2조 (서비스의 제공)", "CodeMaru는 다음 서비스를 제공합니다.", ["CardHybrid: 디지털 명함 및 QR 코드", "Wedding: 디지털 청첩장", "Families: 가족 라이프 플랫폼", "CCTV Viewer: 실시간 카메라 모니터링", "ShopStore: 직영 쇼핑몰", "기타 CodeMaru 운영 서비스"]),
                new("제3조 (이용자의 의무)", Items: ["서비스를 불법적인 목적으로 사용하지 않습니다.", "타인의 개인정보를 무단 수집·이용하지 않습니다.", "서비스의 안정적 운영을 방해하지 않습니다."]),
                new("제4조 (서비스 변경 및 중단)", "운영상 또는 기술상 필요에 따라 서비스를 변경하거나 중단할 수 있으며, 이 경우 사전에 공지합니다."),
                new("제5조 (면책)", "천재지변, 서비스 장애 또는 이용자 귀책 사유로 발생한 손해에 대해서는 책임을 지지 않습니다."),
                new("제6조 (문의)", "이용약관 관련 문의")
            ]),
        "ja" => new(
            "利用規約",
            "CodeMaruが提供するサービスの利用条件、権利、義務および責任についてご案内します。",
            "最終更新日：2026年1月1日",
            [
                new("第1条（目的）", "本規約はCodeMaruが提供するサービスの利用条件と手続き、利用者とサービス間の権利、義務および責任を定めます。"),
                new("第2条（サービスの提供）", "CodeMaruは次のサービスを提供します。", ["CardHybrid：デジタル名刺とQRコード", "Wedding：デジタル招待状", "Families：家族向けプラットフォーム", "CCTV Viewer：リアルタイムカメラ監視", "ShopStore：直営オンラインストア", "その他CodeMaruが運営するサービス"]),
                new("第3条（利用者の義務）", Items: ["サービスを違法な目的で利用しないこと。", "他人の個人情報を無断で収集・利用しないこと。", "サービスの安定運用を妨害しないこと。"]),
                new("第4条（サービスの変更・中断）", "運用上または技術上の必要によりサービスを変更・中断する場合があり、その際は事前に告知します。"),
                new("第5条（免責）", "天災、サービス障害、または利用者の責に帰すべき事由による損害について責任を負いません。"),
                new("第6条（お問い合わせ）", "利用規約に関するお問い合わせ")
            ]),
        "es" => TermsDocument("Términos del servicio", "Condiciones, derechos, obligaciones y responsabilidades de los servicios CodeMaru.", "Última actualización: 1 de enero de 2026",
            "Objeto", "Estos términos regulan el uso de los servicios CodeMaru.", "Servicios", "Incluyen CardHybrid, Wedding, ThankYou, Families, CCTV Viewer, ShopStore y otros servicios operados por CodeMaru.",
            "Obligaciones del usuario", "No usar el servicio con fines ilegales, no tratar datos ajenos sin permiso y no interferir en su funcionamiento.",
            "Cambios o interrupción", "El servicio puede cambiar o interrumpirse por motivos técnicos u operativos con aviso previo.",
            "Limitación de responsabilidad", "CodeMaru no responde por causas de fuerza mayor, fallos externos o hechos atribuibles al usuario.", "Contacto"),
        "fr" => TermsDocument("Conditions d’utilisation", "Conditions, droits, obligations et responsabilités liés aux services CodeMaru.", "Dernière mise à jour : 1er janvier 2026",
            "Objet", "Ces conditions encadrent l’utilisation des services CodeMaru.", "Services", "Ils comprennent CardHybrid, Wedding, ThankYou, Families, CCTV Viewer, ShopStore et les autres services CodeMaru.",
            "Obligations de l’utilisateur", "Ne pas utiliser le service illégalement, traiter les données d’autrui sans autorisation ni perturber son fonctionnement.",
            "Modification ou interruption", "Le service peut évoluer ou être interrompu pour des raisons techniques ou opérationnelles après préavis.",
            "Limitation de responsabilité", "CodeMaru n’est pas responsable des cas de force majeure, pannes externes ou faits imputables à l’utilisateur.", "Contact"),
        "it" => TermsDocument("Termini di servizio", "Condizioni, diritti, obblighi e responsabilità dei servizi CodeMaru.", "Ultimo aggiornamento: 1 gennaio 2026",
            "Scopo", "Questi termini disciplinano l’uso dei servizi CodeMaru.", "Servizi", "Comprendono CardHybrid, Wedding, ThankYou, Families, CCTV Viewer, ShopStore e altri servizi CodeMaru.",
            "Obblighi dell’utente", "Non usare il servizio illegalmente, non trattare dati altrui senza permesso e non interferire con il funzionamento.",
            "Modifiche o interruzioni", "Il servizio può cambiare o interrompersi per ragioni tecniche o operative con preavviso.",
            "Limitazione di responsabilità", "CodeMaru non risponde per forza maggiore, guasti esterni o fatti imputabili all’utente.", "Contatti"),
        "pt" => TermsDocument("Termos de serviço", "Condições, direitos, obrigações e responsabilidades dos serviços CodeMaru.", "Última atualização: 1 de janeiro de 2026",
            "Objetivo", "Estes termos regem o uso dos serviços CodeMaru.", "Serviços", "Incluem CardHybrid, Wedding, ThankYou, Families, CCTV Viewer, ShopStore e outros serviços CodeMaru.",
            "Obrigações do usuário", "Não usar o serviço ilegalmente, não tratar dados de terceiros sem permissão e não interferir na operação.",
            "Alterações ou interrupção", "O serviço pode mudar ou ser interrompido por motivos técnicos ou operacionais mediante aviso.",
            "Limitação de responsabilidade", "CodeMaru não responde por força maior, falhas externas ou atos atribuíveis ao usuário.", "Contato"),
        "vi" => TermsDocument("Điều khoản dịch vụ", "Điều kiện, quyền, nghĩa vụ và trách nhiệm khi sử dụng dịch vụ CodeMaru.", "Cập nhật lần cuối: 1 tháng 1 năm 2026",
            "Mục đích", "Điều khoản này quy định việc sử dụng các dịch vụ CodeMaru.", "Dịch vụ", "Bao gồm CardHybrid, Wedding, ThankYou, Families, CCTV Viewer, ShopStore và các dịch vụ khác của CodeMaru.",
            "Nghĩa vụ người dùng", "Không sử dụng trái pháp luật, không xử lý dữ liệu người khác khi chưa được phép và không cản trở hoạt động.",
            "Thay đổi hoặc gián đoạn", "Dịch vụ có thể thay đổi hoặc tạm dừng vì lý do kỹ thuật hay vận hành sau khi thông báo.",
            "Giới hạn trách nhiệm", "CodeMaru không chịu trách nhiệm do bất khả kháng, lỗi bên ngoài hoặc nguyên nhân từ người dùng.", "Liên hệ"),
        "zh-hans" => TermsDocument("服务条款", "使用 CodeMaru 服务的条件、权利、义务与责任。", "最后更新：2026年1月1日",
            "目的", "本条款规定 CodeMaru 服务的使用条件。", "服务", "包括 CardHybrid、Wedding、ThankYou、Families、CCTV Viewer、ShopStore 及其他 CodeMaru 服务。",
            "用户义务", "不得用于违法目的，不得擅自处理他人数据，不得干扰服务稳定运行。",
            "变更或中断", "因技术或运营需要，服务可在事先通知后变更或中断。",
            "责任限制", "因不可抗力、外部故障或用户原因造成的损失，CodeMaru 不承担责任。", "联系"),
        "zh-hant" => TermsDocument("服務條款", "使用 CodeMaru 服務的條件、權利、義務與責任。", "最後更新：2026年1月1日",
            "目的", "本條款規定 CodeMaru 服務的使用條件。", "服務", "包括 CardHybrid、Wedding、ThankYou、Families、CCTV Viewer、ShopStore 及其他 CodeMaru 服務。",
            "使用者義務", "不得用於違法目的，不得擅自處理他人資料，不得干擾服務穩定運作。",
            "變更或中斷", "因技術或營運需要，服務可在事先通知後變更或中斷。",
            "責任限制", "因不可抗力、外部故障或使用者原因造成的損失，CodeMaru 不承擔責任。", "聯絡"),
        _ => new(
            "Terms of Service",
            "Terms, rights, obligations, and responsibilities governing the use of CodeMaru services.",
            "Last updated: January 1, 2026",
            [
                new("1. Purpose", "These terms define the conditions and procedures for using CodeMaru services and the rights, obligations, and responsibilities of users and CodeMaru."),
                new("2. Services", "CodeMaru provides the following services.", ["CardHybrid: digital cards and QR codes", "Wedding: digital invitations", "Families: a private family platform", "CCTV Viewer: live camera monitoring", "ShopStore: a directly operated store", "Other services operated by CodeMaru"]),
                new("3. User obligations", Items: ["Do not use the services for unlawful purposes.", "Do not collect or use another person's personal data without authorization.", "Do not interfere with stable service operation."]),
                new("4. Changes and interruption", "Services may be changed or interrupted for operational or technical reasons after prior notice."),
                new("5. Disclaimer", "CodeMaru is not liable for losses caused by natural disasters, service failures, or circumstances attributable to the user."),
                new("6. Contact", "Questions about these terms")
            ])
    };

    private static LegalDocument PrivacyDocument(
        string title, string description, string updated,
        string h1, string p1, string h2, string p2, string h3, string p3,
        string h4, string p4, string h5, string p5, string h6, string p6) =>
        new(title, description, updated,
        [
            new($"1. {h1}", p1), new($"2. {h2}", p2), new($"3. {h3}", p3),
            new($"4. {h4}", p4), new($"5. {h5}", p5), new($"6. {h6}", p6)
        ]);

    private static LegalDocument TermsDocument(
        string title, string description, string updated,
        string h1, string p1, string h2, string p2, string h3, string p3,
        string h4, string p4, string h5, string p5, string contact) =>
        new(title, description, updated,
        [
            new($"1. {h1}", p1), new($"2. {h2}", p2), new($"3. {h3}", p3),
            new($"4. {h4}", p4), new($"5. {h5}", p5),
            new($"6. {contact}", "admin@codemaru.co.kr")
        ]);
}
