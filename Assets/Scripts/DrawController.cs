using UnityEngine;
using UnityEngine.UI;

public class DrawController : MonoBehaviour {

	[SerializeField] private CanvasScaler canvasScaler;
	[SerializeField] private RawImage rawImage;
	[SerializeField] private RawImage brushImage;

	private Texture2D texture;
	private int textureWidth;
	private int textureHeight;
	
	private RectTransform surfaceRect;
	
	private Color[] brushPixels;
	private int brushWidth;
	private int brushHeight;

	private Vector2Int prevPixel = new(-1, -1);

	private void Start() {
		Application.targetFrameRate = 60;
		SetTexture();
		SetBrushPixels();
	}

	private void SetTexture() {
		textureWidth = (int)canvasScaler.referenceResolution.x;
		textureHeight = (int)canvasScaler.referenceResolution.y;
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

	private void Update() {
		if (Input.GetKeyDown(KeyCode.R)) {
			Start();
		}

		if (Input.GetMouseButton(0)) {
			brushImage.gameObject.SetActive(true);
			Vector2Int pixel = GetTexturePixel();
			if (prevPixel != pixel) {
				if (RectTransformUtility.ScreenPointToLocalPointInRectangle(surfaceRect, Input.mousePosition, null, out Vector2 localPoint)) {
					brushImage.rectTransform.anchoredPosition = localPoint;
					PaintStampAt(GetLocalPositionFromPixel(pixel));
					texture.Apply();
				}
				prevPixel = pixel;
			}
		} else if (Input.GetMouseButtonUp(0)) {
			brushImage.gameObject.SetActive(false);
			prevPixel = new Vector2Int(-1, -1);
		}
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

				if (dstX < 0 || dstX >= textureWidth || dstY < 0 || dstY >= textureHeight) {
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
