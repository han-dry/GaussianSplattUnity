using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class DynamicUIController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Assign your main PhysicsManager here. If left empty, the script will automatically find it in the active scene.")]
    [SerializeField] private MonoBehaviour physicsController;

    private VisualElement root;
    private ScrollView container;

    void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        // Clear any previous UI generation
        root.Clear();

        // Automatic reference finding if not manually assigned
        if (physicsController == null)
        {
            physicsController = UnityEngine.Object.FindAnyObjectByType<PhysicsManager>(); 
        }

        if (physicsController == null)
        {
            Debug.LogError("DynamicUIController: Could not find PhysicsManager in the scene!");
            return;
        }

        // Create UI container box
        var mainBox = new Box();
        mainBox.style.position = Position.Absolute;
        mainBox.style.top = 20;
        mainBox.style.left = 20;
        mainBox.style.width = 360; // Slightly widened for clean layouts
        mainBox.style.paddingTop = 15;
        mainBox.style.paddingBottom = 15;
        mainBox.style.paddingLeft = 15;
        mainBox.style.paddingRight = 15;
        mainBox.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.9f);
        mainBox.style.borderTopLeftRadius = 8;
        mainBox.style.borderTopRightRadius = 8;
        mainBox.style.borderBottomLeftRadius = 8;
        mainBox.style.borderBottomRightRadius = 8;

        // Panel Title - Enlarged font size
        var title = new Label("Physics Control Panel (F1)");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 20;
        title.style.marginBottom = 20;
        title.style.color = Color.white;
        mainBox.Add(title);

        container = new ScrollView();
        mainBox.Add(container);
        root.Add(mainBox);

        // Start scanning recursively from the main controller
        ScanAndGenerate(physicsController);
    }

    private void ScanAndGenerate(object targetInstance)
    {
        if (targetInstance == null) return;

        Type type = targetInstance.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<ExposeToUIAttribute>();

            if (attribute != null)
            {
                if (field.FieldType == typeof(float) || field.FieldType == typeof(int))
                {
                    CreateSliderForField(targetInstance, field, attribute);
                }
                else if (field.FieldType == typeof(bool))
                {
                    CreateToggleForField(targetInstance, field, attribute);
                }
                else if (field.FieldType == typeof(Vector3))
                {
                    CreateVector3FieldForField(targetInstance, field, attribute);
                }
            }
            else if (IsCustomModule(field.FieldType))
            {
                object moduleInstance = field.GetValue(targetInstance);
                if (moduleInstance != null)
                {
                    ScanAndGenerate(moduleInstance);
                }
            }
        }
    }

    private bool IsCustomModule(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(Vector3) || 
            type == typeof(Vector2) || type == typeof(Color) || type.IsEnum)
        {
            return false;
        }
        return type.IsClass || type.IsValueType;
    }

    private void ApplyTextStyling(VisualElement element)
    {
        // Enforce readable font size and white color onto labels
        var label = element.Q<Label>();
        if (label != null)
        {
            label.style.fontSize = 17;
            label.style.color = Color.white;
        }
    }

    private void CreateSliderForField(object target, FieldInfo field, ExposeToUIAttribute attr)
    {
        float currentValue = Convert.ToSingle(field.GetValue(target));
        var slider = new Slider(attr.Label, attr.Min, attr.Max);
        slider.value = currentValue;
        slider.style.marginBottom = 14;
        slider.showInputField = true;

        ApplyTextStyling(slider);

        // Fix legibility: Make text input field contents dark gray
        var inputField = slider.Q<VisualElement>(className: "unity-base-field__input");
        if (inputField != null)
        {
            inputField.style.color = new Color(0.15f, 0.15f, 0.15f, 1f);
            inputField.style.fontSize = 17;
        }

        slider.RegisterValueChangedCallback(evt =>
        {
            if (field.FieldType == typeof(float))
                field.SetValue(target, evt.newValue);
            else if (field.FieldType == typeof(int))
                field.SetValue(target, Mathf.RoundToInt(evt.newValue));
        });

        container.Add(slider);
    }

    private void CreateToggleForField(object target, FieldInfo field, ExposeToUIAttribute attr)
    {
        bool currentValue = (bool)field.GetValue(target);
        var toggle = new Toggle(attr.Label);
        toggle.value = currentValue;
        toggle.style.marginBottom = 14;

        ApplyTextStyling(toggle);

        toggle.RegisterValueChangedCallback(evt =>
        {
            field.SetValue(target, evt.newValue);
        });

        container.Add(toggle);
    }

    private void CreateVector3FieldForField(object target, FieldInfo field, ExposeToUIAttribute attr)
    {
        Vector3 currentValue = (Vector3)field.GetValue(target);

        // Custom Layout: Create a main container element
        var customVec3Container = new VisualElement();
        customVec3Container.style.marginBottom = 16;

        // Custom Main Label
        var mainLabel = new Label(attr.Label);
        mainLabel.style.fontSize = 17;
        mainLabel.style.color = Color.white;
        mainLabel.style.marginBottom = 6;
        customVec3Container.Add(mainLabel);

        // Sub-container holding X, Y, Z horizontally
        var rowContainer = new VisualElement();
        rowContainer.style.flexDirection = FlexDirection.Row;
        rowContainer.style.justifyContent = Justify.SpaceBetween;

        // Helper method to spawn standardized numerical float inputs for X, Y, and Z axes
        FloatField CreateSubAxisField(string axisLabel, float startVal, Action<float> onValueChange)
        {
            var axisField = new FloatField(axisLabel);
            axisField.value = startVal;
            axisField.style.flexGrow = 1;
            axisField.style.marginLeft = 2;
            axisField.style.marginRight = 2;
            
            var lbl = axisField.Q<Label>();
            if (lbl != null)
            {
                lbl.style.color = Color.white;
                lbl.style.fontSize = 17;
                lbl.style.minWidth = 12;
            }

            var input = axisField.Q<VisualElement>(className: "unity-base-field__input");
            if (input != null)
            {
                input.style.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                input.style.backgroundColor = Color.white;
                input.style.fontSize = 17;
                input.style.paddingLeft = 4;
            }

            axisField.RegisterValueChangedCallback(evt => onValueChange(evt.newValue));
            return axisField;
        }

        // Generate independent fields tracking the reference vector data live
        var xField = CreateSubAxisField("X", currentValue.x, val => {
            Vector3 vec = (Vector3)field.GetValue(target);
            vec.x = val;
            field.SetValue(target, vec);
        });

        var yField = CreateSubAxisField("Y", currentValue.y, val => {
            Vector3 vec = (Vector3)field.GetValue(target);
            vec.y = val;
            field.SetValue(target, vec);
        });

        var zField = CreateSubAxisField("Z", currentValue.z, val => {
            Vector3 vec = (Vector3)field.GetValue(target);
            vec.z = val;
            field.SetValue(target, vec);
        });

        rowContainer.Add(xField);
        rowContainer.Add(yField);
        rowContainer.Add(zField);
        customVec3Container.Add(rowContainer);

        container.Add(customVec3Container);
    }
}