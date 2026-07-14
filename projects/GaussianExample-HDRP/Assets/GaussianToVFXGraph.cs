using UnityEngine;
using UnityEngine.VFX;
using System.Reflection; // <--- Necessario per BindingFlags e FieldInfo
using GaussianSplatting.Runtime;

[ExecuteInEditMode]
public class GaussianToVFXGraph : MonoBehaviour
{
    [Header("Componenti")]
    public GaussianSplatRenderer arasRenderer;
    public VisualEffect vfxGraph;

    [Header("Parametri Vento")]
    [Range(0f, 5f)] public float windSpeed = 1.5f;
    [Range(0f, 2f)] public float windFrequency = 0.4f;
    [Range(0f, 1f)] public float windIntensity = 0.15f;
    public Vector3 windDirection = new Vector3(1f, 0f, 0.5f);

    // Variabili per la Reflection
    private FieldInfo _posDataField;
    private FieldInfo _otherDataField; // Consiglio di prendere anche m_GpuOtherData per le rotazioni/scale dello splat
    private bool _reflectionInitialized = false;

    void Awake()
    {
        InitializeReflection();
    }

    void InitializeReflection()
    {
        if (_reflectionInitialized) return;

        var type = typeof(GaussianSplatRenderer);
        
        // Estraiamo i campi privati m_GpuPosData e m_GpuOtherData
        _posDataField = type.GetField("m_GpuPosData", BindingFlags.NonPublic | BindingFlags.Instance);
        _otherDataField = type.GetField("m_GpuOtherData", BindingFlags.NonPublic | BindingFlags.Instance);

        if (_posDataField != null && _otherDataField != null)
        {
            _reflectionInitialized = true;
        }
        else
        {
            Debug.LogError("Impossibile mappare i campi privati. Verifica la versione del file GaussianSplatRenderer.");
        }
    }

    void Update()
    {
        if (arasRenderer == null || vfxGraph == null || !_reflectionInitialized) return;
        if (!arasRenderer.HasValidAsset || !arasRenderer.HasValidRenderSetup) return;

        // Recuperiamo i valori effettivi dei buffer dall'istanza corrente tramite Reflection
        GraphicsBuffer posBuffer = _posDataField.GetValue(arasRenderer) as GraphicsBuffer;
        GraphicsBuffer otherBuffer = _otherDataField.GetValue(arasRenderer) as GraphicsBuffer;
        int splatCount = arasRenderer.splatCount;

        if (posBuffer == null || otherBuffer == null) return;

        // Inviamo i buffer e i parametri a VFX Graph
        vfxGraph.SetGraphicsBuffer("PositionsBuffer", posBuffer);
        vfxGraph.SetGraphicsBuffer("OtherDataBuffer", otherBuffer);
        vfxGraph.SetInt("SplatCount", splatCount);

        vfxGraph.SetFloat("WindSpeed", windSpeed);
        vfxGraph.SetFloat("WindFrequency", windFrequency);
        vfxGraph.SetFloat("WindIntensity", windIntensity);
        vfxGraph.SetVector3("WindDirection", windDirection.normalized);
    }
}