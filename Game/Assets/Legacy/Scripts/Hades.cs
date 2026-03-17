using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Hades : MonoBehaviour
{
    public GameObject prfHpBar;
    public GameObject canvas;
    RectTransform hpBar;
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
    public GameObject FB;
    public bool revived = false;
    GameObject Lever;
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
        Lever = sword_man.Lever;
        rigid2D = GetComponent<Rigidbody2D>();
        Application.targetFrameRate = 60;
        hpBar = Instantiate(prfHpBar, canvas.transform).GetComponent<RectTransform>();
        hpBar.localScale = new Vector3(2.0f, 1.5f, 0);

        if (enemyName == "Hades_alive")
        {
            SetEnemyStatus(enemyName, 1000, 30, 1.0f, 2.0f, 6f, 20f);
        }
        else if (enemyName == "Hades_revive")
        {
            SetEnemyStatus("Hades_revive", 1200, 40, 1.0f, 2.0f, 6f, 20f);
        }
        nowHpbar = hpBar.transform.GetChild(0).GetComponent<Image>();

        SetAttackSpeed(atkSpeed);

    }

    // Update is called once per frame
    void Update()
    {
        _hpBarPos =
                Camera.main.WorldToScreenPoint(new Vector3(transform.position.x - 1.0f, transform.position.y + height, 0));
        hpBar.position = _hpBarPos;
        nowHpbar.fillAmount = (float)nowHp / (float)maxHp;
        if (nowHp < maxHp && nowHp > 0)
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


                else
                {
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
    void SetAttackSpeed(float speed)
    {
        enemyAnimator.SetFloat("attackSpeed", speed);
    }
    public void Die()
    {
        if (!revived)
        {
            enemyAnimator.SetTrigger("die");
            isDead = true;
            sword_man.nowMp = sword_man.maxMp;
            Lever.SetActive(true);
        }
        // die 애니메이션 실행
        else
        {
            isDead = true;
            sword_man.god = true;
            enemyAnimator.SetTrigger("rdie");
            sword_man.final = true;
        }
        GetComponent<Rigidbody2D>().gravityScale = 0;
        GetComponent<Collider2D>().enabled = false;
        GetComponent<HadesAI>().enabled = false;
        hpBar.gameObject.SetActive(false);
        StartCoroutine(DisableAfterDelay());
    }
    public void Respawn()
    {
        isDead= false;
        revived = true;
        enemyName = "Hades_revive";
        GetComponent<Transform>().position = new Vector3(GetComponent<Transform>().position.x, 3.0f, 0);
        SetEnemyStatus("Hades_revive", 1500, 40, 1.0f, 2.0f, 6f, 10f);
        GetComponent<Transform>().localScale = new Vector3(8.5f, 8.5f, 0);
        hpBar = Instantiate(prfHpBar, canvas.transform).GetComponent<RectTransform>();
        nowHp = maxHp;
        gameObject.SetActive(true);

        hpBar.localScale = new Vector3(hpBar.localScale.x / (sword_man.maxHp / 60.0f), 1, 0);
        hpBar.gameObject.SetActive(true);
        nowHpbar = hpBar.transform.GetChild(0).GetComponent<Image>();

        GetComponent<Rigidbody2D>().gravityScale = 1;
        GetComponent<Collider2D>().enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
        revived = true;
        BGMManager_Hades.Instance.PlaySound(BGMManager_Hades.Instance.final);
        GetComponent<Animator>().SetTrigger("spawn");
        //GetComponent<HadesAI>().enabled = true;
        //StartCoroutine(RespawnAfterDelay());
        
    }
    
    IEnumerator DisableAfterDelay()
    {
        yield return new WaitForSeconds(3f); // 3초 대기
        GetComponent<HadesAI>().enabled = false; // 추적 비활성화
        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false; // 스프라이트 숨기기
    }
    IEnumerator RespawnAfterDelay()
    {

        
        yield return new WaitForSeconds(3f); // 3초 대기=
        enemyName = "Hades_revive";
        GetComponent<Transform>().position = new Vector3(GetComponent<Transform>().position.x, 3.0f, 0);
        SetEnemyStatus("Hades_revive", 1500, 40, 1.0f, 2.0f, 6f, 10f);
        GetComponent<Transform>().localScale = new Vector3(8.5f, 8.5f, 0);
        hpBar = Instantiate(prfHpBar, canvas.transform).GetComponent<RectTransform>();
        isDead = false;
        nowHp = maxHp;
        gameObject.SetActive(true);
        hpBar.gameObject.SetActive(true);
        GetComponent<Rigidbody2D>().gravityScale = 1;
        GetComponent<Collider2D>().enabled = true;
        GetComponent<SpriteRenderer>().enabled = true;
        revived = true;
        BGMManager_Hades.Instance.PlaySound(BGMManager_Hades.Instance.final);
        GetComponent<Animator>().SetTrigger("spawn");
        GetComponent<HadesAI>().enabled = true;
    }
}
