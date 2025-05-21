using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawController : MonoBehaviour {

	[SerializeField] private RawImage rawImage;
	[SerializeField] private RawImage brushImage;
	[SerializeField] private Image drawImage;
	[SerializeField] private TextAsset json;

	private Texture2D texture;
	private int textureWidth;
	private int textureHeight;
	
	private RectTransform surfaceRect;
	
	private Color[] brushPixels;
	private int brushWidth;
	private int brushHeight;

	private Vector2Int prevPixel = new(-1, -1);

	private readonly List<HashSet<Vector2Int>> forms = new();
	private HashSet<Vector2Int> selectedForm;
	
	private void Start() {
		Application.targetFrameRate = 60;
		SetTexture();
		SetBrushPixels();
		SetForms();
	}

	private void SetTexture() {
		textureWidth = drawImage.mainTexture.width;
		textureHeight = drawImage.mainTexture.height;
		texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false) {
			filterMode = FilterMode.Point
		};
		Color[] fillColor = new Color[textureWidth * textureHeight];
		for (int i = 0; i < fillColor.Length; i++) {
			fillColor[i] = Color.white;
		}
		texture.SetPixels(fillColor);
		texture.Apply();
		rawImage.texture = texture;
		surfaceRect = rawImage.rectTransform;
	}

	private void SetBrushPixels() {
		brushWidth = (int)brushImage.rectTransform.sizeDelta.x;
		brushHeight = (int)brushImage.rectTransform.sizeDelta.y;
		brushPixels = GetResizedPixels((Texture2D)brushImage.texture, brushWidth, brushHeight);
		brushImage.gameObject.SetActive(false);
	}

	private void SetForms() {
		forms.Clear();
		OptimizedForm[] optimizedForms = JsonFx.Json.JsonReader.Deserialize<OptimizedForm[]>(json.ToString());
		for (int i = 0; i < optimizedForms.Length; i++) {
			HashSet<Vector2Int> points = new();
			foreach (var line in optimizedForms[i].lines) {
				for (int x = line.startX; x <= line.endX; x++) {
					points.Add(new Vector2Int(x, line.y));
				}
			}
			forms.Add(points);
		}
	}
	
	private void Update() {
		if (Input.GetKeyDown(KeyCode.R)) {
			Start();
		}

		if (Input.GetMouseButtonDown(0)) {
			if (TryGetLocalPoint(out _)) {
				selectedForm = GetSelectedForm(GetTexturePixel());
				// if (selectedForm != null) {
				// 	foreach (var point in selectedForm) {
				// 		texture.SetPixel(point.x, point.y, Color.red);
				// 	}
				// 	texture.Apply();
				// }
			}
		} else if (Input.GetMouseButton(0)) {
			if (selectedForm != null) {
				brushImage.gameObject.SetActive(true);
				Vector2Int pixel = GetTexturePixel();
				if (prevPixel != pixel) {
					if (TryGetLocalPoint(out Vector2 localPoint)) {
						brushImage.rectTransform.anchoredPosition = localPoint;
						PaintBrushAt(localPoint);
						texture.Apply();
					}
					prevPixel = pixel;
				}
			}
		} else if (Input.GetMouseButtonUp(0)) {
			brushImage.gameObject.SetActive(false);
			selectedForm = null;
			prevPixel = new Vector2Int(-1, -1);
		}
	}

	private bool TryGetLocalPoint(out Vector2 localPoint) {
		return RectTransformUtility.ScreenPointToLocalPointInRectangle(surfaceRect, Input.mousePosition, null, out localPoint);
	}

	private HashSet<Vector2Int> GetSelectedForm(Vector2Int point) {
		for (int i = 0; i < forms.Count; i++) {
			if (forms[i].Contains(point)) {
				return forms[i];
			}
		}
		return null;
	}
	
	private void PaintBrushAt(Vector2 localPoint) {
		float dx = (localPoint.x + surfaceRect.rect.width / 2) / surfaceRect.rect.width;
		float dy = (localPoint.y + surfaceRect.rect.height / 2) / surfaceRect.rect.height;
		int centerX = Mathf.RoundToInt(dx * textureWidth);
		int centerY = Mathf.RoundToInt(dy * textureHeight);

		Vector2 brushCenter = new Vector2(brushWidth / 2f, brushHeight / 2f);
		
		for (int x = 0; x < brushWidth; x++) {
			for (int y = 0; y < brushHeight; y++) {
				Vector2 localOffset = new Vector2(x, y) - brushCenter;
				
				int dstX = Mathf.RoundToInt(centerX + localOffset.x);
				int dstY = Mathf.RoundToInt(centerY + localOffset.y);

				if (dstX < 0 || dstX >= textureWidth || dstY < 0 || dstY >= textureHeight) {
					continue;
				}

				if (!selectedForm.Contains(new Vector2Int(dstX, dstY))) {
					continue;
				}
				
				Color brushColor = brushPixels[y * brushWidth + x] * brushImage.color;
				if (brushColor.a == 0) {
					continue;
				}

				Color existing = texture.GetPixel(dstX, dstY);
				Color result = Color.Lerp(existing, brushColor, brushColor.a / 4f);
				result.a = 1;
				texture.SetPixel(dstX, dstY, result);
			}
		}
	}

	private Vector2Int GetTexturePixel() {
		RectTransformUtility.ScreenPointToLocalPointInRectangle(surfaceRect, Input.mousePosition, null, out Vector2 localPoint);
		float dx = (localPoint.x + surfaceRect.rect.width / 2) / surfaceRect.rect.width;
		float dy = (localPoint.y + surfaceRect.rect.height / 2) / surfaceRect.rect.height;
		return new Vector2Int(Mathf.Clamp((int)(dx * textureWidth), 0, textureWidth - 1),
			Mathf.Clamp((int)(dy * textureHeight), 0, textureHeight - 1));
	}
	
	private Vector2 GetLocalPositionFromPixel(Vector2Int pixel) {
		float px = (pixel.x / (float)textureWidth) * surfaceRect.rect.width - surfaceRect.rect.width / 2f;
		float py = (pixel.y / (float)textureHeight) * surfaceRect.rect.height - surfaceRect.rect.height / 2f;
		return new Vector2(px, py);
	}

	private static Color[] GetResizedPixels(Texture2D originalTexture, int newWidth, int newHeight) {
		RenderTexture rt = new RenderTexture(newWidth, newHeight, 0);
		RenderTexture.active = rt;

		Graphics.Blit(originalTexture, rt);

		Texture2D resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
		resizedTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
		resizedTexture.Apply();

		RenderTexture.active = null;
		rt.Release();

		return resizedTexture.GetPixels();
	}
}

[Serializable]
public class OptimizedForm {
	public FormLine[] lines;
}

[Serializable]
public class FormLine {
	public int startX;
	public int endX;
	public int y;
}
