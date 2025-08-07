using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class camera : MonoBehaviour
{
    public Transform target;

    public float smoothSpeed = 3;
    public Vector2 offset;
    public float limitMinX, limitMaxX, limitMinY, limitMaxY;
    public Transform focusPoint;
    Transform endingPoint;
    GameObject Dragon;
    GameObject Hades;
    GameObject Target;
    public float focusDuration = 5f;
    float cameraHalfWidth, cameraHalfHeight;
    public bool isFocusing = false;

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == "second_stage")
        {
            Dragon = GameObject.Find("Dragon");
            focusPoint = Dragon.transform;
            Dragon.SetActive(false);
        }
        if (SceneManager.GetActiveScene().name == "third_stage")
        {
            Hades = GameObject.Find("Hades");
            Target = GameObject.Find("Target");
            focusPoint = Hades.transform;
            endingPoint=Target.transform;
            Hades.SetActive(false);
        }
        Application.targetFrameRate = 60;
        cameraHalfWidth = Camera.main.aspect * Camera.main.orthographicSize;
        cameraHalfHeight = Camera.main.orthographicSize;
    }

    private void LateUpdate()
    {
        if (!isFocusing)
        {
            FollowTarget();
        }
    }
    void FollowTarget()
    {
        Vector3 desiredPosition = new Vector3(
            Mathf.Clamp(target.position.x + offset.x, limitMinX + cameraHalfWidth, limitMaxX - cameraHalfWidth),   // X
            Mathf.Clamp(target.position.y + offset.y, limitMinY + cameraHalfHeight, limitMaxY - cameraHalfHeight), // Y
            -10);                                                                                                  // Z
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
    }
    public void TriggerFocus()
    {
        if (!isFocusing)
        {
            StartCoroutine(FocusOnPoint());
        }
    }
    public void End()
    {
        if (!isFocusing)
        {
            StartCoroutine(EndingScene());
        }
    }

    private IEnumerator FocusOnPoint()
    {
        isFocusing = true;
        // focusPoint 위치로 이동
        Vector3 focusPosition = new Vector3(
            Mathf.Clamp(focusPoint.position.x, limitMinX + cameraHalfWidth, limitMaxX - cameraHalfWidth),   // X
            Mathf.Clamp(focusPoint.position.y, limitMinY + cameraHalfHeight, limitMaxY - cameraHalfHeight), // Y
            -10);                                                                                           // Z

        float elapsedTime = 0f;
        Vector3 initialPosition = transform.position;

        // 지정된 시간 동안 focusPoint를 향해 카메라 이동
        while (elapsedTime < focusDuration)
        {
            transform.position = Vector3.Lerp(initialPosition, focusPosition, elapsedTime / focusDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if (SceneManager.GetActiveScene().name == "second_stage") Dragon.SetActive(true);
        else if (SceneManager.GetActiveScene().name == "third_stage" && !Hades.GetComponent<Hades>().isDead)
        {
            Hades.GetComponent<HadesAI>().enabled = false;
            Hades.SetActive(true);
        }
        else
        {
            Hades.GetComponent<Hades>().Respawn();
        }
        if(SceneManager.GetActiveScene().name == "second_stage" || SceneManager.GetActiveScene().name == "third_stage" && !Hades.GetComponent<Hades>().revived)
            SFXManager.Instance.PlaySound(SFXManager.Instance.spawn);
        yield return new WaitForSeconds(2.0f);
        // focusDuration이 지난 후 캐릭터를 다시 따라감
        isFocusing = false;
        if(SceneManager.GetActiveScene().name == "third_stage")
            Hades.GetComponent<HadesAI>().enabled = true;
        
    }
    private IEnumerator EndingScene()
    {
        SFXManager.Instance.StopAllCoroutines();
        GameObject.Find("trigger").SetActive(false);

        // 계단 오브젝트들을 각각 이동시키고, y 좌표를 체크하는 코루틴을 시작합니다.
        StartCoroutine(MoveAndStopAtTarget(GameObject.Find("Stair1")));
        StartCoroutine(MoveAndStopAtTarget(GameObject.Find("Stair2")));
        StartCoroutine(MoveAndStopAtTarget(GameObject.Find("Stair3")));
        BGMManager_Hades.Instance.PlaySound(BGMManager_Hades.Instance.rs);
        isFocusing = true;

        // focusPoint 위치로 이동
        Vector3 focusPosition = new Vector3(
            Mathf.Clamp(endingPoint.position.x, limitMinX + cameraHalfWidth, limitMaxX - cameraHalfWidth),   // X
            Mathf.Clamp(endingPoint.position.y, limitMinY + cameraHalfHeight, limitMaxY - cameraHalfHeight), // Y
            -10);                                                                                           // Z

        float elapsedTime = 0f;
        Vector3 initialPosition = transform.position;

        // 지정된 시간 동안 focusPoint를 향해 카메라 이동
        while (elapsedTime < focusDuration)
        {
            transform.position = Vector3.Lerp(initialPosition, focusPosition, elapsedTime / focusDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(7.0f);

        // focusDuration이 지난 후 캐릭터를 다시 따라감
        isFocusing = false;
        GameObject.Find("sword_man").GetComponent<Sword_Man>().InputLock();
        GameObject.Find("sword_man").GetComponent<Sword_Man>().MoveRight();
        GameObject.Find("sword_man").GetComponent<Rigidbody2D>().velocity = Vector3.right * 0.2f;
        yield return new WaitForSeconds(2.5f);
        SceneManager.LoadScene("Ending");
    }

    // 계단 오브젝트를 이동시키고, y 좌표가 0 이상이 되면 속도를 0으로 설정하는 코루틴
    private IEnumerator MoveAndStopAtTarget(GameObject stair)
    {
        Rigidbody2D rb = stair.GetComponent<Rigidbody2D>();
        rb.velocity = Vector3.up * 0.8f;

        // y 좌표가 0 이상이 될 때까지 반복 확인
        while (stair.transform.position.y < 0.0f)
        {
            yield return null;
        }

        // 목표에 도달하면 속도를 0으로 설정
        rb.velocity = Vector3.zero;
        Debug.Log(stair.name + " 도착");
    }
}
