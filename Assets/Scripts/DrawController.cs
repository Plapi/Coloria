using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawController : MonoBehaviour {

	private const int textureWidth = 640;
	private const int textureHeight = 1386;

	[SerializeField] private RawImage rawImage;
	[SerializeField] private RawImage brushImage;
	[SerializeField] private Color penColor;
	[SerializeField] private int brushSize;

	private Texture2D texture;
	private RectTransform surfaceRect;
	
	private Color[] brushPixels;
	private int brushWidth;
	private int brushHeight;

	private Vector2Int prevPixel = new(-1, -1);
	private Vector2 prevBrushAnchorPos;

	private readonly HashSet<Vector2Int> paintedPixelsThisStroke = new();

	private void Start() {
		CreateTexture();
		
		Texture2D brushTex = brushImage.texture as Texture2D;
		brushPixels = brushTex.GetPixels();
		brushWidth = brushTex.width;
		brushHeight = brushTex.height;
		brushImage.gameObject.SetActive(false);
	}

	private void CreateTexture() {
		texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false) {
			filterMode = FilterMode.Point
		};
		Color[] fillColor = new Color[640 * 1386];
		for (int i = 0; i < fillColor.Length; i++) {
			fillColor[i] = Color.white;
		}
		texture.SetPixels(fillColor);
		texture.Apply();
		rawImage.texture = texture;
		surfaceRect = rawImage.rectTransform;
	}

	private void Update() {
		if (Input.GetKeyDown(KeyCode.R)) {
			CreateTexture();
		}
		
		if (Input.GetMouseButtonDown(0)) {
			brushImage.gameObject.SetActive(true);
			prevPixel = GetTexturePixel();
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(surfaceRect, Input.mousePosition, null, out Vector2 localPoint)) {
				brushImage.rectTransform.anchoredPosition = localPoint;
				prevBrushAnchorPos = localPoint;
			}
			paintedPixelsThisStroke.Clear();
		}
		if (Input.GetMouseButton(0)) {
			Vector2Int pixel = GetTexturePixel();
			if (prevPixel != pixel) {
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(surfaceRect, Input.mousePosition, null, out Vector2 localPoint)) {
					
					brushImage.rectTransform.anchoredPosition = localPoint;

					if (Vector2.Distance(localPoint, prevBrushAnchorPos) > 10f) {
						Vector2 dir = (localPoint - prevBrushAnchorPos).normalized;
						brushImage.transform.up = dir;
						prevBrushAnchorPos = localPoint;
					}
					
					// Vector2 prevLocal = GetLocalPositionFromPixel(prevPixel);
					// Vector2 dir = (localPoint - prevLocal).normalized;
					// brushImage.rectTransform.up = dir;
					// float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
					// brushImage.rectTransform.rotation = Quaternion.Euler(0, 0, angle);

					DrawBrushImage(prevPixel, pixel);
				}
				prevPixel = pixel;
			}
			
		} else if (Input.GetMouseButtonUp(0)) {
			brushImage.gameObject.SetActive(false);
		}
	}

	private void DrawBrushImage(Vector2Int from, Vector2Int to) {
		
		int steps = Mathf.CeilToInt(Vector2Int.Distance(from, to));
		for (int i = 0; i <= steps; i++) {
			float t = i / (float)steps;
			Vector2Int pixel = Vector2Int.RoundToInt(Vector2.Lerp(from, to, t));
			Vector2 localPoint = GetLocalPositionFromPixel(pixel);

			brushImage.rectTransform.anchoredPosition = localPoint;

			Vector2 dir = (localPoint - prevBrushAnchorPos).normalized;
			if (dir.sqrMagnitude > 0.0001f) {
				brushImage.transform.up = dir;
			}

			prevBrushAnchorPos = localPoint;

			PaintStampAt(localPoint);
		}
		texture.Apply();
	}
	
	private void PaintStampAt(Vector2 localPoint) {
		float dx = (localPoint.x + surfaceRect.rect.width / 2) / surfaceRect.rect.width;
		float dy = (localPoint.y + surfaceRect.rect.height / 2) / surfaceRect.rect.height;
		int centerX = Mathf.RoundToInt(dx * textureWidth);
		int centerY = Mathf.RoundToInt(dy * textureHeight);

		Quaternion rotation = brushImage.rectTransform.rotation;
		Vector2 brushCenter = new Vector2(brushWidth / 2f, brushHeight / 2f);

		for (int x = 0; x < brushWidth; x++) {
			for (int y = 0; y < brushHeight; y++) {
				Vector2 localOffset = new Vector2(x, y) - brushCenter;
				Vector2 rotatedOffset = rotation * localOffset;

				int dstX = Mathf.RoundToInt(centerX + rotatedOffset.x);
				int dstY = Mathf.RoundToInt(centerY + rotatedOffset.y);

				Vector2Int dstPixel = new Vector2Int(dstX, dstY);
				if (paintedPixelsThisStroke.Contains(dstPixel)) {
					continue;
				}

				if (dstX < 0 || dstX >= textureWidth || dstY < 0 || dstY >= textureHeight) {
					continue;
				}
				
				paintedPixelsThisStroke.Add(dstPixel);
				
				Color brushColor = brushPixels[y * brushWidth + x];
				if (brushColor.a == 0) {
					continue;
				}

				texture.SetPixel(dstX, dstY, brushColor);
			}
		}
	}
	
	/*private void PaintStampAt(Vector2 localPoint) {
		float dx = (localPoint.x + surfaceRect.rect.width / 2) / surfaceRect.rect.width;
		float dy = (localPoint.y + surfaceRect.rect.height / 2) / surfaceRect.rect.height;
		int baseX = Mathf.RoundToInt(dx * textureWidth) - brushWidth / 2;
		int baseY = Mathf.RoundToInt(dy * textureHeight) - brushHeight / 2;

		for (int x = 0; x < brushWidth; x++) {
			for (int y = 0; y < brushHeight; y++) {
				int dstX = baseX + x;
				int dstY = baseY + y;
				
				Vector2Int dstPixel = new Vector2Int(dstX, dstY);
				if (paintedPixelsThisStroke.Contains(dstPixel)) {
					continue;
				}
				
				if (dstX < 0 || dstX >= textureWidth || dstY < 0 || dstY >= textureHeight) {
					continue;
				}

				Color brushColor = brushPixels[y * brushWidth + x];
				if (brushColor.a == 0) {
					continue;
				}

				paintedPixelsThisStroke.Add(dstPixel);
				texture.SetPixel(dstX, dstY, brushColor);
			}
		}
	}*/
	
	/*private void DrawBrushImage() {
		Vector2 anchoredPos = brushImage.rectTransform.anchoredPosition;
		float dx = (anchoredPos.x + surfaceRect.rect.width / 2) / surfaceRect.rect.width;
		float dy = (anchoredPos.y + surfaceRect.rect.height / 2) / surfaceRect.rect.height;
		int baseX = Mathf.RoundToInt(dx * textureWidth) - brushWidth / 2;
		int baseY = Mathf.RoundToInt(dy * textureHeight) - brushHeight / 2;
		for (int x = 0; x < brushWidth; x++) {
			for (int y = 0; y < brushHeight; y++) {
				int dstX = baseX + x;
				int dstY = baseY + y;

				if (dstX < 0 || dstX >= textureWidth || dstY < 0 || dstY >= textureHeight) {
					continue;
				}

				Color brushColor = brushPixels[y * brushWidth + x];
				if (brushColor.a == 0) {
					continue;
				}

				texture.SetPixel(dstX, dstY, brushColor); // You can blend here if desired
			}
		}
		texture.Apply();
	}*/

	private void Draw(Vector2Int from, Vector2Int to) {
		int dx = Mathf.Abs(to.x - from.x);
		int dy = Mathf.Abs(to.y - from.y);
		int steps = Mathf.Max(dx, dy);
		
		/*for (int i = 0; i <= steps; i++) {
			float t = i / (float)steps;
			int x = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
			int y = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));
			// DrawPixel(x, y);
			// DrawCircle(x, y);
		}*/
		
		texture.Apply();
	}
	
	private void DrawPixel(int x, int y) {
		if (x >= 0 && x < textureWidth && y >= 0 && y < textureHeight) {
			texture.SetPixel(x, y, penColor);
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

	private static void Navigate(int from, int to, Action<int> navigate) {
		if (from < to) {
			for (int i = from; i <= to; i++) {
				navigate(i);
			}
		} else {
			for (int i = from; i >= to; i--) {
				navigate(i);
			}
		}
	}

	private void DrawCircle(int cx, int cy) {
		for (int x = -brushSize; x <= brushSize; x++) {
			for (int y = -brushSize; y <= brushSize; y++) {
				if (x * x + y * y <= brushSize * brushSize) {
					int px = Mathf.Clamp(cx + x, 0, textureWidth - 1);
					int py = Mathf.Clamp(cy + y, 0, textureHeight - 1);
					texture.SetPixel(px, py, penColor);
				}
			}
		}
		texture.Apply();
	}
}
