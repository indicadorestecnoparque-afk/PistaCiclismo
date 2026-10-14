using TMPro;
using UnityEngine;

public class StartTrigger : MonoBehaviour
{
    [SerializeField] private Timer timerClock;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text welcomeText;
    [SerializeField] private TMP_Text reverseText;
    [SerializeField] private TMP_Text tutorialText;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && timerClock.timeActive == false)
        {
            timerText.gameObject.SetActive(true);
            speedText.gameObject.SetActive(true);
            welcomeText.gameObject.SetActive(false);
            tutorialText.gameObject.SetActive(false);
            reverseText.gameObject.SetActive(false);
            timerClock.timeActive = true;
            Debug.Log("Tiempo activado");
        }
    }
}
