using UnityEngine;
using System.Collections;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public enum GameState
    {
        WaitingForPlayers,
        Playing,
        Ended
    }

    public GameState currentState =
        GameState.WaitingForPlayers;

    void Awake()
    {
        Instance = this;
    }

    // =========================
    // Start Match
    // =========================

    public void StartGame()
    {
        if (currentState !=
            GameState.WaitingForPlayers)
            return;

        currentState = GameState.Playing;

        Debug.Log("MATCH STARTED!");
    }

    // =========================
    // End Match
    // =========================

    public void EndGame(string winner)
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

        StartCoroutine(EndDelay());
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
            PlayerMovement player in players)
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
}