using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonEvent : MonoBehaviour
{
    GameObject swordman;
    Sword_Man sm;
    GameObject LU;
    GameObject EH;
    // Start is called before the first frame update
    void Start()
    {
        swordman = GameObject.Find("sword_man");
        sm = swordman.GetComponent<Sword_Man>();
        LU = GameObject.Find("Levelup");
        EH = GameObject.Find("Enhance");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void LeftBtnDown()
    {
        sm.MoveLeft();
    }
    public void RightBtnDown()
    {
        sm.MoveRight();
    }
    public void OnLevelupClick()
    {
        sm.maxHp += 20;
        sm.maxSt += 10;
        sm.nowHp = sm.maxHp;
        sm.nowSt = sm.maxSt;
        sm.reward = false;
        sm.InputFree();
        LU.SetActive(false);
        EH.SetActive(false);
        sm.isdone = true;
    }
    public void OnEnhanceClick()
    {
        sm.ChangeWeapon();
        sm.reward = false;
        sm.InputFree();
        LU.SetActive(false);
        EH.SetActive(false);
        sm.isdone = true;
    }
    public void OnHomeClick()
    {
        sm.removeAll();
    }
    public void onRetryClick()
    {
        sm.Die();
    }
    public void onStartClick()
    {
        SceneManager.LoadScene("first_stage");
    }
    public void onTutorialClick()
    {
        SceneManager.LoadScene("tutorial_stage");
    }
}
