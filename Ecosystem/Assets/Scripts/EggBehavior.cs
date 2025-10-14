using UnityEngine;

public class EggBehavior : MonoBehaviour
{
    [SerializeField] float hatchTime = 20f;
    [SerializeField] GameObject babySpiderPrefab;
    [SerializeField] GameObject hatchEffect;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= hatchTime)
        {
            Hatch();
        }
    }

    void Hatch()
    {
        Debug.Log("Egg hatched");

        if (hatchEffect != null)
            Instantiate(hatchEffect, transform.position, Quaternion.identity);

        if (babySpiderPrefab != null)
            Instantiate(babySpiderPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
