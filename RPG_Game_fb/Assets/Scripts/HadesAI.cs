using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HadesAI : MonoBehaviour
{
    public Transform target;
    float attackDelay;
    float distance;
    float attackTime;
    Hades enemy;
    Animator enemyAnimator;
    GameObject fb;
    public Sword_Man sword_man;
    private bool isAttacking = false;
    public bool isSecondAttack = false;
    void Start()
    {
        attackTime = 8f;
        Application.targetFrameRate = 60;
        enemy = GetComponent<Hades>();
        enemyAnimator = enemy.enemyAnimator;
    }

    void Update()
    {
        attackDelay -= Time.deltaTime;
        if (attackDelay < 0) attackDelay = 0;

        distance = Vector3.Distance(transform.position, target.position);

        if (attackDelay == 0 && distance <= enemy.fieldOfVision && !target.GetComponent<Sword_Man>().IsSwordManDead && !enemy.isDead && !sword_man.isinputlocked)
        {
            
            FaceTarget();

            // Skeleton 적인 경우에만 연속 공격 수행
            if (distance <= enemy.atkRange && !isAttacking && !enemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Rades_spawn"))
            {
                StartCoroutine(ContinuousAttack());
            }
            else
            {
                if (!enemy.revived)
                {
                    if (!enemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Wizard_attack"))
                    {
                        MoveToTarget();
                    }
                }
                else
                {
                    if(!enemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Rades_attack")|| !enemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Rades_attack2"))
                    {
                        MoveToTarget();
                    }
                }
            }
        }
        else
        {
            if (!enemy.revived) enemyAnimator.SetBool("moving", false);
            else enemyAnimator.SetBool("rmoving", false);
        }
    }

    void MoveToTarget()
    {
        float dir = target.position.x - transform.position.x;
        dir = (dir < 0) ? -1 : 1;
        transform.Translate(new Vector2(dir, 0) * enemy.moveSpeed * Time.deltaTime);
        if (!enemy.revived) enemyAnimator.SetBool("moving", true);
        else enemyAnimator.SetBool("rmoving", true);
    }

    void FaceTarget()
    {
        if (target.position.x - transform.position.x < 0) // 타겟이 왼쪽에 있을 때
        {
            transform.localScale = new Vector3(-1 * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        }
        else // 타겟이 오른쪽에 있을 때
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        }
    }

    // 연속 공격 처리 코루틴
    IEnumerator ContinuousAttack()
    {
        isAttacking = true;

        while (distance <= enemy.atkRange && !enemy.isDead)
        {
            if (enemy.revived)
            {
                if (isSecondAttack)
                {
                    enemyAnimator.SetTrigger("rattack2"); // 두 번째 공격 모션 실행
                }
                else
                {
                    enemyAnimator.SetTrigger("rattack"); // 첫 번째 공격 모션 실행
                }
            }
            else enemyAnimator.SetTrigger("attack");
            int A=3;
            if (enemy.revived) A = 5;
            else A = 3;
            for (int i = 0; i < A; i++)
            {
                fb = Instantiate(enemy.FB);
                fb.transform.position = new Vector3(Random.Range(-9.0f, 28.4f), 0.24f, 0.0f);
            }

            attackDelay = enemy.atkSpeed;
            if(enemy.revived) isSecondAttack=!isSecondAttack;
            
            yield return new WaitForSeconds(enemyAnimator.GetCurrentAnimatorStateInfo(0).length + attackDelay);
            distance = Vector3.Distance(transform.position, target.position);
        }

        isAttacking = false;
    }

    void Hit()
    {
        if (!target.GetComponent<Sword_Man>().god)
        {
            if (distance <= enemy.atkRange && !enemy.isDead)
                target.GetComponent<Sword_Man>().nowHp -= enemy.atkDmg;
        }
    }
}