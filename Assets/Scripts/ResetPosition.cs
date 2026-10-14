using UnityEngine;

public class ResetPosition : MonoBehaviour
{
    //Atributos de la bici
    private BikeControllerSB bikeController;
    private Rigidbody sphereRB;
    private Rigidbody bikeRB;
    [SerializeField] private GameObject bikeMain;
    [SerializeField] private GameObject bike;
    [SerializeField] private GameObject sphere;
    [SerializeField] private GameManager gameManager;

    //Atributos del detector de colision
    [SerializeField] private float detectRatio = 2f;
    [SerializeField] private float raycastHeight = 5f;
    [SerializeField] private float floorGap = 0.5f;
    [SerializeField] private int scanAttempts = 8;
    [SerializeField] private float stepDistance = 3f;
    [SerializeField] private float maxAngle = 45f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private LayerMask floorLayer;



    private void Start()
    {
        bikeController = bikeMain.GetComponent<BikeControllerSB>();
        sphereRB = sphere.GetComponent<Rigidbody>();
        bikeRB = bike.GetComponent<Rigidbody>();
    }

    public void ResetAction()
    {

        if (gameManager.isPaused == false)
        {

            //Reinicia la ubicacion de la bici
            bikeController.enabled = false;

            bikeRB.linearVelocity = Vector3.zero;
            bikeRB.angularVelocity = Vector3.zero;
            bikeRB.isKinematic = true;

            sphereRB.linearVelocity = Vector3.zero;
            sphereRB.angularVelocity = Vector3.zero;
            sphereRB.isKinematic = true;

            Vector3 stuckPosition = sphere.transform.position;
            Vector3 lookDirection = bike.transform.forward;
            Vector3 targetPosition = stuckPosition;
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z).normalized);
            bool safeSpot = false;

            for (int i = 0; i <= scanAttempts; i++)
            {
                float searchRatio = i * stepDistance;

                Vector3 zStep = bike.transform.forward * searchRatio;
                Vector3 xStep = bike.transform.right * searchRatio;

                Vector3[] spots =
                {
                stuckPosition,
                stuckPosition + zStep,
                stuckPosition - zStep,
                stuckPosition + xStep,
                stuckPosition - xStep,
                stuckPosition + (zStep + xStep).normalized * searchRatio,
                stuckPosition + (-zStep - xStep).normalized * searchRatio
            };

                foreach (Vector3 spot in spots)
                {
                    Vector3 raycastOrigin = spot + Vector3.up * raycastHeight;

                    if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit floorHit, raycastHeight * 2f, floorLayer))
                    {
                        float floorAngle = Vector3.Angle(floorHit.normal, Vector3.up);
                        if (floorAngle <= maxAngle)
                        {
                            Vector3 idealPosition = floorHit.point + Vector3.up * floorGap;

                            if (!LockState(idealPosition) || !Physics.Raycast(sphereRB.position, Vector3.down))
                            {
                                targetPosition = idealPosition;
                                Quaternion yBaseRot = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z).normalized);
                                targetRotation = Quaternion.FromToRotation(transform.up, floorHit.normal) * yBaseRot;
                                safeSpot = true;
                                break;
                            }
                        }
                    }
                    if (safeSpot)
                    {
                        break;
                    }
                }

                if (!safeSpot)
                {
                    targetPosition = stuckPosition + Vector3.up * 2f;
                    targetRotation = Quaternion.LookRotation(new Vector3(lookDirection.x, 0, lookDirection.z).normalized);
                }
                bikeMain.transform.position = targetPosition;
                bikeMain.transform.rotation = targetRotation;

                sphere.transform.position = targetPosition;
                sphere.transform.rotation = targetRotation;
                sphereRB.position = targetPosition;
                sphereRB.rotation = targetRotation;

                bike.transform.position = targetPosition;
                bike.transform.rotation = targetRotation;
                bikeRB.position = targetPosition;
                bikeRB.rotation = targetRotation;


                sphereRB.isKinematic = false;
                bikeRB.isKinematic = false;

                sphereRB.linearVelocity = Vector3.zero;
                sphereRB.angularVelocity = Vector3.zero;
                bikeRB.linearVelocity = Vector3.zero;
                bikeRB.angularVelocity = Vector3.zero;

                bikeController.enabled = true;
            }
        }
    }

    private bool LockState(Vector3 position)
    {
        Collider[] hits = Physics.OverlapSphere(position, detectRatio, obstacleLayer);
        return hits.Length > 0;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(sphere.transform.position, detectRatio);
    }
}
