using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class trap : MonoBehaviour
{
    Rigidbody2D rb;
    float speed = 4.0f;
    bool movingUp = true;
    // Start is called before the first frame update
    void Start()
    {
        rb= GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (movingUp)
        {
            rb.velocity = new Vector2(0, speed);
            if (transform.position.y >= 8.0f)
            {
                movingUp = false;
            }
        }
        else
        {
            rb.velocity = new Vector2(0, -speed);
            if (transform.position.y <= -2.0f)
            {
                movingUp = true;
            }
        }
    }
}
