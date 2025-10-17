using UnityEngine;


// moves floaty shit idfk
public class UpDown : MonoBehaviour
{
    [SerializeField]
    public float amplitude = 1f;
    public float speed = 2f;

    private Vector3 startPos;

    void Start()
    {

        startPos = transform.position;
    }

    void Update()
    {
        float newY = startPos.y + Mathf.Sin(Time.time * speed) * amplitude;
        Vector3 pos = transform.position;
        pos.y = newY;
        transform.position = pos;
    }

}
