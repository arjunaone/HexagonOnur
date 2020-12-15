using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Hex : MonoBehaviour
{
    //public Vector2Int coordinate; //unused
    public int tileNumberX; // X coordinate
    public int tileNumberY; // Y coordinate
    public int colorNo; //color index on colorList
    public int isBombStar; //0: normal, 1: star, 2: bomb
    public int bombTimer; //only used for bomb objects

    
    void Awake()
    {
        bombTimer = 8; //Starts with 8 turns, Start method is not used for not overriding at undo method.
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CollideSound()
    {
        MainScr.instance.soundMaster.volume = 0.1f;
        MainScr.instance.soundMaster.PlayOneShot(MainScr.instance.collideSound);
        GetComponent<SpriteRenderer>().sortingOrder = 0;
    }

    //Command to move this hexagon to its coordinated position
    public void MoveToPosition(float delay, float duration)
    {
        Vector3 targetPosition = MainScr.instance.tilePositionList[tileNumberX, tileNumberY];
        transform.DOMove(targetPosition, duration).SetEase(Ease.Linear).SetDelay(delay)
            .OnComplete(() => CollideSound());
    }

    //only used for bombs
    public void EndTurn()
    {
        bombTimer--;
        if (bombTimer > 0)
        {
            MainScr.instance.soundMaster.PlayOneShot(MainScr.instance.alarmSound);
            GetComponentInChildren<TextMesh>().text = bombTimer.ToString();
            transform.DOShakeScale(0.2f, 0.1f, 20, 25f, false);
            transform.DOShakePosition(0.65f, 0.1f, 20, 25f, false);
        }
        else
        {
            GetComponentInChildren<TextMesh>().text = bombTimer.ToString();
            MainScr.instance.GameOver();
            MainScr.instance.started = false;
        }

    }
}
