using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Assertions.Must;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject welcomeText;
    [SerializeField] private GameObject tutorialText;
    [SerializeField] private GameObject reverseText;
    [SerializeField] private BikeControllerSB bikeController;
    [SerializeField] private GameObject startTrigger;
    [SerializeField] private GameObject goalTrigger;
    [SerializeField] private GameObject bikeReverseStart;
    [SerializeField] private GameObject bike;
    [SerializeField] private Rigidbody bikeRB;
    [SerializeField] private Rigidbody sphereRB;
    public bool isPaused;
    public static bool reverse = false;


    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }
    }

    void Start()
    {
        StartCoroutine(TriggerSet());
    }
    public void PauseGame()
    {
        Time.timeScale = 0f;
        hudPanel.SetActive(false);
        pausePanel.SetActive(true);
        isPaused = true;
    }

    public void Continue()
    {
        Time.timeScale = 1f;
        hudPanel.SetActive(true);
        pausePanel.SetActive(false);
        isPaused = false;
    }

    public void ResetScene()
    {
        Time.timeScale = 1f;
        bikeController.inputAct.Enable();
        reverse = false;
        SceneManager.LoadScene("BikeTrack");
    }

    public void ReverseStart()
    {
        Time.timeScale = 1f;
        bikeController.inputAct.Enable();
        reverse = true;
        SceneManager.LoadScene("BikeTrack");
    }

    public IEnumerator TriggerSet()
    {
        if (reverse == true)
        {
            bikeController.enabled = false;
            yield return new WaitForEndOfFrame();
            yield return new WaitForFixedUpdate();

            Vector3 tempStartPos = startTrigger.transform.position;
            Quaternion tempStartRot = startTrigger.transform.rotation;

            startTrigger.transform.position = goalTrigger.transform.position;
            startTrigger.transform.rotation = goalTrigger.transform.rotation;
            goalTrigger.transform.position = tempStartPos;
            goalTrigger.transform.rotation = tempStartRot;

            Vector3 reverseBikePos = bikeReverseStart.transform.position;
            Quaternion reverseBikeRot = bikeReverseStart.transform.rotation;

            bikeRB.linearVelocity = Vector3.zero;
            bikeRB.angularVelocity = Vector3.zero;
            bikeRB.isKinematic = true;

            sphereRB.linearVelocity = Vector3.zero;
            sphereRB.angularVelocity = Vector3.zero;
            sphereRB.isKinematic = true;

            bike.transform.position = reverseBikePos;
            bike.transform.rotation = reverseBikeRot;

            if (bikeRB != null)
            {
                bikeRB.position = reverseBikePos;
                bikeRB.rotation = reverseBikeRot;
            }

            if (sphereRB != null)
            {
                sphereRB.position = reverseBikePos;
                sphereRB.rotation = reverseBikeRot;
            }

            bikeRB.isKinematic = false;
            sphereRB.isKinematic = false;
            bikeController.enabled = true;

            welcomeText.SetActive(false);
            reverseText.SetActive(true);
        }
        else
        {
            welcomeText.SetActive(true);
            tutorialText.SetActive(true);
        }
    }
    public void CloseGame()
    {
        Application.Quit();
    }
}
