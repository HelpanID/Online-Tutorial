using Mirror;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : NetworkBehaviour
{
    bool facingRight = true;
    public static Player localPlayer;
    public TextMesh NameDisplayText;
    [SyncVar] public Color PlayerColor;
    [SyncVar(hook = "DisplayPlayerName")] public string PlayerDisplayName;
    [SyncVar] public string matchID;

    [SyncVar] public Match CurrentMatch;
    public GameObject PlayerLobbyUI;
    public SpriteRenderer[] sprites;

    private Guid netIDGuid;

    private GameObject GameUI;

    private NetworkMatch networkMatch;
    private Animator animator;

    private void Awake()
    {
        networkMatch = GetComponent<NetworkMatch>();
        GameUI = GameObject.FindGameObjectWithTag("GameUI");
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            animator = GetComponent<Animator>();

            CmdSendName(MainMenu.instance.DisplayName, MainMenu.instance.PlayerColor);
        }
    }

    public override void OnStartServer()
    {
        netIDGuid = netId.ToString().ToGuid();
        networkMatch.matchId = netIDGuid;
    }

    public override void OnStartClient()
    {
        if (isLocalPlayer)
        {
            localPlayer = this;
        }
        else
        {
            PlayerLobbyUI = MainMenu.instance.SpawnPlayerUIPrefab(this);
        }
    }

    public override void OnStopClient()
    {
        ClientDisconnect();
    }

    public override void OnStopServer()
    {
        ServerDisconnect();
    }

    [Command]
    void CmdSendSprites(int index)
    {
        RpcSendSprites(index);
    }

    [ClientRpc]
    void RpcSendSprites(int index)
    {
        sprites[0].sprite = MainMenu.instance.Characters[index].Sprites[0];
        sprites[1].sprite = MainMenu.instance.Characters[index].Sprites[1];
        sprites[2].sprite = MainMenu.instance.Characters[index].Sprites[2];
        sprites[3].sprite = MainMenu.instance.Characters[index].Sprites[2];
        sprites[4].sprite = MainMenu.instance.Characters[index].Sprites[3];
        sprites[5].sprite = MainMenu.instance.Characters[index].Sprites[3];
    }

    [Client]
    void SendSprites()
    {
        if (isLocalPlayer)
        {
            CmdSendSprites(PlayerPrefs.GetInt("index"));
        }
    }

    [Command]
    public void CmdSendName(string name, Color color)
    {
        PlayerDisplayName = name;
        PlayerColor = color;
    }

    public void DisplayPlayerName(string name, string playerName)
    {
        name = PlayerDisplayName;
        Debug.Log("Имя " + name + " : " + playerName);
        NameDisplayText.text = playerName;
    }

    [Command]
    public void CmdHandleMessage(string message)
    {
        RpcHandleMessage($"<color=#{ColorUtility.ToHtmlStringRGB(PlayerColor)}>{PlayerDisplayName}:</color> {message}");
    }

    [ClientRpc]
    void RpcHandleMessage(string message)
    {
        MainMenu.instance.SendMessageToServer(message);
    }

    public void HostGame(bool publicMatch, int mapIndex)
    {
        string ID = MainMenu.GetRandomID();
        CmdHostGame(ID, publicMatch, mapIndex);
    }

    [Command]
    public void CmdHostGame(string ID, bool publicMatch, int mapIndex)
    {
        matchID = ID;
        if (MainMenu.instance.HostGame(ID, gameObject, publicMatch, mapIndex))
        {
            Debug.Log("Лобби было создано успешно");
            networkMatch.matchId = ID.ToGuid();
            TargetHostGame(true, ID, mapIndex);
        }
        else
        {
            Debug.Log("Ошибка в создании лобби");
            TargetHostGame(false, ID, mapIndex);
        }
    }

    [TargetRpc]
    void TargetHostGame(bool success, string ID, int mapIndex)
    {
        matchID = ID;
        Debug.Log($"ID {matchID} == {ID}");
        MainMenu.instance.HostSuccess(success, ID, mapIndex);
    }

    public void JoinGame(string inputID)
    {
        CmdJoinGame(inputID);
    }

    [Command]
    public void CmdJoinGame(string ID)
    {
        matchID = ID;
        if (MainMenu.instance.JoinGame(ID, gameObject))
        {
            Debug.Log("Успешное подключение к лобби");
            networkMatch.matchId = ID.ToGuid();
            TargetJoinGame(true, ID);
        }
        else
        {
            Debug.Log("Не удалось подключиться");
            TargetJoinGame(false, ID);
        }
    }

    [TargetRpc]
    void TargetJoinGame(bool success, string ID)
    {
        matchID = ID;
        Debug.Log($"ID {matchID} == {ID}");
        MainMenu.instance.JoinSuccess(success, ID);
        Invoke(nameof(SetLobbyMap), 0.1f);
    } 

    void SetLobbyMap()
    {
        MainMenu.instance.SetLobbyMap(CurrentMatch.Map);
    }

    public void DisconnectGame()
    {
        CmdDisconnectGame();
    }

    [Command(requiresAuthority = false)]
    void CmdDisconnectGame()
    {
        ServerDisconnect();
    }

    void ServerDisconnect()
    {
        MainMenu.instance.PlayerDisconnected(gameObject, matchID);
        RpcDisconnectGame();
        networkMatch.matchId = netIDGuid;
    }

    [ClientRpc]
    void RpcDisconnectGame()
    {
        ClientDisconnect();
    }

    void ClientDisconnect()
    {
        if (PlayerLobbyUI != null)
        {
            if (!isServer)
            {
                Destroy(PlayerLobbyUI);
            }
            else
            {
                PlayerLobbyUI.SetActive(false);
            }
        }
    }

    public void SearchGame()
    {
        CmdSearchGame();
    }

    [Command]
    void CmdSearchGame()
    {
        if (MainMenu.instance.SearchGame(gameObject, out matchID))
        {
            Debug.Log("Игра найдена успешно");
            networkMatch.matchId = matchID.ToGuid();
            TargetSearchGame(true, matchID);

            if (isServer && PlayerLobbyUI != null)
            {
                PlayerLobbyUI.SetActive(true);
            }
        }
        else
        {
            Debug.Log("Поиск игры не удался");
            TargetSearchGame(false, matchID);
        }
    }

    [TargetRpc]
    void TargetSearchGame(bool success, string ID)
    {
        matchID = ID;
        Debug.Log("ID: " + matchID + "==" + ID + " | " + success);
        MainMenu.instance.SearchGameSuccess(success, ID);
        Invoke(nameof(SetLobbyMap), 0.1f);
    }

    [Server]
    public void PlayerCountUpdated(int playerCount)
    {
        TargetPlayerCountUpdated(playerCount);
    }

    [TargetRpc]
    void TargetPlayerCountUpdated(int playerCount)
    {
        if (playerCount > 1)
        {
            MainMenu.instance.SetBeginButtonActive(true);
        }
        else
        {
            MainMenu.instance.SetBeginButtonActive(false);
        }
    }

    public void BeginGame()
    {
        CmdBeginGame();
    }

    [Command]
    public void CmdBeginGame()
    {
        MainMenu.instance.BeginGame(matchID);
        Debug.Log("Игра начилась");
    }

    public void StartGame()
    {
        TargetBeginGame();
    }

    [TargetRpc]
    void TargetBeginGame()
    {
        Debug.Log($"ID {matchID} | начало");

        Player[] players = FindObjectsOfType<Player>();
        for (int i = 0; i < players.Length; i++)
        {
            DontDestroyOnLoad(players[i]);
        }

        GameUI.GetComponent<Canvas>().enabled = true;
        MainMenu.instance.inGame = true;
        SendSprites();
        transform.localScale = new Vector3(0.41664f, 0.41664f, 0.41664f); //Размер вашего игрока (x, y, z)
        facingRight = true;
        SceneManager.LoadScene(MainMenu.instance.Maps[CurrentMatch.Map].MapScene, LoadSceneMode.Additive);
        Invoke(nameof(SetPlayer), 0.1f);
    }

    void SetPlayer()
    {
        FindObjectOfType<GameM>().Follow = transform;

        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        transform.position = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform.position;
    }

    private void Update()
    {
        if (hasAuthority)
        {
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            float speed = 6f * Time.deltaTime;
            transform.Translate(new Vector2(input.x * speed, input.y * speed));
            if (input.x == 0 && input.y == 0)
            {
                animator.SetBool("walk", false);
            }
            else
            {
                animator.SetBool("walk", true);
            }
            if (!facingRight && input.x > 0)
            {
                Flip();
            }
            else if (facingRight && input.x < 0)
            {
                Flip();
            }
        }
    }

    void Flip()
    {
        if (hasAuthority)
        {
            facingRight = !facingRight;
            Vector3 Scale = transform.localScale;
            Scale.x *= -1;
            transform.localScale = Scale;

            if (MainMenu.instance.inGame)
            {
                Vector3 TextScale = NameDisplayText.transform.localScale;
                TextScale.x *= -1;
                NameDisplayText.transform.localScale = TextScale;
            }
        }
    }
}