using System;

[AttributeUsage(AttributeTargets.Field)]
public class ExposeToUIAttribute : Attribute
{
    public string Label { get; }
    public float Min { get; }
    public float Max { get; }
    public bool IsSlider { get; }

    // Constructor for sliders (float / int)
    public ExposeToUIAttribute(string label, float min, float max)
    {
        Label = label;
        Min = min;
        Max = max;
        IsSlider = true;
    }

    // Generic constructor for bool, Vector3, or other types without slider ranges
    public ExposeToUIAttribute(string label)
    {
        Label = label;
        IsSlider = false;
    }
}