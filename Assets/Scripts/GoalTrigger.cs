using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private Timer timerClock;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private BikeControllerSB bikeController;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger activado");
        if (other.CompareTag("Player") && timerClock.timeActive == true)
        {
            timerClock.timeActive = false;
            Debug.Log("Tiempo desactivado");
            clearPanel.gameObject.SetActive(true);
            speedText.gameObject.SetActive(false);
            timerText.gameObject.SetActive(false);
            bikeController.inputAct.Disable();
        }
    }
}
