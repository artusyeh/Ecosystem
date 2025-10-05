using UnityEngine;
using System.Collections.Generic;

public class SpiderBehavior : MonoBehaviour
{
    [Header("Idle Movement Settings")]
    [SerializeField] Transform[] possibleTargets; // Idle points
    [SerializeField] float lerpTimeMax = 2f; // Movement duration between points
    [SerializeField] AnimationCurve idleWalkCurve; // Smooth curve for movement

    [Header("Hunger Settings")]
    [SerializeField] float hungerInterval = 10f; // Time between hunger phases
    [SerializeField] int maxHunger = 5; // Hunger refilled after eating

    private enum SpiderStates { Idling, Eating }
    private SpiderStates state = SpiderStates.Idling;

    private Transform target; // Current movement target
    private Vector3 startPos; // Start position for lerp
    private float lerpTime; // Tracks movement progress

    private float hungerTimer; // Counts down to hunger
    private int hungerVal; // Current hunger value

    private List<GameObject> allFood = new List<GameObject>(); // All food in scene
    private GameObject touchingObj; // Object currently touching

    private bool facingRight = true; // Track facing direction

    void Start()
    {
        hungerVal = maxHunger;
        hungerTimer = hungerInterval;
        FindAllFood();

        // Default movement curve if none provided
        if (idleWalkCurve == null || idleWalkCurve.keys.Length == 0)
        {
            idleWalkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
    }

    void Update()
    {
        switch (state)
        {
            case SpiderStates.Idling:
                RunIdle();
                break;
            case SpiderStates.Eating:
                RunEat();
                break;
        }
    }

    // ---------------------- STATE: IDLE ----------------------
    void RunIdle()
    {
        // Pick a random target if none
        if (target == null && possibleTargets.Length > 0)
        {
            target = possibleTargets[Random.Range(0, possibleTargets.Length)];
            startPos = transform.position;
            lerpTime = 0;
        }

        // Move toward target
        if (target != null)
        {
            Vector3 newPos = Move();
            FlipByDirection(newPos.x - transform.position.x);
            transform.position = newPos;

            // When finished moving, pick a new random target
            if (lerpTime >= lerpTimeMax)
            {
                target = null;
            }
        }

        // Countdown hunger
        hungerTimer -= Time.deltaTime;
        if (hungerTimer <= 0)
        {
            hungerVal = 0; // spider becomes hungry
            hungerTimer = hungerInterval; // reset timer
            target = null;
            state = SpiderStates.Eating; // switch to eating mode
        }
    }

    // ---------------------- STATE: EATING ----------------------
    void RunEat()
    {
        // If no food left, go back to idling
        if (allFood.Count == 0)
        {
            state = SpiderStates.Idling;
            return;
        }

        // If no target, find nearest food
        if (target == null)
        {
            Transform foodTarget = FindNearest(allFood);
            if (foodTarget == null)
            {
                state = SpiderStates.Idling;
                return;
            }

            target = foodTarget;
            startPos = transform.position;
            lerpTime = 0;
        }

        // Move toward food
        Vector3 newPos2 = Move();
        FlipByDirection(newPos2.x - transform.position.x);
        transform.position = newPos2;

        // Check if touching food
        if (touchingObj != null && touchingObj.CompareTag("Food"))
        {
            // Eat food
            GameObject foodToDestroy = touchingObj;
            allFood.Remove(foodToDestroy);

            Destroy(foodToDestroy);
            touchingObj = null;

            hungerVal = maxHunger;
            target = null;
            state = SpiderStates.Idling;
        }
    }

    // ---------------------- MOVEMENT ----------------------
    Vector3 Move()
    {
        lerpTime += Time.deltaTime;
        float t = Mathf.Clamp01(lerpTime / lerpTimeMax);
        float easedT = idleWalkCurve.Evaluate(t);

        Vector3 newPos = Vector3.Lerp(startPos, target.position, easedT);
        newPos.z = 0; // Stay in 2D
        return newPos;
    }

    // ---------------------- FLIP BY DIRECTION ----------------------
    void FlipByDirection(float xMovement)
    {
        if (xMovement > 0 && !facingRight)
        {
            facingRight = true;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
        else if (xMovement < 0 && facingRight)
        {
            facingRight = false;
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    // ---------------------- FOOD MANAGEMENT ----------------------
    void FindAllFood()
    {
        allFood.Clear();
        allFood.AddRange(GameObject.FindGameObjectsWithTag("Food"));
    }

    Transform FindNearest(List<GameObject> objsToFind)
    {
        float minDist = Mathf.Infinity;
        Transform nearest = null;

        foreach (GameObject obj in objsToFind)
        {
            if (obj == null) continue;
            float dist = Vector3.Distance(transform.position, obj.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = obj.transform;
            }
        }
        return nearest;
    }

    // ---------------------- COLLISION HANDLING ----------------------
    void OnTriggerEnter2D(Collider2D col)
    {
        if (col != null)
        {
            touchingObj = col.gameObject;
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col != null && col.gameObject == touchingObj)
        {
            touchingObj = null;
        }
    }
}
