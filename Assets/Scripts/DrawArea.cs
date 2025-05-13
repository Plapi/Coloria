using System.Collections.Generic;
using UnityEngine;

public class DrawArea : MonoBehaviour {

	[SerializeField] private List<RectTransform> rectTransforms;
	
	private void OnDrawGizmos() {
		if (!Application.isPlaying) {
			rectTransforms ??= new List<RectTransform>();
			if (rectTransforms.Count != transform.childCount) {
				rectTransforms.Clear();
				foreach (Transform child in transform) {
					child.name = $"Point{rectTransforms.Count}";
					rectTransforms.Add(child.GetComponent<RectTransform>());
				}
			}
		}
		
		Gizmos.color = Color.blue;
		for (int i = 0; i < rectTransforms.Count; i++) {
			Gizmos.DrawLine(rectTransforms[i].position, rectTransforms[i + 1 < rectTransforms.Count ? i + 1 : 0].position);
		}
	}

	public bool IsInside(Vector2 point) {
		bool isInside = false;
		int j = rectTransforms.Count - 1;
		for (int i = 0; i < rectTransforms.Count; j = i++) {
			if (((rectTransforms[i].anchoredPosition.y > point.y) != (rectTransforms[j].anchoredPosition.y > point.y)) &&
			    (point.x < (rectTransforms[j].anchoredPosition.x - rectTransforms[i].anchoredPosition.x) *
				    (point.y - rectTransforms[i].anchoredPosition.y) / (rectTransforms[j].anchoredPosition.y - rectTransforms[i].anchoredPosition.y) +
				    rectTransforms[i].anchoredPosition.x)) {
				isInside = !isInside;
			}
		}
		return isInside;
	}
}
