using UnityEngine;

public class FlyBehavior : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 2f;           // Normal wandering speed
    [SerializeField] float fleeSpeed = 5f;           // Speed when fleeing from spider
    [SerializeField] float panicSpeed = 7f;          // Speed when spider is very close
    [SerializeField] float minWaitTime = 1f;
    [SerializeField] float maxWaitTime = 3f;
    [SerializeField] float edgePadding = 0.5f;

    [Header("Wiggle Settings")]
    [SerializeField] float wiggleAmplitude = 0.2f;
    [SerializeField] float wiggleFrequency = 8f;

    [Header("Spider Detection")]
    [SerializeField] float detectionRange = 3f;      // Range to start fleeing
    [SerializeField] float panicRange = 1.2f;        // Range for panic dart
    [SerializeField] string spiderTag = "Spider";    // Tag used by the spider

    private Vector3 targetPos;
    private bool moving = false;
    private float waitTimer = 0f;
    private Camera mainCam;
    private Vector3 basePosition;
    private Transform spiderTarget;
    private bool fleeing = false;
    private bool panicking = false;

    void Start()
    {
        mainCam = Camera.main;
        basePosition = transform.position;

        GameObject spiderObj = GameObject.FindGameObjectWithTag(spiderTag);
        if (spiderObj != null)
            spiderTarget = spiderObj.transform;

        PickNewTarget();
    }

    void Update()
    {
        if (spiderTarget != null)
        {
            float dist = Vector3.Distance(transform.position, spiderTarget.position);

            if (dist < panicRange)
            {
                // PANIC — random dart away fast
                if (!panicking)
                {
                    panicking = true;
                    fleeing = false;
                    PickPanicDirection();
                }
                RunPanic();
            }
            else if (dist < detectionRange)
            {
                // FLEE — move directly away from spider
                fleeing = true;
                panicking = false;
                RunFromSpider();
            }
            else
            {
                // Calm — wander normally
                fleeing = false;
                panicking = false;
                if (moving)
                    MoveTowardTarget();
                else
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0f)
                        PickNewTarget();
                }
            }
        }

        ApplyWiggle();
    }

    // Normal wandering
    void MoveTowardTarget()
    {
        basePosition = Vector3.MoveTowards(basePosition, targetPos, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(basePosition, targetPos) < 0.05f)
        {
            moving = false;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    // Runs away in a straight line from spider
    void RunFromSpider()
    {
        Vector3 directionAway = (transform.position - spiderTarget.position).normalized;
        basePosition += directionAway * fleeSpeed * Time.deltaTime;
        ClampToCameraBounds();
    }

    // Random darting direction (panic mode)
    void PickPanicDirection()
    {
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        targetPos = basePosition + new Vector3(randomDir.x, randomDir.y, 0f) * 2f;
    }

    void RunPanic()
    {
        basePosition = Vector3.MoveTowards(basePosition, targetPos, panicSpeed * Time.deltaTime);
        if (Vector3.Distance(basePosition, targetPos) < 0.1f)
        {
            panicking = false; // end panic after reaching dart point
        }
        ClampToCameraBounds();
    }

    // Random idle wandering target
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

    // Keep fly inside camera view
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

    // Add small fluttery wiggle
    void ApplyWiggle()
    {
        float offsetX = Mathf.Sin(Time.time * wiggleFrequency) * wiggleAmplitude;
        float offsetY = Mathf.Cos(Time.time * wiggleFrequency * 0.8f) * wiggleAmplitude;
        transform.position = basePosition + new Vector3(offsetX, offsetY, 0f);
    }
}
