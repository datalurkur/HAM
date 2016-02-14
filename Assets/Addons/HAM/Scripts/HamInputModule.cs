using UnityEngine;
using UnityEngine.EventSystems;

public class HamInputModule : StandaloneInputModule
{
	protected override void ProcessMove(PointerEventData pointerEvent)
	{
		base.ProcessMove(pointerEvent);

		GameObject pointer = pointerEvent.pointerCurrentRaycast.gameObject;
		if (pointer == null) { return; }

		GameObject clickable = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointer);
		if (clickable == null) { return; }

		if (EventSystem.current.currentSelectedGameObject != clickable)
		{
			EventSystem.current.SetSelectedGameObject(clickable);
		}
	}
}