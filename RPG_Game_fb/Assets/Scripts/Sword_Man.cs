using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.Windows;
using static UnityEngine.EventSystems.EventTrigger;
using Input = UnityEngine.Input;

public class Sword_Man : MonoBehaviour
{
    public GameObject objSwordMan;
    Animator animator;
    public float maxHp = 0;
    public float nowHp;
    public float maxMp = 0;
    public float nowMp;
    public float maxSt = 0;
    public float nowSt;
    public float maxsk = 0;
    public float atkDmg = 0;
    public float atkSpeed = 1;
    public bool attacked = false;
    public float Hpheal;
    public float Stheal;
    
    public int jc = 0;
    public float lowDmg;
    public Image nowHpbar;
    public Image nowMpbar;
    public Image nowStbar;
    public Image skill;
    public Image heal;
    public Image element;
    public bool inputRight = false;
    public bool inputLeft = false;
    public bool inputUp = false;
    public float jumpPower = 300;
    public float moveSpeed = 1000;
    public float hrt;
    public float rt;
    public float resttime;
    public bool ss = false;
    public bool hs = false;
    public bool god = false;
    public Vector3 clearpos;
    public GameObject fireball;
    public float minarea;
    public float allowarea;
    GameObject fb;
    Rigidbody2D rigid2D;
    BoxCollider2D col2D;
    CapsuleCollider2D cap2D;
    ParticleSystem ps;
    GameObject levelup;
    GameObject enhance;
    GameObject Panel;
    GameObject Gostart;
    GameObject Retry;
    GameObject Info;
    GameObject Light1;
    GameObject Light2;
    GameObject Light3;
    GameObject Glow1;
    GameObject Glow2;
    GameObject Glow3;
    MonsterManager mm;
    public bool IsSwordManDead = false;
    public SpriteRenderer weapon;
    public Sprite newweapon;
    bool allow = false;
    public bool istouched = false;
    public bool cango = false;
    public bool reward = false;
    public bool isdone = false;
    public bool isenhanced = false;
    bool success = false;
    float tmpdmg = 0;
    public GameManager GM;
    float st_maxhp;
    float st_maxst;
    float st_maxmp;
    Vector3 startPos;
    GameObject maxHpbar;
    GameObject maxStbar;
    GameObject maxMpbar;
    GameObject S1, S2, G1, G2;
    List<int> cl;
    public bool c_touch;
    string colorname;
    public bool colorflag;
    public int stackcount;
    public bool stone_touch;
    string stonename;
    public int color_num;
    bool isdoorup = false;
    public camera cam;
    Color curcolor;
    public bool clear;
    int level = 0;
    bool tutostone = false;
    public TextMeshProUGUI inform;
    string scenename;
    bool escaped = false;
    public bool isinputlocked = false;
    bool sptouched = false;
    bool spawned = false;
    GameObject Hades;
    public bool final = false;
    bool ltouched = false;
    public GameObject Lever;
    void Start()
    {
        scenename = SceneManager.GetActiveScene().name;
        st_maxhp = 60.0f;
        st_maxst = 70.0f;
        st_maxmp = 50.0f;
        clearpos = new Vector3(-5, 0, 0);
        minarea = -10.0f;
        allowarea = 60.0f;
        Hpheal = 5.0f;
        Stheal = 5.0f;
        moveSpeed = 1000;
        cl=new List<int>();
        colorflag = false;
        if(scenename == "second_stage")
        {
            allowarea = -5.0f;
        }
        Application.targetFrameRate = 60;
        if (PlayerPrefs.HasKey("saved"))
        {
            GM.Load();
        }
        else
        {
            maxHp = 60;
            maxMp = 50;
            maxSt = 70;
            atkDmg = 30;
            maxsk = 20.0f;
        }
        nowHp = maxHp;
        nowMp = maxMp;
        nowSt = maxSt;
        tmpdmg = atkDmg;
        objSwordMan.transform.position = clearpos;
        animator = GetComponent<Animator>();
        SetAttackSpeed(1.5f);
        rigid2D = GetComponent<Rigidbody2D>();
        //col2D = GetComponent<BoxCollider2D>();
        cap2D = GetComponent<CapsuleCollider2D>();
        ps = GetComponent<ParticleSystem>();
        maxHpbar = GameObject.Find("bghp_bar");
        maxStbar = GameObject.Find("bgst_bar");
        maxMpbar = GameObject.Find("bgmp_bar");
        Panel = GameObject.Find("Panel");
        Gostart = GameObject.Find("Gostart");
        Retry = GameObject.Find("Retry");
        if (scenename != "tutorial_stage")
        {
            levelup = GameObject.Find("Levelup");
            enhance = GameObject.Find("Enhance");
            Info = GameObject.Find("Info");
            levelup.SetActive(false);
            enhance.SetActive(false);
            Info.SetActive(false);
            Panel.SetActive(false);
        }
        if (scenename == "second_stage")
        {
            Light1 = GameObject.Find("Light1");
            Light2 = GameObject.Find("Light2");
            Light3 = GameObject.Find("Light3");
            Glow1 = GameObject.Find("Stone1/Glow");
            Glow2 = GameObject.Find("Stone2/Glow");
            Glow3 = GameObject.Find("Stone3/Glow");
            Light1.SetActive(false);
            Light2.SetActive(false);
            Light3.SetActive(false);
            Glow1.SetActive(false);
            Glow2.SetActive(false);
            Glow3.SetActive(false);
            element.gameObject.SetActive(false);
        }
        if (scenename == "third_stage")
        {
            S1 = GameObject.Find("Skeleton1");
            S2 = GameObject.Find("Skeleton2");
            G1 = GameObject.Find("Goblin1");
            G2 = GameObject.Find("Goblin2");
            Hades = GameObject.Find("Hades");
            Lever = GameObject.Find("Lever");
            S1.SetActive(false);
            S2.SetActive(false);
            G1.SetActive(false);
            G2.SetActive(false);
            Lever.SetActive(false);
            minarea = -9.0f;
            allowarea = 28.4f;
        }
        if (scenename == "tutorial_stage")
        {
            Light1 = GameObject.Find("Light");
            Light1.SetActive(false);
        }
        mm = FindObjectOfType<MonsterManager>();
        Gostart.SetActive(false);
        Retry.SetActive(false);
        obstacleLayer = LayerMask.GetMask("Monster");
        weapon = transform.Find("body/Weapon/Weapon-Sword").GetComponent<SpriteRenderer>();
        StartCoroutine(CheckSwordManDeath());
    }

    // Update is called once per frame
    void Update()
    {
        if (scenename == "tutorial_stage")
        {
            if (transform.position.x < 2.4f)
            {
                level = 0;
            }
            if(transform.position.x>=2.4f && transform.position.x<11.5f)
            {
                level = 1;
            }
            if (transform.position.x >= 11.5f && transform.position.x<=23.0f)
            {
                level = 2;
            }
            if (transform.position.x > 23.0f && transform.position.x<35.0f)
            {
                level = 3;
            }
            if(tutostone)
            {
                level = 4;
                atkDmg = 100;
            }
            if (clear && level==4) level = 5;
            if(!escaped)
                tutor();
        }
        if (clear)
        {
            InputLock();
            Gostart.SetActive(true);
        }
        stackcount = cl.Count;
        if (reward)
        {
            InputLock();
            choice();
        }
        if (isdone && success)
        {
            if (scenename == "first_stage") clearpos = new Vector3(105.0f, 3.0f, 0.0f);
            if (scenename == "second_stage") clearpos = new Vector3(135.0f, 5.0f, 0.0f);
            gostart();
            InputFree();
            reward = false;
            isdone = false;
            success = false;
        }
        if (allow && scenename == "first_stage")
            GameObject.Find("Trap3").GetComponent<Rigidbody2D>().velocity = Vector3.up * 1.0f;
        if (weapon.sprite == newweapon)
            isenhanced = true;
        //if (IsSwordManDead) return;
        if (objSwordMan.transform.position.y <= -5.0f)
        {
            nowHp *= 0.8f;
            gostart();
            mm.RespawnAllMonsters();
            if(scenename == "first_stage")
            {
                GameObject.Find("Trap2").GetComponent<Transform>().position = new Vector3(0, 0, 0);
                GameObject.Find("Trap3").GetComponent<Transform>().position = new Vector3(8, 0, 0);
                GameObject.Find("Trap4").GetComponent<Transform>().position = new Vector3(11, 0, 0);
                GameObject.Find("Trap2").GetComponent<Rigidbody2D>().velocity = new Vector3(0, 0, 0);
                GameObject.Find("Trap3").GetComponent<Rigidbody2D>().velocity = new Vector3(0, 0, 0);
                GameObject.Find("Trap4").GetComponent<Rigidbody2D>().velocity = new Vector3(0, 0, 0);
            }
            
        }
        lowDmg = atkDmg / 2;
        RaycastHit2D raycastHit = Physics2D.CapsuleCast(cap2D.bounds.center, cap2D.bounds.size,cap2D.direction, 0f, Vector2.down, 0.02f, LayerMask.GetMask("Ground"));
        if (raycastHit.collider != null)
            animator.SetBool("jumping", false);
        else animator.SetBool("jumping", true);
        maxHpbar.GetComponent<Transform>().localScale = new Vector3(maxHp / st_maxhp, 1, 0);
        maxStbar.GetComponent<Transform>().localScale = new Vector3(maxSt / st_maxst, 1, 0);
        maxMpbar.GetComponent<Transform>().localScale = new Vector3(maxMp / st_maxmp, 1, 0);
        nowHpbar.fillAmount = (float)nowHp / (float)maxHp;
        nowMpbar.fillAmount = (float)nowMp / (float)maxMp;
        nowStbar.fillAmount = (float)nowSt / (float)maxSt;
        heal.fillAmount = (float)hrt / (float)maxsk;
        skill.fillAmount = (float)rt / (float)maxsk;
        
        if (!isinputlocked)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            {
                MoveRight();
                MoveLeftf();
                SFXManager.Instance.PlaySound(SFXManager.Instance.playerWalk);
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow) && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            {
                MoveLeft();
                MoveRightf();
                SFXManager.Instance.PlaySound(SFXManager.Instance.playerWalk);
            }
            else if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                MoveLeftf();
            }
            else if (Input.GetKeyUp(KeyCode.RightArrow))
                MoveRightf();
            else animator.SetBool("moving", false);
            if (Input.GetKeyDown(KeyCode.Space) && !animator.GetBool("jumping") && nowSt >= 3)
            {
                jump();
            }
            if (Input.GetKeyUp(KeyCode.Space)) inputUp = false;
            if (Input.GetKeyDown(KeyCode.A) && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
            {
                attack();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(scenename);
            }

            if (Input.GetKeyDown(KeyCode.D) && nowMp >= 10)
            {
                power();
            }
            if (Input.GetKeyDown(KeyCode.H) && nowMp >= 10)
            {
                healing();
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                throwfb();
            }
            if (Input.GetKeyDown(KeyCode.F) && nowSt >= 10 && nowMp >= 3)
            {
                teleport();
            }
            if (scenename == "tutorial_stage" && stone_touch && Input.GetKeyDown(KeyCode.G))
            {
                tutostone = true;
                Light1.SetActive(!Light1.activeSelf);
            }
            if (Input.GetKeyDown(KeyCode.F2))
            {
                nowHp = maxHp;
            }
            if (Input.GetKeyDown(KeyCode.F3))
            {
                nowSt = maxSt;
            }
            if (Input.GetKeyDown(KeyCode.F4))
            {
                atkDmg = 1000;
            }
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SceneManager.LoadScene("second_stage");
            }
            if (Input.GetKeyDown(KeyCode.F6))
            {
                SceneManager.LoadScene("third_stage");
            }
        }
        
        
        if (inputRight) { animator.SetBool("moving", true); rigid2D.AddForce(Vector2.right * moveSpeed * Time.deltaTime); }
        if (inputLeft) { animator.SetBool("moving", true); rigid2D.AddForce(Vector2.left * moveSpeed * Time.deltaTime); }
        if (inputUp) { inputUp = false; rigid2D.velocity = new Vector2(rigid2D.velocity.x, jumpPower); }
        if (rigid2D.velocity.x >= 2.5f) rigid2D.velocity = new Vector2(2.5f, rigid2D.velocity.y);
        else if (rigid2D.velocity.x <= -2.5f) rigid2D.velocity = new Vector2(-2.5f, rigid2D.velocity.y);
        if (nowSt <= 0) nowSt = 0;
        if (rigid2D.velocity.y >= 7.5f) rigid2D.velocity = new Vector2(rigid2D.velocity.x, 7.5f);
        if (nowHp >= maxHp) nowHp = maxHp;
        
        
        if (hs && hrt > 0)
        {
            hrt -= Hpheal * Time.deltaTime;
            nowHp += Hpheal * Time.deltaTime;
        }
        else if (hs && hrt <= 0)
        {
            hs = false;
            ps.Stop();
        }

        if (ss && rt > 0)
        {
            atkDmg = 2 * tmpdmg;
            rt -= 5f * Time.deltaTime;
            SetAttackSpeed(2.0f);
        }

        else if (ss && rt <= 0)
        {
            SetAttackSpeed(1.5f);
            atkDmg = tmpdmg;
            ss = false;
            ps.Stop();
        }
        if (nowSt < maxSt && !animator.GetBool("jumping") && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            resttime += 10.0f * Time.deltaTime;
            if (resttime > 300.0f && resttime < 700.0f)
            {
                nowSt += Stheal * Time.deltaTime;
            }
            else if (resttime >= 7.0f)
            {
                nowSt += 2 * Stheal * Time.deltaTime;
            }
            
        }
        if (nowMp < maxMp)
        {
            nowMp += 0.5f * Stheal * Time.deltaTime;
        }
        if (Input.GetKeyDown(KeyCode.F1))
        {
            nowMp += 20;
        }
        if (istouched && Input.GetKeyDown(KeyCode.G) && cango)
        {
            if (scenename == "first_stage")
                SceneManager.LoadScene("second_stage");
            else if (scenename == "second_stage")
                SceneManager.LoadScene("third_stage");
        }
        if (c_touch && Input.GetKeyDown(KeyCode.G))
        {
            switch (colorname)
            {
                case "fire_red":
                    cl.Add(1);
                    Setelement(Color.red);
                    break;
                case "fire_blue":
                    Setelement(Color.blue);
                    cl.Add(2);
                    break;
                case "fire_green":
                    cl.Add(3);
                    Setelement(new Color(0, 255, 0));
                    break;
            }
        }
        if (scenename == "second_stage" && stone_touch && Input.GetKeyDown(KeyCode.G) && colorflag)
        {
            Debug.Log(stonename + color_num);
            switch (stonename)
            {
                case "Stone1":
                    if (color_num == 3)
                    {
                        Light1.SetActive(true);
                    }
                    break;
                case "Stone2":
                    if (color_num == 1)
                    {
                        Light2.SetActive(true);
                    }
                    break;
                case "Stone3":
                    if (color_num == 2)
                    {
                        Light3.SetActive(true);
                    }
                    break;
            }
            colorflag = false;
        }
        if (cl.Count == 1)
        {
            if (!element.isActiveAndEnabled)
            {
                Debug.Log("element on");
                element.gameObject.SetActive(true);
            }
        }
        if (cl.Count == 2)
        {
            SetColor();
            cl.Clear();
            if(colorflag) ps.Play();
        }
        if (rt <= 0 && !colorflag) ps.Stop();
        
        if (scenename == "second_stage" && Light1.activeSelf && Light2.activeSelf && Light3.activeSelf &&!isdoorup)
        {
            Glow1.SetActive(true);
            Glow2.SetActive(true);
            Glow3.SetActive(true);
            nowMp = maxMp;
            nowSt = maxSt;
            nowHp = maxHp;
            GameObject.Find("Door2").GetComponent<Rigidbody2D>().velocity = new Vector2(0, 1.0f);
            isdoorup = true;
            element.gameObject.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.Escape) && !escaped)
        {
            InputLock();
            if (scenename != "tutorial_stage")
            {
                Info.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                Info.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                Info.GetComponent<RectTransform>().anchoredPosition = new Vector3(270, 60, 0);
                Info.GetComponent<TextMeshProUGUI>().text = "Paused";
                Info.SetActive(true);
                Panel.SetActive(true);
            }
            else
            {
                inform.text = "Paused";
            }
            
            escaped = true;
            Gostart.SetActive(true);
            Retry.SetActive(true);
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && escaped)
        {
            InputFree();
            escaped = false;
            if(scenename != "tutorial_stage"){
                Panel.SetActive(false);
                Info.SetActive(false);
            }
            Gostart.SetActive(false);
            Retry.SetActive(false);
        }
        if (scenename=="third_stage"&&sptouched && Input.GetKeyDown(KeyCode.G) && !spawned)
        { 
            S1.SetActive(true);
            S2.SetActive(true);
            G1.SetActive(true);
            G2.SetActive(true);
            spawned = true;
        }
        if (scenename == "third_stage" && sptouched && Input.GetKeyDown(KeyCode.G)&&
            !S1.activeSelf&&!S2.activeSelf&&!G1.activeSelf&&!G2.activeSelf && spawned)
        {
            SpawnBoss();
        }
        if(scenename == "third_stage" && ltouched && !final && Input.GetKeyDown(KeyCode.G))
        {
            InputLock();
            cam.TriggerFocus();
            InputFree();
        }
        if (scenename == "third_stage" && ltouched && final &&Input.GetKeyDown(KeyCode.G))
        {
            Lever.SetActive(false);
            GameObject.Find("Door").GetComponent<Rigidbody2D>().velocity = Vector3.up;
        }
    }
    void SpawnBoss()
    {
        InputLock();
        BGMManager_Hades.Instance.PlaySound(BGMManager_Hades.Instance.boss);
        cam.TriggerFocus();
        GameObject.Find("Spawner").SetActive(false);
        InputFree();
    }
    void fbdes()
    {
        Destroy(fb);
    }
    public void throwfb()
    {
        if (fb==null&&nowMp >= 20 && !animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            nowMp -= 20;
            animator.SetTrigger("attack");
            fb = Instantiate(fireball);
            if (objSwordMan.transform.localScale == new Vector3(1, 1, 1))
            {
                fb.transform.localScale = new Vector3(-10, 10, 1);
                fb.transform.position = objSwordMan.transform.position + new Vector3(-1.5f, 0.5f, 0);
                fb.GetComponent<Rigidbody2D>().velocity = Vector3.left * 4.0f;
            }
            else if (objSwordMan.transform.localScale == new Vector3(-1, 1, 1))
            {
                fb.transform.position = objSwordMan.transform.position + new Vector3(1.5f, 0.5f, 0);
                fb.GetComponent<Rigidbody2D>().velocity = Vector3.right * 4.0f;
            }
            SFXManager.Instance.PlaySound(SFXManager.Instance.Fireball);
            Invoke("fbdes", 1.2f);
        }
    }
    public void MoveLeft()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            inputLeft = true;
            objSwordMan.transform.localScale = new Vector3(1, 1, 1);
            animator.SetBool("moving", true);
        }

    }
    public void MoveLeftf()
    {
        inputLeft = false;
    }
    public void MoveRight()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            inputRight = true;
            objSwordMan.transform.localScale = new Vector3(-1, 1, 1);
            ps.transform.position = objSwordMan.transform.position;
            animator.SetBool("moving", true);
        }
    }
    public void MoveRightf()
    {
        inputRight = false;
    }

    public void jump()
    {
        if (!animator.GetBool("jumping") && nowSt >= 3)
        {
            jc += 1;
            inputUp = true;
            resttime = 0;
            SFXManager.Instance.PlaySound(SFXManager.Instance.playerJump);
        }
    }
    public void attack()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            animator.SetTrigger("attack");
            nowSt -= 6;
            resttime = 0;
            SFXManager.Instance.PlaySound(SFXManager.Instance.playerAttack);
        }
    }
    public void power()
    {
        if (nowMp >= 10)
        {
            ps.startColor = Color.yellow;
            ps.Play();
            rt = 20.0f;
            nowMp -= 10;
            tmpdmg = atkDmg;
            ss = true;
            SFXManager.Instance.PlaySound(SFXManager.Instance.Power);
        }
    }
    public void healing()
    {
        if (nowMp >= 10)
        {
            ps.startColor = Color.cyan;
            ps.Play();
            hrt = 20.0f;
            nowMp -= 10;
            hs = true;
            SFXManager.Instance.PlaySound(SFXManager.Instance.Heal);
        }
    }
    public Tilemap groundTilemap;
    LayerMask obstacleLayer;
    public void InputLock()
    {
        SFXManager.Instance.audioSource.Stop();
        inputLeft = false;
        inputRight = false;
        isinputlocked = true;
    }
    public void InputFree()
    {
        isinputlocked = false;
    }
    void teleport()
    {
        Vector3 teleportOffset;
        Vector3 boxSize = new Vector3(2, 1, 0);
        if (nowSt >= 10 && nowMp >= 3)
        {
            if(transform.localScale == new Vector3(-1, 1, 1))
        {
                teleportOffset = Vector3.right * 4f;
            }
        else
            {
                teleportOffset = Vector3.left * 4f;
            }
            Vector3 targetPosition = transform.position + teleportOffset; 
            // 텔레포트 하게 될 장소에서의 오브젝트 충돌 체크를 위한 벡터 변수
            Vector3Int cellPosition = groundTilemap.WorldToCell(targetPosition);
            if (targetPosition.x > allowarea || targetPosition.x < minarea) return;
            if (!groundTilemap.HasTile(cellPosition) && 
                !Physics.CheckBox(targetPosition, boxSize, Quaternion.identity, obstacleLayer))
            {
                transform.position = targetPosition;
            }
            else
            {
                Vector3 nearestPosition = FindNearestValidPosition(cellPosition);
                transform.position = nearestPosition;
            }
            god = true; // 무적 상태를 조절하는 boolean 변수
            nowSt -= 10; // 현재 스테미너
            nowMp -= 3; // 현재 마나
            resttime = 0; // 휴식 시간 조절
            SFXManager.Instance.PlaySound(SFXManager.Instance.Teleport);
            Invoke("ungod", 1f);
        }
    }

    Vector3 FindNearestValidPosition(Vector3Int startCellPosition)
    {
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        Vector3 boxSize = transform.localScale * 2f;
        foreach (var direction in directions)
        {
            Vector3Int checkPosition = startCellPosition + direction;
            if (!groundTilemap.HasTile(checkPosition) && !Physics.CheckBox(checkPosition, boxSize, Quaternion.identity, obstacleLayer))
            {
                return groundTilemap.CellToWorld(checkPosition) + new Vector3(0.5f, 0.5f);
            }
        }

        return transform.position;
    }
    void ungod()
    {
        god = false;
    }
    void AttackTrue() 
    {
        attacked = true;
    }
    void AttackFalse()
    {
        attacked = false;
    }
    void SetAttackSpeed(float speed)
    {
        animator.SetFloat("attackSpeed", speed);
        atkSpeed = speed;
    }
    void SetColor()
    {
        if (cl[0] == 1 && cl[1] == 2)
        {
            colorflag = true;
            ps.startColor = Color.magenta;
            Setelement(Color.magenta);
            color_num = 1;
        }
        else if (cl[0] == 2 && cl[1] == 3)
        {
            colorflag = true;
            ps.startColor = new Color(255, 163, 0);
            Setelement(new Color(255, 163, 0));
            color_num = 2;
        }
        else if (cl[0] == 3 && cl[1] == 1)
        {
            colorflag = true;
            ps.startColor = Color.white;
            Setelement(Color.white);
            color_num = 3;
        }
        else
        {
            Setelement(Color.black);
            ps.Stop();
            colorflag = false;
        }
    }
    void Setelement(Color c)
    {
        element.GetComponent<Image>().color = c;
    }
    IEnumerator CheckSwordManDeath()
    {
        while (true)
        {

            // 체력이 0이하일 때
            if (nowHp <= 0)
            {
                InputLock();
                Debug.Log("주금");
                IsSwordManDead = true;
                animator.SetTrigger("die");
                SFXManager.Instance.PlaySound(SFXManager.Instance.playerDie);
                yield return new WaitForSeconds(2);
                Panel.SetActive(true);
                Retry.SetActive(true);
                Gostart.SetActive(true);
                Info.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                Info.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                Info.GetComponent<RectTransform>().anchoredPosition = new Vector3(200, 60, 0); 
                Info.GetComponent<TextMeshProUGUI>().text = "YOU DIED.";
                Info.SetActive(true);
                break;
            }
            yield return new WaitForEndOfFrame(); // 매 프레임의 마지막 마다 실행
        }
    }
    public void choice()
    {
        levelup.SetActive(true);
        if (!isenhanced) enhance.SetActive(true);
    }
    public void Die()
    {
        SceneManager.LoadScene(scenename);
    }
    public void removeAll()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene("start");
    }
    public void ChangeWeapon()
    {
        if (weapon != null)
        {
            weapon.sprite = newweapon;
            atkDmg += 8;
        }
    }
    
    public void gostart()
    {
        animator.SetBool("moving", false);
        objSwordMan.transform.position = clearpos;
    }
    void tutor()
    {
        
        switch (level)
        {
            case 0:
                inform.text="\n\t키보드를 통해 움직여 보세요";
                break;
            case 1:
                inform.text = "\n\t공격과 회피기를 사용해보세요" +
                    "\n\t공격과 회피기는 스테미너를 소모합니다.";
                break;
            case 2:
                inform.text="스킬을 사용해보세요\nD : 일정 시간동안 공격력과 공격속도가 2배가 됩니다." +
                    "\nH : 일정 시간동안 체력을 회복합니다\n" +
                    "B : 전방으로 화염구를 발사해 적에게 피해를 입힙니다.";
                break;
            case 3:
                inform.text = "\nG 키를 통해 물체와 상호작용 할 수 있습니다\n" +
                    "마법의 돌과 상호작용해 불을 켜보세요";
                break;
            case 4:
                inform.text = "\n공격과 스킬을 사용해서 적을 무찔러보세요\n" +
                    "tip! 점프중에 공격을 하면 치명타가 발동됩니다";
                break;
            case 5:
                inform.text = "\n\t축하드립니다!\n\t튜토리얼을 완료했습니다";
                break;
        }
    }
    private void OnTriggerEnter2D(Collider2D col)
    {

        if (col.CompareTag("reward"))
        {
            reward = true;
            GameObject.Find("box").SetActive(false);
        }
        if (col.CompareTag("key"))
        {
            newweapon = GameObject.Find("key").GetComponent<SpriteRenderer>().sprite;
            reward = true;
            allow = true;
            allowarea = 138.0f;
            success = true;
        }
        if (col.CompareTag("portal"))
        {
            GM.Save();
            istouched = true;
        }
        if (col.CompareTag("RockHead"))
        {
            nowHp -= 20.0f;
            gostart();
        }
        if (col.CompareTag("cup"))
        {
            c_touch = true;
            colorname = col.gameObject.name;
        }
        if (col.CompareTag("stone"))
        {
            stone_touch = true;
            stonename = col.gameObject.name;
        }
        if (col.CompareTag("ending"))
        {
            SceneManager.LoadScene("Ending");
        }
        if (col.CompareTag("hidden"))
        {
            if(scenename == "second_stage")
            {
                InputLock();
                cam.TriggerFocus();
                GameObject.Find("key2").SetActive(false);
                InputFree();
            }
            else if (scenename == "third_stage")
            {
                sptouched = true;
            }
        }
        if (col.CompareTag("lever"))
        {
            ltouched = true;
        }
        if (col.CompareTag("CS"))
        {
            inputLeft = false;
            inputRight = false;
            SFXManager.Instance.StopSound();
            InputLock();
            cam.End();
        }
    }
    public void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("portal"))
        {
            istouched = false;
        }
        if (col.CompareTag("cup"))
        {
            c_touch = false;
            colorname = "";
        }
        if (col.CompareTag("stone"))
        {
            stone_touch = false;
            stonename = "";
        }
        if (col.CompareTag("hidden"))
        {
            sptouched = false;
        }
        if (col.CompareTag("lever"))
        {
            ltouched = false;
        }
        //if (col.CompareTag("reward")) reward = false;
    }
}

