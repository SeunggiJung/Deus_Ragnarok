using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FireBomb : MonoBehaviour
{
    public Transform target;
    float exprange;
    float distance;
    // Start is called before the first frame update
    void Start()
    {
        target = GameObject.Find("sword_man").GetComponent<Transform>();
        exprange = 2.0f;
    }

    // Update is called once per frame
    void Update()
    {
       distance =  Vector3.Distance(transform.position, target.position);
    }
    void FBdes()
    {
        Destroy(this.gameObject);
    }
    void Explosion()
    {
        if (distance <= exprange && !target.GetComponent<Sword_Man>().god)
            target.GetComponent<Sword_Man>().nowHp -= 15.0f;
    }
}
