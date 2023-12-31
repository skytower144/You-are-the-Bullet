using System.Collections;
using UnityEngine;
using DG.Tweening;

public class DragShoot : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float aliveTime;

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D bulletCollider;
    [SerializeField] private TrajectoryRenderer tr;
    [SerializeField] private Animator anim;
    [SerializeField] private GameObject reflectEffect;

    private Vector3 clickedPoint, releasePoint, movDir;
    private Camera cam;
    private bool isHolding = false;
    [SerializeField] private bool isReleased = false;
    private bool enableDrag = false;
    private bool isCountdown = false;

    private Vector2 lastVelocity;
    private int currentCombo = 0;

    void Start()
    {
        currentCombo = 0;
        cam = Camera.main;
        DOTween.Rewind("SpinBullet");
        DOTween.Play("SpinBullet");
    }

    void FixedUpdate()
    {
        lastVelocity = rb.velocity;
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player")) {
            if (isReleased) {
                GameManager.instance.playerPhysics.FlashEffect();
                GameManager.instance.playerHealth.DecreaseHealth();
                rb.velocity = Vector3.zero;
                anim.Play("Bullet_Die", -1, 0f);

                GameManager.instance.soundEffects.PlaySound(3);
                return;
            }
        }
        
        rb.velocity = Vector2.Reflect(lastVelocity, other.contacts[0].normal);
        RotateSprite(rb.velocity);

        if (other.gameObject.CompareTag("Enemy")) {
            if (!isReleased) PlayAngryToHappy();
            
            else {
                currentCombo++;
                GameManager.instance.scoreManager.UpdateComboScore(other.gameObject.GetComponent<EnemyBehaviour2>().enemy_type, currentCombo);
                StartCoroutine(other.gameObject.GetComponent<EnemyBehaviour2>().DestroyEnemy());

                GameManager.instance.soundEffects.PlaySound(2);
            }
        }

        else if (other.gameObject.CompareTag("Wall")) {
            WallHit(transform.localPosition, other.gameObject.GetComponent<WallType>().side);

            GameManager.instance.soundEffects.PlaySound(1);
        }
    }
    void Update()
    {
        if (!enableDrag || GameManager.instance.isGameOver) return;
        if (isCountdown) AliveTimeCountDown();

        if (isReleased) return;
        RotateBullet();

        if (Input.GetMouseButtonDown(0))
        {
            GameManager.instance.StopAllObjects();
            DOTween.Pause("SpinBullet");
            DOTween.Rewind("SpinBullet");
            PressMouse();

            GameManager.instance.soundEffects.PlaySound(4);
        }

        if (Input.GetMouseButton(0))
        {
            PressMouse();
            tr.RenderLine(transform.localPosition, cam.ScreenToWorldPoint(Input.mousePosition));

            if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Bullet_Stretch"))
                anim.Play("Bullet_Stretch", -1, 0f);
        }
        else if (Input.GetMouseButtonUp(0))
        {
            ReleaseMouse();
            tr.EndLine();
            MoveBullet();
        }
    }

    private void PressMouse()
    {
        rb.velocity = Vector3.zero;
        clickedPoint = transform.localPosition;
        isHolding = true;
    }

    private void ReleaseMouse()
    {
        if (!isHolding) return;

        anim.Play("Bullet_Shoot", -1, 0f);
        releasePoint = cam.ScreenToWorldPoint(Input.mousePosition);

        isHolding = false;
        isReleased = true;
        isCountdown = true;

        GameManager.instance.soundEffects.PlaySound(0);
    }

    private void MoveBullet()
    {
        movDir = (transform.localPosition - releasePoint).normalized;
        rb.velocity = movDir * speed;
    }

    private void RotateBullet()
    {
        if (!isHolding) return;
        
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        Vector3 direction = mousePos - transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.Rotate(0, 0, 90);
    }

    private void RotateSprite(Vector3 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        while (angle < 0f)
        {
            angle += 360f;
        }
        angle %= 360;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    public void PlayAngryAnimation()
    {
        anim.Play("Bullet_Angry", -1, 0f);
    }

    private void PlayAngryToHappy()
    {
        anim.Play("Bullet_AngryToHappy", -1, 0f);
    }

    private void ReturnShootingAnimation()
    {
        anim.Play("Bullet_Shoot", -1, 0f);
    }

    private void ReturnIdleAnimation()
    {
        anim.Play("Bullet_Idle", -1, 0f);
    }

    private void AliveTimeCountDown()
    {
        aliveTime -= Time.deltaTime;
        if (aliveTime < 0) {
            isCountdown = false;
            DestroyBullet();
        }
    }

    private void DestroyBullet()
    {
        GameManager.instance.PlayAllObjects();
        GameManager.instance.scoreManager.CalculateFinalComboScore(currentCombo);
        Destroy(gameObject);
    }

    public IEnumerator EnableDrag()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        bulletCollider.enabled = true;
        enableDrag = true;
    }

    private void WallHit(Vector2 pos, WallSide side)
    {
        GameObject effect = Instantiate(reflectEffect);
        effect.transform.localPosition = new Vector2(pos.x, pos.y);

        switch (side) {
            case WallSide.Top:
                effect.transform.localScale = new Vector2(1, -1);
                break;
            
            case WallSide.Left:
                effect.transform.rotation = Quaternion.Euler(0, 0, -90);
                break;
            
            case WallSide.Right:
                effect.transform.rotation = Quaternion.Euler(0, 0, 90);
                break;
            
            default:
                break;
        }
        
    }
}
