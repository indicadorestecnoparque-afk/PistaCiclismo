using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform bike;
    [SerializeField] private Vector3 cameraFollow = new Vector3(0f, 0f, 0f);

    [SerializeField] private GameManager gameManager;
    [SerializeField] private BikeControllerSB bikeController;

    public InputActionAsset inputAct;
    private InputAction lookAction;
    [SerializeField] private float mouseSensitivity;
    [SerializeField] private float xAngleLimit;
    [SerializeField] private float yMinAngleLimit;
    [SerializeField] private float yMaxAngleLimit;

    private float xRotation;
    private float yRotation;

    void Awake()
    {
        lookAction = InputSystem.actions.FindAction("Look");
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false; //Oculta el mouse, se puede anular con la tecla esc.
    }

    void Update()
    {
        if (gameManager.isPaused == true || bikeController.inputAct.enabled == false)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (gameManager.isPaused == false)
        {
            Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
            xRotation -= mouseDelta.y * mouseSensitivity;
            yRotation += mouseDelta.x * mouseSensitivity;

            xRotation = Mathf.Clamp(xRotation, yMinAngleLimit, yMaxAngleLimit);
            yRotation = Mathf.Clamp(yRotation, -xAngleLimit, xAngleLimit);

            transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }
    }

    private void LateUpdate()
    {
        Vector3 cameraPosition = bike.TransformPoint(cameraFollow);
        transform.position = cameraPosition; //Permite que la camara siga la bici desde una posicion similar a los ojos del ciclista
        Quaternion cameraRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        transform.rotation = bike.rotation * cameraRotation;
    }
}
