// PROTOTYPE - NOT FOR PRODUCTION
// Question: Does the card durability + devour transformation mechanic create meaningful strategic decisions?
// Date: 2026-04-16

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// DeepDiner core loop prototype.
/// Attach to an empty GameObject in a new scene. All UI is created at runtime.
///
/// Interaction: Click a card to select it, then click a seated diner to play it as add-on.
/// Click "End Round" to settle all diners.
/// </summary>
public class PrototypeCoreLoop : MonoBehaviour
{
    #region Data Definitions

    public class DinerTemplate
    {
        public string name;
        public string race;
        public int goldMin, goldMax;
        public int baseFee;
        public Color color;
    }

    public class CardTemplate
    {
        public string name;
        public string race;
        public int addOnCost;
        public int maxDurability;
        public Color color;
    }

    public class DinerState
    {
        public string name;
        public string race;
        public int gold;
        public int baseFee;
        public int totalSpent;
        public Color color;
    }

    public class CardState
    {
        public string name;
        public string race;
        public int addOnCost;
        public int durability;
        public int maxDurability;
        public Color color;
    }

    #endregion

    #region Configuration

    const int SEAT_COUNT = 4;
    const int ROUNDS_PER_DAY = 4;
    const int DINERS_PER_ROUND_MIN = 1;
    const int DINERS_PER_ROUND_MAX = 3;
    const int MAX_HAND_SIZE = 10;

    static readonly DinerTemplate[] DINER_TEMPLATES = new DinerTemplate[]
    {
        new DinerTemplate { name = "哥布林", race = "Goblin",  goldMin = 30, goldMax = 60,  baseFee = 10, color = new Color(0.4f, 0.8f, 0.3f) },
        new DinerTemplate { name = "史莱姆", race = "Slime",   goldMin = 50, goldMax = 90,  baseFee = 15, color = new Color(0.3f, 0.7f, 0.9f) },
        new DinerTemplate { name = "深渊鱼", race = "DeepSea", goldMin = 70, goldMax = 130, baseFee = 20, color = new Color(0.5f, 0.3f, 0.8f) },
        new DinerTemplate { name = "兽人",   race = "Orc",     goldMin = 100,goldMax = 180, baseFee = 25, color = new Color(0.8f, 0.4f, 0.2f) },
    };

    // Quality tiers: Common(10-19), Uncommon(20-29), Rare(30-39), Epic(40-50)
    static readonly (string name, int costMin, int costMax, int dur)[] CARD_QUALITY_POOL = new (string, int, int, int)[]
    {
        // Common (白) x3
        ("粗粮面包",   10, 19, 4),
        ("清水汤",     10, 19, 4),
        ("野菜沙拉",   10, 19, 4),
        // Uncommon (绿) x2
        ("烤蘑菇",     20, 29, 3),
        ("香料炖肉",   20, 29, 3),
        // Rare (蓝) x1
        ("深渊寿司",   30, 39, 2),
        // Epic (紫) x1
        ("龙息浓汤",   40, 50, 2),
    };

    static readonly Color COLOR_COMMON   = new Color(0.75f, 0.75f, 0.75f);
    static readonly Color COLOR_UNCOMMON = new Color(0.3f, 0.9f, 0.3f);
    static readonly Color COLOR_RARE     = new Color(0.3f, 0.6f, 1f);
    static readonly Color COLOR_EPIC     = new Color(0.75f, 0.3f, 1f);

    static readonly Dictionary<string, CardTemplate> DEVOUR_CARDS = new Dictionary<string, CardTemplate>
    {
        { "Goblin",  new CardTemplate { name = "哥布林肉排",  race = "Goblin",  addOnCost = 6,  maxDurability = 2, color = new Color(0.6f, 0.9f, 0.5f) } },
        { "Slime",   new CardTemplate { name = "史莱姆果冻", race = "Slime",   addOnCost = 8,  maxDurability = 2, color = new Color(0.4f, 0.8f, 1f) } },
        { "DeepSea", new CardTemplate { name = "深渊鱼刺身", race = "DeepSea", addOnCost = 10, maxDurability = 2, color = new Color(0.6f, 0.4f, 0.9f) } },
        { "Orc",     new CardTemplate { name = "兽人烤肉",   race = "Orc",     addOnCost = 12, maxDurability = 2, color = new Color(0.9f, 0.5f, 0.3f) } },
    };

    #endregion

    #region Game State

    enum RoundPhase { Assignment, Playing, Settlement, DayEnd, GameOver }

    RoundPhase phase;
    int currentDay = 1;
    int currentRound = 1;
    int dailyRevenue = 0;
    int dailyTarget = 50;
    int totalRevenue = 0;
    int selectedCardIndex = -1;
    int devourCount = 0;

    List<CardState> hand = new List<CardState>();
    DinerState[] seats = new DinerState[SEAT_COUNT];
    StringBuilder gameLog = new StringBuilder();

    #endregion

    #region UI References

    Font uiFont;
    Text dayLabel, roundLabel, revenueLabel, targetLabel, phaseLabel;
    GameObject[] seatPanels = new GameObject[SEAT_COUNT];
    Text[] seatNameTexts = new Text[SEAT_COUNT];
    Text[] seatGoldTexts = new Text[SEAT_COUNT];
    Text[] seatFeeTexts = new Text[SEAT_COUNT];
    Text[] seatSpentTexts = new Text[SEAT_COUNT];
    Text[] seatBillTexts = new Text[SEAT_COUNT];

    List<GameObject> cardPanels = new List<GameObject>();

    Button endRoundBtn;
    Button newDayBtn;
    Button restartBtn;
    Text logText;
    Transform handContainer;
    GameObject logArea;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
        StartNewGame();
    }

    #endregion

    #region UI Construction

    void BuildUI()
    {
        // --- Canvas ---
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        gameObject.AddComponent<GraphicRaycaster>();

        // Full-screen dark background
        MakeImage(transform, new Color(0.12f, 0.08f, 0.18f), Vector2.zero, Vector2.one);

        // --- Top Bar (90-100%) ---
        var topBar = MakeImage(transform, new Color(0.1f, 0.06f, 0.15f), new Vector2(0, 0.92f), new Vector2(1, 1));
        dayLabel     = MakeLabel(topBar.transform, "Day: 1",        20, Color.white,  TextAnchor.MiddleLeft, new Vector2(0.01f, 0), new Vector2(0.2f, 1));
        roundLabel   = MakeLabel(topBar.transform, "Round: 1/4",   20, Color.white,  TextAnchor.MiddleLeft, new Vector2(0.21f, 0), new Vector2(0.42f, 1));
        revenueLabel = MakeLabel(topBar.transform, "Revenue: 0",   20, Color.yellow, TextAnchor.MiddleLeft, new Vector2(0.43f, 0), new Vector2(0.65f, 1));
        targetLabel  = MakeLabel(topBar.transform, "Target: 50",   20, Color.white,  TextAnchor.MiddleLeft, new Vector2(0.66f, 0), new Vector2(0.85f, 1));

        // Total revenue in top-right
        var totalLabel = MakeLabel(topBar.transform, "Total: 0 | Devours: 0", 16, new Color(0.6f, 0.6f, 0.6f),
            TextAnchor.MiddleRight, new Vector2(0.86f, 0), new Vector2(0.99f, 1));

        // --- Hint / Phase Bar (87-92%) ---
        phaseLabel = MakeLabel(transform, "", 16, Color.cyan, TextAnchor.MiddleCenter,
            new Vector2(0, 0.87f), new Vector2(0.65f, 0.92f));

        // Action buttons (right side of phase bar)
        var btnHost = MakeImage(transform, Color.clear, new Vector2(0.65f, 0.87f), new Vector2(1f, 0.92f));

        endRoundBtn = MakeBtn(btnHost.transform, "End Round", OnEndRound,
            new Color(0.75f, 0.25f, 0.2f), new Vector2(0.34f, 0), new Vector2(0.56f, 1));
        newDayBtn = MakeBtn(btnHost.transform, "Next Day", OnNewDay,
            new Color(0.2f, 0.55f, 0.75f), new Vector2(0.58f, 0), new Vector2(0.8f, 1));
        newDayBtn.gameObject.SetActive(false);
        restartBtn = MakeBtn(btnHost.transform, "Restart", () => StartNewGame(),
            new Color(0.5f, 0.5f, 0.5f), new Vector2(0.82f, 0), new Vector2(1f, 1));
        restartBtn.gameObject.SetActive(false);

        // --- Seat Area (30-85%, left 70%) ---
        var seatArea = MakeImage(transform, new Color(0.08f, 0.05f, 0.12f),
            new Vector2(0.02f, 0.3f), new Vector2(0.68f, 0.85f));
        MakeLabel(seatArea.transform, "-- Restaurant --", 14, new Color(0.4f, 0.4f, 0.4f),
            TextAnchor.MiddleCenter, new Vector2(0, 0.92f), new Vector2(1, 1));

        for (int i = 0; i < SEAT_COUNT; i++)
        {
            float x0 = i * 0.25f + 0.01f;
            float x1 = x0 + 0.23f;
            int idx = i;

            var seat = MakeImage(seatArea.transform, new Color(0.14f, 0.1f, 0.2f),
                new Vector2(x0, 0.02f), new Vector2(x1, 0.88f));
            seatPanels[i] = seat;

            seatNameTexts[i]  = MakeLabel(seat.transform, "Empty", 16, Color.gray, TextAnchor.MiddleCenter,
                new Vector2(0, 0.78f), new Vector2(1, 0.95f));
            seatGoldTexts[i]  = MakeLabel(seat.transform, "", 13, Color.yellow, TextAnchor.MiddleCenter,
                new Vector2(0, 0.6f), new Vector2(1, 0.75f));
            seatFeeTexts[i]   = MakeLabel(seat.transform, "", 13, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0, 0.44f), new Vector2(1, 0.58f));
            seatSpentTexts[i] = MakeLabel(seat.transform, "", 13, new Color(1f, 0.6f, 0.3f), TextAnchor.MiddleCenter,
                new Vector2(0, 0.28f), new Vector2(1, 0.42f));
            seatBillTexts[i]  = MakeLabel(seat.transform, "", 14, Color.green, TextAnchor.MiddleCenter,
                new Vector2(0, 0.08f), new Vector2(1, 0.25f));

            // Invisible button overlay
            var btnObj = new GameObject("SeatBtn");
            btnObj.transform.SetParent(seat.transform, false);
            var r = btnObj.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            btnObj.AddComponent<Image>().color = Color.clear;
            var seatBtn = btnObj.AddComponent<Button>();
            int captured = i;
            seatBtn.onClick.AddListener(() => OnSeatClicked(captured));
        }

        // --- Log Area (30-85%, right 30%) ---
        logArea = MakeImage(transform, new Color(0.06f, 0.04f, 0.1f),
            new Vector2(0.7f, 0.3f), new Vector2(0.98f, 0.85f));
        MakeLabel(logArea.transform, "-- Log --", 14, new Color(0.4f, 0.4f, 0.4f),
            TextAnchor.MiddleCenter, new Vector2(0, 0.93f), new Vector2(1, 1));
        logText = MakeLabel(logArea.transform, "", 11, new Color(0.7f, 0.7f, 0.7f),
            TextAnchor.LowerLeft, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.9f));

        // --- Hand Area (2-28%) ---
        var handArea = MakeImage(transform, new Color(0.1f, 0.06f, 0.14f),
            new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.28f));
        MakeLabel(handArea.transform, "-- Hand (click to select, then click a diner) --", 14, new Color(0.4f, 0.4f, 0.4f),
            TextAnchor.MiddleCenter, new Vector2(0, 0.88f), new Vector2(1, 1));
        handContainer = handArea.transform;
    }

    GameObject MakeImage(Transform parent, Color color, Vector2 aMin, Vector2 aMax)
    {
        var obj = new GameObject("Img");
        obj.transform.SetParent(parent, false);
        var r = obj.AddComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        obj.AddComponent<Image>().color = color;
        return obj;
    }

    Text MakeLabel(Transform parent, string txt, int size, Color color, TextAnchor anchor, Vector2 aMin, Vector2 aMax)
    {
        var obj = new GameObject("Lbl");
        obj.transform.SetParent(parent, false);
        var r = obj.AddComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        var t = obj.AddComponent<Text>();
        t.font = uiFont;
        t.text = txt;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        return t;
    }

    Button MakeBtn(Transform parent, string label, UnityEngine.Events.UnityAction action, Color bg, Vector2 aMin, Vector2 aMax)
    {
        var obj = new GameObject("Btn");
        obj.transform.SetParent(parent, false);
        var r = obj.AddComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        var img = obj.AddComponent<Image>();
        img.color = bg;
        var btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);

        var lbl = new GameObject("L");
        lbl.transform.SetParent(obj.transform, false);
        var lr = lbl.AddComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
        var lt = lbl.AddComponent<Text>();
        lt.font = uiFont;
        lt.text = label;
        lt.fontSize = 14;
        lt.color = Color.white;
        lt.alignment = TextAnchor.MiddleCenter;

        return btn;
    }

    #endregion

    #region Game Flow

    void StartNewGame()
    {
        currentDay = 1;
        currentRound = 1;
        dailyRevenue = 0;
        dailyTarget = 50;
        totalRevenue = 0;
        selectedCardIndex = -1;
        devourCount = 0;
        gameLog.Clear();
        AddLog("=== 深渊食堂 - Core Loop Prototype ===");
        AddLog("Click card -> click diner -> End Round");

        hand.Clear();
        // Generate 7 starting cards with quality-weighted random draw:
        // Common x3, Uncommon x2, Rare x1, Epic x1
        int[] qualityDraw = { 0, 1, 2, 3, 4, 5, 6 };
        // Shuffle
        for (int s = qualityDraw.Length - 1; s > 0; s--)
        {
            int j = Random.Range(0, s + 1);
            int tmp = qualityDraw[s]; qualityDraw[s] = qualityDraw[j]; qualityDraw[j] = tmp;
        }
        for (int i = 0; i < qualityDraw.Length; i++)
        {
            int qi = qualityDraw[i];
            var pool = CARD_QUALITY_POOL[qi];
            Color cardColor;
            if (qi <= 2) cardColor = COLOR_COMMON;
            else if (qi <= 4) cardColor = COLOR_UNCOMMON;
            else if (qi == 5) cardColor = COLOR_RARE;
            else cardColor = COLOR_EPIC;

            hand.Add(new CardState
            {
                name = pool.name,
                race = "Basic",
                addOnCost = Random.Range(pool.costMin, pool.costMax + 1),
                durability = pool.dur,
                maxDurability = pool.dur,
                color = cardColor,
            });
        }

        for (int i = 0; i < SEAT_COUNT; i++)
            seats[i] = null;

        newDayBtn.gameObject.SetActive(false);
        restartBtn.gameObject.SetActive(false);
        endRoundBtn.gameObject.SetActive(true);

        BeginRound();
    }

    void BeginRound()
    {
        phase = RoundPhase.Playing;
        selectedCardIndex = -1;

        // Count empty seats
        var empty = new List<int>();
        for (int i = 0; i < SEAT_COUNT; i++)
            if (seats[i] == null) empty.Add(i);

        // Assign random diners
        int count = Mathf.Min(Random.Range(DINERS_PER_ROUND_MIN, DINERS_PER_ROUND_MAX + 1), empty.Count);
        for (int d = 0; d < count; d++)
        {
            if (empty.Count == 0) break;
            int pick = Random.Range(0, empty.Count);
            int seatIdx = empty[pick];
            empty.RemoveAt(pick);

            seats[seatIdx] = MakeRandomDiner();
            AddLog($"+ {seats[seatIdx].name} -> Seat {seatIdx + 1}  (G:{seats[seatIdx].gold} Fee:{seats[seatIdx].baseFee})");
        }

        AddLog($"--- Day {currentDay} Round {currentRound}/{ROUNDS_PER_DAY} ---");
        RefreshAll();
    }

    void OnCardClicked(int index)
    {
        if (phase != RoundPhase.Playing) return;
        selectedCardIndex = (selectedCardIndex == index) ? -1 : index;
        RefreshAll();
    }

    void OnSeatClicked(int seatIdx)
    {
        if (phase != RoundPhase.Playing) return;
        if (selectedCardIndex < 0 || selectedCardIndex >= hand.Count) return;
        if (seats[seatIdx] == null) return;

        var card = hand[selectedCardIndex];
        var diner = seats[seatIdx];

        diner.totalSpent += card.addOnCost;
        card.durability--;
        AddLog($"  Play [{card.name}] +{card.addOnCost} on {diner.name}  (dur {card.durability}/{card.maxDurability})");

        if (card.durability <= 0)
        {
            AddLog($"    >> [{card.name}] EXHAUSTED!");
            hand.RemoveAt(selectedCardIndex);
        }

        selectedCardIndex = -1;
        RefreshAll();
    }

    void OnEndRound()
    {
        if (phase != RoundPhase.Playing) return;
        phase = RoundPhase.Settlement;
        SettleRound();
    }

    void SettleRound()
    {
        AddLog("== Settlement ==");
        int roundRevenue = 0;
        int roundDevours = 0;

        for (int i = 0; i < SEAT_COUNT; i++)
        {
            var d = seats[i];
            if (d == null) continue;

            int bill = d.baseFee + d.totalSpent;

            if (bill <= d.gold)
            {
                roundRevenue += bill;
                AddLog($"  {d.name}: Bill {bill} <= Gold {d.gold} -> PAID +{bill}");
                seats[i] = null;
            }
            else
            {
                roundRevenue += d.gold;
                roundDevours++;
                AddLog($"  {d.name}: Bill {bill} > Gold {d.gold} -> DEVOURED! +{d.gold}");

                if (DEVOUR_CARDS.ContainsKey(d.race) && hand.Count < MAX_HAND_SIZE)
                {
                    var nc = MakeCard(DEVOUR_CARDS[d.race]);
                    hand.Add(nc);
                    AddLog($"    >> New card: [{nc.name}] +{nc.addOnCost} dur {nc.durability}");
                }

                seats[i] = null;
            }
        }

        dailyRevenue += roundRevenue;
        totalRevenue += roundRevenue;
        devourCount += roundDevours;
        AddLog($"Round revenue: {roundRevenue}  |  Daily: {dailyRevenue}/{dailyTarget}");

        // Check day end conditions
        if (dailyRevenue >= dailyTarget)
        {
            AddLog($">>> Day {currentDay} PASSED! ({dailyRevenue}/{dailyTarget})");
            phase = RoundPhase.DayEnd;
            endRoundBtn.gameObject.SetActive(false);
            newDayBtn.gameObject.SetActive(true);
            restartBtn.gameObject.SetActive(false);
        }
        else if (currentRound >= ROUNDS_PER_DAY)
        {
            AddLog($">>> GAME OVER - Target not met ({dailyRevenue}/{dailyTarget})");
            phase = RoundPhase.GameOver;
            endRoundBtn.gameObject.SetActive(false);
            newDayBtn.gameObject.SetActive(false);
            restartBtn.gameObject.SetActive(true);
        }
        else
        {
            currentRound++;
            BeginRound();
        }

        RefreshAll();
    }

    void OnNewDay()
    {
        currentDay++;
        currentRound = 1;
        dailyRevenue = 0;
        dailyTarget = 150 + (currentDay - 1) * 60;

        for (int i = 0; i < SEAT_COUNT; i++)
            seats[i] = null;

        AddLog($"\n=== Day {currentDay} | Target: {dailyTarget} ===");

        newDayBtn.gameObject.SetActive(false);
        restartBtn.gameObject.SetActive(false);
        endRoundBtn.gameObject.SetActive(true);

        BeginRound();
    }

    #endregion

    #region Helpers

    DinerState MakeRandomDiner()
    {
        var t = DINER_TEMPLATES[Random.Range(0, DINER_TEMPLATES.Length)];
        return new DinerState
        {
            name = t.name, race = t.race,
            gold = Random.Range(t.goldMin, t.goldMax + 1),
            baseFee = t.baseFee,
            totalSpent = 0,
            color = t.color,
        };
    }

    CardState MakeCard(CardTemplate t)
    {
        return new CardState
        {
            name = t.name, race = t.race,
            addOnCost = t.addOnCost,
            durability = t.maxDurability,
            maxDurability = t.maxDurability,
            color = t.color,
        };
    }

    void AddLog(string msg)
    {
        gameLog.AppendLine(msg);
        if (logText == null) return;

        logText.text = gameLog.ToString();
        // Keep last 25 lines
        var lines = gameLog.ToString().Split('\n');
        if (lines.Length > 26)
        {
            gameLog.Clear();
            for (int i = lines.Length - 26; i < lines.Length; i++)
                gameLog.AppendLine(lines[i]);
            logText.text = gameLog.ToString();
        }
    }

    #endregion

    #region UI Refresh

    void RefreshAll()
    {
        // --- Top bar ---
        dayLabel.text = $"Day {currentDay}";
        roundLabel.text = $"Round {currentRound}/{ROUNDS_PER_DAY}";
        revenueLabel.text = $"Rev: {dailyRevenue}";
        targetLabel.text = $"Target: {dailyTarget}";

        // --- Phase hint ---
        switch (phase)
        {
            case RoundPhase.Playing:
                if (selectedCardIndex >= 0 && selectedCardIndex < hand.Count)
                    phaseLabel.text = $">> Selected: [{hand[selectedCardIndex].name}] - click a diner to play";
                else
                    phaseLabel.text = "Select a card from hand, then click a diner";
                phaseLabel.color = Color.cyan;
                break;
            case RoundPhase.DayEnd:
                phaseLabel.text = $"Day {currentDay} complete! Click 'Next Day' to continue.";
                phaseLabel.color = Color.green;
                break;
            case RoundPhase.GameOver:
                phaseLabel.text = $"Game Over on Day {currentDay}. Total revenue: {totalRevenue}, Devours: {devourCount}";
                phaseLabel.color = Color.red;
                break;
            default:
                phaseLabel.text = phase.ToString();
                phaseLabel.color = Color.white;
                break;
        }

        // --- Seats ---
        for (int i = 0; i < SEAT_COUNT; i++)
        {
            var d = seats[i];
            if (d != null)
            {
                seatPanels[i].GetComponent<Image>().color = new Color(0.2f, 0.15f, 0.28f);
                seatNameTexts[i].text = d.name;
                seatNameTexts[i].color = d.color;
                seatGoldTexts[i].text = $"Gold: {d.gold}";
                seatFeeTexts[i].text = $"BaseFee: {d.baseFee}";
                seatSpentTexts[i].text = d.totalSpent > 0 ? $"+Spent: {d.totalSpent}" : "";
                int bill = d.baseFee + d.totalSpent;
                seatBillTexts[i].text = $"Bill: {bill}";
                seatBillTexts[i].color = bill > d.gold ? new Color(1f, 0.3f, 0.3f) : new Color(0.3f, 1f, 0.3f);
            }
            else
            {
                seatPanels[i].GetComponent<Image>().color = new Color(0.1f, 0.07f, 0.14f);
                seatNameTexts[i].text = "-- Empty --";
                seatNameTexts[i].color = new Color(0.3f, 0.3f, 0.3f);
                seatGoldTexts[i].text = "";
                seatFeeTexts[i].text = "";
                seatSpentTexts[i].text = "";
                seatBillTexts[i].text = "";
            }
        }

        // --- Cards in hand ---
        foreach (var go in cardPanels)
            Destroy(go);
        cardPanels.Clear();

        int handCount = hand.Count;
        if (handCount == 0) return;

        float cardW = 1f / handCount;
        for (int i = 0; i < handCount; i++)
        {
            float x0 = i * cardW + 0.005f;
            float x1 = (i + 1) * cardW - 0.005f;
            bool sel = (selectedCardIndex == i);

            Color bg = sel ? new Color(0.4f, 0.3f, 0.55f) : new Color(0.16f, 0.12f, 0.22f);
            var card = MakeImage(handContainer, bg, new Vector2(x0, 0.02f), new Vector2(x1, 0.84f));
            cardPanels.Add(card);

            // Border highlight for selected
            if (sel)
            {
                var border = MakeImage(card.transform, Color.cyan, Vector2.zero, Vector2.one);
                border.transform.SetAsFirstSibling();
                border.GetComponent<RectTransform>().sizeDelta = new Vector2(-4, -4);
            }

            var c = hand[i];
            MakeLabel(card.transform, c.name, 13, c.color, TextAnchor.MiddleCenter,
                new Vector2(0, 0.72f), new Vector2(1, 0.95f));
            MakeLabel(card.transform, $"+{c.addOnCost}", 18, Color.yellow, TextAnchor.MiddleCenter,
                new Vector2(0, 0.35f), new Vector2(1, 0.68f));
            MakeLabel(card.transform, $"{c.durability}/{c.maxDurability}", 13,
                c.durability == 1 ? Color.red : Color.white, TextAnchor.MiddleCenter,
                new Vector2(0, 0.05f), new Vector2(1, 0.3f));

            // Clickable
            var btnObj = new GameObject("CB");
            btnObj.transform.SetParent(card.transform, false);
            var br = btnObj.AddComponent<RectTransform>();
            br.anchorMin = Vector2.zero; br.anchorMax = Vector2.one;
            br.offsetMin = Vector2.zero; br.offsetMax = Vector2.zero;
            btnObj.AddComponent<Image>().color = Color.clear;
            var cb = btnObj.AddComponent<Button>();
            int ci = i;
            cb.onClick.AddListener(() => OnCardClicked(ci));
        }
    }

    #endregion
}
