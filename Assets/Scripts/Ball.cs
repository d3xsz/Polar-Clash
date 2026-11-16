using UnityEngine;

public class Ball : MonoBehaviour
{
    public int team = 1;   // 1 = sari (P1 rengi), 2 = cyan (P2 rengi)
    public int points = 1;
    void Awake()
    {
        if (GetComponent<BallTrail>() == null) gameObject.AddComponent<BallTrail>();
    }

    Rigidbody2D body;
    public Rigidbody2D Body
    {
        get { if (body == null) body = GetComponent<Rigidbody2D>(); return body; }
    }
}
