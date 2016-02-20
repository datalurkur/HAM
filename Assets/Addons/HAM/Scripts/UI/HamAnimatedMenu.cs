using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class HamAnimatedMenu : MonoBehaviour
{
    public enum AnimationState
    {
        Inactive,
        Hydrating,
        Active,
        Dehydrating
    }

    public const float kAnimationSpeed = 1f;
    public const float kAnimationMagnitude = 1000f;

    public GameObject ContentContainer;

    public bool MenuActive = false;
    private AnimationState state = AnimationState.Inactive;
    private float animationProgress = 0f;

    private float inactiveYValue;
    private float activeYValue;

    public bool MenuHydrated
    {
        get
        {
            return (this.state != AnimationState.Inactive);
        }
    }

    protected void Start()
    {
        this.activeYValue = this.ContentContainer.transform.position.y;
        this.inactiveYValue = this.activeYValue + kAnimationMagnitude;

        SetContainerPosition(this.inactiveYValue);
        SetInteractable(false);
    }

    protected void Update()
    {
        switch (this.state)
        {
        case AnimationState.Inactive:
            if (this.MenuActive)
            {
                this.state = AnimationState.Hydrating;
                this.animationProgress = 0f;
            }    
            break;
        case AnimationState.Hydrating:
            this.animationProgress = Mathf.Clamp01(this.animationProgress + Time.deltaTime * kAnimationSpeed);
            SetContainerPosition(Mathf.Lerp(this.inactiveYValue, this.activeYValue, HamMath.EaseInBounce(this.animationProgress)));
            if (this.animationProgress >= 1f)
            {
                this.state = AnimationState.Active;
                SetInteractable(true);
            }
            break;
        case AnimationState.Active:
            if (!this.MenuActive)
            {
                this.state = AnimationState.Dehydrating;
                this.animationProgress = 1f;
            }
            break;
        case AnimationState.Dehydrating:
            this.animationProgress = Mathf.Clamp01(this.animationProgress - Time.deltaTime * kAnimationSpeed);
            SetContainerPosition(Mathf.Lerp(this.inactiveYValue, this.activeYValue, HamMath.Sinerp(this.animationProgress)));
            if (this.animationProgress <= 0f)
            {
                this.state = AnimationState.Inactive;
                SetInteractable(false);
            }
            break;
        }
    }

    private void SetContainerPosition(float y)
    {
        Vector3 p = this.ContentContainer.transform.position;
        p.y = y;
        this.ContentContainer.transform.position = p;
    }

    private void SetInteractable(bool interactable)
    {
        Button[] buttons = this.ContentContainer.GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; ++i)
        {
            buttons[i].interactable = interactable;
        } 
    }
}