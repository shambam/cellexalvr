using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class ScrollbarVR : MonoBehaviour
{
    private Scrollbar scrollbar;
    private SteamVR_TrackedController activeHand;
    private float size;
    private bool isVertical;

    // Start is called before the first frame update
    void Awake()
    {
        if (!scrollbar)
        {
            scrollbar = GetComponent<Scrollbar>();
            isVertical = scrollbar.direction == Scrollbar.Direction.BottomToTop || scrollbar.direction == Scrollbar.Direction.TopToBottom;
            RectTransform rt = GetComponent<RectTransform>();
            size = isVertical ? rt.rect.height : rt.rect.width;
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        SteamVR_TrackedController incommingHand = other.GetComponentInParent<SteamVR_TrackedController>();
        if (activeHand == null && incommingHand != null)
        {
            activeHand = incommingHand;
            if (scrollbar.transition == Button.Transition.ColorTint)
                scrollbar.targetGraphic.color = scrollbar.colors.highlightedColor;
            if (scrollbar.transition == Button.Transition.Animation)
                scrollbar.animator.Play("Highlighted");
            if (scrollbar.transition == Button.Transition.SpriteSwap)
                ((Image)scrollbar.targetGraphic).sprite = scrollbar.spriteState.highlightedSprite;
        }
            
    }

    private void OnTriggerExit(Collider other)
    {
        SteamVR_TrackedController leavingHand = other.GetComponentInParent<SteamVR_TrackedController>();
        if (ReferenceEquals(leavingHand, activeHand))
        {
            activeHand = null;
            if (scrollbar.transition == Button.Transition.ColorTint)
                scrollbar.targetGraphic.color = scrollbar.colors.normalColor;
            if (scrollbar.transition == Button.Transition.Animation)
                scrollbar.animator.Play("Normal");
            if (scrollbar.transition == Button.Transition.SpriteSwap)
                ((Image)scrollbar.targetGraphic).sprite = scrollbar.spriteState.pressedSprite;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (activeHand)
        {
            if (activeHand.triggerPressed)
            {
                Vector3 coord = scrollbar.transform.InverseTransformPoint(activeHand.transform.position);
                float pos = isVertical ? coord.y : coord.x;
                float handleSize = isVertical ? scrollbar.handleRect.rect.height : scrollbar.handleRect.rect.width;
                float prop = 0f;
                if (handleSize < size)
                    prop = (pos-handleSize/2f) / (size-handleSize);// - handleSize);
                prop = Mathf.Clamp01(prop);
                scrollbar.value = prop;
            }
        }
    }
}
        
