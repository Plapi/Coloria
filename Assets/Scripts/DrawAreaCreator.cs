#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[ExecuteInEditMode]
public class DrawAreaCreator : MonoBehaviour {

	[SerializeField] private Sprite sprite;
	[SerializeField] private Image drawingImage;
	[SerializeField] private int randomColorsCount;
	
	[Space]
	[SerializeField] private Color[] colors;
	[SerializeField] private List<FormColor> formColors;
	
	[Space]
	[SerializeField] private UnityEngine.Object jsonFolder;
	
	private Texture2D drawingTexture;
	private HashSet<Vector2Int> visited;
	private int width;
	private int height;

	[ContextMenu("Set Random Colors")]
	private void SetColors() {
		colors = new Color[randomColorsCount];
		float hueStep = 1.0f / randomColorsCount;
		for (int i = 0; i < randomColorsCount; i++) {
			float hue = i * hueStep;
			Color color = Color.HSVToRGB(hue, 0.8f, 0.9f);
			color.a = 1f;
			colors[i] = color;
		}
		Utils.ShuffleArray(colors);
	}
	
	[ContextMenu("Generate Draw Areas")]
	private void GenerateDrawAres() {
		SetDrawingTexture();

		visited = new HashSet<Vector2Int>();
		formColors = new List<FormColor>();
		int currentColorIndex = 0;

		for (int x = 0; x < width; x++) {
			for (int y = 0; y < height; y++) {
				Vector2Int point = new Vector2Int(x, y);
				if (drawingTexture.GetPixel(x, y).a < 0.01f && !visited.Contains(point)) {
					formColors.Add(new FormColor {
						color = colors[currentColorIndex],
						points = new List<Vector2Int>()
					});
					Execute(point);
					currentColorIndex++;
					if (currentColorIndex >= colors.Length) {
						currentColorIndex = 0;
					}
				}
			}
		}
		
		drawingTexture.Apply();
	}

	[ContextMenu("Save Forms JSON")]
	private void SaveFormsJSON() {
		if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(jsonFolder, out string guid, out long _)) {
			Debug.LogError($"Couldn't find asset GUID: {guid}");
			return;
		}
		string path = $"{Application.dataPath}{AssetDatabase.GUIDToAssetPath(guid).Replace("Assets", string.Empty)}/json.txt";

		Form[] forms = new Form[formColors.Count];
		for (int i = 0; i < forms.Length; i++) {
			forms[i] = new Form {
				points = new FormPoint[formColors[i].points.Count],
			};
			for (int j = 0; j < forms[i].points.Length; j++) {
				forms[i].points[j] = new FormPoint {
					x = formColors[i].points[j].x,
					y = formColors[i].points[j].y
				};
			}
		}
		
		Debug.LogError(forms.Length);
		File.WriteAllText(path, JsonFx.Json.JsonWriter.Serialize(forms));
		AssetDatabase.Refresh();
	}
	
	private void SetDrawingTexture() {
		drawingImage.sprite = sprite;
		Texture2D texture = (Texture2D)drawingImage.mainTexture;
		width = texture.width;
		height = texture.height;
		drawingTexture = new Texture2D(width, height);
		drawingTexture.SetPixels(texture.GetPixels());
		drawingTexture.Apply();
		drawingImage.sprite = Sprite.Create(drawingTexture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
	}
	
	private void Execute(Vector2Int startPoint) {
		Stack<Vector2Int> stack = new();
		stack.Push(startPoint);
		
		while (stack.Count > 0) {
			Vector2Int point = stack.Pop();
			if (visited.Contains(point)) {
				continue;
			}
			if (point.x < 0 || point.y < 0 || point.x >= width || point.y >= height) {
				continue;
			}
			visited.Add(point);
			formColors[^1].points.Add(point);
			if (drawingTexture.GetPixel(point.x, point.y).a > 0.9f) {
				continue;
			}
			drawingTexture.SetPixel(point.x, point.y, formColors[^1].color);
			stack.Push(new Vector2Int(point.x + 1, point.y));
			stack.Push(new Vector2Int(point.x - 1, point.y));
			stack.Push(new Vector2Int(point.x, point.y + 1));
			stack.Push(new Vector2Int(point.x, point.y - 1));
		}
	}
	
	[Serializable]
	private class FormColor {
		public Color color;
		[HideInInspector] public List<Vector2Int> points;
	}
	
	[Serializable]
	private class Form {
		public FormPoint[] points;
	}

	[Serializable]
	private class FormPoint {
		public int x;
		public int y;
	}
}
#endif
