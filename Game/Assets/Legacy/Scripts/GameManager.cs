using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Sword_Man swordman;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Save()
    {
        PlayerPrefs.SetFloat("maxhp", swordman.maxHp);
        PlayerPrefs.SetFloat("maxmp", swordman.maxMp);
        PlayerPrefs.SetFloat("maxst", swordman.maxSt);
        PlayerPrefs.SetFloat("maxsk", swordman.maxsk);
        PlayerPrefs.SetFloat("atkdmg", swordman.atkDmg);
        PlayerPrefs.SetString("weapon", swordman.weapon.sprite.name);
        PlayerPrefs.SetInt("saved", 1);
    }
    public void Load()
    {
        swordman.maxHp = PlayerPrefs.GetFloat("maxhp");
        swordman.maxMp = PlayerPrefs.GetFloat("maxmp");
        swordman.maxSt = PlayerPrefs.GetFloat("maxst");
        swordman.maxsk = PlayerPrefs.GetFloat("maxsk");
        swordman.atkDmg = PlayerPrefs.GetFloat("atkdmg");
        swordman.weapon.sprite = Resources.Load<Sprite>(PlayerPrefs.GetString("weapon"));
    }
}
