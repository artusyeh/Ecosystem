using UnityEngine;

public class SpiderBehavior : MonoBehaviour
{
    [SerializeField]
    Transform[] possibleTargets;
    Transform target = null;

    float lerpTime;

    [SerializeField]
    float lerpTimeMax;

    enum SpiderStates
    {
        eating,
        showering,
        dying,
        idling
    }

    SpiderStates state = SpiderStates.idling;

    [SerializeField]
    AnimationCurve idleWalkCurve;

    float hungerVal = 100;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case SpiderStates.idling:
                break;
            case SpiderStates.eating:
                break;
            case SpiderStates.showering:
                break;
            case SpiderStates.dying:
                break;
            default:
                break;
        }

    }

    void RunIdle()
    {
        if (target == null)
        {
            int newTarget = Random.Range(0, possibleTargets.Length);
            target = possibleTargets[newTarget];
            lerpTime = 0;
        }
        else
        {
            transform.position = Move();
        }
        StepNeeds();
        if (hungerVal <= 0)
        {
            state = SpiderStates.eating;
        }
        //find random position
        //set target to that position
        //move to that position
        //when at position, pause, wait random time to find another

    }

    void RunEat()
    {
        
    }

    void StepNeeds()
    {
        hungerVal -= 0.1f;
    }

    Vector3 Move()
    {
        lerpTime += Time.deltaTime;
        float percent = idleWalkCurve.Evaluate(lerpTime / lerpTimeMax);
        Vector3 newPos = Vector3.Lerp(transform.position, target.position, lerpTime / lerpTimeMax);
        return newPos;
    }
}
