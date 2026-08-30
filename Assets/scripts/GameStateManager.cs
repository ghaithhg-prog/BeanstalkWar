using UnityEngine;
using System.Collections;
using TMPro;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public enum GameState
    {
        WaitingForPlayers,
        Countdown,
        Playing,
        Ended
    }

    public GameState currentState =
        GameState.WaitingForPlayers;

    [Header("Match Start")]
    public float countdownDuration = 3f;

    [Header("UI")]
    public TextMeshProUGUI countdownText;

    private bool countdownStarted = false;

    void Awake()
    {
        Instance = this;

        // نخفي النص عند بداية اللعبة
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    // =========================
    // Request Match Start
    // =========================

    public void RequestStartGame()
    {
        if (currentState !=
            GameState.WaitingForPlayers)
            return;

        if (countdownStarted)
            return;

        countdownStarted = true;

        StartCoroutine(
            CountdownCoroutine()
        );
    }

    // =========================
    // Countdown
    // =========================

    IEnumerator CountdownCoroutine()
    {
        currentState =
            GameState.Countdown;

        int count =
            Mathf.CeilToInt(
                countdownDuration
            );

        // نظهر النص
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        while (count > 0)
        {
            Debug.Log(
                "MATCH STARTS IN " +
                count
            );

            if (countdownText != null)
            {
                countdownText.text =
                    count.ToString();
            }

            yield return
                new WaitForSeconds(1f);

            count--;
        }

        // =========================
        // GO
        // =========================

        Debug.Log("GO!");

        if (countdownText != null)
        {
            countdownText.text =
                "GO!";
        }

        yield return
            new WaitForSeconds(0.7f);

        StartGame();

        // نخفي النص بعد البداية
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    // =========================
    // Start Match
    // =========================

    void StartGame()
    {
        if (currentState !=
            GameState.Countdown)
            return;

        currentState =
            GameState.Playing;

        Debug.Log(
            "MATCH STARTED!"
        );
    }

    // =========================
    // End Match
    // =========================

    public void EndGame(
        string winner)
    {
        if (currentState ==
            GameState.Ended)
            return;

        currentState =
            GameState.Ended;

        Debug.Log(
            "Game Ended! Winner: " +
            winner
        );

        StartCoroutine(
            EndDelay()
        );
    }

    IEnumerator EndDelay()
    {
        yield return
            new WaitForSeconds(3f);

        PlayerMovement[] players =
            FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

        foreach (
            PlayerMovement player
            in players)
        {
            player.enabled = false;
        }

        Debug.Log(
            "Players disabled after delay."
        );
    }

    // =========================
    // Helpers
    // =========================

    public bool IsPlaying()
    {
        return currentState ==
               GameState.Playing;
    }

    public bool IsWaiting()
    {
        return currentState ==
               GameState.WaitingForPlayers;
    }
}