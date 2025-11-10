// Assets/Scripts/UI/UIResponsiveManager.cs
using UnityEngine;
using UnityEngine.UIElements;

public class UIResponsiveManager
{
    private readonly VisualElement _root;
    private readonly float[] _breakpoints = { 768f, 1280f, 1920f, 2560f, 3840f };

    public UIResponsiveManager(VisualElement root)
    {
        _root = root;
        Initialize();
    }

    private void Initialize()
    {
        _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        // УБРАТЬ: UpdateResponsiveClasses(); — будет NaN
        Debug.Log("Responsive UI manager initialized");
        HideScrollers();
        Debug.Log("Responsive UI manager hide scrollers");
    }

    private void HideScrollers()
    {
        _root.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;
        _root.Q<ScrollView>().horizontalScrollerVisibility = ScrollerVisibility.Hidden;
    }

    private void OnGeometryChanged(GeometryChangedEvent evt)
    {
        UpdateResponsiveClasses();
    }

    private void UpdateResponsiveClasses()
    {
        if (_root == null) return;

        float width = _root.resolvedStyle.width;

        // ЗАЩИТА ОТ NaN
        if (float.IsNaN(width) || width <= 0)
        {
            // Отложим до первого валидного события
            return;
        }

        // Удаляем все классы
        _root.RemoveFromClassList("sm-screen");
        _root.RemoveFromClassList("md-screen");
        _root.RemoveFromClassList("lg-screen");
        _root.RemoveFromClassList("xl-screen");
        _root.RemoveFromClassList("xxl-screen");

        // Добавляем нужный
        if (width < _breakpoints[0])
            _root.AddToClassList("sm-screen");
        else if (width < _breakpoints[1])
            _root.AddToClassList("md-screen");
        else if (width < _breakpoints[2])
            _root.AddToClassList("lg-screen");
        else if (width < _breakpoints[3])
            _root.AddToClassList("xl-screen");
        else
            _root.AddToClassList("xxl-screen");

        Debug.Log($"Screen width: {width}px, Class: {GetCurrentBreakpoint(width)}");
    }

    private string GetCurrentBreakpoint(float width)
    {
        if (width < _breakpoints[0]) return "sm";
        else if (width < _breakpoints[1]) return "md";
        else if (width < _breakpoints[2]) return "lg";
        else if (width < _breakpoints[3]) return "xl";
        else return "xxl";
    }

    public void RefreshResponsiveUI()
    {
        UpdateResponsiveClasses();
    }

    public void Cleanup()
    {
        if (_root != null)
        {
            _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
    }
}