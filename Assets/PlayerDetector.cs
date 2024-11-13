using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDetector : MonoBehaviour
{
    private Enemy enemy;
    void Start()
    {
        enemy = GetComponentInParent<Enemy>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Debug.Log("Enter player collision with" + collision.gameObject.name);
        if (collision.CompareTag("Player"))
        {
            enemy.playerDetected = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Debug.Log("Exit player collision with" + collision.gameObject.name);
        if (collision.CompareTag("Player"))
        {
            enemy.playerDetected = false;
        }
    }
}
