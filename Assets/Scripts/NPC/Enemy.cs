using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float speed = 2f;
    public Transform[] points;
    private int i;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (animator != null)
        {
            animator.Play("Enemy_Run");
        }

        if (Vector2.Distance(transform.position, points[i].position) < 0.25f)
        {
            i++;
            if (i == points.Length)
            {
                i = 0;
            }
        }

        transform.position = Vector2.MoveTowards(transform.position, points[i].position, speed * Time.deltaTime);
        spriteRenderer.flipX = (transform.position.x - points[i].position.x) < 0f;
    }
}

    