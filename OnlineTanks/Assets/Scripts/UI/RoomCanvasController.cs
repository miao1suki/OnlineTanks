using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class RoomCanvasController : MonoBehaviour
{
    public static RoomCanvasController Instance;

    [Header("Root")]
    public GameObject root;

    [Header("BackgroundPanel")]
    public GameObject backgroundPanel;

    [Header("Texts")]
    public TMP_Text roomStateText;
    public TMP_Text countdownText;
    public TMP_Text winnerText;

    [Header("Player List")]
    public Transform playerListRoot;
    public GameObject playerItemPrefab;

    public bool CanSurrender { get; private set; }

    float playTimer;
    bool playingTimer;
    double prepareEndTime;
    double generateEndTime;

    Dictionary<uint, PlayerListItem> items =
        new Dictionary<uint, PlayerListItem>();

    void Awake()
    {
        if(Instance != null && Instance != this)
    {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        root.SetActive(false);
    }

    void OnEnable()
    {
        if (Instance == null)
            Instance = this;
    }

    void Update()
    {
        if (playingTimer)
        {
            playTimer -= Time.deltaTime;

            // 不显示剩余秒数
            if (playTimer <= 0 && !CanSurrender)
            {
                CanSurrender = true;

                countdownText.text =
                    "按B键放弃";
            }
        }

        // 客户端显示开始游戏倒计时
        switch (MatchManager.Instance?.currentState)
        {
            case RoomState.Preparing:
                TickPreparing(prepareEndTime);
                break;

            case RoomState.Generating:
                TickGenerating(generateEndTime);
                break;
        }
    }

    public void SetPrepareEnd(double endTime)
    {
        prepareEndTime = endTime;
    }

    public void SetGenerateEnd(double endTime)
    {
        generateEndTime = endTime;
    }

    public void TickPreparing(double endTime)
    {
        float remain = (float)(endTime - NetworkTime.time);
        if (remain < 0) remain = 0;

        countdownText.text =
            "等待玩家就绪: " + Mathf.CeilToInt(remain);
    }

    public void TickGenerating(double endTime)
    {
        float remain = (float)(endTime - NetworkTime.time);
        if (remain < 0) remain = 0;

        countdownText.text =
            "加载中: " + Mathf.CeilToInt(remain);
    }

    public void ShowCanvas(bool b)
    {
        Debug.Log("尝试更改canvas开关");
        root.SetActive(b);
    }

    public void RefreshPlayerList
    (
        List<PlayerController> players
    )
    {
        foreach (
            Transform t in playerListRoot
        )
        {
            Destroy(t.gameObject);
        }

        items.Clear();

        foreach (var p in players)
        {
            GameObject go =
                Instantiate(
                    playerItemPrefab,
                    playerListRoot
                );

            PlayerListItem item =
                go.GetComponent<PlayerListItem>();

            item.Bind(p);

            items[p.netId] = item;
        }
    }

    public void ShowWaiting()
    {
        ShowCanvas(true);
        backgroundPanel.SetActive(true);
        playerListRoot.gameObject.SetActive(true);

        roomStateText.text =
            "等待玩家进入";

        countdownText.text = "";
        winnerText.text = "";
    }

    public void ShowPreparing()
    {
        if (!root.activeSelf)
        {
            ShowCanvas(true);
        }

        backgroundPanel.SetActive(true);
        playerListRoot.gameObject.SetActive(true);

        roomStateText.text =
            "准备阶段";

        countdownText.text = "" ;
        winnerText.text = "";

    }

    public void ShowGenerating()
    {
        root.SetActive(true);
        backgroundPanel.SetActive(true);
        playerListRoot.gameObject.SetActive(true);
    }

    public void ShowPlaying()
    {
        ShowCanvas(true);
        backgroundPanel.SetActive(false);
        playerListRoot.gameObject.SetActive(false);

        roomStateText.text = "";
        countdownText.text = "";
        winnerText.text = "";

        playTimer = 120f;

        playingTimer = true;

        CanSurrender = false;
    }

    public void ShowSettlement(string winner)
    {
        playingTimer = false;

        CanSurrender = false;

        ShowCanvas(true);
        backgroundPanel.SetActive(true);
        playerListRoot.gameObject.SetActive(false);

        roomStateText.text =
            "结算阶段";

        winnerText.text =
            "胜利玩家：" + winner;

        countdownText.text = "";
    }

    public void ResetUI()
    {
        playingTimer = false;

        CanSurrender = false;

        prepareEndTime = 0;
        generateEndTime = 0;

        countdownText.text = "";
        roomStateText.text = "";
        winnerText.text = "";

        foreach (Transform t in playerListRoot)
            Destroy(t.gameObject);

        items.Clear();

        root.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}