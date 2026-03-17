using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    public GameObject prfHpBar;
    public GameObject canvas;
    private MonsterManager mm;
    RectTransform hpBar;
    GameObject G1;
    GameObject G2;
    GameObject portal;
    GameObject me;
    public float height = 1.7f;
    public string enemyName;
    public int maxHp;
    public float nowHp;
    public int atkDmg;
    public float atkSpeed;
    public float moveSpeed;
    public float atkRange;
    public float fieldOfVision;
    public bool isDead = false;
    Vector3 clearPos;
    Rigidbody2D rigid2D;
    Vector3 _hpBarPos;
    void SetEnemyStatus(string _enemyName, int _maxHp, int _atkDmg, float _atkSpeed, float _moveSpeed, float _atkRange, float _fieldOfVision)
    {
        enemyName = _enemyName;
        maxHp = _maxHp;
        nowHp = _maxHp;
        atkDmg = _atkDmg;
        atkSpeed = _atkSpeed;
        moveSpeed = _moveSpeed;
        atkRange = _atkRange;
        fieldOfVision = _fieldOfVision;
    }
    // Start is called before the first frame update
    void Start()
    {
        me = gameObject;
        clearPos = GetComponent<Transform>().position;
        mm=FindObjectOfType<MonsterManager>();
        mm.RegisterEnemy(this);
        rigid2D = GetComponent<Rigidbody2D>();
        Application.targetFrameRate = 60;
        hpBar = Instantiate(prfHpBar, canvas.transform).GetComponent<RectTransform>();
        //hpBar.localScale = new Vector3(hpBar.localScale.x / (sword_man.maxHp / 60.0f), 1, 0);
        if (enemyName == "Boss")
        {
            SetEnemyStatus("Boss", 600, 10, 3.0f, 3, 5.0f, 9f);
            hpBar.localScale = new Vector3(2.0f, 1.5f, 0);
        }
        else if (enemyName == "Golem")
        {
            SetEnemyStatus("Golem", 150, 12, 1.5f, 1.5f, 5f, 6f);
        }
        else if (enemyName == "Golem2")
        {
            SetEnemyStatus("Golem2", 200, 15, 1.5f, 1.5f, 5f, 6f);
        }
        else if (enemyName == "Dragon")
        {
            SetEnemyStatus("Dragon", 800, 30, 1.5f, 3f, 4f, 9f);
            hpBar.localScale = new Vector3(2.0f, 1.5f, 0);
            G1 = GameObject.Find("Goal");
            G2 = GameObject.Find("Goal2");
            portal = GameObject.Find("Portal");
            G1.SetActive(false);
            G2.SetActive(false);
            portal.SetActive(false);
        }
        else if (enemyName == "tutorial")
        {
            SetEnemyStatus("tutorial", 1000, 0, 0, 0, 0, 0);
        }
        else if (enemyName == "Skeleton")
        {
            SetEnemyStatus("Skeleton", 300, 8, 1.0f, 1.5f, 4f, 7f);
        }
        else if (enemyName == "Goblin")
        {
            SetEnemyStatus("Goblin", 250, 10, 0.7f, 1.5f, 3.5f, 7f);
        }
        else if (enemyName == "Hades_alive")
        {
            SetEnemyStatus("Hades", 1000, 30, 1.0f, 2.0f, 6f, 10f);
        }
        else if (enemyName == "Hades_revive")
        {
            SetEnemyStatus("Hades", 1500, 40, 1.0f, 2.0f, 6f, 10f);
        }
        else
        {
            SetEnemyStatus("Enemy", 120, 10, 1.5f, 2, 1.5f, 7f);
        }
        nowHpbar = hpBar.transform.GetChild(0).GetComponent<Image>();

        SetAttackSpeed(atkSpeed);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (enemyName == "Boss")
        {
            atkSpeed = Random.Range(1.0f, 3.0f);
            _hpBarPos =
                Camera.main.WorldToScreenPoint(new Vector3(transform.position.x, transform.position.y + height + 1.0f, 0));
        }
        else if (enemyName == "Dragon")
        {
            atkSpeed = Random.Range(1.2f, 2.4f);
            _hpBarPos =
                Camera.main.WorldToScreenPoint(new Vector3(transform.position.x-2.0f, transform.position.y + 1.0f, 0));
        }
        else if (enemyName == "tutorial")
        {
            _hpBarPos =
                Camera.main.WorldToScreenPoint(new Vector3(transform.position.x-1.0f, transform.position.y +1.0f, 0));
        }
        else if (enemyName == "Skeleton")
        {
            _hpBarPos =
                Camera.main.WorldToScreenPoint(new Vector3(transform.position.x - 1.0f, transform.position.y +height, 0));
        }
        else if (enemyName == "Goblin")
        {
            _hpBarPos =
                Camera.main.WorldToScreenPoint(new Vector3(transform.position.x - 1.0f, transform.position.y + height, 0));
        }
        else if (enemyName == "Hades")
        {
            _hpBarPos =
                Camera.main.WorldToScreenPoint(new Vector3(transform.position.x - 1.0f, transform.position.y + height, 0));
        }
        else {
            _hpBarPos =
                Camera.main.WorldToScreenPoint(new Vector3(transform.position.x-0.5f, transform.position.y + height, 0));
        }
        hpBar.position = _hpBarPos;
        nowHpbar.fillAmount = (float)nowHp/(float)maxHp;
        if (nowHp < maxHp && nowHp>0)
        {
            nowHp = nowHp += 2f * Time.deltaTime;
        }
        
    }

    public Sword_Man sword_man;
    Image nowHpbar;
    public Animator enemyAnimator;
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Sword"))
        {
            if (sword_man.attacked)
            {
                if (sword_man.nowSt < 6)
                {                   
                    if (sword_man.GetComponent<Animator>().GetBool("jumping"))
                    {
                        nowHp -= sword_man.lowDmg * 2;
                    }
                    else nowHp -= sword_man.lowDmg;
                }


                else {                  
                    if (sword_man.GetComponent<Animator>().GetBool("jumping"))
                    {
                        nowHp -= sword_man.atkDmg * 2;
                    }
                    else nowHp -= sword_man.atkDmg;
                }

                Debug.Log(nowHp);
                sword_man.attacked = false;
                if (nowHp <= 0)
                {
                    Die();
                }
            }
        }
        if (col.CompareTag("fire"))
        {
            nowHp -= sword_man.atkDmg * 1.5f;
            if (nowHp <= 0)
            {
                Die();
            }
        }
    }
    public void Die()
    {
        isDead = true;
        enemyAnimator.SetTrigger("die");            // die 애니메이션 실행
        GetComponent<Rigidbody2D>().gravityScale = 0;
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(DisableAfterDelay());
        //Destroy(GetComponent<Rigidbody2D>());       // 중력 비활성화
        //Destroy(gameObject, 3);                     // 3초후 제거
        //Destroy(hpBar.gameObject, 3);               // 3초후 체력바 제거
        mm.UnregisterEnemy(this);
        if (enemyName == "Boss")
        {
            GameObject Door = GameObject.Find("Door");
            Door.GetComponent<Rigidbody2D>().AddForce(Vector2.up*25.0f);
            GameObject.Find("Trap").SetActive(false);
            enemyAnimator.SetTrigger("die2");
            if (SceneManager.GetActiveScene().name == "second_stage")
            {
                sword_man.allowarea = 107.0f;
            }
        }
        if (enemyName == "Dragon")
        {
            G1.SetActive(true);
            G2.SetActive(true);
            portal.SetActive(true);
        }
        sword_man.nowMp = sword_man.maxMp;
        if (enemyName == "Golem2" ||enemyName=="Dragon")
        {
            sword_man.cango = true;
        }
        if (enemyName == "tutorial")
        {
            sword_man.clear = true;
        }
    }
    public void Respawn()
    {
        isDead = false;
        nowHp = maxHp;
        
        gameObject.SetActive(true);
        hpBar.gameObject.SetActive(true);
        me.GetComponent<Transform>().position = clearPos;
        GetComponent<Rigidbody2D>().gravityScale = 1;
        GetComponent<EnemyAI>().enabled = true;    // 추적 비활성화
        GetComponent<Collider2D>().enabled = true; // 충돌체 비활성화
    }
    void SetAttackSpeed(float speed)
    {
        enemyAnimator.SetFloat("attackSpeed", speed);
    }
    IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(3f); // 3초 대기
        if(SceneManager.GetActiveScene().name!="tutorial_stage")
            GetComponent<EnemyAI>().enabled = false;    // 추적 비활성화
        //GetComponent<Collider2D>().enabled = false; // 충돌체 비활성화
        gameObject.SetActive(false);
        hpBar.gameObject.SetActive(false);
    }
}
