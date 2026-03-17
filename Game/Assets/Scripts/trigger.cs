using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class trigger : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position.y<0.5 && GameObject.Find("Trap3").GetComponent<Transform>().position.y<4) GameObject.Find("Trap3").GetComponent<Rigidbody2D>().AddForce(Vector2.up * 10);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("trigger")) {
            /*
           GameObject.Find("Trap3").GetComponent<Rigidbody2D>().AddForce(Vector2.up * 10);
            */
            SceneManager.LoadScene("Ending");
        }
    }
}
