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
        PlayerInputHandler[] all =
    FindObjectsByType<PlayerInputHandler>(
        FindObjectsSortMode.None
    );

        foreach (var p in all)
        {
            NetworkBehaviour nb =
                p.GetComponent<NetworkBehaviour>();

            if (nb != null && nb.isLocalPlayer)
            {
                input = p;
                break;
            }
        }
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
            PlayerInputHandler[] all =
    FindObjectsByType<PlayerInputHandler>(
        FindObjectsSortMode.None
    );

            foreach (var p in all)
            {
                NetworkBehaviour nb =
                    p.GetComponent<NetworkBehaviour>();

                if (nb != null && nb.isLocalPlayer)
                {
                    input = p;
                    break;
                }
            }

            return;
        }

        if (input.PausePressed)
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
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
        opened = false;
        root.SetActive(false);

        GameManager.instance.LeaveRoom();
    }
}