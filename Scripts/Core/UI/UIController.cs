using UnityEngine;
using UnityEngine.UIElements;

public abstract class UIController : MonoBehaviour
{
    protected UIDocument uiDocument;
    protected VisualElement root;

    protected virtual void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }

        root = uiDocument.rootVisualElement;
    }

    public virtual void Show()
    {
        if (root != null)
            root.style.display = DisplayStyle.Flex;
    }

    public virtual void Hide()
    {
        if (root != null)
            root.style.display = DisplayStyle.None;
    }
}