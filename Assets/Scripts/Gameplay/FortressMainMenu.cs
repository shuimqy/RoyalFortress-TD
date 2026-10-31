using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public sealed class FortressMainMenu : MonoBehaviour
{
    public const string SceneName = "MainMenu";
    private static string returningFrom;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetNavigation() { returningFrom = null; }
    public static void ReturnToSelection()
    {
        returningFrom = SceneManager.GetActiveScene().name;
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneName);
    }
    [Serializable] public sealed class MapOption
    {
        public string title, subtitle, description, sceneName;
        public Sprite preview;
        public int startingGold, towerSites, waves;
        public string routeLabel;
        public Color accent = new Color(0.42f, 0.77f, 0.59f);
    }
    public MapOption[] maps;
    public Sprite titleBackground;
    public int SelectedIndex { get; private set; }
    public bool Selecting { get; private set; }
    public bool Loading { get; private set; }
    public Button StartButton { get; private set; }
    public Button EnterButton { get; private set; }
    private Font font;
    private RectTransform canvasRect, content;
    private GameObject titlePage, selectionPage;
    private Image backdrop, backdropShade;
    private Image[] cardBorders;
    private Text[] cardLabels;
    private Text detailTitle, detailText, enterText;
    private readonly Color cream = new Color32(244, 235, 214, 255);
    private readonly Color gold = new Color32(223, 185, 111, 255);
    private readonly Color muted = new Color32(160, 177, 176, 255);
    private readonly Color ink = new Color32(12, 22, 27, 255);

    private void Awake()
    {
        Time.timeScale = 1;
        font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 32);
        if (maps == null || maps.Length == 0) { Debug.LogError("Main menu needs map options.", this); return; }
        EnsureEventSystem();
        CreateUI();
        int preferred = string.IsNullOrEmpty(returningFrom) ? 0 : Array.FindIndex(maps, option => option.sceneName == returningFrom);
        SelectMap(Mathf.Max(0, preferred));
        if (string.IsNullOrEmpty(returningFrom)) ShowTitle(); else ShowMapSelection();
        returningFrom = null;
    }
    private void LateUpdate()
    {
        if (!content || !canvasRect) return;
        float scale = Mathf.Min(canvasRect.rect.width / 1600f, canvasRect.rect.height / 900f);
        content.localScale = Vector3.one * scale;
    }
    private void OnDestroy() { if (font) Destroy(font); }
    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>()) return;
        var node = new GameObject("Menu EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        node.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
        node.AddComponent<StandaloneInputModule>();
#endif
    }
    private void CreateUI()
    {
        var canvasNode = new GameObject("Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasNode.transform.SetParent(transform, false);
        var canvas = canvasNode.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasNode.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = canvasNode.GetComponent<RectTransform>();
        var background = Panel("Backdrop", canvasNode.transform, Color.white);
        Stretch(background.rectTransform);
        backdrop = background;
        var ratio = background.gameObject.AddComponent<AspectRatioFitter>();
        ratio.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ratio.aspectRatio = 4f / 3f;
        var shade = Panel("Backdrop Shade", canvasNode.transform, new Color32(5, 13, 19, 174));
        backdropShade = shade;
        Stretch(shade.rectTransform);
        content = new GameObject("Menu Content", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(canvasNode.transform, false);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(1600, 900);
        titlePage = Page("Title");
        selectionPage = Page("Map Selection");
        CreateTitle();
        CreateSelection();
    }
    private GameObject Page(string name)
    {
        var page = new GameObject(name, typeof(RectTransform));
        page.transform.SetParent(content, false);
        Stretch(page.GetComponent<RectTransform>());
        return page;
    }
    private void CreateTitle()
    {
        var parent = titlePage.transform;
        var veil = Box(parent, "Title Veil", 46, 72, 780, 754, new Color32(9, 21, 26, 95));
        Box(parent, "Gold Accent", 82, 124, 62, 3, gold);
        TextAt(parent, "R O Y A L   F O R T R E S S", 16, gold, 82, 151, 670, 30);
        TextAt(parent, "王国边境", 76, cream, 76, 211, 690, 106, true);
        TextAt(parent, "守住弯道，守住王国。", 29, cream, 82, 335, 650, 50);
        TextAt(parent, "在森林与王城之间部署防线。\n选择战场，购买防御塔，迎击来犯之敌。", 20, muted, 84, 412, 660, 84);
        StartButton = ButtonAt(parent, "Start", "开始游戏   ›", 84, 548, 320, 66, gold, ink, ShowMapSelection);
        ButtonAt(parent, "Exit", "退出游戏", 84, 634, 320, 48, new Color32(27, 42, 47, 255), cream, ExitGame);
        TextAt(parent, "拖卡建塔  /  自由缩放  /  多地图防守", 14, muted, 84, 757, 690, 28);
        var feature = Box(parent, "Featured Map", 1000, 566, 500, 248, new Color32(9, 21, 26, 224));
        Box(feature.transform, "Featured Line", 0, 0, 500, 3, maps[maps.Length - 1].accent);
        TextAt(feature.transform, "远征路线 · CAMPAIGN", 14, gold, 28, 25, 448, 26);
        TextAt(feature.transform, "守卫与远征", 36, cream, 26, 65, 448, 54, true);
        TextAt(feature.transform, "要塞 → 圣林 → 亡者之门\n守住边境，获得加护，封印裂隙。", 18, muted, 28, 130, 444, 78);
    }
    private void CreateSelection()
    {
        var parent = selectionPage.transform;
        TextAt(parent, "选择战场", 46, cream, 100, 46, 1000, 70, true);
        TextAt(parent, "每一条道路，都是新的防线。", 18, muted, 104, 118, 1100, 34);
        ButtonAt(parent, "Back", "‹ 返回", 1330, 68, 166, 44, new Color32(17, 32, 39, 240), cream, ShowTitle);
        cardBorders = new Image[maps.Length];
        cardLabels = new Text[maps.Length];
        float width = (1400f - 28f * (maps.Length - 1)) / maps.Length;
        for (int i = 0; i < maps.Length; i++)
        {
            int index = i;
            var option = maps[i];
            float x = 100 + i * (width + 28);
            var frame = Box(parent, "Map Card " + i, x, 190, width, 444, new Color32(65, 79, 80, 255), true);
            cardBorders[i] = frame;
            var button = frame.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            button.onClick.AddListener(() => SelectMap(index));
            var face = Box(frame.transform, "Card Surface", 2, 2, width - 4, 440, new Color32(13, 26, 33, 255));
            var crop = Box(face.transform, "Preview Frame", 0, 0, width - 4, 264, Color.white);
            crop.gameObject.AddComponent<RectMask2D>();
            var picture = Panel("Map Preview", crop.transform, Color.white);
            Stretch(picture.rectTransform);
            picture.sprite = option.preview;
            var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = option.preview ? option.preview.rect.width / option.preview.rect.height : 1.5f;
            var number = Box(frame.transform, "Map Index", 18, 18, 78, 32, new Color32(10, 24, 28, 235));
            TextAt(number.transform, "0" + (i + 1), 18, option.accent, 13, 0, 60, 32, true);
            TextAt(face.transform, option.title, 30, cream, 24, 279, width - 52, 44, true);
            TextAt(face.transform, option.subtitle, 15, option.accent, 26, 324, width - 56, 28);
            TextAt(face.transform, option.towerSites + " 个塔位    ·    " + option.waves + " 波敌人    ·    " + option.startingGold + " 初始金币",
                16, muted, 26, 363, width - 56, 30);
            cardLabels[i] = TextAt(face.transform, "", 14, gold, 26, 402, width - 56, 24);
        }
        var detail = Box(parent, "Selection Detail", 100, 670, 1400, 144, new Color32(9, 21, 26, 234));
        detailTitle = TextAt(detail.transform, "", 25, cream, 24, 16, 900, 42, true);
        detailText = TextAt(detail.transform, "", 17, muted, 26, 65, 960, 58);
        EnterButton = ButtonAt(detail.transform, "Enter Map", "", 1054, 40, 320, 64, gold, ink, () => EnterSelectedMap());
        enterText = EnterButton.GetComponentInChildren<Text>();
        TextAt(parent, "金币与波次在每次进入关卡时重新开始。", 13, muted, 104, 837, 1300, 28);
    }
    public void ShowMapSelection()
    {
        if (Loading) return;
        Selecting = true;
        SetBackdrop(maps[SelectedIndex].preview, false);
        titlePage.SetActive(false);
        selectionPage.SetActive(true);
    }
    public void ShowTitle()
    {
        if (Loading) return;
        Selecting = false;
        SetBackdrop(titleBackground ? titleBackground : maps[SelectedIndex].preview, true);
        selectionPage.SetActive(false);
        titlePage.SetActive(true);
    }
    public void SelectMap(int index)
    {
        if (Loading || maps == null || index < 0 || index >= maps.Length) return;
        SelectedIndex = index;
        var option = maps[index];
        if (Selecting) SetBackdrop(option.preview, false);
        for (int i = 0; i < maps.Length; i++)
        {
            cardBorders[i].color = i == index ? gold : new Color32(65, 79, 80, 255);
            cardLabels[i].text = i == index ? "已选择 · " + maps[i].routeLabel : "点击选择 · " + maps[i].routeLabel;
        }
        detailTitle.text = option.title + " / " + option.routeLabel;
        detailText.text = option.description;
        enterText.text = "进入" + option.title + "   ›";
    }
    private void SetBackdrop(Sprite sprite, bool title)
    {
        backdrop.sprite = sprite;
        backdrop.GetComponent<AspectRatioFitter>().aspectRatio = sprite ? sprite.rect.width / sprite.rect.height : 16f / 9f;
        backdropShade.color = new Color32(5, 13, 19, title ? (byte)54 : (byte)174);
    }
    public bool EnterSelectedMap()
    {
        if (Loading || maps == null || SelectedIndex < 0 || SelectedIndex >= maps.Length) return false;
        var option = maps[SelectedIndex];
        if (!Application.CanStreamedLevelBeLoaded(option.sceneName)) return false;
        Loading = true;
        EnterButton.interactable = false;
        enterText.text = "正在进入战场…";
        StartCoroutine(LoadMap(option.sceneName));
        return true;
    }
    private IEnumerator LoadMap(string sceneName)
    {
        yield return null;
        Time.timeScale = 1;
        yield return SceneManager.LoadSceneAsync(sceneName);
    }
    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private Image Panel(string name, Transform parent, Color color, bool raycast = false)
    {
        var node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        node.transform.SetParent(parent, false);
        var image = node.GetComponent<Image>();
        image.color = color; image.raycastTarget = raycast;
        return image;
    }
    private Image Box(Transform parent, string name, float x, float y, float w, float h, Color color, bool raycast = false)
    {
        var image = Panel(name, parent, color, raycast);
        Position(image.rectTransform, x, y, w, h);
        return image;
    }
    private Text TextAt(Transform parent, string value, int size, Color color, float x, float y, float w, float h, bool bold = false)
    {
        var node = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        node.transform.SetParent(parent, false);
        var label = node.GetComponent<Text>();
        label.font = font; label.text = value; label.fontSize = size; label.color = color;
        label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        label.raycastTarget = false; label.alignment = TextAnchor.MiddleLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
        Position(label.rectTransform, x, y, w, h);
        return label;
    }
    private Button ButtonAt(Transform parent, string name, string text, float x, float y, float w, float h,
        Color background, Color foreground, UnityEngine.Events.UnityAction click)
    {
        var face = Box(parent, name, x, y, w, h, background, true);
        var button = face.gameObject.AddComponent<Button>();
        button.targetGraphic = face; button.onClick.AddListener(click);
        var colors = button.colors;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1);
        button.colors = colors;
        var label = TextAt(face.transform, text, 22, foreground, 0, 0, w, h, true);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f); rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static void Position(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
    }
}
