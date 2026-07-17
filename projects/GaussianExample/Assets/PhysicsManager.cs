/*
using UnityEngine;
using GaussianSplatting.Runtime;

[ExecuteInEditMode]
public class GaussianWindManager_v1 : MonoBehaviour
{
    [Header("Riferimenti")]
    public GaussianSplatRenderer targetRenderer;
    public Shader windShader; // Trascina qui il file "Render Splats Wind.shader"

    [Header("Parametri Vento (Attivo solo in Play Mode)")]
    [Range(0f, 5f)] public float windSpeed = 1.5f;
    [Range(0f, 2f)] public float windFrequency = 0.4f;
    [Range(0f, 1f)] public float windIntensity = 0.2f;
    public Vector3 windDirection = new Vector3(1f, 0f, 0.5f);

    private Shader _originalShader;

    void OnEnable()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<GaussianSplatRenderer>();
        }
            

        // Salviamo lo shader originale per poterlo ripristinare
        if (targetRenderer != null)
        {
            _originalShader = targetRenderer.m_ShaderSplats;
        }
    }

    void Update()
    {
        if (targetRenderer == null) return;

        if (Application.isPlaying)
        {
            // Forza l'assegnazione dello shader per il vento
            if (windShader != null && targetRenderer.m_ShaderSplats != windShader)
            {
                targetRenderer.m_ShaderSplats = windShader;
            }

            // Inviamo i parametri di controllo alla GPU
            Shader.SetGlobalFloat("_WindSpeed", windSpeed);
            Shader.SetGlobalFloat("_WindFrequency", windFrequency);
            Shader.SetGlobalFloat("_WindIntensity", windIntensity);
            Shader.SetGlobalVector("_WindDirection", windDirection.normalized);
            Shader.SetGlobalFloat("_TimeGlobal", Time.time);
        }
        else
        {
            if (_originalShader != null && targetRenderer.m_ShaderSplats != _originalShader)
            {
                targetRenderer.m_ShaderSplats = _originalShader;
            }
            Shader.SetGlobalFloat("_WindIntensity", 0f);
        }
    }

    void OnDisable()
    {
        // Ripristino di sicurezza quando lo script viene disattivato o rimosso
        if (targetRenderer != null && _originalShader != null)
        {
            targetRenderer.m_ShaderSplats = _originalShader;
        }
        Shader.SetGlobalFloat("_WindIntensity", 0f);
    }
}
*/

using UnityEngine;

[System.Serializable]
public class WindModule
{

    [Header("Wind Activation")]
    [ExposeToUI("Wind Enabled")]
    [Tooltip("Enable or disable the wind effect on the splats.")]
    public bool enabled = true;

    [Header("Wind Primary Parameters")]
    [Tooltip("Speed of the wind affecting the splats.")]
    [ExposeToUI("Wind Speed", 0f, 10f)]
    [Range(0f, 10f)]
    public float windSpeed = 5f;

    [Tooltip("Intensity of the wind affecting the splats.")]
    [ExposeToUI("Wind Intensity", 0f, 1f)]
    [Range(0f, 1f)]
    public float windIntensity = 0.1f;

    [Tooltip("Direction of the wind affecting the splats.")]
    [ExposeToUI("Wind Direction")]
    public Vector3 windDirection = new Vector3(1.0f, 0.0f, 0.5f);

    [Header("Advanced Wind Settings")]
    [Tooltip("Intensity of the rotation/bending of the splat.")]
    [Range(0f, 2f)]
    [ExposeToUI("Wind Bending", 0f, 2f)]
    public float windBending = 0.5f;

    [Tooltip("Green threshold: higher values limit the movement to only the purest leaves.")]
    [Range(0f, 0.5f)]
    [ExposeToUI("Wind Edge Cutoff", 0f, 0.5f)]
    public float windEdgeCutoff = 0.1f;

    [Tooltip("Frequency of the wind gusts (creates more jagged and realistic movements).")]
    [Range(0f, 10f)]
    [ExposeToUI("Wind Turbulence", 0f, 10f)]
    public float windTurbulence = 4.0f;

    public void UpdateShader(Matrix4x4 worldToCameraMatrix)
    {
        if (enabled)
        {
            Shader.SetGlobalFloat("_WindActive", 1.0f);

            Shader.SetGlobalFloat("_TimeGlobal", Time.time);
            Shader.SetGlobalFloat("_WindSpeed", windSpeed);
            Shader.SetGlobalFloat("_WindIntensity", windIntensity);
            
            // Passiamo i nuovi parametri esposti nell'Inspector
            Shader.SetGlobalFloat("_WindBending", windBending);
            Shader.SetGlobalFloat("_WindEdgeCutoff", windEdgeCutoff);
            Shader.SetGlobalFloat("_WindTurbulence", windTurbulence);

            Vector3 viewWindDir = worldToCameraMatrix.MultiplyVector(windDirection.normalized);
            Shader.SetGlobalVector("_WindDirection", new Vector4(viewWindDir.x, viewWindDir.y, viewWindDir.z, 0));
        }
        else
        {
            Shader.SetGlobalFloat("_WindActive", 0.0f);
        }
    }
}

[System.Serializable]
public class RainModule
{
    public bool enabled = false;
    [Range(0f, 1f)] public float intensity = 0.5f;
    public float wetness = 0.8f;

    public void UpdateShader()
    {
        if (enabled)
        {
            Shader.SetGlobalFloat("_RainActive", 1.0f);
            Shader.SetGlobalFloat("_RainIntensity", intensity);
            Shader.SetGlobalFloat("_RainWetness", wetness);
        }
        else
        {
            Shader.SetGlobalFloat("_RainActive", 0.0f);
        }
    }
}

public class PhysicsManager : MonoBehaviour
{
    [Header("Ambient Effects")]
    [Tooltip("Configuration and shader updates for wind simulation.")]
    public WindModule wind = new WindModule(); // Initialized to prevent NullReferenceException

    [Tooltip("Configuration and shader updates for rain simulation.")]
    public RainModule rain = new RainModule(); // Initialized to prevent NullReferenceException

    void Update()
    {
        Camera cam = Camera.main;
        Matrix4x4 viewMatrix = cam != null ? cam.worldToCameraMatrix : Matrix4x4.identity;

        // Update the individual modules, passing the necessary data
        if (wind != null) wind.UpdateShader(viewMatrix);
        if (rain != null) rain.UpdateShader();
    }

    void OnDisable()
    {
        // Force the shaders to reset when the script is disabled or when exiting Play Mode
        Shader.SetGlobalFloat("_WindActive", 0.0f);
        Shader.SetGlobalFloat("_RainActive", 0.0f);
    }
}