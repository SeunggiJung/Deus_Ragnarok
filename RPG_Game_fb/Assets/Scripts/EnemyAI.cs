using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyAI : MonoBehaviour
{
    public Transform target;
    float attackDelay;
    float distance;
    float attackTime;
    Enemy enemy;
    Animator enemyAnimator;
    public Sword_Man sword_man;
    private bool isAttacking = false; // 공격 중인지 확인하는 변수
    private bool isSecondAttack = false; // 첫 번째와 두 번째 공격 트리거 구분

    void Start()
    {
        attackTime = 8f;
        Application.targetFrameRate = 60;
        enemy = GetComponent<Enemy>();
        enemyAnimator = enemy.enemyAnimator;
    }

    void Update()
    {
        attackDelay -= Time.deltaTime;
        if (attackDelay < 0) attackDelay = 0;

        distance = Vector3.Distance(transform.position, target.position);

        if (attackDelay == 0 && distance <= enemy.fieldOfVision && !target.GetComponent<Sword_Man>().IsSwordManDead && !enemy.isDead && !sword_man.isinputlocked)
        {
            if (enemy.enemyName == "Boss")
            {
                if (SceneManager.GetActiveScene().name != "tutorial_stage")
                {
                    GameObject Trap = GameObject.Find("Trap");
                    Trap.GetComponent<Rigidbody2D>().gravityScale = 1.0f;
                }
                if (SceneManager.GetActiveScene().name == "first_stage")
                {
                    sword_man.minarea = 30.0f;
                    sword_man.allowarea = 61.5f;
                }
                if (SceneManager.GetActiveScene().name == "second_stage")
                {
                    sword_man.minarea = 53.0f;
                    sword_man.allowarea = 84.0f;
                }
            }
            if (enemy.enemyName == "Dragon")
            {
                sword_man.minarea = 178.0f;
                sword_man.allowarea = 218.0f;
            }
            FaceTarget();

            // Skeleton 적인 경우에만 연속 공격 수행
            if (distance <= enemy.atkRange && !isAttacking)
            {
                StartCoroutine(ContinuousAttack());
            }
            else
            {
                if (!enemyAnimator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
                {
                    MoveToTarget();
                }
            }
        }
        else
        {
            enemyAnimator.SetBool("moving", false);
        }
    }

    void MoveToTarget()
    {
        float dir = target.position.x - transform.position.x;
        dir = (dir < 0) ? -1 : 1;
        transform.Translate(new Vector2(dir, 0) * enemy.moveSpeed * Time.deltaTime);
        enemyAnimator.SetBool("moving", true);
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
            if (enemy.enemyName == "Skeleton" || enemy.enemyName == "Goblin")
            {
                if (isSecondAttack)
                {
                    enemyAnimator.SetTrigger("attack2"); // 두 번째 공격 모션 실행
                }
                else
                {
                    enemyAnimator.SetTrigger("attack"); // 첫 번째 공격 모션 실행
                }
            }
            else
            {
                enemyAnimator.SetTrigger("attack");
            }

            attackDelay = enemy.atkSpeed;

            // 다음 공격 트리거 변경
            if (enemy.enemyName == "Skeleton" || enemy.enemyName == "Goblin")
                isSecondAttack = !isSecondAttack;

            // 현재 애니메이션 상태의 길이만큼 대기 후 거리 업데이트
            yield return new WaitForSeconds(enemyAnimator.GetCurrentAnimatorStateInfo(0).length + attackDelay);
            distance = Vector3.Distance(transform.position, target.position);
        }

        isAttacking = false;
        isSecondAttack = false; // 연속 공격 종료 시 초기화
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