﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform target;
    public LayerMask obstacleMask;

    public Vector2 gravity;
    public float gravityForce;
    public float movementSpeed;

    public Vector2 localVelocity;

    private ContactPoint2D[] contactPoints = new ContactPoint2D[16];
    private RaycastHit2D[] raycastHits = new RaycastHit2D[16];
    private GameObject platform;

    public Vector2 platformNormal;

    public bool grounded;
    private Rigidbody2D rigid;
    public bool canBeControlled = true;
    public bool lockedIntoMovement;

    private float height;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        gravity = Vector2.down * gravityForce;
        platformNormal = Vector2.up;

        height = GetComponent<Collider2D>().bounds.extents.y - 0.05f;
    }

    private void Update()
    {
        if (target == null)
        {
            //animator
            //Destroy(script)
        }

        Vector2 origin = (Vector2)transform.position - platformNormal * height;
        Vector2 direction = (Vector2)target.position - origin;

        Debug.DrawRay(origin, direction);

        var hit = Physics2D.Raycast(origin, direction.normalized, direction.magnitude, obstacleMask);
        if (hit && hit.rigidbody.gameObject == platform)
        {
            if (!lockedIntoMovement || localVelocity.x == 0)
            {
                lockedIntoMovement = true;
                if (Vector2.SignedAngle(-platformNormal, direction) > 0)
                {
                    localVelocity.x = movementSpeed;
                }
                else
                {
                    localVelocity.x = -movementSpeed;
                }
                StartCoroutine(UnlockMovement());
            }
        }
        else if (!hit && Mathf.Abs(Vector2.SignedAngle(platformNormal, direction)) > 60)
        {
            if (Vector2.SignedAngle(-platformNormal, direction) > 0)
            {
                localVelocity.x = movementSpeed;
            }
            else
            {
                localVelocity.x = -movementSpeed;
            }
        }
        else
        {
            if (canBeControlled && grounded)
                GetComponent<EnemyLeap>().Leap(target);

            localVelocity.x = 0;
        }
    }

    private void FixedUpdate()
    {
        localVelocity.y -= gravityForce * Time.deltaTime;

        if (canBeControlled)
        {
            if (grounded)
            {
                gravity = -platformNormal * gravityForce;

                var tangent = new Vector2(platformNormal.y, -platformNormal.x);
                MoveLocked(localVelocity.x * tangent * Time.deltaTime + localVelocity.y * platformNormal * Time.deltaTime);
            }
            else
            {
                var tangent = new Vector2(platformNormal.y, -platformNormal.x);
                Move(localVelocity.x * tangent * Time.deltaTime + localVelocity.y * platformNormal * Time.deltaTime);
            }
        }

        if (grounded)
        {
            localVelocity.y = 0;
        }
    }

    private void MoveLocked(Vector2 movement)
    {
        int count = rigid.Cast(movement, raycastHits, movement.magnitude);

        rigid.MovePosition(rigid.position + movement);
        //transform.Translate(movement);

        //Correct movement on the edges
        if (count == 0 && grounded)
        {
            count = rigid.Cast(Physics2D.gravity, raycastHits, Physics2D.gravity.magnitude * Time.deltaTime);
            if (count > 0)
            {
                if (raycastHits[0].distance < 0.01f)
                    return;

                var newMovement = raycastHits[0].distance * Physics2D.gravity.normalized;
                transform.Translate(newMovement);
            }
        }
    }

    private void Move(Vector2 movement)
    {
        float distance = movement.magnitude;

        int count = rigid.Cast(movement, raycastHits, movement.magnitude);
        if (count > 0)
        {
            distance = raycastHits[0].distance;
        }

        rigid.MovePosition(rigid.position + movement.normalized * distance);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            int count = collision.GetContacts(contactPoints);
            for (int i = 0; i < count; i++)
            {
                var newNormal = collision.GetContact(i).normal;

                //TODO check grounded and stuff
                if (!grounded || Vector2.Dot(platformNormal, newNormal) > 0.9f)  //FIXME how well normals align
                {
                    Debug.DrawRay(collision.GetContact(i).point, newNormal, Color.red);

                    platformNormal = newNormal;
                    platform = collision.gameObject;
                    break;
                }
            }
            grounded = true;
        }
    }

    private IEnumerator UnlockMovement()
    {
        yield return new WaitForSeconds(2);
        lockedIntoMovement = false;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            grounded = false;
            platform = null;
        }
    }
}