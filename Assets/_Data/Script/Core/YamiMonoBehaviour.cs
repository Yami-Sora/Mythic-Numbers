using UnityEngine;

public class YamiMonoBehaviour : MonoBehaviour
{
    protected virtual void Reset()
    {
        this.LoadComponents();
        this.ResetValue();
    }
    protected virtual void Awake()
    {
        this.LoadComponents();
    }
    protected virtual void Start()
    {
        //For override
    }
    protected virtual void LoadComponents()
    {
        //for override  
    }
    protected virtual void ResetValue()
    {
        //for override  
    }
    protected virtual void OnEnable()
    {
        //for override  
    }
    protected virtual void OnDisable()
    {
        //for override  
    }
}