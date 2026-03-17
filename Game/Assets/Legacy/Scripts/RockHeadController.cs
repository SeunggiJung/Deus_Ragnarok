using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockHeadController : MonoBehaviour
{
    Rigidbody2D rigid2D;
    Animator animator;
    public float droptiming = 0.0f;
    public bool up;
    private System.Random random = new System.Random();
    Sword_Man swordman;
    Vector3 clearPos;
    public float rand;
    public bool fup;
    bool actionTrigger;
    public bool acting;
    // Start is called before the first frame update
    void Start()
    {
        rand = random.Next(0, 101);
        clearPos = transform.position;
        Application.targetFrameRate = 60;
        this.rigid2D = GetComponent<Rigidbody2D>();
        this.animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!up&&!fup)
        {
            droptiming += 0.05f;
        }
        if (droptiming > 5.0f &&!acting)
        {
            rigid2D.gravityScale = 1;
            Down();
        }
        if (up)
        {
            Up();
        }
        if (fup)
        {
            FakeUp();
        }
    }
    public void Up()
    {
        transform.position = new Vector3(clearPos.x, transform.position.y, transform.position.z);
        transform.rotation = Quaternion.identity;
        animator.SetTrigger("up");
        rigid2D.velocity = new Vector2(0, 2.0f);
        if (transform.position.y >= 5.0f)
        {
            acting = false;
            droptiming = 0.0f;
            rigid2D.gravityScale = 0;
            rigid2D.velocity = Vector2.zero;
            up = false;
            fup = false;
            rand = random.Next(0, 101);
        }
    }
    public void Down()
    {
        acting = true;
        if (rand < 80.0f)
            animator.SetTrigger("drop");
        else
            animator.SetTrigger("fake");
    }
    public void FakeDown()
    {
        animator.SetTrigger("fake");
    }
    public void FakeUp()
    {
        transform.position = new Vector3(clearPos.x, transform.position.y, transform.position.z);
        transform.rotation = Quaternion.identity;
        rigid2D.velocity = new Vector2(0, 2.0f);
        if (transform.position.y >= 2.5f)
        {
            rigid2D.velocity = Vector2.zero;
            rigid2D.gravityScale = 1;
            up = false;
            fup = false;
        }
    }
    public void fakeidle()
    {
        fup = true;
    }
    public void toidle()
    {
        up = true;
    }
    public void nonmoving()
    {
        acting=false;
    }
}
