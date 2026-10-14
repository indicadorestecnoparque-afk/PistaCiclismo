using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

public class BikeControllerSB : MonoBehaviour
{
    [Header("Controles originales")]
    public InputActionAsset inputAct;

    private InputAction moveAction;
    private InputAction turnAction;
    private InputAction brakeAction;
    private InputAction sprintAction;
    private InputAction resetAction;
    private InputAction pauseAction;

    [SerializeField] private GameManager gameManager;
    [SerializeField] private ResetPosition resetManager;

    private Vector2 movement;
    private Vector2 turnRotation;
    private bool brakeStop;
    private bool sprintStart;

    private float rayLength;
    private RaycastHit hit;
    private Animator anim;

    [Header("Fisica original")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float baseMaxSpeed = 10f;
    [SerializeField] private float speedIncrease = 3f;
    [SerializeField] private float acceleration = 1f;
    [SerializeField] private float turnStrength = 1f;
    [Range(1, 10)]
    [SerializeField] private float brakeSmoothness = 5f;
    [SerializeField] private float gravity = 9.81f;
    [SerializeField] private float xTiltIncrease = 5f;
    [SerializeField] private float zTilt = 10f;
    [SerializeField] private float currentVelocity;
    [SerializeField] private float handleRotation = 30f;
    [SerializeField] private float handleSpeed = 8f;
    [SerializeField] private float tiltSmoothness;
    [SerializeField] private float tiltSpeed = 5f;

    private Vector3 velocity;

    public Rigidbody sphereRB;
    public Rigidbody bikeRB;
    public GameObject handle;
    public LayerMask derivableSurface;

    [Header("WiFi ESP32-C3")]
    [SerializeField] private bool usarESP32 = true;
    [SerializeField] private string ipESP32 = "192.168.4.1";
    [SerializeField] private int puertoESP32 = 4211;
    [SerializeField] private int puertoUnity = 4210;

    [Tooltip("Velocidad fisica que corresponde a aceleracion virtual completa.")]
    [SerializeField] private float velocidadMaximaPedaleoKmh = 25f;

    [Tooltip("Desaceleracion al dejar de pedalear, en unidades por segundo.")]
    [SerializeField] private float desaceleracionSinPedal = 2f;

    [Tooltip("Invierte el sentido del MPU si el manubrio gira al reves.")]
    [SerializeField] private bool invertirDireccionMPU = false;

    [Tooltip("Angulo fisico del MPU que corresponde al giro completo del manubrio.")]
    [SerializeField] private float anguloMaximoMPU = 35f;

    private UdpClient udp;
    private IPEndPoint destinoESP32;
    private float ultimoHello;

    private bool esp32Conectado;
    private bool recibioDatos;

    private int vueltasESP32;
    private float velocidadRuedaKmh;
    private float anguloMPU;
    private bool frenoIzqESP32;
    private bool frenoDerESP32;

    private Quaternion rotacionInicialManubrio;

    // =================================================
    // INICIALIZACION
    // =================================================
    private void Awake()
    {
        anim = bikeRB != null
            ? bikeRB.GetComponent<Animator>()
            : null;

        moveAction = InputSystem.actions.FindAction("Move");
        turnAction = InputSystem.actions.FindAction("Turn");
        brakeAction = InputSystem.actions.FindAction("Brake");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        resetAction = InputSystem.actions.FindAction("Reset");
        pauseAction = InputSystem.actions.FindAction("Pause");
    }

    private void Start()
    {
        baseMaxSpeed = maxSpeed;

        if (sphereRB != null)
        {
            sphereRB.transform.parent = null;

            SphereCollider sphereCollider =
                sphereRB.GetComponent<SphereCollider>();

            if (sphereCollider != null)
                rayLength = sphereCollider.radius + 0.2f;
            else
                rayLength = 1f;
        }

        if (bikeRB != null)
            bikeRB.transform.parent = null;

        if (handle != null)
            rotacionInicialManubrio = handle.transform.localRotation;

        if (usarESP32)
            IniciarUDP();
    }

    private void IniciarUDP()
    {
        try
        {
            destinoESP32 = new IPEndPoint(
                IPAddress.Parse(ipESP32),
                puertoESP32
            );

            udp = new UdpClient(puertoUnity);
            udp.Client.Blocking = false;

            EnviarHello();
        }
        catch (Exception e)
        {
            Debug.LogError("No se pudo iniciar UDP: " + e.Message);
        }
    }

    private void EnviarHello()
    {
        if (udp == null || destinoESP32 == null)
            return;

        try
        {
            byte[] datos = Encoding.UTF8.GetBytes("HELLO");
            udp.Send(datos, datos.Length, destinoESP32);
            ultimoHello = Time.unscaledTime;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error enviando HELLO: " + e.Message);
        }
    }

    // =================================================
    // RECIBIR DATOS ESP32
    // =================================================
    private void LeerUDP()
    {
        if (udp == null)
            return;

        try
        {
            while (udp.Available > 0)
            {
                IPEndPoint origen = new IPEndPoint(
                    IPAddress.Any, 0
                );

                byte[] datos = udp.Receive(ref origen);

                string mensaje = Encoding.UTF8.GetString(datos);
                string[] partes = mensaje.Trim().Split(',');

                if (partes.Length < 7 || partes[0] != "DATA")
                    continue;

                int vueltas;
                float velocidad;
                float angulo;
                int frenoIzq;
                int frenoDer;

                if (!int.TryParse(partes[1], out vueltas))
                    continue;

                if (!float.TryParse(
                    partes[2],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out velocidad))
                    continue;

                if (!float.TryParse(
                    partes[3],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out angulo))
                    continue;

                if (!int.TryParse(partes[5], out frenoIzq))
                    continue;

                if (!int.TryParse(partes[6], out frenoDer))
                    continue;

                vueltasESP32 = vueltas;
                velocidadRuedaKmh = Mathf.Max(0f, velocidad);
                anguloMPU = angulo;

                frenoIzqESP32 = frenoIzq == 1;
                frenoDerESP32 = frenoDer == 1;

                recibioDatos = true;
                esp32Conectado = true;
            }
        }
        catch (SocketException)
        {
            // No hay paquetes disponibles en este instante.
        }
        catch (Exception e)
        {
            Debug.LogWarning("Error leyendo ESP32: " + e.Message);
        }
    }

    // =================================================
    // UPDATE
    // =================================================
    private void Update()
    {
        if (usarESP32)
        {
            LeerUDP();

            if (Time.unscaledTime - ultimoHello >= 1f)
                EnviarHello();
        }

        // Conserva los controles de teclado originales.
        if (moveAction != null)
            movement = moveAction.ReadValue<Vector2>();

        if (turnAction != null)
            turnRotation = turnAction.ReadValue<Vector2>();

        bool frenoTeclado =
            brakeAction != null && brakeAction.IsPressed();

        sprintStart =
            sprintAction != null && sprintAction.IsPressed();

        // Si hay comunicacion con ESP32, los pedales fisicos
        // reemplazan la entrada de aceleracion del teclado.
        if (usarESP32 && esp32Conectado && recibioDatos)
        {
            float aceleracionFisica = Mathf.Clamp01(
                velocidadRuedaKmh /
                Mathf.Max(1f, velocidadMaximaPedaleoKmh)
            );

            movement.y = aceleracionFisica;

            float anguloControl = invertirDireccionMPU
                ? -anguloMPU
                : anguloMPU;

            turnRotation.x = Mathf.Clamp(
                anguloControl / Mathf.Max(1f, anguloMaximoMPU),
                -1f,
                1f
            );

            brakeStop =
                frenoTeclado ||
                frenoIzqESP32 ||
                frenoDerESP32;
        }
        else
        {
            // Si ESP32 no esta conectado, se mantienen
            // los controles originales.
            brakeStop = frenoTeclado;
        }

        ActualizarManubrio();

        if (resetAction != null &&
            resetAction.WasPressedThisFrame() &&
            resetManager != null)
        {
            resetManager.ResetAction();
        }

        if (pauseAction != null &&
            pauseAction.WasPressedThisFrame() &&
            gameManager != null)
        {
            if (!gameManager.isPaused)
                gameManager.PauseGame();
            else
                gameManager.Continue();
        }
    }

    private void ActualizarManubrio()
    {
        if (handle == null)
            return;

        Quaternion objetivo =
            rotacionInicialManubrio *
            Quaternion.Euler(
                0f,
                handleRotation * turnRotation.x,
                0f
            );

        handle.transform.localRotation = Quaternion.Slerp(
            handle.transform.localRotation,
            objetivo,
            handleSpeed * Time.deltaTime
        );
    }

    // =================================================
    // FISICA
    // =================================================
    private void FixedUpdate()
    {
        if (sphereRB == null || bikeRB == null)
            return;

        transform.position = sphereRB.position;

        Move();

        velocity = bikeRB.transform.InverseTransformDirection(
            bikeRB.linearVelocity
        );

        currentVelocity = Mathf.Clamp(
            velocity.z / Mathf.Max(0.1f, baseMaxSpeed),
            -2f,
            2f
        );

        // Al dejar de pedalear, desacelera gradualmente.
        // Solo se aplica este comportamiento con el ESP32 activo.
        if (usarESP32 &&
            esp32Conectado &&
            recibioDatos &&
            velocidadRuedaKmh < 0.2f &&
            !brakeStop)
        {
            Vector3 velocidadLocal =
                transform.InverseTransformDirection(
                    sphereRB.linearVelocity
                );

            velocidadLocal.z = Mathf.MoveTowards(
                velocidadLocal.z,
                0f,
                desaceleracionSinPedal * Time.fixedDeltaTime
            );

            sphereRB.linearVelocity =
                transform.TransformDirection(velocidadLocal);
        }

        Vector3 localVel =
            transform.InverseTransformDirection(
                sphereRB.linearVelocity
            );

        localVel.x = 0f;

        sphereRB.linearVelocity =
            transform.TransformDirection(localVel);
    }

    private void Move()
    {
        if (Grounded())
        {
            Turn();

            if (!brakeStop)
            {
                MoveForward();
                Accel();
            }

            Brake();
        }
        else
        {
            Gravity();
        }

        BikeTilt();
    }

    private void MoveForward()
    {
        Vector3 forceDirection = transform.forward;

        if (Grounded())
        {
            forceDirection = Vector3.ProjectOnPlane(
                transform.forward,
                hit.normal
            ).normalized;
        }

        Vector3 totalForce = movement.y * forceDirection;

        Vector3 localVelocity =
            transform.InverseTransformDirection(
                sphereRB.linearVelocity
            );

        if (localVelocity.z < maxSpeed &&
            Mathf.Abs(movement.y) > 0.01f)
        {
            float currentAccel = sprintStart
                ? acceleration * 1.5f
                : acceleration;

            sphereRB.AddForce(
                totalForce * moveSpeed * currentAccel,
                ForceMode.Acceleration
            );
        }

        if (anim != null)
            anim.SetFloat("Move", movement.y);
    }

    private void Turn()
    {
        float direction = currentVelocity >= 0f ? 1f : -1f;

        float turnSpeed = 1f /
            Mathf.Clamp(Mathf.Abs(currentVelocity), 1f, 3.5f);

        float yTurnAngle =
            turnRotation.x *
            direction *
            turnSpeed *
            turnStrength *
            100f *
            Time.fixedDeltaTime;

        if (Mathf.Abs(currentVelocity) < 0.08f)
            yTurnAngle = 0f;

        transform.Rotate(0f, yTurnAngle, 0f, Space.World);

        if (anim != null)
            anim.SetFloat("Rotate", turnRotation.x);
    }

    private void Brake()
    {
        if (brakeStop)
        {
            sphereRB.linearVelocity *= brakeSmoothness / 10f;

            if (anim != null)
                anim.SetBool("Brake", true);
        }
        else
        {
            if (anim != null)
                anim.SetBool("Brake", false);
        }
    }

    private bool Grounded()
    {
        return Physics.Raycast(
            sphereRB.position,
            Vector3.down,
            out hit,
            rayLength,
            derivableSurface
        );
    }

    private void Gravity()
    {
        sphereRB.AddForce(
            gravity * Vector3.down,
            ForceMode.Acceleration
        );
    }

    private void BikeTilt()
    {
        if (!Grounded())
            return;

        float xRotation = (
            Quaternion.FromToRotation(
                bikeRB.transform.up,
                hit.normal
            ) * bikeRB.transform.rotation
        ).eulerAngles.x;

        float zRotation = 0f;
        float zValue = turnRotation.x;

        tiltSmoothness = Mathf.MoveTowards(
            tiltSmoothness,
            zValue,
            Time.fixedDeltaTime * tiltSpeed
        );

        if (currentVelocity > 0f)
        {
            zRotation =
                -zTilt * tiltSmoothness * currentVelocity;
        }

        Quaternion targetRotation = Quaternion.Euler(
            xRotation,
            transform.eulerAngles.y,
            zRotation
        );

        Quaternion newRotation = Quaternion.Slerp(
            bikeRB.transform.rotation,
            targetRotation,
            xTiltIncrease * Time.fixedDeltaTime
        );

        bikeRB.MoveRotation(newRotation);
    }

    private void Accel()
    {
        float limitSpeed = sprintStart
            ? baseMaxSpeed + speedIncrease
            : baseMaxSpeed;

        maxSpeed = Mathf.MoveTowards(
            maxSpeed,
            limitSpeed,
            15f * Time.fixedDeltaTime
        );
    }

    // =================================================
    // DATOS PARA VERIFICAR LA CONEXION EN UNITY
    // =================================================
    private void OnGUI()
    {
        if (!usarESP32)
            return;

        GUI.Label(
            new Rect(10, 10, 320, 25),
            "ESP32: " + (esp32Conectado ? "CONECTADO" : "ESPERANDO")
        );

        GUI.Label(
            new Rect(10, 35, 320, 25),
            "Vueltas: " + vueltasESP32
        );

        GUI.Label(
            new Rect(10, 60, 320, 25),
            "Velocidad rueda: " +
            velocidadRuedaKmh.ToString("F2") + " km/h"
        );

        GUI.Label(
            new Rect(10, 85, 320, 25),
            "Angulo MPU: " + anguloMPU.ToString("F1")
        );

        GUI.Label(
            new Rect(10, 110, 320, 25),
            "Freno izquierdo: " + (frenoIzqESP32 ? "ACTIVO" : "LIBRE")
        );

        GUI.Label(
            new Rect(10, 135, 320, 25),
            "Freno derecho: " + (frenoDerESP32 ? "ACTIVO" : "LIBRE")
        );
    }

    private void OnDestroy()
    {
        if (udp != null)
        {
            udp.Close();
            udp = null;
        }
    }
}