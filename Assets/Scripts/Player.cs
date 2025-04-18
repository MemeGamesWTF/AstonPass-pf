using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{ 
    private bool canMove;
    private bool canShoot;

    [SerializeField]
    private AudioClip _moveClip, _pointClip, _scoreClip, _loseClip;

    [SerializeField]
    private GameObject _explosionPrefab;

    private void Awake()
    {
        canShoot = false;
        canMove = false;
        // Ensure the player starts at the left edge (_moveStartPos)
        transform.position = _moveStartPos;
        currentMoveDistance = 0f; // Explicitly set to 0 to match _moveStartPos
    }

    private void OnEnable()
    {
        GameManager.Instance.GameStarted += GameStarted;
        GameManager.Instance.GameEnded += OnGameEnded;
    }

    private void OnDisable()
    {
        GameManager.Instance.GameStarted -= GameStarted;
        GameManager.Instance.GameEnded -= OnGameEnded;
    }

    private void GameStarted()
    {
        canMove = true;
        canShoot = true;

        moveSpeed = Mathf.Abs(1f / _moveTime); // Start moving right initially
        currentMoveDistance = 0f; // Ensure player starts at _moveStartPos (left edge)
        moveOffset = _moveEndPos - _moveStartPos;
        transform.position = _moveStartPos; // Reinforce starting position
    }

    private void Update()
    {
        if (canShoot && Input.GetMouseButtonDown(0))
        {
            isMoving = true; // Start moving when mouse is clicked
            AudioManager.Instance.PlaySound(_moveClip);
        }
    }

    [SerializeField] private float _moveTime;
    [SerializeField] private Vector3 _moveStartPos, _moveEndPos;

    private Vector3 moveOffset;
    private float moveSpeed;
    private float currentMoveDistance;
    private bool isMoving = false; // Tracks if the player should move

    private void FixedUpdate()
    {
        if (!canMove || !isMoving) return; // Only move if canMove and isMoving are true

        currentMoveDistance += moveSpeed * Time.fixedDeltaTime;

        // Check if we've reached or exceeded the edges
        if (currentMoveDistance >= 1f)
        {
            currentMoveDistance = 1f; // Clamp to end position
            moveSpeed = -Mathf.Abs(moveSpeed); // Reverse direction (towards start)
            isMoving = false; // Stop moving when edge is reached
        }
        else if (currentMoveDistance <= 0f)
        {
            currentMoveDistance = 0f; // Clamp to start position
            moveSpeed = Mathf.Abs(moveSpeed); // Reverse direction (towards end)
            isMoving = false; // Stop moving when edge is reached
        }

        // Update position based on currentMoveDistance
        transform.position = _moveStartPos + currentMoveDistance * moveOffset;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(Constants.Tags.SCORE))
        {
            collision.gameObject.GetComponent<Score>().OnGameEnded(); // Assuming this is intentional
            GameManager.Instance.UpdateScore();            
            AudioManager.Instance.PlaySound(_scoreClip);
        }

        if (collision.CompareTag(Constants.Tags.OBSTACLE))
        {
            Destroy(Instantiate(_explosionPrefab, transform.position, Quaternion.identity), 3f);
            AudioManager.Instance.PlaySound(_loseClip);
            GameManager.Instance.EndGame();
            Destroy(gameObject);
        }
    }

    [SerializeField] private float _destroyTime;

    public void OnGameEnded()
    {
        StartCoroutine(Rescale());
    }

    private IEnumerator Rescale()
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        Vector3 scaleOffset = endScale - startScale;
        float timeElapsed = 0f;
        float speed = 1f / _destroyTime;
        var updateTime = new WaitForFixedUpdate();
        while (timeElapsed < 1f)
        {
            timeElapsed += speed * Time.fixedDeltaTime;
            transform.localScale = startScale + timeElapsed * scaleOffset;
            yield return updateTime;
        }

        Destroy(gameObject);
    }
}