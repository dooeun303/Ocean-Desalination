using UnityEngine;
using UnityEngine.UI;

namespace PromiseDT.UI
{
    /// <summary>
    /// PROMISE DT 디자인 시스템 토큰 + UI 빌더 헬퍼.
    /// 스타일 가이드(색·타이포·간격·라운드·그라데이션)를 코드로 일원화한 정적 클래스.
    /// 런타임에서도 동작하도록 UnityEditor 의존성 없음.
    ///
    /// 사용 예)
    ///   var card = DS.MakePanel(parent, DS.Radius.Card, DS.Surface);
    ///   DS.MakeText(card.transform, "총 설비", DS.Type.Subheading, DS.TextBody, TextAnchor.MiddleLeft);
    ///   var btn  = DS.MakePrimaryButton(parent, "설비 등록", new Vector2(180, 44));
    /// </summary>
    public static class DS
    {
        // ─────────────────────────────────────────────
        // 1. 컬러 토큰
        // ─────────────────────────────────────────────
        public static Color FromHex(string h, float a = 1f)
        {
            h = h.Replace("#", "");
            int r = System.Convert.ToInt32(h.Substring(0, 2), 16);
            int g = System.Convert.ToInt32(h.Substring(2, 2), 16);
            int b = System.Convert.ToInt32(h.Substring(4, 2), 16);
            return new Color(r / 255f, g / 255f, b / 255f, a);
        }

        // 인터랙션 / 액센트
        public static readonly Color Point      = FromHex("73CF79"); // 브랜드 강조, 그래프, 뱃지
        public static readonly Color Link        = FromHex("5CC196"); // 포커스, 링크
        public static readonly Color IconActive  = FromHex("50B8B8"); // 활성 상태
        public static readonly Color Information  = FromHex("3978B8"); // 정보 강조

        // 레이아웃
        public static readonly Color Background  = FromHex("EAF3F5");
        public static readonly Color Surface     = Color.white;             // 패널 면
        public static readonly Color Border      = FromHex("E2E8F0");
        public static readonly Color HeaderTone  = FromHex("A8C4CE");

        // 텍스트
        public static readonly Color TextCritical = FromHex("1A202D"); // 제목, 핵심 수치
        public static readonly Color TextBody     = FromHex("3D5A6A"); // 본문
        public static readonly Color TextSub      = FromHex("7A9EAB"); // 캡션, 단위
        public static readonly Color TextHint     = FromHex("A0AEC0"); // placeholder

        // 상태
        public static readonly Color StatusNormal  = FromHex("50B8B8");
        public static readonly Color StatusStopped = FromHex("5F6F85");
        public static readonly Color StatusWarning = FromHex("F59E0B");
        public static readonly Color StatusCritical= FromHex("EF4444");
        public static readonly Color StatusComm    = FromHex("8B5CF6");

        // 그라데이션 (135°)
        public static readonly Color PrimaryGradA = FromHex("50B8B8");
        public static readonly Color PrimaryGradB = FromHex("3978B8");

        public static Color Alpha(Color c, float a) { c.a = a; return c; }

        // ─────────────────────────────────────────────
        // 2. 타이포그래피 (pt → px 근사, 96dpi 기준 1pt≈1.333px)
        // ─────────────────────────────────────────────
        public static class Type
        {
            public const int Heading    = 30; // 페이지 제목
            public const int BigNumber  = 50; // KPI 값
            public const int Subheading = 17; // 카드 제목
            public const int Body       = 16; // 본문
            public const int Caption    = 14; // 보조
            public const int Menu       = 15; // 메뉴
            public const int TableHeader= 13;
            public const int TableBody  = 13;
            public const int Unit       = 18;
        }

        // ─────────────────────────────────────────────
        // 3. 간격 / 라운드
        // ─────────────────────────────────────────────
        public static class Space
        {
            public const int ContentTop = 24, ContentSide = 32, ContentBottom = 40;
            public const int PanelGap = 16;
            public const int PanelPadV = 18, PanelPadH = 20;
            public const int TopBarH = 84, SidebarW = 230, FieldH = 44;
        }

        public static class Radius
        {
            public const int Card = 14, Field = 12, Button = 8, IconBtn = 15, Modal = 16, Submenu = 6;
        }

        // ─────────────────────────────────────────────
        // 4. 폰트 (한글 지원, 시스템 폰트 폴백)
        // ─────────────────────────────────────────────
        private static Font _font;
        public static Font Font
        {
            get
            {
                if (_font == null)
                    _font = UnityEngine.Font.CreateDynamicFontFromOSFont(
                        new[] { "Noto Sans KR", "Malgun Gothic", "맑은 고딕", "Arial" }, 18);
                return _font;
            }
        }

        // ─────────────────────────────────────────────
        // 5. 프로시저럴 스프라이트 (라운드 / 원 / 그라데이션) — 캐시
        // ─────────────────────────────────────────────
        private static readonly System.Collections.Generic.Dictionary<int, Sprite> _roundCache
            = new System.Collections.Generic.Dictionary<int, Sprite>();

        /// <summary>라운드 사각형 9-슬라이스 스프라이트(흰색). Image.color로 틴트해 사용.</summary>
        public static Sprite Rounded(int radius)
        {
            if (_roundCache.TryGetValue(radius, out var cached) && cached != null) return cached;
            int size = radius * 2 + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = 1f, cx = -1, cy = -1;
                    if (x < radius && y < radius) { cx = radius; cy = radius; }
                    else if (x >= size - radius && y < radius) { cx = size - radius - 1; cy = radius; }
                    else if (x < radius && y >= size - radius) { cx = radius; cy = size - radius - 1; }
                    else if (x >= size - radius && y >= size - radius) { cx = size - radius - 1; cy = size - radius - 1; }
                    if (cx >= 0) { float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)); a = Mathf.Clamp01(radius - d + 0.5f); }
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            _roundCache[radius] = sp;
            return sp;
        }

        private static Sprite _circle;
        public static Sprite Circle()
        {
            if (_circle != null) return _circle;
            int s = 16; var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            float r = s / 2f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Mathf.Sqrt((x - r + .5f) * (x - r + .5f) + (y - r + .5f) * (y - r + .5f));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
                }
            tex.Apply();
            _circle = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100);
            return _circle;
        }

        /// <summary>135° 그라데이션 + 라운드(고정 크기). 프라이머리 버튼 등.</summary>
        public static Sprite GradientRounded(int w, int h, int radius, Color c1, Color c2)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float t = Mathf.Clamp01((x + (h - y)) / (float)(w + h));
                    Color col = Color.Lerp(c1, c2, t);
                    float a = 1f, cx = -1, cy = -1;
                    if (x < radius && y < radius) { cx = radius; cy = radius; }
                    else if (x >= w - radius && y < radius) { cx = w - radius - 1; cy = radius; }
                    else if (x < radius && y >= h - radius) { cx = radius; cy = h - radius - 1; }
                    else if (x >= w - radius && y >= h - radius) { cx = w - radius - 1; cy = h - radius - 1; }
                    if (cx >= 0) { float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)); a = Mathf.Clamp01(radius - d + 0.5f); }
                    col.a = a;
                    tex.SetPixel(x, y, col);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100);
        }

        // ─────────────────────────────────────────────
        // 6. UI 빌더 헬퍼
        // ─────────────────────────────────────────────
        public static Text MakeText(Transform parent, string content, int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal, bool stretch = true)
        {
            var go = new GameObject("Text", typeof(Text));
            go.transform.SetParent(parent, false);
            if (stretch)
            {
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            var t = go.GetComponent<Text>();
            t.text = content; t.font = Font; t.fontSize = size; t.fontStyle = style;
            t.color = color; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>라운드 패널(카드). Image.type=Sliced 적용된 GameObject 반환.</summary>
        public static GameObject MakePanel(Transform parent, int radius, Color color)
        {
            var go = new GameObject("Panel", typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = Rounded(radius);
            img.type = Image.Type.Sliced;
            img.color = color;
            return go;
        }

        /// <summary>프라이머리 그라데이션 버튼.</summary>
        public static Button MakePrimaryButton(Transform parent, string label, Vector2 size)
        {
            var go = new GameObject("PrimaryButton", typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.sprite = GradientRounded((int)size.x, (int)size.y, Radius.Button, PrimaryGradA, PrimaryGradB);
            img.type = Image.Type.Simple;
            MakeText(go.transform, label, 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            return go.GetComponent<Button>();
        }

        /// <summary>상태 점(6px 컬러 인디케이터) + 라벨 묶음.</summary>
        public static GameObject MakeStatusTag(Transform parent, string label, Color color)
        {
            var cell = new GameObject("StatusTag", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cell.transform.SetParent(parent, false);
            var hlg = cell.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6; hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

            var dot = new GameObject("Dot", typeof(Image));
            dot.transform.SetParent(cell.transform, false);
            dot.GetComponent<Image>().sprite = Circle();
            dot.GetComponent<Image>().color = color;
            var dle = dot.AddComponent<LayoutElement>(); dle.preferredWidth = 8; dle.preferredHeight = 8; dle.minWidth = 8;

            var lbl = new GameObject("Lbl", typeof(RectTransform));
            lbl.transform.SetParent(cell.transform, false);
            lbl.AddComponent<LayoutElement>().preferredWidth = 50;
            MakeText(lbl.transform, label, Type.TableBody, color, TextAnchor.MiddleLeft, FontStyle.Bold);
            return cell;
        }
    }
}
