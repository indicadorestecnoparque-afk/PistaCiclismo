using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text finishText;
    public bool timeActive;
    private float timeElapsed;
    private int minutes;
    private int seconds;
    private int cents;

    private void Update()
    {
        if (timeActive == true)
        {
            timeElapsed += Time.deltaTime;
            minutes = (int)(timeElapsed / 60f);
            seconds = (int)(timeElapsed - minutes * 60f);
            cents = (int)((timeElapsed - (int)timeElapsed) * 100f);
            //Muestra las variables de forma separada, indica que muestre 2 numeros enteros como minimo
            timerText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, cents);
            finishText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, cents);
        }
    }
}
