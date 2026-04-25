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

    Dictionary<uint, PlayerListItem> items =
        new Dictionary<uint, PlayerListItem>();

    void Awake()
    {
        Instance = this;

        root.SetActive(false);
    }

    void Update()
    {
        if (playingTimer)
        {
            playTimer -= Time.deltaTime;

            if (playTimer > 0)
            {
                countdownText.text =
                    "剩余时间: " +
                    Mathf.CeilToInt(playTimer);
            }
            else
            {
                countdownText.text =
                    "按B键放弃";

                CanSurrender = true;
            }
        }
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

        roomStateText.text =
            "等待玩家进入";

        countdownText.text = "";
        winnerText.text = "";
    }

    public void ShowPreparing(float remain)
    {
        ShowCanvas(true);
        backgroundPanel.SetActive(true);

        roomStateText.text =
            "准备阶段";

        countdownText.text =
            "开始倒计时: " +
            Mathf.CeilToInt(remain);
    }

    public void ShowGenerating()
    {
        root.SetActive(true);
        backgroundPanel.SetActive(true);
    }

    public void ShowPlaying()
    {
        ShowCanvas(true);
        backgroundPanel.SetActive(false);

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

        roomStateText.text =
            "结算阶段";

        winnerText.text =
            "胜利玩家：" + winner;
    }
}