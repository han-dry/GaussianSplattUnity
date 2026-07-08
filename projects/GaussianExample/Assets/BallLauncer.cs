using UnityEngine;

public class BallLauncher : MonoBehaviour
{
    public Rigidbody ball;
    public Camera playerCamera;

    [Header("Launch")]
    public float launchForce = 8f;
    public float spawnDistance = 0.8f;
    public float maxAimDistance = 100f;
    public KeyCode launchKey = KeyCode.F;

    [Header("Random impulse")]
    public float randomImpulseForce = 5f;
    public KeyCode randomImpulseKey = KeyCode.G;
    public bool keepSomeUpwardBias = true;

    [Header("Raycast")]
    public LayerMask aimLayers = ~0;
    public bool ignoreTriggers = true;

    [Header("Visual feedback")]
    public bool showCrosshair = true;
    public bool showDebugRay = true;
    public bool showHitMarker = true;
    public float hitMarkerDuration = 0.5f;
    public float hitMarkerSize = 0.12f;
    public Color debugRayColor = Color.red;
    public Color hitMarkerColor = Color.yellow;
    public Color crosshairColor = Color.white;
    public float crosshairSize = 12f;

    private bool launchRequested;
    private bool randomImpulseRequested;

    private Vector3 lastHitPoint;
    private float hitMarkerTimer;
    private bool hasHitPoint;

    private GUIStyle crosshairStyle;

    void Update()
    {
        if (ball == null) return;

        if (Input.GetKeyDown(launchKey))
            launchRequested = true;

        if (Input.GetKeyDown(randomImpulseKey))
            randomImpulseRequested = true;

        if (hitMarkerTimer > 0f)
        {
            hitMarkerTimer -= Time.deltaTime;
            if (hitMarkerTimer <= 0f)
                hasHitPoint = false;
        }

        if (showHitMarker && hasHitPoint)
            DrawWorldHitMarker();
    }

    void FixedUpdate()
    {
        if (launchRequested)
        {
            launchRequested = false;
            RelaunchBallFromCameraCenter();
        }

        if (randomImpulseRequested)
        {
            randomImpulseRequested = false;
            AddRandomImpulse();
        }
    }

    void RelaunchBallFromCameraCenter()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null || ball == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        QueryTriggerInteraction triggerMode = ignoreTriggers
            ? QueryTriggerInteraction.Ignore
            : QueryTriggerInteraction.Collide;

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayers, triggerMode))
        {
            targetPoint = hit.point;
            lastHitPoint = hit.point;
            hasHitPoint = true;
            hitMarkerTimer = hitMarkerDuration;
        }
        else
        {
            targetPoint = ray.GetPoint(maxAimDistance);
            hasHitPoint = false;
        }

        Vector3 spawnPosition = playerCamera.transform.position + playerCamera.transform.forward * spawnDistance;
        Vector3 shotDirection = (targetPoint - spawnPosition).normalized;

        ball.velocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.isKinematic = true;
        ball.position = spawnPosition;
        ball.rotation = Quaternion.identity;
        ball.isKinematic = false;
        ball.WakeUp();

        ball.AddForce(shotDirection * launchForce, ForceMode.Impulse);

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxAimDistance, debugRayColor, 1.0f);
            Debug.DrawLine(spawnPosition, targetPoint, Color.green, 1.0f);
        }
    }

    void AddRandomImpulse()
    {
        if (ball == null) return;

        Vector3 randomDirection = Random.onUnitSphere;

        if (keepSomeUpwardBias && randomDirection.y < 0f)
            randomDirection.y *= -1f;

        randomDirection.Normalize();

        ball.WakeUp();
        ball.AddForce(randomDirection * randomImpulseForce, ForceMode.Impulse);
    }

    void DrawWorldHitMarker()
    {
        float s = hitMarkerSize;
        Debug.DrawLine(lastHitPoint - Vector3.right * s, lastHitPoint + Vector3.right * s, hitMarkerColor);
        Debug.DrawLine(lastHitPoint - Vector3.up * s, lastHitPoint + Vector3.up * s, hitMarkerColor);
        Debug.DrawLine(lastHitPoint - Vector3.forward * s, lastHitPoint + Vector3.forward * s, hitMarkerColor);
    }

    void SetupCrosshairStyle()
    {
        crosshairStyle = new GUIStyle(GUI.skin.label);
        crosshairStyle.alignment = TextAnchor.MiddleCenter;
        crosshairStyle.fontSize = 32;   // prova 32, 40 o 48
        crosshairStyle.normal.textColor = crosshairColor;
    }  

    void OnGUI()
    {
        if (!showCrosshair) return;

        if (crosshairStyle == null)
            SetupCrosshairStyle();

        float size = 40f; // area del rettangolo
        float x = (Screen.width - size) * 0.5f;
        float y = (Screen.height - size) * 0.5f;

        GUI.Label(new Rect(x, y, size, size), "+", crosshairStyle);

        crosshairStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.04f);
    }
}