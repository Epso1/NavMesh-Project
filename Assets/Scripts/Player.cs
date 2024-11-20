using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static event Action<Vector2> OnPlayerMoving;
    public float moveSpeed = 10f;
    private Rigidbody2D rb2D;
    private Vector3 movement;
    private Animator animator;
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        float xAxis = Input.GetAxis("Horizontal");
        float yAxis = Input.GetAxis("Vertical");

        movement = new Vector3 (xAxis, yAxis).normalized;

        if (movement.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (movement.x > 0) 
        { 
            transform.localScale = new Vector3(1, 1, 1);
        }

        if (movement != Vector3.zero)
        {
            OnPlayerMoving?.Invoke(transform.position);
            animator.Play("Player_Walk");
        }
        else
        {
            animator.Play("Player_Idle");
        }


    }

    private void FixedUpdate()
    {
        rb2D.MovePosition(rb2D.position + (Vector2)movement * moveSpeed * Time.deltaTime);
    }
}
