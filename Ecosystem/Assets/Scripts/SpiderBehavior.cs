using UnityEngine;
using System.Collections.Generic;

public class SpiderBehavior : MonoBehaviour
{
    [Header("Idle Movement Settings")]

    [SerializeField]
    Transform[] possibleTargets; // Idle points

    [SerializeField]
    float lerpTimeMax = 2f; // Movement duration between points

    [SerializeField]
    AnimationCurve idleWalkCurve; // Smooth curve for movement

    [Header("Hunger Settings")]

    [SerializeField]
    float hungerInterval = 10f; // Time between hunger phases

    [SerializeField]
    int maxHunger = 5; // Hunger refilled after eating

    [SerializeField]
    float maxHuntTime = 3f; // Maximum duration spider will hunt before giving up

    [Header("Effects")]

    [SerializeField]
    GameObject bloodParticlePrefab; // Assign your blood particle prefab

    [SerializeField]
    AudioClip crunchSound;          // Assign your crunch sound
    
    [SerializeField, Range(0f, 1f)]
    float crunchVolume = 0.8f;

    private AudioSource audioSource;

    private enum SpiderStates { Idling, Eating }
    private SpiderStates state = SpiderStates.Idling;

    private Transform target;
    private Vector3 startPos;
    private float lerpTime;

    private float hungerTimer;
    private int hungerVal;

    private List<GameObject> allFood = new List<GameObject>();
    private GameObject touchingObj;

    private bool facingRight = true;

    // Track how long the spider has been hunting
    private float huntTimer;

    void Start()
    {
        hungerVal = maxHunger;
        hungerTimer = hungerInterval;
        FindAllFood();

        if (idleWalkCurve == null || idleWalkCurve.keys.Length == 0)
            idleWalkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
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
        if (target == null && possibleTargets.Length > 0)
        {
            target = possibleTargets[Random.Range(0, possibleTargets.Length)];
            startPos = transform.position;
            lerpTime = 0;
        }

        if (target != null)
        {
            Vector3 newPos = Move();
            FlipByDirection(newPos.x - transform.position.x);
            transform.position = newPos;

            if (lerpTime >= lerpTimeMax)
                target = null;
        }

        hungerTimer -= Time.deltaTime;
        if (hungerTimer <= 0)
        {
            hungerVal = 0;
            hungerTimer = hungerInterval;
            target = null;

            // Enter hunting mode and reset hunt timer
            huntTimer = 0f;
            state = SpiderStates.Eating;
        }
    }

    // ---------------------- STATE: EATING ----------------------
    void RunEat()
    {
        huntTimer += Time.deltaTime;
        if (huntTimer >= maxHuntTime)
        {
            state = SpiderStates.Idling;
            target = null;
            return;
        }

        if (allFood.Count == 0)
        {
            state = SpiderStates.Idling;
            return;
        }

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

        Vector3 newPos2 = Move();
        FlipByDirection(newPos2.x - transform.position.x);
        transform.position = newPos2;

        // Check if touching food
        if (touchingObj != null && touchingObj.CompareTag("Food"))
        {
            GameObject foodToDestroy = touchingObj;
            allFood.Remove(foodToDestroy);

            // Spawn blood particle effect
            if (bloodParticlePrefab != null)
            {
                GameObject blood = Instantiate(
                    bloodParticlePrefab,
                    foodToDestroy.transform.position,
                    Quaternion.identity
                );
                Destroy(blood, 2f);
            }

            // Play crunch sound
            if (crunchSound != null && audioSource != null)
                audioSource.PlayOneShot(crunchSound, crunchVolume);

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
        newPos.z = 0;
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
            touchingObj = col.gameObject;
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col != null && col.gameObject == touchingObj)
            touchingObj = null;
    }
}
