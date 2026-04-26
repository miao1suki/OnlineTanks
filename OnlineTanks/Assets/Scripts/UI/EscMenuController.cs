using Mirror;
using UnityEngine;

public class EscMenuController : MonoBehaviour
{
    public static EscMenuController Instance;

    public GameObject root;

    bool opened;

    PlayerInputHandler input;

    void Awake()
    {
        Instance = this;

        root.SetActive(false);
    }

    void Start()
    {
        input =
            FindFirstObjectByType<PlayerInputHandler>();
    }

    void Update()
    {
        if (!UnityEngine.SceneManagement
            .SceneManager
            .GetActiveScene()
            .name.Equals("Game"))
            return;

        if (input == null)
        {
            input =
                FindFirstObjectByType<PlayerInputHandler>();

            return;
        }

        if (input.PausePressed)
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        opened = !opened;

        root.SetActive(opened);
    }

    public bool IsOpen()
    {
        return opened;
    }

    public void ResumeGame()
    {
        opened = false;

        root.SetActive(false);
    }

    public void ExitRoom()
    {
        // œ»Õ£ LAN π„≤•
        LANRoomCreator creator =
            FindFirstObjectByType<LANRoomCreator>();

        if (creator != null)
            creator.StopRoom();

        // ‘ŸÕ£ Mirror
        if (NetworkServer.active && NetworkClient.active)
            NetworkManager.singleton.StopHost();
        else
            NetworkManager.singleton.StopClient();
    }
}