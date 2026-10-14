using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class BycicleWifiController : MonoBehaviour
{
    [Header("Manubrio virtual")]
    public GameObject manubrio;
    public float anguloMaximoManubrio = 35f;
    public bool invertirManubrio = false;

    [Header("Conexion con ESP32-C3")]
    public string ipESP32 = "192.168.4.1";
    public int puertoESP32 = 4211;
    public int puertoUnity = 4210;

    [Header("Bicicleta de Unity")]
    public Rigidbody bicicleta;

    [Tooltip("Desactiva si otro script ya controla el movimiento.")]
    public bool controlarMovimiento = false;

    [Header("Movimiento con teclado")]
    public bool usarTecladoSinESP32 = true;
    public float aceleracion = 3f;
    public float velocidadMaximaTeclado = 8f;
    public float velocidadGiroTeclado = 70f;

    [Header("Calibracion de direccion")]
    public float anguloMaximoGiro = 35f;
    public bool invertirDireccion = false;

    [Header("Datos recibidos")]
    public bool conectado = false;
    public int vueltas = 0;
    public float velocidadKmh = 0f;
    public float angulo = 0f;
    public string direccion = "CENTRO";
    public bool frenoIzquierdo = false;
    public bool frenoDerecho = false;

    private UdpClient udp;
    private IPEndPoint destinoESP32;

    private float ultimoMensaje = -100f;
    private float ultimoHello = -100f;

    private float velocidadTeclado = 0f;
    private float giroTeclado = 0f;

    private string estado = "Iniciando";

    private void Start()
    {
        if (bicicleta == null)
            bicicleta = GetComponent<Rigidbody>();

        try
        {
            destinoESP32 = new IPEndPoint(
                IPAddress.Parse(ipESP32),
                puertoESP32
            );

            udp = new UdpClient(puertoUnity);
            udp.Client.Blocking = false;

            estado = "Esperando ESP32";
            EnviarHello();
        }
        catch (Exception e)
        {
            estado = "Error de conexion";
            Debug.LogError(
                "Error iniciando UDP: " + e.Message
            );
        }
    }

    private void Update()
    {
        RecibirDatos();

        if (Time.unscaledTime - ultimoHello >= 1f)
            EnviarHello();

        conectado = Time.unscaledTime - ultimoMensaje < 3f;

        if (conectado)
            estado = "ESP32 conectado";
        else
            estado = "Esperando datos del ESP32";

        if (!conectado && usarTecladoSinESP32)
            LeerTeclado();
        ActualizarManubrio();
    }

    private void EnviarHello()
    {
        if (udp == null || destinoESP32 == null)
            return;

        try
        {
            byte[] mensaje = Encoding.ASCII.GetBytes("HELLO");
            udp.Send(mensaje, mensaje.Length, destinoESP32);
            ultimoHello = Time.unscaledTime;
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "Error enviando HELLO: " + e.Message
            );
        }
    }

    private void RecibirDatos()
    {
        if (udp == null)
            return;

        try
        {
            while (udp.Available > 0)
            {
                IPEndPoint origen = new IPEndPoint(
                    IPAddress.Any,
                    0
                );

                byte[] bytes = udp.Receive(ref origen);
                string mensaje = Encoding.ASCII.GetString(bytes);

                ProcesarMensaje(mensaje);
            }
        }
        catch (SocketException e)
        {
            if (e.SocketErrorCode != SocketError.WouldBlock &&
                e.SocketErrorCode != SocketError.IOPending)
            {
                Debug.LogWarning(
                    "Error recibiendo UDP: " + e.Message
                );
            }
        }
        catch (ObjectDisposedException)
        {
            // El socket ya se ha cerrado.
        }
    }

    private void ProcesarMensaje(string mensaje)
    {
        string[] campos = mensaje.Trim().Split(',');

        // Formato esperado:
        // DATA,vueltas,velocidad,angulo,direccion,frenoIzq,frenoDer

        if (campos.Length < 7 || campos[0] != "DATA")
            return;

        try
        {
            vueltas = int.Parse(
                campos[1],
                CultureInfo.InvariantCulture
            );

            velocidadKmh = float.Parse(
                campos[2],
                CultureInfo.InvariantCulture
            );

            angulo = float.Parse(
                campos[3],
                CultureInfo.InvariantCulture
            );

            direccion = campos[4];
            frenoIzquierdo = campos[5] == "1";
            frenoDerecho = campos[6] == "1";

            ultimoMensaje = Time.unscaledTime;
            conectado = true;
        }
        catch (FormatException)
        {
            Debug.LogWarning(
                "Paquete recibido con formato incorrecto: " + mensaje
            );
        }
        catch (OverflowException)
        {
            Debug.LogWarning(
                "Un valor recibido esta fuera de rango: " + mensaje
            );
        }
    }

    private void LeerTeclado()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        giroTeclado = horizontal;

        if (Input.GetKey(KeyCode.Space))
        {
            velocidadTeclado = 0f;
        }
        else
        {
            velocidadTeclado +=
                vertical * aceleracion * Time.deltaTime;

            velocidadTeclado = Mathf.Clamp(
                velocidadTeclado,
                0f,
                velocidadMaximaTeclado
            );
        }
    }

    private void ActualizarManubrio()
    {
        if (!conectado || manubrio == null)
            return;

        float anguloManubrio = Mathf.Clamp(
            angulo,
            -anguloMaximoManubrio,
            anguloMaximoManubrio
        );

        if (invertirManubrio)
            anguloManubrio = -anguloManubrio;

        // Giro local alrededor del eje vertical del manubrio.
        Quaternion rotacion = Quaternion.Euler(manubrio.transform.localRotation.x, manubrio.transform.localRotation.y, manubrio.transform.localRotation.z);

        rotacion.y = anguloManubrio;

        manubrio.transform.localRotation = rotacion;
    }
    private void FixedUpdate()
    {
        if (!controlarMovimiento || bicicleta == null)
            return;

        float velocidadObjetivo;
        float giro;

        if (conectado)
        {
            // Convertir km/h a m/s.
            velocidadObjetivo = velocidadKmh / 3.6f;

            if (frenoIzquierdo || frenoDerecho)
                velocidadObjetivo = 0f;

            giro = Mathf.Clamp(
                angulo / Mathf.Max(anguloMaximoGiro, 1f),
                -1f,
                1f
            );

            if (invertirDireccion)
                giro = -giro;
        }
        else if (usarTecladoSinESP32)
        {
            velocidadObjetivo = velocidadTeclado;
            giro = giroTeclado;
        }
        else
        {
            return;
        }

        // Girar la bicicleta.
        Quaternion rotacion =
            Quaternion.Euler(
                0f,
                giro * velocidadGiroTeclado * Time.fixedDeltaTime,
                0f
            );

        bicicleta.MoveRotation(
            bicicleta.rotation * rotacion
        );

        // Movimiento hacia adelante.
        Vector3 nuevaVelocidad =
            bicicleta.transform.forward * velocidadObjetivo;

        bicicleta.linearVelocity = nuevaVelocidad;
    }
    private void OnGUI()
    {
        GUI.Box(
            new Rect(10, 10, 290, 205),
            "Monitor de bicicleta"
        );

        GUI.Label(
            new Rect(20, 35, 270, 22),
            "Conexion: " + estado
        );

        GUI.Label(
            new Rect(20, 58, 270, 22),
            "Vueltas: " + vueltas
        );

        GUI.Label(
            new Rect(20, 81, 270, 22),
            "Velocidad: " + velocidadKmh.ToString("F1") + " km/h"
        );

        GUI.Label(
            new Rect(20, 104, 270, 22),
            "Angulo: " + angulo.ToString("F1") + " grados"
        );

        GUI.Label(
            new Rect(20, 127, 270, 22),
            "Direccion: " + direccion
        );

        GUI.Label(
            new Rect(20, 150, 270, 22),
            "Freno izquierdo: " +
            (frenoIzquierdo ? "ACTIVADO" : "LIBRE")
        );

        GUI.Label(
            new Rect(20, 173, 270, 22),
            "Freno derecho: " +
            (frenoDerecho ? "ACTIVADO" : "LIBRE")
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

    private void OnApplicationQuit()
    {
        if (udp != null)
        {
            udp.Close();
            udp = null;
        }
    }
}
