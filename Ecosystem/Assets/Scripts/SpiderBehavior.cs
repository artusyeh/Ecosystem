using UnityEngine;
using System.Collections.Generic;

public class SpiderBehavior : MonoBehaviour
{
    [SerializeField] Transform[] possibleTargets;
    [SerializeField] float lerpTimeMax = 2f;
    [SerializeField] AnimationCurve idleWalkCurve;

    [SerializeField] float hungerInterval = 10f;
    [SerializeField] int maxHunger = 5;
    [SerializeField] float maxHuntTime = 3f;

    [SerializeField] GameObject eggPrefab;
    [SerializeField] Transform eggSpawnPoint;
    [SerializeField] int foodBeforeLayEgg = 3;

    [SerializeField] GameObject bloodParticlePrefab;
    [SerializeField] AudioClip crunchSound;
    [SerializeField, Range(0f, 1f)] float crunchVolume = 0.8f;

    private AudioSource audioSource;

    private enum SpiderStates { Idling, Eating, LayingEgg }
    private SpiderStates state = SpiderStates.Idling;

    private Transform target;
    private Vector3 startPos;
    private float lerpTime;

    private float hungerTimer;
    private int hungerVal;

    private List<GameObject> allFood = new List<GameObject>();
    private GameObject touchingObj;

    private bool facingRight = true;
    private float huntTimer;

    private int foodEatenCount = 0;

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
            case SpiderStates.LayingEgg:
                RunLayEgg();
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

            if (bloodParticlePrefab != null)
            {
                GameObject blood = Instantiate(bloodParticlePrefab, foodToDestroy.transform.position, Quaternion.identity);
                Destroy(blood, 2f);
            }

            if (crunchSound != null && audioSource != null)
                audioSource.PlayOneShot(crunchSound, crunchVolume);

            Destroy(foodToDestroy);
            touchingObj = null;

            hungerVal = maxHunger;
            target = null;

            foodEatenCount++;
            if (foodEatenCount >= foodBeforeLayEgg)
            {
                state = SpiderStates.LayingEgg;
            }
            else
            {
                state = SpiderStates.Idling;
            }
        }
    }

    // ---------------------- STATE: LAYING EGG ----------------------
    void RunLayEgg()
    {
        if (eggPrefab != null)
        {
            Vector3 spawnPos = eggSpawnPoint != null ? eggSpawnPoint.position : transform.position;
            Instantiate(eggPrefab, spawnPos, Quaternion.identity);
        }

        foodEatenCount = 0;
        state = SpiderStates.Idling;
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

    void FindAllFood()
    {
        allFood.Clear();
        allFood.AddRange(GameObject.FindGameObjectsWithTag("Food"));
    }

    Transform FindNearest(List<GameObject> objsToFind) //find nearest
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
