using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameState
{
    Start,
    Playing,
    Event,
    Pause,
    Result,
    Prepare,
    Wait
}

public class GameManager : MonoBehaviour
{
    public bool nowState;
    void Update()
    {
        if (nowState)
            Debug.Log(currentGameState);
    }

    public static GameManager Instance;

    // 現在の状態
    public static GameState currentGameState;


    void Awake()
    {
        Instance = this;
        SetCurrentState(GameState.Playing);
    }


    // 外からこのメソッドを使って状態を変更
    public void SetCurrentState(GameState state)
    {
        currentGameState = state;
        OnGameStateChanged(currentGameState);
    }

    // 状態が変わったら何をするか
    void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Start:
                StartAction();
                break;
            case GameState.Event:
                EventAction();
                break;
            case GameState.Playing:
                PlayingAction();
                break;
            case GameState.Pause:
                PauseAction();
                break;
            default:
                break;
        }
    }

    // Startになったときの処理
    void StartAction()
    {
    }
    void EventAction()
    {
        /*
        PlayerInput.cool = true;
        GameObject.Find("Player").GetComponent<WarpControl>().setBan(true);
        //RuruAnime.stop = true;
        */
    }

    // Playingになったときの処理
    void PlayingAction()
    {
        /*
        PlayerInput.cool = false;
        GameObject.Find("Player").GetComponent<WarpControl>().setBan(false);
        //RuruAnime.stop = false;
        */
    }
    // Endになったときの処理
    void PauseAction()
    {
    }
}