using UnityEngine;
using System.Collections.Generic;

public class GiantMonsterController : MonoBehaviour
{
    [Header("Monster Stages")]
    [SerializeField] GameObject[] monsterStages;
    [SerializeField] float moveSpeedStage1 = 1f;
    [SerializeField] float moveSpeedStage2 = 1.5f;
    [SerializeField] float lungeSpeed = 12f;

    [Header("Trigger Conditions")]
    [SerializeField] string flyTag = "Food";
    [SerializeField] string spiderTag = "Spider";
    [SerializeField] int requiredFlies = 1;
    [SerializeField] int requiredSpiders = 1;

    [Header("Attack Settings")]
    [SerializeField] int fliesToEatMin = 1;
    [SerializeField] int fliesToEatMax = 4;
    [SerializeField] int spidersToEatMin = 1;
    [SerializeField] int spidersToEatMax = 2;
    [SerializeField] float resetDelay = 5f;

    private Camera mainCam;
    private enum MonsterPhase { Idle, Stage1, Stage2, Stage3, Resetting }
    private MonsterPhase phase = MonsterPhase.Idle;

    private Vector3 stage1Start, stage2Start, stage3Start;
    private Vector3 stage1End, stage2End, stage3End;

    private float resetTimer;

    // Stage 3 control
    private float stage3Timer;
    private bool isSlidingIn;
    private bool isPausing;
    private bool isLunging;
    private bool isRetreating;

    private Vector3 stage3PausePos;
    private Vector3 stage3RetreatPos;

    void Start()
    {
        mainCam = Camera.main;
        SetupPositions();

        foreach (var obj in monsterStages)
            if (obj != null) obj.SetActive(false);
    }

    void Update()
    {
        switch (phase)
        {
            case MonsterPhase.Idle:
                CheckTrigger();
                break;
            case MonsterPhase.Stage1:
                MoveStage(monsterStages[0], moveSpeedStage1, stage1End, MonsterPhase.Stage2);
                break;
            case MonsterPhase.Stage2:
                MoveStage(monsterStages[1], moveSpeedStage2, stage2End, MonsterPhase.Stage3);
                break;
            case MonsterPhase.Stage3:
                RunStage3Behavior();
                break;
            case MonsterPhase.Resetting:
                RunReset();
                break;
        }
    }

    void CheckTrigger()
    {
        int flyCount = GameObject.FindGameObjectsWithTag(flyTag).Length;
        int spiderCount = GameObject.FindGameObjectsWithTag(spiderTag).Length;

        if (flyCount >= requiredFlies && spiderCount >= requiredSpiders)
        {
            if (monsterStages[0] != null)
            {
                monsterStages[0].SetActive(true);
                monsterStages[0].transform.position = stage1Start;
                phase = MonsterPhase.Stage1;
            }
        }
    }

    void MoveStage(GameObject monster, float speed, Vector3 target, MonsterPhase nextPhase)
    {
        if (monster == null) return;

        monster.transform.position = Vector3.MoveTowards(
            monster.transform.position,
            target,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(monster.transform.position, target) < 0.1f)
        {
            monster.SetActive(false);

            if (nextPhase == MonsterPhase.Stage2 && monsterStages[1] != null)
            {
                monsterStages[1].SetActive(true);
                monsterStages[1].transform.position = stage2Start;
            }

            if (nextPhase == MonsterPhase.Stage3 && monsterStages[2] != null)
            {
                monsterStages[2].SetActive(true);
                monsterStages[2].transform.position = stage3Start;
                ResetStage3State();
            }

            phase = nextPhase;
        }
    }

    void RunStage3Behavior()
    {
        if (monsterStages[2] == null) return;
        GameObject monster = monsterStages[2];

        // Step 1: Slide in smoothly from right
        if (isSlidingIn)
        {
            monster.transform.position = Vector3.Lerp(
                monster.transform.position,
                stage3PausePos,
                Time.deltaTime * 1.5f
            );

            if (Vector3.Distance(monster.transform.position, stage3PausePos) < 0.2f)
            {
                isSlidingIn = false;
                isPausing = true;
                stage3Timer = 3f; //pause for 3 seconds
            }
        }


        //Pause before lunge
        else if (isPausing)
        {
            stage3Timer -= Time.deltaTime;
            if (stage3Timer <= 0f)
            {
                isPausing = false;
                isLunging = true;
            }
        }
        //ATTACKKK
        else if (isLunging)
        {
            monster.transform.position = Vector3.Lerp(
                monster.transform.position,
                stage3End,
                Time.deltaTime * (lungeSpeed / 2f)
            );

            if (Vector3.Distance(monster.transform.position, stage3End) < 0.4f)
            {
                EatCreatures(flyTag, Random.Range(fliesToEatMin, fliesToEatMax + 1));
                EatCreatures(spiderTag, Random.Range(spidersToEatMin, spidersToEatMax + 1));

                isLunging = false;
                isRetreating = true;
            }
        }
        //retreat
        else if (isRetreating)
        {
            monster.transform.position = Vector3.Lerp(
                monster.transform.position,
                stage3RetreatPos,
                Time.deltaTime * 1.2f
            );

            if (Vector3.Distance(monster.transform.position, stage3RetreatPos) < 0.3f)
            {
                isRetreating = false;
                phase = MonsterPhase.Resetting;
                resetTimer = resetDelay;
            }
        }
    }

    void RunReset()
    {
        resetTimer -= Time.deltaTime;
        if (resetTimer <= 0f)
        {
            foreach (var obj in monsterStages)
                if (obj != null) obj.SetActive(false);

            phase = MonsterPhase.Idle;
        }
    }

    void EatCreatures(string tag, int amount)
    {
        GameObject[] creatures = GameObject.FindGameObjectsWithTag(tag);
        int toEat = Mathf.Min(amount, creatures.Length);

        for (int i = 0; i < toEat; i++)
        {
            GameObject victim = creatures[Random.Range(0, creatures.Length)];
            if (victim != null)
                Destroy(victim);
        }
    }

    void SetupPositions()
    {
        if (mainCam == null) return;

        float camHeight = mainCam.orthographicSize * 2f;
        float camWidth = camHeight * mainCam.aspect;
        Vector3 camPos = mainCam.transform.position;

        float rightX = camPos.x + camWidth / 2f + 2f;
        float leftX  = camPos.x - camWidth / 2f - 2f;

        // Stage 1
        if (monsterStages.Length >= 1 && monsterStages[0] != null)
        {
            float y = monsterStages[0].transform.position.y;
            float z = monsterStages[0].transform.position.z;
            stage1Start = new Vector3(rightX, y, z);
            stage1End   = new Vector3(leftX,  y, z);
        }

        // Stage 2
        if (monsterStages.Length >= 2 && monsterStages[1] != null)
        {
            float y = monsterStages[1].transform.position.y;
            float z = monsterStages[1].transform.position.z;
            stage2Start = new Vector3(rightX, y, z);
            stage2End   = new Vector3(leftX,  y, z);
        }

        // Stage 3
        if (monsterStages.Length >= 3 && monsterStages[2] != null)
        {
            float y = monsterStages[2].transform.position.y;
            float z = monsterStages[2].transform.position.z;

            stage3Start      = new Vector3(rightX + 6f, y, z);   // start from right
            stage3PausePos   = new Vector3(camPos.x + 10f, y, z); // paus
            stage3End        = new Vector3(leftX + 10f, y, z);         // lunge to left
            stage3RetreatPos = new Vector3(rightX + 8f, y, z);   // retreat
        }
    }
    void ResetStage3State()
    {
        isSlidingIn = true;
        isPausing = false;
        isLunging = false;
        isRetreating = false;
        stage3Timer = 0f;
    }
}
