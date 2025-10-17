using UnityEngine;

public class FlyBehavior : MonoBehaviour
{
    private enum FlyState { Idle, Flee, Panic }

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float fleeSpeed = 5f;
    [SerializeField] float panicSpeed = 7f;
    [SerializeField] float minWaitTime = 1f;
    [SerializeField] float maxWaitTime = 3f;
    [SerializeField] float edgePadding = 0.5f;

    [Header("Wiggle")]
    [SerializeField] float wiggleAmplitude = 0.2f;
    [SerializeField] float wiggleFrequency = 8f;

    [Header("Detection")]
    [SerializeField] float detectionRange = 3f;
    [SerializeField] float panicRange = 1.2f;
    [SerializeField] string spiderTag = "Spider";

    [Header("Eggs")]
    [SerializeField] GameObject eggPrefab;
    [SerializeField] Vector2 eggLayInterval = new Vector2(10f, 20f);
    [SerializeField] Vector2Int eggCountRange = new Vector2Int(1, 3);
    [SerializeField] float eggDropOffset = 0.3f;

    private FlyState currentState = FlyState.Idle;
    private Vector3 targetPos;
    private bool moving = false;
    private float waitTimer = 0f;
    private Camera mainCam;
    private Vector3 basePosition;
    private Transform spiderTarget;


    private float eggTimer;
    private float nextEggTime;

    void Start()
    {
        mainCam = Camera.main;
        basePosition = transform.position;

        GameObject spiderObj = GameObject.FindGameObjectWithTag(spiderTag);
        if (spiderObj != null)
            spiderTarget = spiderObj.transform;

        PickNewTarget();

        ResetEggTimer();
    }

    void Update()
    {
        if (spiderTarget == null) return;

        float dist = Vector3.Distance(transform.position, spiderTarget.position);
        UpdateStateBasedOnDistance(dist);
        UpdateStateBehavior();

        ApplyWiggle();

        HandleEggLaying();
    }


    void UpdateStateBasedOnDistance(float dist)
    {
        switch (currentState)
        {
            case FlyState.Panic:
                if (dist > panicRange)
                    currentState = (dist < detectionRange) ? FlyState.Flee : FlyState.Idle;
                break;

            case FlyState.Flee:
                if (dist < panicRange)
                    currentState = FlyState.Panic;
                else if (dist >= detectionRange)
                    currentState = FlyState.Idle;
                break;

            case FlyState.Idle:
                if (dist < panicRange)
                    currentState = FlyState.Panic;
                else if (dist < detectionRange)
                    currentState = FlyState.Flee;
                break;
        }
    }

    void UpdateStateBehavior()
    {
        switch (currentState)
        {
            case FlyState.Idle:
                UpdateIdle();
                break;

            case FlyState.Flee:
                UpdateFlee();
                break;

            case FlyState.Panic:
                UpdatePanic();
                break;
        }
    }


    void UpdateIdle()
    {
        if (moving)
        {
            basePosition = Vector3.MoveTowards(basePosition, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(basePosition, targetPos) < 0.05f)
            {
                moving = false;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }
        }
        else
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
                PickNewTarget();
        }
    }

    void UpdateFlee()
    {
        Vector3 directionAway = (transform.position - spiderTarget.position).normalized;
        basePosition += directionAway * fleeSpeed * Time.deltaTime;
        ClampToCameraBounds();
    }

    void UpdatePanic()
    {
        if (!moving)
        {
            PickPanicDirection();
            moving = true;
        }

        basePosition = Vector3.MoveTowards(basePosition, targetPos, panicSpeed * Time.deltaTime);

        if (Vector3.Distance(basePosition, targetPos) < 0.1f)
        {
            moving = false;
        }

        ClampToCameraBounds();
    }


    void HandleEggLaying()
    {
        eggTimer += Time.deltaTime;
        if (eggTimer >= nextEggTime)
        {
            LayEggs();
            ResetEggTimer();
        }
    }

    void LayEggs()
    {
        int eggCount = Random.Range(eggCountRange.x, eggCountRange.y + 1);

        for (int i = 0; i < eggCount; i++)
        {
            Vector3 dropPos = transform.position + (Vector3)Random.insideUnitCircle * eggDropOffset;
            Instantiate(eggPrefab, dropPos, Quaternion.identity);
        }
    }

    void ResetEggTimer()
    {
        eggTimer = 0f;
        nextEggTime = Random.Range(eggLayInterval.x, eggLayInterval.y);
    }


    void PickNewTarget()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        Vector3 camBottomLeft = mainCam.ViewportToWorldPoint(new Vector3(0, 0, -mainCam.transform.position.z));
        Vector3 camTopRight = mainCam.ViewportToWorldPoint(new Vector3(1, 1, -mainCam.transform.position.z));

        float minX = camBottomLeft.x + edgePadding;
        float maxX = camTopRight.x - edgePadding;
        float minY = camBottomLeft.y + edgePadding;
        float maxY = camTopRight.y - edgePadding;

        float newX = Random.Range(minX, maxX);
        float newY = Random.Range(minY, maxY);

        targetPos = new Vector3(newX, newY, transform.position.z);
        moving = true;
    }

    void PickPanicDirection()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        targetPos = basePosition + new Vector3(randomDir.x, randomDir.y, 0f) * 2f;
    }

    void ClampToCameraBounds()
    {
        Vector3 camBottomLeft = mainCam.ViewportToWorldPoint(new Vector3(0, 0, -mainCam.transform.position.z));
        Vector3 camTopRight = mainCam.ViewportToWorldPoint(new Vector3(1, 1, -mainCam.transform.position.z));

        float minX = camBottomLeft.x + edgePadding;
        float maxX = camTopRight.x - edgePadding;
        float minY = camBottomLeft.y + edgePadding;
        float maxY = camTopRight.y - edgePadding;

        basePosition.x = Mathf.Clamp(basePosition.x, minX, maxX);
        basePosition.y = Mathf.Clamp(basePosition.y, minY, maxY);
    }

    void ApplyWiggle()
    {
        float offsetX = Mathf.Sin(Time.time * wiggleFrequency) * wiggleAmplitude;
        float offsetY = Mathf.Cos(Time.time * wiggleFrequency * 0.8f) * wiggleAmplitude;
        transform.position = basePosition + new Vector3(offsetX, offsetY, 0f);
    }
}
