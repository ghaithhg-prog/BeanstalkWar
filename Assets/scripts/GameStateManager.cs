using UnityEngine;
using System.Collections;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    public enum GameState
    {
        Playing,
        Ended
    }

    public GameState currentState = GameState.Playing;

    void Awake()
    {
        Instance = this;
    }

    public void EndGame(string winner)
    {
        if (currentState == GameState.Ended)
            return;

        currentState = GameState.Ended;

        Debug.Log("Game Ended! Winner: " + winner);

        StartCoroutine(EndDelay());
    }

    IEnumerator EndDelay()
    {
        // ✅ اللاعبون يتحركون 3 ثوانٍ
        yield return new WaitForSeconds(3f);

        // ✅ إيقاف حركة جميع اللاعبين
        PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();

        foreach (PlayerMovement player in players)
        {
            player.enabled = false;
        }

        Debug.Log("Players disabled after delay.");
    }
}