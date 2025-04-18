using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    #region MONOBEHAVIOUR METHODS

    public static GameManager Instance { get; private set; }

    [SerializeField]
    private TMP_Text _scoreText, _endScoreText, _highScoreText;

    private int score;

    [SerializeField]
    private Animator _scoreAnimator;

    [SerializeField]
    private AnimationClip _scoreClip;
 
    [SerializeField]
    private GameObject _endPanel;

    [SerializeField]
    private GameObject _pausePanel;  // Added pause panel reference

    [SerializeField]
    private Image _soundImage;

    [SerializeField]
    private Sprite _activeSoundSprite, _inactiveSoundSprite;

    private bool isPaused = false;  // Track pause state
    private bool hasGameEnded;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        ColorChanged += OnColorChanged;
    }

    private void OnDisable()
    {
        ColorChanged -= OnColorChanged;
    }

    private void Start()
    {
        AudioManager.Instance.AddButtonSound();
        StartCoroutine(IStartGame());
    }

    private void Update()
    {
        // Optional: Add keyboard shortcut for pausing (Escape key)
        if (Input.GetKeyDown(KeyCode.Escape) && !hasGameEnded)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    #endregion

    #region UI

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;  // Reset timescale when going to main menu
        SceneManager.LoadScene(Constants.DATA.MAIN_MENU_SCENE);
    }

    public void ReloadGame()
    {
        Time.timeScale = 1f;  // Reset timescale when reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ToggleSound()
    {
        bool sound = (PlayerPrefs.HasKey(Constants.DATA.SETTINGS_SOUND) ? PlayerPrefs.GetInt(Constants.DATA.SETTINGS_SOUND)
            : 1) == 1;
        sound = !sound;
        PlayerPrefs.SetInt(Constants.DATA.SETTINGS_SOUND, sound ? 1 : 0);
        _soundImage.sprite = sound ? _activeSoundSprite : _inactiveSoundSprite;
        AudioManager.Instance.ToggleSound();
    }

    public void UpdateScore()
    {
        score++;
        _scoreText.text = score.ToString();
        _scoreAnimator.Play(_scoreClip.name, -1, 0f);

        if(score % 2 == 0)
        {
            CurrentColorId = (CurrentColorId + 1) % _colors.Count;
        }
    }

    // Pause/Resume methods
    public void PauseGame()
    {
        if (hasGameEnded || isPaused) return;

        isPaused = true;
        Time.timeScale = 0f;  // Freeze game time
        _pausePanel.SetActive(true);
        // AudioManager.Instance.PauseBackgroundMusic();  // Optional: if you have background music
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = 1f;  // Resume game time
        _pausePanel.SetActive(false);
        // AudioManager.Instance.ResumeBackgroundMusic();  // Optional: if you have background music
    }

    #endregion

    #region GAME_START_END

    public void EndGame()
    {
        StartCoroutine(GameOver());
    }

    [SerializeField] private Animator _highScoreAnimator;
    [SerializeField] private AnimationClip _highScoreClip;

    [SerializeField]
    private Vector3 _cameraStartPos, _cameraEndPos;
    [SerializeField]
    private float _timeToMoveCamera;

    public UnityAction GameStarted, GameEnded;

    private IEnumerator GameOver()
    {
        hasGameEnded = true;
        _scoreText.gameObject.SetActive(false);
        GameEnded?.Invoke();

        yield return new WaitForSeconds(1f);
        // yield return MoveCamera(new Vector3(_cameraStartPos.x, -_cameraStartPos.y, _cameraStartPos.z));

        _endPanel.SetActive(true);
        _endScoreText.text = score.ToString();
        _endPanel.GetComponent<Image>().color = CurrentColor;

        bool sound = (PlayerPrefs.HasKey(Constants.DATA.SETTINGS_SOUND) ?
          PlayerPrefs.GetInt(Constants.DATA.SETTINGS_SOUND) : 1) == 1;
        _soundImage.sprite = sound ? _activeSoundSprite : _inactiveSoundSprite;

        int highScore = PlayerPrefs.HasKey(Constants.DATA.HIGH_SCORE) ? PlayerPrefs.GetInt(Constants.DATA.HIGH_SCORE) : 0;
        if (score > highScore)
        {
            _highScoreText.text = "NEW BEST";
            _highScoreAnimator.Play(_highScoreClip.name, -1, 0f);
            highScore = score;
            PlayerPrefs.SetInt(Constants.DATA.HIGH_SCORE, highScore);
        }
        else
        {
            _highScoreText.text = "BEST " + highScore.ToString();
        }
    }

    private IEnumerator IStartGame()
    {
        hasGameEnded = false;
        Camera.main.transform.position = _cameraStartPos;
        _scoreText.gameObject.SetActive(false);
        yield return MoveCamera(_cameraEndPos);

        _scoreText.gameObject.SetActive(true);
        score = 0;
        _scoreText.text = score.ToString();
        _scoreAnimator.Play(_scoreClip.name, -1, 0f);

        _currentColorId = 0;

        StartCoroutine(SpawnObstacles());
        GameStarted?.Invoke();
    }

    private IEnumerator MoveCamera(Vector3 cameraPos)
    {
        Transform cameraTransform = Camera.main.transform;
        float timeElapsed = 0f;
        Vector3 startPos = cameraTransform.position;
        Vector3 offset = cameraPos - startPos;
        float speed = 1 / _timeToMoveCamera;
        while(timeElapsed < 1f)
        {
            timeElapsed += speed * Time.deltaTime;
            cameraTransform.position = startPos + timeElapsed * offset;
            yield return null;
        }
        cameraTransform.position = cameraPos;
    }

    #endregion

    #region OBSTACLE_SPAWNING

    [SerializeField]
    private GameObject[] _obstaclePrefabs;
    [SerializeField]
    private GameObject[] _scorePrefabs;

    [SerializeField]
    private float _obstacleSpawnTime;

    private IEnumerator SpawnObstacles()
    {
        float spawnTime;

        if (_obstaclePrefabs == null || _obstaclePrefabs.Length == 0)
        {
            Debug.LogError("No obstacle prefabs assigned in GameManager!");
            yield break;
        }

        if (_scorePrefabs == null || _scorePrefabs.Length == 0)
        {
            Debug.LogError("No score prefabs assigned in GameManager!");
            yield break;
        }

        while(!hasGameEnded)
        {            
            int randomIndex = UnityEngine.Random.Range(0, _obstaclePrefabs.Length);
            Instantiate(_obstaclePrefabs[randomIndex]);

            if(UnityEngine.Random.Range(0,2) == 0)
            {
                int scoreIndex = UnityEngine.Random.Range(0, _scorePrefabs.Length);
                Instantiate(_scorePrefabs[scoreIndex]);
            }

            spawnTime = _obstacleSpawnTime * UnityEngine.Random.Range(2, 5) * 0.5f;
            yield return new WaitForSeconds(spawnTime);
        }
    }

    #endregion

    #region COLOR_CHANGE

    [SerializeField]
    private List<Color> _colors;

    [HideInInspector]
    public Color CurrentColor => _colors[CurrentColorId];

    [HideInInspector]
    public UnityAction<Color> ColorChanged;

    private int _currentColorId;

    private int CurrentColorId
    {
        get
        {
            return _currentColorId;
        }
        set
        {
            _currentColorId = value;
            ColorChanged?.Invoke(CurrentColor);
        }
    }

    [SerializeField]
    private Camera main;

    private void OnColorChanged(Color col)
    {
        // main.backgroundColor = col;
    }

    #endregion
}