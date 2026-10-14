using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class Speedometer : MonoBehaviour
{
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float scaleFactor;  //numero para dividir el valor de la velocidad a uno mas realista 
    //El valor minimo debe ser 1
    private float speed;


    private void Update()
    {
        if (scaleFactor == 0)
        {
            scaleFactor = 1;
        }
        speed = Mathf.Round(rb.linearVelocity.magnitude * 3.6f / scaleFactor);
        speedText.text = speed + "Km/h";
    }
}
