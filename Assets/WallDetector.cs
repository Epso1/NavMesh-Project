using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallDetector : MonoBehaviour
{
    private Enemy enemy;
    void Start()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log("Enter wall collision with" + collision.gameObject.name);
        if (collision.CompareTag("Walls"))
        {
            enemy.wallDetected = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        //Debug.Log("Exit wall collision with" + collision.gameObject.name);
        if (collision.CompareTag("Walls"))
        {
            enemy.wallDetected = false;
        }
    }
}
