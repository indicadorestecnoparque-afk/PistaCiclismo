using TMPro;
using UnityEngine;
using System.Collections;

public class LimitTrigger : MonoBehaviour
{

    [SerializeField] private TMP_Text warningText;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private BikeControllerSB bikeController;
    [SerializeField] private GameObject bike;
    [SerializeField] private Rigidbody bikeRB;
    [SerializeField] private Rigidbody sphereRB;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(ResetPosition());
        }
    }

    public IEnumerator ResetPosition()
    {
        bikeController.inputAct.Disable();
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(2.5f);

        bikeRB.linearVelocity = Vector3.zero;
        bikeRB.angularVelocity = Vector3.zero;
        bikeRB.isKinematic = true;

        sphereRB.linearVelocity = Vector3.zero;
        sphereRB.angularVelocity = Vector3.zero;
        sphereRB.isKinematic = true;


        Vector3 spawnPos = spawnPoint.transform.position;
        Quaternion spawnRot = spawnPoint.transform.rotation;

        bike.transform.position = spawnPos;
        bike.transform.rotation = spawnRot;
        bikeRB.position = spawnPos;
        bikeRB.rotation = spawnRot;
        sphereRB.position = spawnPos;
        sphereRB.rotation = spawnRot;

        bikeRB.isKinematic = false;
        bikeRB.linearVelocity = Vector3.zero;
        bikeRB.angularVelocity = Vector3.zero;

        sphereRB.isKinematic = false;
        sphereRB.linearVelocity = Vector3.zero;
        sphereRB.angularVelocity = Vector3.zero;

        bikeController.inputAct.Enable();
        warningText.gameObject.SetActive(false);
    }
}
