using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool _gameOver = false;

}


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public class PlayerControler : MonoBehaviour
{
    [SerializeField ] GameManager _gameManager;

    Rigidbody2D _rb;
    Camera _mainCamera;

    float _moveVertical;
    float _moveHorizontal;
    float _moveSpeed = 5f;
    float _speedLimiter = 0.7f; // Use to decrease the movement speed of our spawnObject:
    Vector2 _moveVelocity;

    Vector2 _mousePos;
    Vector2 _offset;

    [SerializeField] GameObject _bullet;
    [SerializeField] GameObject _bulletSpawn;

    bool _isShooting = false;
    float _bulletSpeed = 15f;


    void Start()
    {
        _rb = gameObject.GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        _moveHorizontal = Input.GetAxisRaw("Horizontal");
        _moveVertical = Input.GetAxisRaw("Vertical");

        _moveVelocity = new Vector2(_moveHorizontal, _moveVertical) * _moveSpeed;

        if (Input.GetMouseButtonDown(0))
        {
            _isShooting = true;
        }
    }
    private void FixedUpdate()
    {
        MovePlayer();
        RotatePlayer();

        if (_isShooting)
        {
            StartCoroutine(Fire());
        }
    }
    void MovePlayer()
    {
        if (_moveHorizontal != 0 || _moveVertical != 0)
        {
            if (_moveHorizontal != 0 && _moveVertical != 0)
            {
                _moveVelocity *= _speedLimiter;
            }
            _rb.linearVelocity = _moveVelocity;
        }
        else
        {
            _moveVelocity = new Vector2(0f, 0f);
            _rb.linearVelocity = _moveVelocity;
        }
    }
    void RotatePlayer()
    {
        _mousePos = Input.mousePosition;
        
        // It is use to find the location of Player on comparing to Screen.
        Vector3 screenPoint = _mainCamera.WorldToScreenPoint(transform.localPosition);//transform.localPosition -> It define the position of Player.
        
        //It is use to compare actual position of player and mouse curser position in the screen.
        _offset = new Vector2(_mousePos.x - screenPoint.x , _mousePos.y - screenPoint.y).normalized; //Here normalized use to normalize the speed of bullet firing.

        float angle = Mathf.Atan2(_offset.y,_offset.x) * Mathf.Rad2Deg; //Rad2Deg -> Convert Radian to Degree.

        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f); //Quaternion.Euler -> Use to rotate with a certain angle.  angle - 90f -> Use to rotate in proper direction.
    }


    IEnumerator Fire()
    {
        _isShooting = false;
        GameObject bullet = Instantiate(_bullet, _bulletSpawn.transform.position, Quaternion.identity);
        bullet.GetComponent<Rigidbody2D>().linearVelocity = _offset * _bulletSpeed;
        yield return new WaitForSeconds(1f);
        Destroy(bullet);
    }
}


using System.Collections;
using UnityEngine;

public class EnemyBehaviour : MonoBehaviour
{
    public GameOverScript gameOverScript; // ✅ Assign in Inspector or Find in Start()
    private GameManager _gameManager;
    private GameObject _player;

    private float _enemyHealth = 100f;
    private float _enemymoveSpeed = 2f;

    private Quaternion _targetRotation;
    private bool _disableEnemy = false;

    private Vector2 _moveDirection;

    void Start()
    {
        _gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        _player = GameObject.FindGameObjectWithTag("Player");

        // ✅ Find the GameOverScript automatically
        if (gameOverScript == null)
        {
            gameOverScript = Object.FindAnyObjectByType<GameOverScript>();
        }
    }

    void Update()
    {
        if (!_gameManager._gameOver && !_disableEnemy)
        {
            MoveEnemy();
            RotateEnemy();
        }
    }

    void MoveEnemy()
    {
        if (_player != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, _player.transform.position, _enemymoveSpeed * Time.deltaTime);
        }
    }

    void RotateEnemy()
    {
        if (_player != null)
        {
            _moveDirection = _player.transform.position - transform.position;
            _moveDirection.Normalize();

            _targetRotation = Quaternion.LookRotation(Vector3.forward, _moveDirection);

            if (transform.rotation != _targetRotation)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, 200 * Time.deltaTime);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("bullet"))
        {
            StartCoroutine(Damaged());

            _enemyHealth -= 50f;

            if (_enemyHealth <= 0)
            {
                Destroy(gameObject);
            }
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            _gameManager._gameOver = true;
            collision.gameObject.SetActive(false); // ✅ Hide Player
            _disableEnemy = true; // ✅ Stop enemy movement
            StartCoroutine(GameOverSequence()); // ✅ Wait before showing Game Over
        }
    }

    IEnumerator GameOverSequence()
    {
        yield return new WaitForSeconds(2f); // ✅ Wait for death animation

        if (gameOverScript != null)
        {
            gameOverScript.EnableGameOverMenu(); // ✅ Now show Game Over screen
        }
        else
        {
            Debug.LogError("GameOverScript is missing! Assign it in the scene.");
        }
    }

    IEnumerator Damaged()
    {
        _disableEnemy = true;
        yield return new WaitForSeconds(0.5f);
        _disableEnemy = false;
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawn : MonoBehaviour
{
    [SerializeField]GameManager _gameManager;
    [SerializeField] GameObject[] _spawnPoints;
    [SerializeField] GameObject _enemy;
    float _spawnTimer = 2f;
    float _spawnRateIncrease = 5f;

    void Start()
    {
        StartCoroutine(SpawnNextEnemy());
        StartCoroutine(SpawnRateIncrease());
    }

    IEnumerator SpawnNextEnemy()
    {
        //It tell that from where enemy are come from and Random.Range(0, _spawnPoints.Length) -> this say that the array start from Zero to lenght of the Array.
        int nextSpawnLocation = Random.Range(0, _spawnPoints.Length-1);

        Instantiate(_enemy, _spawnPoints[nextSpawnLocation].transform.position, Quaternion.identity);

        yield return new WaitForSeconds(_spawnTimer);

        if (!_gameManager._gameOver)
        {
            StartCoroutine(SpawnNextEnemy());
        }
    }IEnumerator SpawnRateIncrease()
    {
        yield return new WaitForSeconds(_spawnRateIncrease);

        if (_spawnTimer >= 0.5f)
        {
            _spawnTimer -= 0.2f;
        }

        StartCoroutine(SpawnRateIncrease());
    }
}


using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadSceneAsync("SampleScene");
    }
    public void QuitGame()
    {
        Application.Quit();
    }

}



using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] GameObject _PauseMenu;
    public void Pause()
    {
        _PauseMenu.SetActive(true);
        Time.timeScale = 0;
    }
    public void Home()
    {
        SceneManager.LoadScene("Main Menu");
        Time.timeScale = 1;
    }
    public void Resume()
    {
        _PauseMenu.SetActive(false);
        Time.timeScale = 1;
    }
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;
    }

}


using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverScript : MonoBehaviour
{
    public GameObject gameOverMenu; // Assign in Inspector
    private bool isGameOver = false; // ✅ Prevent multiple triggers

    private void Start()
    {
        gameOverMenu.SetActive(false); // ✅ Hide Game Over screen at the start
    }

    public void EnableGameOverMenu()
    {
        if (!isGameOver) // ✅ Prevent multiple triggers
        {
            isGameOver = true;
            gameOverMenu.SetActive(true); // ✅ Show the Game Over menu
            Time.timeScale = 0; // ✅ Pause the game
        }
    }

    public void Restart()
    {
        Time.timeScale = 1; // ✅ Reset time before reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Home()
    {
        Time.timeScale = 1; // ✅ Reset time before returning to the main menu
        SceneManager.LoadScene("Main Menu");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

