#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class DrawFormCreator : MonoBehaviour {

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

		OptimizedForm[] forms = new OptimizedForm[formColors.Count];
		for (int i = 0; i < formColors.Count; i++) {
			List<Vector2Int> points = formColors[i].points;
			Dictionary<int, List<int>> yToXs = new();
			
			foreach (var point in points) {
				if (!yToXs.ContainsKey(point.y)) {
					yToXs[point.y] = new List<int>();
				}
				yToXs[point.y].Add(point.x);
			}

			List<FormLine> lines = new();
			foreach (var (y, xs) in yToXs) {
				xs.Sort();
				
				int startX = xs[0];
				int prevX = xs[0];
				for (int j = 1; j < xs.Count; j++) {
					if (xs[j] != prevX + 1) {
						lines.Add(new FormLine { startX = startX, endX = prevX, y = y });
						startX = xs[j];
					}
					prevX = xs[j];
				}
				lines.Add(new FormLine { startX = startX, endX = prevX, y = y });
			}

			forms[i] = new OptimizedForm {
				lines = lines.ToArray()
			};
		}

		File.WriteAllText(path, JsonFx.Json.JsonWriter.Serialize(forms));
		AssetDatabase.Refresh();
	}

	[ContextMenu("Test Forms Json")]
	private void TestFormsJSON() {
		if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(jsonFolder, out string guid, out long _)) {
			Debug.LogError("Couldn't find asset GUID.");
			return;
		}
		string path = $"{Application.dataPath}{AssetDatabase.GUIDToAssetPath(guid).Replace("Assets", string.Empty)}/json.txt";
		if (!File.Exists(path)) {
			Debug.LogError("JSON file not found at path: " + path);
			return;
		}
		
		string json = File.ReadAllText(path);
		OptimizedForm[] forms = JsonFx.Json.JsonReader.Deserialize<OptimizedForm[]>(json);
		
		SetDrawingTexture();
		
		for (int i = 0; i < forms.Length; i++) {
			Color color = colors[i % colors.Length];
			foreach (var line in forms[i].lines) {
				for (int x = line.startX; x <= line.endX; x++) {
					drawingTexture.SetPixel(x, line.y, color);
				}
			}
		}
		
		drawingTexture.Apply();
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
		[NonSerialized] public List<Vector2Int> points;
	}
}
#endif
