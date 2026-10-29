using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LuminaFlow
{
    /// <summary>Portrait-first interface generated from native uGUI; supports mouse, touch and safe areas.</summary>
    public sealed class UIManager : MonoBehaviour
    {
        public RectTransform BoardZone { get; private set; }
        private GameManager game;
        private RectTransform frame, modal;
        private GameObject overlay;
        private Font font;
        private Text title, subtitle, moves, crystals, targets, toast, soundLabel, musicLabel, hapticLabel;
        private Button undo, reset, hint;
        private float toastUntil;
        private static readonly Color Clear = new Color(0, 0, 0, 0);

        public void Initialize(GameManager manager)
        {
            game = manager;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Lumina Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1;
            var safe = Rect("Safe area", canvasObject.transform, Vector2.zero, Vector2.one);
            frame = Rect("Portrait frame", safe, new Vector2(.5f, 0), new Vector2(.5f, 1));
            frame.sizeDelta = new Vector2(1080, 0);
            safe.gameObject.AddComponent<SafeAreaLayout>().Frame = frame;
            BuildHud();
            game.LevelLoaded += OnLevelLoaded;
            game.Changed += Refresh;
            game.Won += ShowWin;
            game.Notification += ShowToast;
        }

        private void BuildHud()
        {
            var brand = Label(frame, "LUMINA / FLOW", 24, NeonPalette.Cyan, TextAnchor.MiddleLeft);
            brand.rectTransform.anchorMin = new Vector2(0, 1); brand.rectTransform.anchorMax = new Vector2(.36f, 1);
            brand.rectTransform.offsetMin = new Vector2(40, -85); brand.rectTransform.offsetMax = new Vector2(-8, -25);
            var levels = Action(frame, "MÀN CHƠI", ShowLevels, NeonPalette.Panel, 24);
            Place((RectTransform)levels.transform, new Vector2(1, 1), new Vector2(-32, -55), new Vector2(195, 66), new Vector2(1, .5f));
            var share = Action(frame, "CHIA SẺ", ShowShare, NeonPalette.Panel, 21);
            Place((RectTransform)share.transform, new Vector2(1, 1), new Vector2(-425, -55), new Vector2(145, 66), new Vector2(1, .5f));
            var skins = Action(frame, "SẮC MÀU", ShowSkins, NeonPalette.Panel, 21);
            Place((RectTransform)skins.transform, new Vector2(1, 1), new Vector2(-260, -55), new Vector2(145, 66), new Vector2(1, .5f));
            title = Label(frame, "Thắp sáng hành trình", 62, NeonPalette.Text, TextAnchor.MiddleLeft);
            StretchTop(title.rectTransform, 44, 44, 145, 90);
            subtitle = Label(frame, "Chạm vào gương để dẫn lối ánh sáng.", 27, NeonPalette.Muted, TextAnchor.MiddleLeft);
            StretchTop(subtitle.rectTransform, 46, 44, 223, 58);
            var stats = Rect("Status", frame, new Vector2(0, 1), new Vector2(1, 1));
            stats.pivot = new Vector2(.5f, 1);
            stats.offsetMin = new Vector2(40, -395);
            stats.offsetMax = new Vector2(-40, -294);
            moves = Stat(stats, 0, "LƯỢT XOAY", "0 / 0");
            crystals = Stat(stats, 1, "PHA LÊ", "0 / 0");
            targets = Stat(stats, 2, "LÕI SÁNG", "0 / 0");
            BoardZone = Rect("Board touch area", frame, new Vector2(0, .255f), new Vector2(1, .755f));
            BoardZone.offsetMin = new Vector2(36, 8);
            BoardZone.offsetMax = new Vector2(-36, -8);
            var help = Label(frame, "CHẠM ĐỂ XOAY  ·  THẮP SÁNG MỌI LÕI", 23, NeonPalette.Muted);
            StretchBottom(help.rectTransform, 28, 28, 405, 50);
            var legend = Label(frame, "Gương /   •   Lăng kính ◇   •   Pha lê ◆", 23, NeonPalette.Cyan);
            StretchBottom(legend.rectTransform, 20, 20, 355, 38);
            var controls = Rect("Move controls", frame, new Vector2(0, 0), new Vector2(1, 0));
            controls.pivot = new Vector2(.5f, 0);
            controls.offsetMin = new Vector2(40, 190);
            controls.offsetMax = new Vector2(-40, 310);
            undo = Action(controls, "LÙI BƯỚC", () => game.Undo(), NeonPalette.Panel, 28);
            reset = Action(controls, "CHƠI LẠI", () => game.ResetLevel(), NeonPalette.Panel, 28);
            hint = Action(controls, "GỢI Ý", () => game.Hint(), NeonPalette.Hex("17383F"), 28);
            Third((RectTransform)undo.transform, 0);
            Third((RectTransform)reset.transform, 1);
            Third((RectTransform)hint.transform, 2);
            var footer = Rect("Settings", frame, new Vector2(0, 0), new Vector2(1, 0));
            footer.pivot = new Vector2(.5f, 0);
            footer.offsetMin = new Vector2(40, 82);
            footer.offsetMax = new Vector2(-40, 156);
            var sound = Action(footer, "", () => { ProgressStore.ToggleSound(); RefreshSettings(); AudioManager.Instance?.PlayTap(); }, Clear, 23);
            var music = Action(footer, "", () => { ProgressStore.ToggleMusic(); RefreshSettings(); }, Clear, 23);
            var haptics = Action(footer, "", () => { ProgressStore.ToggleHaptics(); RefreshSettings(); HapticManager.Tap(); }, Clear, 23);
            Third((RectTransform)sound.transform, 0); Third((RectTransform)music.transform, 1); Third((RectTransform)haptics.transform, 2);
            soundLabel = sound.GetComponentInChildren<Text>(); musicLabel = music.GetComponentInChildren<Text>(); hapticLabel = haptics.GetComponentInChildren<Text>();
            var signature = Label(frame, "MỘT CHÚT TĨNH LẶNG. MỘT LUỒNG ÁNH SÁNG.", 19, NeonPalette.Hex("50627D"));
            StretchBottom(signature.rectTransform, 20, 20, 20, 38);
            toast = Label(frame, "", 25, NeonPalette.Text);
            StretchBottom(toast.rectTransform, 50, 50, 465, 85);
            toast.gameObject.SetActive(false);
            RefreshSettings();
        }
        private Text Stat(Transform parent, int index, string caption, string initial)
        {
            var panel = Panel(parent, caption, NeonPalette.Panel);
            Third(panel, index);
            var label = Label(panel, caption, 19, NeonPalette.Muted);
            label.rectTransform.anchorMin = new Vector2(0, .55f); label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            var value = Label(panel, initial, 32, NeonPalette.Text);
            value.rectTransform.anchorMin = Vector2.zero; value.rectTransform.anchorMax = new Vector2(1, .62f);
            value.rectTransform.offsetMin = value.rectTransform.offsetMax = Vector2.zero;
            return value;
        }
        private void OnLevelLoaded() { CloseModal(); Refresh(); }
        private void Refresh()
        {
            if (!string.IsNullOrEmpty(game.LoadError))
            {
                title.text = "Chưa thể mở màn chơi";
                subtitle.text = game.LoadError;
                undo.interactable = reset.interactable = hint.interactable = false;
                return;
            }
            var level = game.CurrentLevel;
            if (level == null) return;
            title.text = (game.IsCampaign ? level.id.ToString("00") : "UGC") + "  /  " + level.name;
            subtitle.text = level.subtitle;
            moves.text = game.Moves + " / " + level.par;
            moves.color = game.Moves <= level.par ? NeonPalette.Text : NeonPalette.Gold;
            int crystalCount = 0, targetCount = 0;
            foreach (var piece in level.pieces) { if (piece.type == "crystal") crystalCount++; if (piece.type == "target") targetCount++; }
            crystals.text = game.Result.LitCrystals.Count + " / " + crystalCount;
            targets.text = game.Result.LitTargets.Count + " / " + targetCount;
            undo.interactable = game.Moves > 0 && !game.IsWon;
            reset.interactable = true;
            hint.interactable = !game.IsWon;
            RefreshSettings();
        }
        private void RefreshSettings()
        {
            soundLabel.text = "ÂM: " + (ProgressStore.SoundEnabled ? "BẬT" : "TẮT");
            musicLabel.text = "NHẠC: " + (ProgressStore.MusicEnabled ? "BẬT" : "TẮT");
            hapticLabel.text = "RUNG: " + (ProgressStore.HapticsEnabled ? "BẬT" : "TẮT");
            soundLabel.color = ProgressStore.SoundEnabled ? NeonPalette.Cyan : NeonPalette.Muted;
            musicLabel.color = ProgressStore.MusicEnabled ? NeonPalette.Cyan : NeonPalette.Muted;
            hapticLabel.color = ProgressStore.HapticsEnabled ? NeonPalette.Cyan : NeonPalette.Muted;
            AudioManager.Instance?.RefreshSettings();
        }
        private void ShowWin()
        {
            bool final = game.IsCampaign && game.CurrentIndex == game.Catalog.levels.Count - 1;
            bool chooseLevel = final || !game.IsCampaign;
            OpenModal(final ? "Hành trình rực sáng" : "Ánh sáng đã kết nối", 900);
            var eyebrow = Label(modal, "H O À N  T H À N H", 23, NeonPalette.Cyan);
            StretchTop(eyebrow.rectTransform, 35, 35, 150, 55);
            var stars = Label(modal, StarText(game.EarnedStars), 100, NeonPalette.Gold);
            StretchTop(stars.rectTransform, 30, 30, 260, 150);
            var detail = Label(modal, game.Moves + " lượt xoay  ·  " + game.Result.LitCrystals.Count + " pha lê\n" + (game.IsCampaign ? "Mỗi tia sáng đều tìm được đường về." : "Bản đồ chia sẻ • không tính sao hành trình."), 29, NeonPalette.Muted);
            StretchTop(detail.rectTransform, 45, 45, 400, 105);
            var next = Action(modal, chooseLevel ? "CHỌN MÀN CHƠI" : "MÀN TIẾP THEO  →", () => { if (chooseLevel) ShowLevels(); else game.NextLevel(); }, NeonPalette.Hex("1C5359"), 32);
            StretchBottom((RectTransform)next.transform, 65, 65, 220, 110);
            var replay = Action(modal, "CHƠI LẠI", () => game.ResetLevel(), NeonPalette.Panel, 27);
            StretchBottom((RectTransform)replay.transform, 65, 65, 85, 100);
        }
        public void ShowLevels()
        {
            if (game.Catalog == null) return;
            OpenModal("Chọn một luồng sáng", 1420);
            var total = Label(modal, ProgressStore.TotalStars + " / " + game.Catalog.levels.Count * 3 + " SAO  ·  " + game.Catalog.levels.Count + " MÀN", 25, NeonPalette.Gold);
            StretchTop(total.rectTransform, 35, 35, 157, 55);
            var scrollPanel = Rect("Level scroll", modal, new Vector2(0, 0), Vector2.one);
            scrollPanel.offsetMin = new Vector2(38, 165); scrollPanel.offsetMax = new Vector2(-38, -225);
            var scroll = scrollPanel.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = Panel(scrollPanel, "Viewport", new Color(0, 0, 0, .02f));
            Stretch(viewport);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = Rect("Level cards", viewport, new Vector2(0, 1), new Vector2(1, 1));
            content.pivot = new Vector2(.5f, 1);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.spacing = new Vector2(14, 16);
            Canvas.ForceUpdateCanvases();
            grid.cellSize = new Vector2(Mathf.Max(90, (viewport.rect.width - 42) / 4), 145);
            int rows = (game.Catalog.levels.Count + 3) / 4;
            content.sizeDelta = new Vector2(0, rows * 161 - 16);
            scroll.content = content; scroll.viewport = viewport;
            for (int i = 0; i < game.Catalog.levels.Count; i++)
            {
                int index = i;
                var level = game.Catalog.levels[i];
                bool unlocked = game.IsUnlocked(i);
                string label = unlocked ? level.id.ToString("00") + "\n" + StarText(ProgressStore.Stars(level.id)) : level.id.ToString("00") + "\nKHÓA";
                var button = Action(content, label, () => game.LoadLevel(index), i == game.CurrentIndex ? NeonPalette.Hex("20434E") : NeonPalette.Panel, 32);
                button.interactable = unlocked;
                if (!unlocked) button.GetComponentInChildren<Text>().color = NeonPalette.Hex("52627B");
            }
            var close = Action(modal, "TRỞ LẠI", CloseOrWin, NeonPalette.Hex("1B303F"), 27);
            StretchBottom((RectTransform)close.transform, 60, 60, 40, 95);
        }
        public void ShowShare()
        {
            if (game.CurrentLevel == null) return;
            OpenModal("Chia sẻ luồng sáng", 1320);
            var description = Label(modal, "Dán mã Base64 hoặc JSON để chơi bản đồ chia sẻ.\nTiến trình hành trình được lưu riêng.", 25, NeonPalette.Muted);
            StretchTop(description.rectTransform, 40, 40, 175, 100);
            var fieldRect = Panel(modal, "Share code input", NeonPalette.Background);
            fieldRect.anchorMin = Vector2.zero; fieldRect.anchorMax = Vector2.one;
            fieldRect.offsetMin = new Vector2(45, 440); fieldRect.offsetMax = new Vector2(-45, -255);
            var input = fieldRect.gameObject.AddComponent<InputField>();
            input.lineType = InputField.LineType.MultiLineNewline;
            input.characterLimit = LevelSerializer.MaxShareLength;
            var text = Label(fieldRect, "", 23, NeonPalette.Text, TextAnchor.UpperLeft);
            text.resizeTextForBestFit = false;
            text.rectTransform.offsetMin = new Vector2(24, 24); text.rectTransform.offsetMax = new Vector2(-24, -24);
            input.textComponent = text;
            var placeholder = Label(fieldRect, "Dán mã bản đồ tại đây…", 23, NeonPalette.Muted, TextAnchor.UpperLeft);
            placeholder.rectTransform.offsetMin = new Vector2(24, 24); placeholder.rectTransform.offsetMax = new Vector2(-24, -24);
            input.placeholder = placeholder;
            var status = Label(modal, "", 23, NeonPalette.Gold);
            StretchBottom(status.rectTransform, 45, 45, 235, 65);
            try { input.text = LevelSerializer.Serialize(game.ExportLevel); }
            catch (Exception exception)
            {
                input.text = "";
                status.text = "Không thể xuất mã: " + exception.Message;
            }
            var copy = Action(modal, "SAO CHÉP MÃ", () => { GUIUtility.systemCopyBuffer = input.text; status.text = "Đã sao chép mã bản đồ."; }, NeonPalette.Panel, 24);
            var paste = Action(modal, "DÁN MÃ", () => { input.text = GUIUtility.systemCopyBuffer; input.ActivateInputField(); }, NeonPalette.Panel, 24);
            StretchBottom((RectTransform)copy.transform, 45, modal.sizeDelta.x * .5f + 8, 310, 95);
            StretchBottom((RectTransform)paste.transform, modal.sizeDelta.x * .5f + 8, 45, 310, 95);
            var import = Action(modal, "CHƠI BẢN ĐỒ NÀY", () =>
            {
                try
                {
                    string value = input.text.Trim();
                    var level = value.StartsWith("{") ? LevelSerializer.FromJson(value) : LevelSerializer.Deserialize(value);
                    game.LoadSharedLevel(level);
                    ShowToast("Bản đồ chia sẻ đã mở. Chúc bạn tìm thấy luồng sáng!");
                }
                catch (Exception exception) { status.text = "Mã chưa hợp lệ: " + exception.Message; }
            }, NeonPalette.Hex("1C5359"), 29);
            StretchBottom((RectTransform)import.transform, 45, 45, 135, 95);
            var close = Action(modal, "TRỞ LẠI", CloseOrWin, NeonPalette.Panel, 25);
            StretchBottom((RectTransform)close.transform, 45, 45, 30, 80);
        }
        public void ShowSkins()
        {
            if (game.Catalog == null) return;
            OpenModal("Sắc màu ánh sáng", 1160);
            var description = Label(modal, ProgressStore.TotalStars + " SAO ĐÃ GÓP NHẶT\nĐổi sắc hào quang, giữ màu tia để ghép lõi.", 26, NeonPalette.Muted);
            StretchTop(description.rectTransform, 40, 40, 180, 110);
            for (int i = 0; i < LaserSkins.Ids.Length; i++)
            {
                int index = i;
                string id = LaserSkins.Ids[i];
                bool available = LaserSkins.IsUnlocked(id, ProgressStore.TotalStars);
                bool selected = ProgressStore.Skin == id;
                string caption = LaserSkins.Names[i] + "  ·  " + (selected ? "ĐANG DÙNG" : available ? "SỬ DỤNG" : LaserSkins.Costs[i] + " SAO ĐỂ MỞ");
                var choice = Action(modal, caption, () =>
                {
                    if (!ProgressStore.SelectSkin(LaserSkins.Ids[index])) return;
                    game.RefreshPresentation();
                    AudioManager.Instance?.PlayTap();
                    ShowSkins();
                }, selected ? NeonPalette.Hex("20434E") : NeonPalette.Panel, 28);
                StretchTop((RectTransform)choice.transform, 50, 50, 340 + i * 165, 130);
                choice.interactable = available;
            }
            var close = Action(modal, "TRỞ LẠI", CloseOrWin, NeonPalette.Hex("1B303F"), 27);
            StretchBottom((RectTransform)close.transform, 60, 60, 45, 100);
        }
        private void OpenModal(string caption, float height)
        {
            CloseModal();
            game.InputBlocked = true;
            var shade = Panel(frame, "Modal shade", new Color(.015f, .025f, .065f, .93f));
            Stretch(shade);
            overlay = shade.gameObject;
            modal = Panel(shade, "Dialog", NeonPalette.Hex("111F36"));
            modal.anchorMin = modal.anchorMax = new Vector2(.5f, .5f);
            modal.pivot = new Vector2(.5f, .5f);
            modal.sizeDelta = new Vector2(Mathf.Min(970, frame.rect.width - 55), height);
            modal.anchoredPosition = Vector2.zero;
            var heading = Label(modal, caption, 43, NeonPalette.Text);
            StretchTop(heading.rectTransform, 40, 40, 85, 90);
            modal.gameObject.AddComponent<PopupAnimation>();
        }
        private void CloseOrWin() { if (game.IsWon) ShowWin(); else CloseModal(); }
        private void CloseModal()
        {
            if (overlay != null) { overlay.SetActive(false); Destroy(overlay); }
            overlay = null;
            game.InputBlocked = false;
        }
        private void ShowToast(string message)
        {
            toast.text = message;
            toast.gameObject.SetActive(true);
            toastUntil = Time.unscaledTime + 3.5f;
        }
        private void Update() { if (toast != null && toast.gameObject.activeSelf && Time.unscaledTime > toastUntil) toast.gameObject.SetActive(false); }
        private static string StarText(int stars) => new string('★', Mathf.Clamp(stars, 0, 3)) + new string('☆', 3 - Mathf.Clamp(stars, 0, 3));
        private RectTransform Panel(Transform parent, string name, Color color)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = ProceduralArt.Tile;
            image.type = Image.Type.Simple;
            image.color = color;
            return rect;
        }
        private Text Label(Transform parent, string value, int size, Color color, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var rect = Rect(value, parent, Vector2.zero, Vector2.one);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = false;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(15, size / 2);
            text.resizeTextMaxSize = size;
            text.raycastTarget = false;
            return text;
        }
        private Button Action(Transform parent, string caption, UnityAction onClick, Color color, int size)
        {
            var rect = Panel(parent, caption, color);
            var button = rect.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1);
            colors.pressedColor = new Color(.7f, .85f, .9f, 1);
            colors.disabledColor = new Color(.5f, .5f, .5f, .65f);
            button.colors = colors;
            button.onClick.AddListener(onClick);
            var label = Label(rect, caption, size, NeonPalette.Text);
            label.rectTransform.offsetMin = new Vector2(12, 5);
            label.rectTransform.offsetMax = new Vector2(-12, -5);
            return button;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Third(RectTransform rect, int index)
        {
            rect.anchorMin = new Vector2(index / 3f, 0); rect.anchorMax = new Vector2((index + 1) / 3f, 1);
            rect.offsetMin = new Vector2(index == 0 ? 0 : 8, 0); rect.offsetMax = new Vector2(index == 2 ? 0 : -8, 0);
        }
        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.sizeDelta = size; rect.anchoredPosition = position;
        }
        private static void StretchTop(RectTransform rect, float left, float right, float centerY, float height)
        {
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(left, -centerY - height * .5f); rect.offsetMax = new Vector2(-right, -centerY + height * .5f);
        }
        private static void StretchBottom(RectTransform rect, float left, float right, float y, float height)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(.5f, 0);
            rect.offsetMin = new Vector2(left, y); rect.offsetMax = new Vector2(-right, y + height);
        }
        private void OnDestroy()
        {
            if (game == null) return;
            game.LevelLoaded -= OnLevelLoaded; game.Changed -= Refresh; game.Won -= ShowWin; game.Notification -= ShowToast;
        }
    }
}
